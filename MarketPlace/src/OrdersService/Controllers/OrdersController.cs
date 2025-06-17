using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrdersService.Models;

namespace OrdersService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrdersController : ControllerBase
    {
        private readonly OrdersDbContext _db;

        public OrdersController(OrdersDbContext db)
        {
            _db = db;
        }

        [HttpPost]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
        {
            var order = new Order
            {
                Id = Guid.NewGuid(),
                UserId = request.UserId,
                Amount = request.Amount,
                Description = request.Description,
                Status = OrderStatus.New
            };
            _db.Orders.Add(order);

            var outbox = new OutboxMessage
            {
                Id = Guid.NewGuid(),
                OccurredOn = DateTime.UtcNow,
                Type = "OrderCreated",
                Content = System.Text.Json.JsonSerializer.Serialize(
                    new { order.Id, order.UserId, order.Amount }),
                Processed = false
            };
            _db.OutboxMessages.Add(outbox);

            await _db.SaveChangesAsync();
            return CreatedAtAction(nameof(GetOrderStatus), new { id = order.Id }, order);
        }

        [HttpGet]
        public async Task<IActionResult> GetOrders([FromQuery] string userId)
        {
            var orders = await _db.Orders
                .Where(o => o.UserId == userId)
                .ToListAsync();
            return Ok(orders);
        }

        [HttpGet("{id}/status")]
        public async Task<IActionResult> GetOrderStatus(Guid id)
        {
            var order = await _db.Orders.FindAsync(id);
            if (order == null) return NotFound();
            return Ok(new { order.Id, order.Status });
        }
    }
} 