using System.Net.WebSockets;

namespace OrdersService.WebSockets
{
    public interface IWebSocketService
    {
        Task HandleWebSocketAsync(WebSocket webSocket);
        Task SendOrderStatusUpdate(Guid orderId, string status);
    }
} 