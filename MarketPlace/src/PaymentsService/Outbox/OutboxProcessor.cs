using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PaymentsService.Models;
using RabbitMQ.Client;
using System.Text;
using System.Threading;

namespace PaymentsService.Outbox
{
    public class OutboxProcessor : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfiguration _configuration;
        public OutboxProcessor(IServiceProvider serviceProvider, IConfiguration configuration)
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
            channel.ExchangeDeclare("payments.orders", ExchangeType.Fanout, durable: true);

            while (!stoppingToken.IsCancellationRequested)
            {
                using var scope = _serviceProvider.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<PaymentsDbContext>();
                var messages = await db.OutboxMessages.Where(m => !m.Processed).Take(10).ToListAsync(stoppingToken);
                foreach (var msg in messages)
                {
                    var body = Encoding.UTF8.GetBytes(msg.Content);
                    channel.BasicPublish(exchange: "payments.orders", routingKey: "", basicProperties: null, body: body);
                    msg.Processed = true;
                    msg.ProcessedOn = DateTime.UtcNow;
                }
                await db.SaveChangesAsync(stoppingToken);
                await Task.Delay(2000, stoppingToken);
            }
        }
    }
} 