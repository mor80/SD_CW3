using Microsoft.EntityFrameworkCore;

namespace OrdersService.Models
{
    public class OrdersDbContext : DbContext
    {
        public OrdersDbContext(DbContextOptions<OrdersDbContext> options) : base(options) { }

        public DbSet<Order> Orders { get; set; } = null!;
        public DbSet<OutboxMessage> OutboxMessages { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Order>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Status).HasConversion<string>();
            });
            modelBuilder.Entity<OutboxMessage>(entity =>
            {
                entity.HasKey(e => e.Id);
            });
        }
    }
} 