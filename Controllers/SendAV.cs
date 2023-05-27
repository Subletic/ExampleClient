using Microsoft.AspNetCore.Mvc;
using System.Buffers;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Web;
using System;

namespace WebSocketsSample.Controllers;

public class SendAVWSC : ControllerBase
{
    [Route("/in-av")]
    public async Task Get()
    {
        if (HttpContext.WebSockets.IsWebSocketRequest)
        {
            using var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();

            // loop
            Task handleCloseTask = HandleClose (webSocket); // start waiting for close request
            await BinaryData (webSocket); // send out data
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

    private async Task BinaryData (WebSocket webSocket)
    {
        byte[] binData = new byte[256];
        for (int i = 0; i <= 255; ++i)
        {
            binData[i] = (byte) i;
        }

        while (!shouldClose)
        {
            SendMessageSegments (webSocket, binData, 16);
            Thread.Sleep (1000);
        }

        await webSocket.CloseAsync(
            WebSocketCloseStatus.NormalClosure,
            "Closing from client",
            CancellationToken.None);
    }

    // send one text spread over several chunks
    private static async void SendMessageSegments (WebSocket webSocket, byte[] message, int segmentSize)
    {
        int messageBytesSent = 0;

        while (messageBytesSent < message.Length) {
            int messageSegmentSize = Math.Min (
                segmentSize,
                message.Length - messageBytesSent);
            byte[] messageSegmentBuffer = new ArraySegment<byte> (
                message,
                messageBytesSent,
                messageSegmentSize).ToArray();
            messageBytesSent += messageSegmentSize;

            PrintMessage (messageSegmentBuffer);

            await webSocket.SendAsync(
                messageSegmentBuffer,
                WebSocketMessageType.Binary,
                messageBytesSent >= message.Length,
                CancellationToken.None);
        }
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
