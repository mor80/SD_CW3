using Microsoft.EntityFrameworkCore;
using PaymentsService.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Payments Service API",
        Version = "v1",
        Description = "API for managing payments in the shopping system"
    });
});

builder.Services.AddDbContext<PaymentsDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("PaymentsDb")));

builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("PaymentsDb"));

builder.Services.AddHostedService<PaymentsService.Inbox.InboxProcessor>();
builder.Services.AddHostedService<PaymentsService.Outbox.OutboxProcessor>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
