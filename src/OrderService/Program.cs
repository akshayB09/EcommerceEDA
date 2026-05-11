using Contracts;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using OrderService.Consumers;
using OrderService.Data;
using OrderService.Models;

var builder = WebApplication.CreateBuilder(args);

var dbPath = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
    ".ecommerce-eda", "orders.db");

builder.Services.AddDbContext<OrderDbContext>(opts => opts.UseSqlite($"Data Source={dbPath}"));

builder.Services.AddMassTransit(x =>
{
    x.SetEndpointNameFormatter(new KebabCaseEndpointNameFormatter("order", false));
    x.AddConsumer<InventoryReservedConsumer>();
    x.AddConsumer<PaymentProcessedConsumer>();
    x.AddConsumer<StockInsufficientConsumer>();
    x.AddConsumer<PaymentFailedConsumer>();

    x.UsingRabbitMq((ctx, cfg) =>
    {
        cfg.Host(builder.Configuration["RabbitMq:Host"] ?? "localhost", "/", h =>
        {
            h.Username(builder.Configuration["RabbitMq:Username"] ?? "guest");
            h.Password(builder.Configuration["RabbitMq:Password"] ?? "guest");
        });
        cfg.ConfigureEndpoints(ctx);
    });
});

builder.Services.AddOpenApi();

var app = builder.Build();

Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
    db.Database.EnsureCreated();
}

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

// POST /api/orders — place a new order
app.MapPost("/api/orders", async (PlaceOrderRequest req, OrderDbContext db, IPublishEndpoint publisher) =>
{
    var order = new Order
    {
        Id = Guid.NewGuid(),
        CustomerId = req.CustomerId,
        TotalAmount = req.Items.Sum(i => i.Quantity * i.UnitPrice),
        Items = req.Items.Select(i => new OrderLineItem
        {
            Id = Guid.NewGuid(),
            ProductId = i.ProductId,
            ProductName = i.ProductName,
            Quantity = i.Quantity,
            UnitPrice = i.UnitPrice
        }).ToList()
    };

    db.Orders.Add(order);
    await db.SaveChangesAsync();

    await publisher.Publish(new OrderPlaced(
        order.Id,
        order.CustomerId,
        order.Items.Select(i => new OrderItem(i.ProductId, i.ProductName, i.Quantity, i.UnitPrice)).ToList(),
        order.TotalAmount,
        DateTime.UtcNow));

    return Results.Created($"/api/orders/{order.Id}", new { order.Id, order.Status });
});

// GET /api/orders/{id} — check order status
app.MapGet("/api/orders/{id:guid}", async (Guid id, OrderDbContext db) =>
{
    var order = await db.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == id);
    return order is null ? Results.NotFound() : Results.Ok(order);
});

app.Run();

record PlaceOrderRequestItem(string ProductId, string ProductName, int Quantity, decimal UnitPrice);
record PlaceOrderRequest(string CustomerId, List<PlaceOrderRequestItem> Items);
