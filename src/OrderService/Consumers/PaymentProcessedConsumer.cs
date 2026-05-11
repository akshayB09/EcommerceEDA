using Contracts;
using MassTransit;
using OrderService.Data;
using OrderService.Models;

namespace OrderService.Consumers;

public class PaymentProcessedConsumer(OrderDbContext db, ILogger<PaymentProcessedConsumer> logger)
    : IConsumer<PaymentProcessed>
{
    public async Task Consume(ConsumeContext<PaymentProcessed> context)
    {
        var msg = context.Message;
        var order = await db.Orders.FindAsync(msg.OrderId);
        if (order is null) return;

        order.Status = OrderStatus.PaymentProcessed;
        await db.SaveChangesAsync();
        logger.LogInformation("Order {OrderId} status → PaymentProcessed (txn: {TxnId})", msg.OrderId, msg.TransactionId);
    }
}
