using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Net.Http.Json;

namespace ApiGateway.Controllers
{
    public class CreateAccountRequest
    {
        public string UserId { get; set; } = null!;
    }
    public class DepositRequest
    {
        public string UserId { get; set; } = null!;
        public decimal Amount { get; set; }
    }

    [ApiController]
    [Route("accounts")]
    public class AccountsController : ControllerBase
    {
        private readonly HttpClient _http;
        public AccountsController(IHttpClientFactory factory)
        {
            _http = factory.CreateClient("payments");
        }

        [HttpPost]
        public async Task<IActionResult> CreateAccount([FromBody] CreateAccountRequest req)
        {
            var response = await _http.PostAsJsonAsync("/api/accounts", req);
            var content = await response.Content.ReadAsStringAsync();
            return StatusCode((int)response.StatusCode, content);
        }

        [HttpPost("deposit")]
        public async Task<IActionResult> Deposit([FromBody] DepositRequest req)
        {
            var response = await _http.PostAsJsonAsync("/api/accounts/deposit", req);
            var content = await response.Content.ReadAsStringAsync();
            return StatusCode((int)response.StatusCode, content);
        }

        [HttpGet("balance")]
        public async Task<IActionResult> GetBalance([FromQuery] string userId)
        {
            var response = await _http.GetAsync($"/api/accounts/balance?userId={userId}");
            var content = await response.Content.ReadAsStringAsync();
            return StatusCode((int)response.StatusCode, content);
        }
    }
} 