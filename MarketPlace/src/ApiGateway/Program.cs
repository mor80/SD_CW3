using Yarp.ReverseProxy;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Shopping Tool API Gateway",
        Version = "v1",
        Description = "API Gateway for the shopping system"
    });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontendOrigin",
        builder =>
        {
            builder.WithOrigins("http://localhost:3000")
                   .AllowAnyHeader()
                   .AllowAnyMethod();
        });
});

builder.Services.AddHttpClient("orders", c =>
{
    c.BaseAddress = new Uri("http://orders-service:5000");
});

builder.Services.AddHttpClient("payments", c =>
{
    c.BaseAddress = new Uri("http://payments-service:5001");
});

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("AllowFrontendOrigin");

app.MapControllers();
app.MapReverseProxy();

app.Run();
