using Microsoft.AspNetCore.Mvc;
using System.Buffers;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Web;
using System;

namespace WebSocketsSample.Controllers;

public class SendSubtitlesWSC : ControllerBase
{
    [Route("/in-subtitles")]
    public async Task Get()
    {
        if (HttpContext.WebSockets.IsWebSocketRequest)
        {
            using var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();

            // loop
            Task handleCloseTask = HandleClose (webSocket); // start waiting for close request
            await LoremIpsum (webSocket); // send out data
            await handleCloseTask;
        }
        else
        {
            HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        }
    }

    private bool shouldClose = false;

    private async Task HandleClose (WebSocket webSocket)
    {
        while (!shouldClose)
        {
            byte[] buffer = new byte[1];
            var receiveResult = await webSocket.ReceiveAsync(
                new ArraySegment<byte>(buffer),
                CancellationToken.None);
            if (receiveResult.CloseStatus.HasValue)
            {
                shouldClose = true;
            }
        }
    }

    private async Task LoremIpsum (WebSocket webSocket)
    {
        const string loremipsum = "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor "
            + "incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco "
            + "laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate "
            + "velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt "
            + "in culpa qui officia deserunt mollit anim id est laborum.";

        while (!shouldClose)
        {
            foreach (string word in loremipsum.Split (' '))
            {
                if (shouldClose) break;
                SendMessageFull (webSocket, PrepareStringForProcessing (word));
                Thread.Sleep (1000);
            }
        }

        await webSocket.CloseAsync(
            WebSocketCloseStatus.NormalClosure,
            "Closing from client",
            CancellationToken.None);
    }

    // send one text in its entirety
    private async void SendMessageFull (WebSocket webSocket, byte[] message)
    {
        PrintMessage (message);

        await webSocket.SendAsync(
            message,
            WebSocketMessageType.Text,
            true,
            HttpContext.RequestAborted);
    }

    private static byte[] PrepareStringForProcessing (string msg)
    {
        UTF8Encoding encoding = new UTF8Encoding();
        return encoding.GetBytes (msg.Trim('\0'));
    }

    private static void PrintMessage (byte[] data)
    {
        Encoding ascii = Encoding.ASCII;

        Console.Write ("Sending message: ");
        foreach (byte elem in data)
        {
            Console.Write ((elem > 31 && elem < 127) ? ascii.GetString (new[] { elem }) : ".");
        }
        Console.WriteLine ("");
    }
}
