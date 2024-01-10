namespace MockServer.Services;

using System.Net.WebSockets;
using System.Text;

/// <summary>
/// Represents a client for interacting with the Subletic service.
/// </summary>
public class SubleticClientService : BackgroundService
{
    private const string DEFAULT_BACKEND_WEBSOCKET_URL = "ws://localhost:40114/transcribe";
    private const int MAX_RECEIVABLE_CHARACTER_LENGTH_OF_SUBTITLES_IN_KILOBYTE = 4;
    private const string FALLBACK_SUBTITLE_FORMAT = "vtt";
    private readonly string subtitleFormat;
    private readonly IConfiguration configuration;
    private string exportFilePath;
    private bool stopSendingEarly = false;

    /// <summary>
    /// Initializes a new instance of the <see cref="SubleticClientService"/> class.
    /// </summary>
    /// <param name="configuration">Holds the appsettings.json variables</param>
    public SubleticClientService(IConfiguration configuration)
    {
        this.configuration = configuration;
        this.subtitleFormat = configuration.GetValue<string>("SubleticClientSettings:SubtitleFormat") ??
                              FALLBACK_SUBTITLE_FORMAT;
        this.exportFilePath = this.evaluateFilePath();
    }

    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (stoppingToken.IsCancellationRequested is false)
        {
            Console.WriteLine($"{DateTime.Now:HH:mm:ss} - Trying to connect to Subletic...");
            await this.ConnectToSubletic(stoppingToken);
            await Task.Delay(1000, stoppingToken); // Wait for one second before attempting reconnect.
        }
    }

    private async Task ConnectToSubletic(CancellationToken stoppingToken)
    {
        ClientWebSocket client = new ClientWebSocket();
        await Init_WebSocket(client, stoppingToken);

        this.exportFilePath = this.evaluateFilePath();

        // Submit our preferred subtitle format
        var subtitleFormatMessageBuffer = Encoding.UTF8.GetBytes(this.subtitleFormat);
        await client.SendAsync(
            buffer: subtitleFormatMessageBuffer,
            messageType: WebSocketMessageType.Text,
            endOfMessage: true,
            cancellationToken: stoppingToken);

        stopSendingEarly = false;
        Task receiveTask = this.ReceiveMessages(client, stoppingToken);

        try
        {
            string videoPath = "Media/" + this.configuration.GetValue<string>("SubleticClientSettings:TestVideoName");
            await using (var fileStream = new FileStream(videoPath, FileMode.Open, FileAccess.Read))
            {
                byte[] buffer = new byte[2048];
                Memory<byte> memoryBuffer = new Memory<byte>(buffer);

                int bytesRead;
                while (!stopSendingEarly && (bytesRead = await fileStream.ReadAsync(memoryBuffer, stoppingToken)) > 0)
                {
                    var segment = new ArraySegment<byte>(buffer, 0, bytesRead);
                    await client.SendAsync(
                        segment,
                        WebSocketMessageType.Binary,
                        bytesRead < buffer.Length,
                        stoppingToken);
                    await Task.Delay(
                        this.configuration.GetValue<int>("SubleticClientSettings:VideoSnippetInterval"),
                        stoppingToken);
                }
            }

            await client.CloseOutputAsync(
                WebSocketCloseStatus.NormalClosure,
                "done with my side of the transmission",
                stoppingToken);

            await receiveTask;
        }
        catch (Exception e)
        {
            switch (e)
            {
                case NullReferenceException:
                    Console.WriteLine("Please provide a valid video name in appsettings.json");
                    break;
                case ObjectDisposedException: break;
                default:
                    Console.WriteLine(e.Message);
                    break;
            }
        }
    }

    private string evaluateFilePath()
    {
        const string FILE_NAME = "ReceivedSubtitles";
        var fileType = this.configuration.GetValue<string>("SubleticClientSettings:SubtitleFormat") ?? "vtt";
        var fileId = 0;
        string filePath = FILE_NAME + "." + fileType;
        while (true)
        {
            if (File.Exists(filePath))
            {
                fileId++;
                filePath = FILE_NAME + $" ({fileId})." + fileType;
                continue;
            }

            if (fileId == 0)
            {
                return FILE_NAME + "." + fileType;
            }

            return filePath;
        }
    }

    private static async Task Init_WebSocket(ClientWebSocket client, CancellationToken stoppingToken)
    {
        try
        {
            string targetWebSocketUrl = Environment.GetEnvironmentVariable("BACKEND_WEBSOCKET_URL") ??
                                        DEFAULT_BACKEND_WEBSOCKET_URL;
            await client.ConnectAsync(new Uri(targetWebSocketUrl), stoppingToken);
            Console.WriteLine($"{DateTime.Now:HH:mm:ss)} - Connected to Subletic.");
        }
        catch (Exception e)
        {
            switch (e)
            {
                case NullReferenceException:
                    Console.WriteLine("Please provide a valid WebSocket URL in appsettings.json");
                    break;
                case WebSocketException:
                    Console.WriteLine(
                        "No connection to the WebSocket server possible. Please check the URL in appsettings.json");
                    break;
                case ObjectDisposedException:
                    Console.WriteLine("Backend not available.");
                    break;
            }
        }
    }

    private async Task ReceiveMessages(ClientWebSocket client, CancellationToken stoppingToken)
    {
        var buffer = new byte[1024 * MAX_RECEIVABLE_CHARACTER_LENGTH_OF_SUBTITLES_IN_KILOBYTE];

        while (!stoppingToken.IsCancellationRequested)
        {
            var result = await client.ReceiveAsync(new ArraySegment<byte>(buffer), stoppingToken);

            if (result.MessageType == WebSocketMessageType.Text)
            {
                var message = Encoding.UTF8.GetString(buffer, 0, result.Count);

                File.AppendAllText(this.exportFilePath, message + Environment.NewLine);
            }
            else if (result.MessageType == WebSocketMessageType.Close)
            {
                stopSendingEarly = result.CloseStatus! != WebSocketCloseStatus.NormalClosure;
                break;
            }
        }
    }
}
