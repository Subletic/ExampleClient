using Microsoft.AspNetCore.Mvc;
using System.Buffers;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Web;
using System;

namespace WebSocketsSample.Controllers;

public class ReceiveAVSWSC : ControllerBase
{
    [Route("/out-avs")]
    public async Task Get()
    {
        if (HttpContext.WebSockets.IsWebSocketRequest)
        {
            using var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();

            // loop
            await BinaryData (webSocket); // receive data
        }
        else
        {
            HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        }
    }

    private async Task BinaryData (WebSocket webSocket)
    {
        byte[] dataBuffer = new byte[8 * 1024 * 1024];
        var receiveResult = await webSocket.ReceiveAsync (
            new ArraySegment<byte> (dataBuffer),
            CancellationToken.None);

        // for now, just keep swallowing the data
        while (!receiveResult.CloseStatus.HasValue)
        {
            receiveResult = await webSocket.ReceiveAsync (
                new ArraySegment<byte> (dataBuffer),
                CancellationToken.None);
        }

        await webSocket.CloseAsync (
            receiveResult.CloseStatus.Value,
            receiveResult.CloseStatusDescription,
            CancellationToken.None);
    }
}
