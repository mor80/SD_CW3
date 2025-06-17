namespace PaymentsService.Models
{
    public class DepositRequest
    {
        public string UserId { get; set; } = null!;
        public decimal Amount { get; set; }
    }
} 