using InventoryService.Models;
using MediatR;

namespace InventoryService.Queries;

public record GetProductStockQuery(string ProductId) : IRequest<Product?>;
