using MediatR;

namespace InventoryService.Commands;

public record ReserveStockCommand(Guid OrderId, List<ReserveStockItem> Items)
    : IRequest<ReserveStockResult>;

public record ReserveStockItem(string ProductId, string ProductName, int Quantity);

public record ReserveStockResult(bool Success, string? FailureReason);
