using InventoryService.Data;
using InventoryService.Models;
using MediatR;

namespace InventoryService.Queries;

public class GetProductStockQueryHandler(InventoryDbContext db)
    : IRequestHandler<GetProductStockQuery, Product?>
{
    public async Task<Product?> Handle(GetProductStockQuery query, CancellationToken ct)
        => await db.Products.FindAsync([query.ProductId], ct);
}
