using Contracts;
using MassTransit;
using OrderService.Data;
using OrderService.Models;

namespace OrderService.Consumers;

public class StockInsufficientConsumer(OrderDbContext db, ILogger<StockInsufficientConsumer> logger)
    : IConsumer<StockInsufficient>
{
    public async Task Consume(ConsumeContext<StockInsufficient> context)
    {
        var msg = context.Message;
        var order = await db.Orders.FindAsync(msg.OrderId);
        if (order is null) return;

        order.Status = OrderStatus.Failed;
        await db.SaveChangesAsync();
        logger.LogWarning("Order {OrderId} failed — stock insufficient: {Reason}", msg.OrderId, msg.Reason);
    }
}

public class PaymentFailedConsumer(OrderDbContext db, ILogger<PaymentFailedConsumer> logger)
    : IConsumer<PaymentFailed>
{
    public async Task Consume(ConsumeContext<PaymentFailed> context)
    {
        var msg = context.Message;
        var order = await db.Orders.FindAsync(msg.OrderId);
        if (order is null) return;

        order.Status = OrderStatus.Failed;
        await db.SaveChangesAsync();
        logger.LogWarning("Order {OrderId} failed — payment failed: {Reason}", msg.OrderId, msg.Reason);
    }
}
