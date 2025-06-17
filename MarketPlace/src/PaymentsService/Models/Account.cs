namespace PaymentsService.Models
{
    public class Account
    {
        public Guid Id { get; set; }
        public string UserId { get; set; } = null!;
        public decimal Balance { get; set; }
    }
} 