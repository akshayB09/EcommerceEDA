using Contracts;
using MassTransit;
using MediatR;

namespace PaymentService.Commands;

public class ProcessPaymentCommandHandler(IPublishEndpoint publisher, ILogger<ProcessPaymentCommandHandler> logger)
    : IRequestHandler<ProcessPaymentCommand, ProcessPaymentResult>
{
    public async Task<ProcessPaymentResult> Handle(ProcessPaymentCommand command, CancellationToken ct)
    {
        logger.LogInformation("Processing payment for Order {OrderId}...", command.OrderId);

        var success = Random.Shared.Next(100) >= 20;

        if (success)
        {
            var transactionId = $"TXN-{Guid.NewGuid():N}"[..16].ToUpper();
            logger.LogInformation("Payment approved for Order {OrderId} — txn {TxnId}", command.OrderId, transactionId);
            await publisher.Publish(new PaymentProcessed(command.OrderId, transactionId, DateTime.UtcNow), ct);
            return new ProcessPaymentResult(true, transactionId, null);
        }

        logger.LogWarning("Payment declined for Order {OrderId}", command.OrderId);
        await publisher.Publish(new PaymentFailed(command.OrderId, "Card declined (simulated)"), ct);
        return new ProcessPaymentResult(false, null, "Card declined (simulated)");
    }
}
