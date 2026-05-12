using MediatR;
using OrderService.Models;

namespace OrderService.Queries;

public record GetOrderByIdQuery(Guid OrderId) : IRequest<Order?>;
