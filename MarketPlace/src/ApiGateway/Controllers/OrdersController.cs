using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Net.Http.Json;

namespace ApiGateway.Controllers
{
    public class CreateOrderRequest
    {
        public string UserId { get; set; } = null!;
        public decimal Amount { get; set; }
        public string Description { get; set; } = null!;
    }

    [ApiController]
    [Route("orders")]
    public class OrdersController : ControllerBase
    {
        private readonly HttpClient _http;
        public OrdersController(IHttpClientFactory factory)
        {
            _http = factory.CreateClient("orders");
        }

        [HttpPost]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest req)
        {
            var response = await _http.PostAsJsonAsync("/api/orders", req);
            var content = await response.Content.ReadAsStringAsync();
            return StatusCode((int)response.StatusCode, content);
        }

        [HttpGet]
        public async Task<IActionResult> GetOrders([FromQuery] string userId)
        {
            var response = await _http.GetAsync($"/api/orders?userId={userId}");
            var content = await response.Content.ReadAsStringAsync();
            return StatusCode((int)response.StatusCode, content);
        }

        [HttpGet("{id}/status")]
        public async Task<IActionResult> GetOrderStatus([FromRoute] string id)
        {
            var response = await _http.GetAsync($"/api/orders/{id}/status");
            var content = await response.Content.ReadAsStringAsync();
            return StatusCode((int)response.StatusCode, content);
        }
    }
} 