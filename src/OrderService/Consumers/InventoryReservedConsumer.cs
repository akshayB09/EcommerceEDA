using Contracts;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using OrderService.Data;
using OrderService.Models;

namespace OrderService.Consumers;

public class InventoryReservedConsumer(OrderDbContext db, ILogger<InventoryReservedConsumer> logger)
    : IConsumer<InventoryReserved>
{
    public async Task Consume(ConsumeContext<InventoryReserved> context)
    {
        var msg = context.Message;
        var order = await db.Orders.FindAsync(msg.OrderId);
        if (order is null) return;

        order.Status = OrderStatus.InventoryReserved;
        await db.SaveChangesAsync();
        logger.LogInformation("Order {OrderId} status → InventoryReserved", msg.OrderId);
    }
}
