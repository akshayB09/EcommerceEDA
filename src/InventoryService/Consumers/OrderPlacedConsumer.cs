using Contracts;
using InventoryService.Data;
using MassTransit;

namespace InventoryService.Consumers;

public class OrderPlacedConsumer(InventoryDbContext db, IPublishEndpoint publisher, ILogger<OrderPlacedConsumer> logger)
    : IConsumer<OrderPlaced>
{
    public async Task Consume(ConsumeContext<OrderPlaced> context)
    {
        var msg = context.Message;
        logger.LogInformation("Checking inventory for Order {OrderId}", msg.OrderId);

        foreach (var item in msg.Items)
        {
            var product = await db.Products.FindAsync(item.ProductId);

            if (product is null || product.Stock < item.Quantity)
            {
                var reason = product is null
                    ? $"Product {item.ProductId} not found"
                    : $"Only {product.Stock} units of '{product.Name}' available, {item.Quantity} requested";

                logger.LogWarning("Stock insufficient for Order {OrderId}: {Reason}", msg.OrderId, reason);
                await publisher.Publish(new StockInsufficient(msg.OrderId, reason));
                return;
            }
        }

        // All items available — reserve them
        foreach (var item in msg.Items)
        {
            var product = await db.Products.FindAsync(item.ProductId);
            product!.Stock -= item.Quantity;
        }

        await db.SaveChangesAsync();
        logger.LogInformation("Inventory reserved for Order {OrderId}", msg.OrderId);
        await publisher.Publish(new InventoryReserved(msg.OrderId, DateTime.UtcNow));
    }
}
