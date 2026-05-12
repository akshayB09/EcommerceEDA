using Contracts;
using InventoryService.Data;
using MassTransit;
using MediatR;

namespace InventoryService.Commands;

public class ReserveStockCommandHandler(
    InventoryDbContext db,
    IPublishEndpoint publisher,
    ILogger<ReserveStockCommandHandler> logger)
    : IRequestHandler<ReserveStockCommand, ReserveStockResult>
{
    public async Task<ReserveStockResult> Handle(ReserveStockCommand command, CancellationToken ct)
    {
        logger.LogInformation("Checking inventory for Order {OrderId}", command.OrderId);

        foreach (var item in command.Items)
        {
            var product = await db.Products.FindAsync([item.ProductId], ct);

            if (product is null || product.Stock < item.Quantity)
            {
                var reason = product is null
                    ? $"Product {item.ProductId} not found"
                    : $"Only {product.Stock} units of '{product.Name}' available, {item.Quantity} requested";

                logger.LogWarning("Stock insufficient for Order {OrderId}: {Reason}", command.OrderId, reason);
                await publisher.Publish(new StockInsufficient(command.OrderId, reason), ct);
                return new ReserveStockResult(false, reason);
            }
        }

        foreach (var item in command.Items)
        {
            var product = await db.Products.FindAsync([item.ProductId], ct);
            product!.Stock -= item.Quantity;
        }

        await db.SaveChangesAsync(ct);
        logger.LogInformation("Inventory reserved for Order {OrderId}", command.OrderId);
        await publisher.Publish(new InventoryReserved(command.OrderId, DateTime.UtcNow), ct);
        return new ReserveStockResult(true, null);
    }
}
