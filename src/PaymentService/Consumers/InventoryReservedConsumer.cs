using Contracts;
using MassTransit;
using MediatR;
using PaymentService.Commands;

namespace PaymentService.Consumers;

public class InventoryReservedConsumer(IMediator mediator, ILogger<InventoryReservedConsumer> logger)
    : IConsumer<InventoryReserved>
{
    public async Task Consume(ConsumeContext<InventoryReserved> context)
    {
        var msg = context.Message;
        logger.LogInformation("Received InventoryReserved for Order {OrderId}", msg.OrderId);
        await mediator.Send(new ProcessPaymentCommand(msg.OrderId));
    }
}
