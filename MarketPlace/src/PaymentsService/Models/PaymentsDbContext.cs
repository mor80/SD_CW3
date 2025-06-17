using Microsoft.EntityFrameworkCore;

namespace PaymentsService.Models
{
    public class PaymentsDbContext : DbContext
    {
        public PaymentsDbContext(DbContextOptions<PaymentsDbContext> options) : base(options) { }

        public DbSet<Account> Accounts { get; set; } = null!;
        public DbSet<InboxMessage> InboxMessages { get; set; } = null!;
        public DbSet<OutboxMessage> OutboxMessages { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Account>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.UserId).IsUnique();
            });
            modelBuilder.Entity<InboxMessage>(entity =>
            {
                entity.HasKey(e => e.Id);
            });
            modelBuilder.Entity<OutboxMessage>(entity =>
            {
                entity.HasKey(e => e.Id);
            });
        }
    }
} 