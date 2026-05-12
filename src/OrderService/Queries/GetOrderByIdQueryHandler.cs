using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderService.Data;
using OrderService.Models;

namespace OrderService.Queries;

public class GetOrderByIdQueryHandler(OrderDbContext db)
    : IRequestHandler<GetOrderByIdQuery, Order?>
{
    public Task<Order?> Handle(GetOrderByIdQuery query, CancellationToken ct)
        => db.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == query.OrderId, ct);
}
