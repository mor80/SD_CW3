using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using Microsoft.Extensions.Logging;

namespace OrdersService.WebSockets
{
    public class WebSocketService : IWebSocketService
    {
        private readonly ConcurrentDictionary<Guid, ConcurrentBag<WebSocket>> _orderConnections = new();
        private readonly ILogger<WebSocketService> _logger;

        public WebSocketService(ILogger<WebSocketService> logger)
        {
            _logger = logger;
        }

        public async Task HandleWebSocketAsync(WebSocket webSocket)
        {
            var buffer = new byte[1024 * 4];
            var receiveResult = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

            if (receiveResult.MessageType == WebSocketMessageType.Text)
            {
                var orderIdString = Encoding.UTF8.GetString(buffer, 0, receiveResult.Count);
                if (Guid.TryParse(orderIdString, out var orderId))
                {
                    _logger.LogInformation($"WebSocket opened for OrderId: {orderId}");
                    _orderConnections.GetOrAdd(orderId, new ConcurrentBag<WebSocket>()).Add(webSocket);

                    while (!webSocket.CloseStatus.HasValue)
                    {
                        receiveResult = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                    }

                    _logger.LogInformation($"WebSocket closed for OrderId: {orderId} with status: {webSocket.CloseStatus}");
                }
                else
                {
                    _logger.LogWarning($"Invalid OrderId received: {orderIdString}");
                    await webSocket.CloseAsync(WebSocketCloseStatus.InvalidPayloadData, "Invalid OrderId", CancellationToken.None);
                }
            }
            else if (receiveResult.MessageType == WebSocketMessageType.Close)
            {
                _logger.LogInformation($"WebSocket closed by client with status: {receiveResult.CloseStatus}");
            }
        }

        public async Task SendOrderStatusUpdate(Guid orderId, string status)
        {
            if (_orderConnections.TryGetValue(orderId, out var connections))
            {
                string message = $"{{\"orderId\":\"{orderId}\",\"status\":\"{status}\"}}";
                var bytes = Encoding.UTF8.GetBytes(message);

                foreach (var ws in connections.ToList())
                {
                    if (ws.State == WebSocketState.Open)
                    {
                        try
                        {
                            await ws.SendAsync(new ArraySegment<byte>(bytes, 0, bytes.Length), WebSocketMessageType.Text, true, CancellationToken.None);
                            _logger.LogInformation($"Sent status update for OrderId {orderId} to client.");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"Failed to send WebSocket message for OrderId {orderId}.");
                        }
                    }
                }
            }
        }
    }
} 