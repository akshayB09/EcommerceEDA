using Contracts;
using MassTransit;

namespace NotificationService.Consumers;

public class OrderPlacedConsumer(ILogger<OrderPlacedConsumer> logger) : IConsumer<OrderPlaced>
{
    public Task Consume(ConsumeContext<OrderPlaced> context)
    {
        var msg = context.Message;
        logger.LogInformation("[NOTIFY] Order placed — Customer: {CustomerId}, Order: {OrderId}, Total: ${Total:F2}",
            msg.CustomerId, msg.OrderId, msg.TotalAmount);
        return Task.CompletedTask;
    }
}

public class InventoryReservedConsumer(ILogger<InventoryReservedConsumer> logger) : IConsumer<InventoryReserved>
{
    public Task Consume(ConsumeContext<InventoryReserved> context)
    {
        logger.LogInformation("[NOTIFY] Inventory reserved for Order {OrderId}", context.Message.OrderId);
        return Task.CompletedTask;
    }
}

public class StockInsufficientConsumer(ILogger<StockInsufficientConsumer> logger) : IConsumer<StockInsufficient>
{
    public Task Consume(ConsumeContext<StockInsufficient> context)
    {
        logger.LogWarning("[NOTIFY] Order {OrderId} cancelled — out of stock: {Reason}",
            context.Message.OrderId, context.Message.Reason);
        return Task.CompletedTask;
    }
}

public class PaymentProcessedConsumer(ILogger<PaymentProcessedConsumer> logger) : IConsumer<PaymentProcessed>
{
    public Task Consume(ConsumeContext<PaymentProcessed> context)
    {
        var msg = context.Message;
        logger.LogInformation("[NOTIFY] Payment confirmed for Order {OrderId} — txn {TxnId}",
            msg.OrderId, msg.TransactionId);
        return Task.CompletedTask;
    }
}

public class PaymentFailedConsumer(ILogger<PaymentFailedConsumer> logger) : IConsumer<PaymentFailed>
{
    public Task Consume(ConsumeContext<PaymentFailed> context)
    {
        logger.LogWarning("[NOTIFY] Payment failed for Order {OrderId} — {Reason}",
            context.Message.OrderId, context.Message.Reason);
        return Task.CompletedTask;
    }
}
