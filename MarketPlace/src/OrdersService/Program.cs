using Microsoft.EntityFrameworkCore;
using OrdersService.Models;
using OrdersService.WebSockets;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Orders Service API",
        Version = "v1",
        Description = "API for managing orders in the shopping system"
    });
});

builder.Services.AddSingleton<IWebSocketService, WebSocketService>();

builder.Services.AddDbContext<OrdersDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("OrdersDb")));

builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("OrdersDb"));

builder.Services.AddHostedService<OrdersService.Outbox.OutboxProcessor>();
builder.Services.AddHostedService<OrdersService.Outbox.PaymentResultListener>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthorization();

app.UseWebSockets();

app.Map("/ws", async context =>
{
    if (context.WebSockets.IsWebSocketRequest)
    {
        var ws = await context.WebSockets.AcceptWebSocketAsync();
        var webSocketService = context.RequestServices.GetRequiredService<IWebSocketService>();
        await webSocketService.HandleWebSocketAsync(ws);
    }
    else
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
    }
});

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
