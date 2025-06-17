using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PaymentsService.Models;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using System.Threading;

namespace PaymentsService.Inbox
{
    public class InboxProcessor : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfiguration _configuration;
        public InboxProcessor(IServiceProvider serviceProvider, IConfiguration configuration)
        {
            _serviceProvider = serviceProvider;
            _configuration = configuration;
        }

        private IConnection CreateConnectionWithRetry(ConnectionFactory factory, int maxRetries = 10, int delaySeconds = 5)
        {
            for (int i = 0; i < maxRetries; i++)
            {
                try { return factory.CreateConnection(); }
                catch { Thread.Sleep(TimeSpan.FromSeconds(delaySeconds)); }
            }
            throw new Exception("Could not connect to RabbitMQ after several attempts");
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var factory = new ConnectionFactory { HostName = _configuration["RabbitMQ:Host"] ?? "rabbitmq" };
            using var connection = CreateConnectionWithRetry(factory);
            using var channel = connection.CreateModel();
            channel.ExchangeDeclare("orders.payments", ExchangeType.Fanout, durable: true);
            var queueName = channel.QueueDeclare("payments.inbox", durable: true, exclusive: false, autoDelete: false).QueueName;
            channel.QueueBind(queue: queueName, exchange: "orders.payments", routingKey: "");

            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += async (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                var orderEvent = JsonSerializer.Deserialize<OrderCreatedEvent>(message);
                if (orderEvent == null) { channel.BasicAck(ea.DeliveryTag, false); return; }
                using var scope = _serviceProvider.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<PaymentsDbContext>();
                if (await db.InboxMessages.AnyAsync(m => m.Content.Contains(orderEvent.Id.ToString()) && m.Processed))
                {
                    channel.BasicAck(ea.DeliveryTag, false);
                    return;
                }
                var inbox = new Models.InboxMessage
                {
                    Id = Guid.NewGuid(),
                    OccurredOn = DateTime.UtcNow,
                    Type = "OrderCreated",
                    Content = message,
                    Processed = false
                };
                db.InboxMessages.Add(inbox);
                var account = await db.Accounts.FirstOrDefaultAsync(a => a.UserId == orderEvent.UserId);
                string status;
                if (account != null && account.Balance >= orderEvent.Amount)
                {
                    account.Balance -= orderEvent.Amount;
                    status = "FINISHED";
                }
                else
                {
                    status = "CANCELLED";
                }
                inbox.Processed = true;
                inbox.ProcessedOn = DateTime.UtcNow;
                db.OutboxMessages.Add(new Models.OutboxMessage
                {
                    Id = Guid.NewGuid(),
                    OccurredOn = DateTime.UtcNow,
                    Type = "PaymentResult",
                    Content = JsonSerializer.Serialize(new { orderEvent.Id, Status = status }),
                    Processed = false
                });
                await db.SaveChangesAsync();
                channel.BasicAck(ea.DeliveryTag, false);
            };
            channel.BasicConsume(queue: queueName, autoAck: false, consumer: consumer);

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(2000, stoppingToken);
            }
        }
    }

    public class OrderCreatedEvent
    {
        public Guid Id { get; set; }
        public string UserId { get; set; } = null!;
        public decimal Amount { get; set; }
    }
} 