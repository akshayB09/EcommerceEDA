using Contracts;
using InventoryService.Commands;
using MassTransit;
using MediatR;

namespace InventoryService.Consumers;

public class OrderPlacedConsumer(IMediator mediator, ILogger<OrderPlacedConsumer> logger)
    : IConsumer<OrderPlaced>
{
    public async Task Consume(ConsumeContext<OrderPlaced> context)
    {
        var msg = context.Message;
        logger.LogInformation("Received OrderPlaced for Order {OrderId}", msg.OrderId);

        await mediator.Send(new ReserveStockCommand(
            msg.OrderId,
            msg.Items.Select(i => new ReserveStockItem(i.ProductId, i.ProductName, i.Quantity)).ToList()));
    }
}
