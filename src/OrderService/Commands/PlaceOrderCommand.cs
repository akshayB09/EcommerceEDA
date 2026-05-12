using MediatR;
using OrderService.Models;

namespace OrderService.Commands;

public record PlaceOrderCommand(string CustomerId, List<PlaceOrderCommandItem> Items)
    : IRequest<PlaceOrderResult>;

public record PlaceOrderCommandItem(string ProductId, string ProductName, int Quantity, decimal UnitPrice);

public record PlaceOrderResult(Guid OrderId, OrderStatus Status);
