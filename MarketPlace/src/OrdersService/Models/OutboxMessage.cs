using System;

namespace OrdersService.Models
{
    public class OutboxMessage
    {
        public Guid Id { get; set; }
        public DateTime OccurredOn { get; set; }
        public string Type { get; set; } = null!;
        public string Content { get; set; } = null!;
        public bool Processed { get; set; }
        public DateTime? ProcessedOn { get; set; }
    }
} 