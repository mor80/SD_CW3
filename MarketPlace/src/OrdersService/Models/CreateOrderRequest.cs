namespace OrdersService.Models
{
    public class CreateOrderRequest
    {
        public string UserId { get; set; } = null!;
        public decimal Amount { get; set; }
        public string Description { get; set; } = null!;
    }
} 