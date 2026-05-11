using Contracts;
using MassTransit;

namespace PaymentService.Consumers;

public class InventoryReservedConsumer(IPublishEndpoint publisher, ILogger<InventoryReservedConsumer> logger)
    : IConsumer<InventoryReserved>
{
    public async Task Consume(ConsumeContext<InventoryReserved> context)
    {
        var msg = context.Message;
        logger.LogInformation("Processing payment for Order {OrderId}...", msg.OrderId);

        // Simulate payment: fail 20% of the time so you can see both paths
        var success = Random.Shared.Next(100) >= 20;

        if (success)
        {
            var transactionId = $"TXN-{Guid.NewGuid():N}"[..16].ToUpper();
            logger.LogInformation("Payment approved for Order {OrderId} — txn {TxnId}", msg.OrderId, transactionId);
            await publisher.Publish(new PaymentProcessed(msg.OrderId, transactionId, DateTime.UtcNow));
        }
        else
        {
            logger.LogWarning("Payment declined for Order {OrderId}", msg.OrderId);
            await publisher.Publish(new PaymentFailed(msg.OrderId, "Card declined (simulated)"));
        }
    }
}
