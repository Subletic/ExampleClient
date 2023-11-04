using System.Text;
using System.Net.WebSockets;

namespace MockServer.Services;

public class SubleticClient : BackgroundService
{
    private readonly IConfiguration configuration;
    private ClientWebSocket client;
    private const int MAX_RECEIVABLE_CHARACTER_LENGTH_OF_SUBTITLES_IN_KILOBYTE = 4;
    private readonly string exportFilePath;

    public SubleticClient(IConfiguration configuration)
    {
        this.configuration = configuration;
        client = new ClientWebSocket();
        exportFilePath = "ReceivedSubtitles." + configuration.GetValue<string>("SubleticClientSettings:SubtitleFormat");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Init_WebSocket(stoppingToken);

        try
        {
            string videoPath = "Media/" + configuration.GetValue<string>("SubleticClientSettings:TestVideoName");
            using (var fileStream = new FileStream(videoPath, FileMode.Open, FileAccess.Read))
            {
                byte[] buffer = new byte[2048];
                Memory<byte> memoryBuffer = new Memory<byte>(buffer);

                int bytesRead;
                while ((bytesRead = await fileStream.ReadAsync(memoryBuffer, stoppingToken)) > 0)
                {
                    var segment = new ArraySegment<byte>(buffer, 0, bytesRead);
                    await client.SendAsync(segment, WebSocketMessageType.Binary, bytesRead < buffer.Length, stoppingToken);
                    await Task.Delay(configuration.GetValue<int>("SubleticClientSettings:VideoSnippetInterval"), stoppingToken);
                }
            }
        }
        catch (System.NullReferenceException)
        {
            Console.WriteLine("Please provide a valid video name in appsettings.json");
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
    }

    private async void Init_WebSocket(CancellationToken stoppingToken)
    {
        try
        {
            string targetWebSocketUrl = configuration.GetValue<string>("SubleticClientSettings:WebSocketUrl")!;
            await client.ConnectAsync(new Uri(targetWebSocketUrl), stoppingToken);
            var _ = ReceiveMessages(stoppingToken);
        }
        catch (System.NullReferenceException)
        {
            Console.WriteLine("Please provide a valid WebSocket URL in appsettings.json");
            return;
        }
        catch (System.Net.WebSockets.WebSocketException)
        {
            Console.WriteLine("No connection to the WebSocket server possible. Please check the URL in appsettings.json");
            return;
        }
    }

    private async Task ReceiveMessages(CancellationToken stoppingToken)
    {
        var buffer = new byte[1024 * MAX_RECEIVABLE_CHARACTER_LENGTH_OF_SUBTITLES_IN_KILOBYTE];

        while (!stoppingToken.IsCancellationRequested)
        {
            var result = await client.ReceiveAsync(new ArraySegment<byte>(buffer), stoppingToken);

            if (result.MessageType == WebSocketMessageType.Text)
            {
                var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                File.AppendAllText(exportFilePath, message + Environment.NewLine);
            }
            else if (result.MessageType == WebSocketMessageType.Close)
            {
                await client.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
            }
        }
    }
}