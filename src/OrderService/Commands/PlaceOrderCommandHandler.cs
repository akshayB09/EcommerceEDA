using Contracts;
using MassTransit;
using MediatR;
using OrderService.Data;
using OrderService.Models;

namespace OrderService.Commands;

public class PlaceOrderCommandHandler(OrderDbContext db, IPublishEndpoint publisher)
    : IRequestHandler<PlaceOrderCommand, PlaceOrderResult>
{
    public async Task<PlaceOrderResult> Handle(PlaceOrderCommand command, CancellationToken ct)
    {
        var order = new Order
        {
            Id = Guid.NewGuid(),
            CustomerId = command.CustomerId,
            TotalAmount = command.Items.Sum(i => i.Quantity * i.UnitPrice),
            Items = command.Items.Select(i => new OrderLineItem
            {
                Id = Guid.NewGuid(),
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice
            }).ToList()
        };

        db.Orders.Add(order);
        await db.SaveChangesAsync(ct);

        await publisher.Publish(new OrderPlaced(
            order.Id,
            order.CustomerId,
            order.Items.Select(i => new OrderItem(i.ProductId, i.ProductName, i.Quantity, i.UnitPrice)).ToList(),
            order.TotalAmount,
            DateTime.UtcNow), ct);

        return new PlaceOrderResult(order.Id, order.Status);
    }
}
