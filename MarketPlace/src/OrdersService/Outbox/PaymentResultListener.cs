using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OrdersService.Models;
using OrdersService.WebSockets;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using System.Threading;

namespace OrdersService.Outbox
{
    public class PaymentResultListener : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfiguration _configuration;
        public PaymentResultListener(IServiceProvider serviceProvider, IConfiguration configuration)
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
            var queueName = channel.QueueDeclare("orders.paymentresults", durable: true, exclusive: false, autoDelete: false).QueueName;
            channel.QueueBind(queue: queueName, exchange: "payments.orders", routingKey: "");

            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += async (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                var paymentResult = JsonSerializer.Deserialize<PaymentResultEvent>(message);
                if (paymentResult == null) { channel.BasicAck(ea.DeliveryTag, false); return; }
                using var scope = _serviceProvider.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
                var webSocketService = scope.ServiceProvider.GetRequiredService<IWebSocketService>();

                var order = await db.Orders.FirstOrDefaultAsync(o => o.Id == paymentResult.Id);
                if (order != null)
                {
                    OrderStatus oldStatus = order.Status;
                    if (paymentResult.Status == "FINISHED")
                        order.Status = OrderStatus.Finished;
                    else if (paymentResult.Status == "CANCELLED")
                        order.Status = OrderStatus.Cancelled;

                    if (order.Status != oldStatus)
                    {
                         await db.SaveChangesAsync();
                         await webSocketService.SendOrderStatusUpdate(order.Id, order.Status.ToString());
                    }
                }
                channel.BasicAck(ea.DeliveryTag, false);
            };
            channel.BasicConsume(queue: queueName, autoAck: false, consumer: consumer);

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(2000, stoppingToken);
            }
        }
    }

    public class PaymentResultEvent
    {
        public Guid Id { get; set; }
        public string Status { get; set; } = null!;
    }
} 