namespace Contracts;

public record OrderItem(string ProductId, string ProductName, int Quantity, decimal UnitPrice);

public record OrderPlaced(
    Guid OrderId,
    string CustomerId,
    List<OrderItem> Items,
    decimal TotalAmount,
    DateTime PlacedAt);

public record InventoryReserved(
    Guid OrderId,
    DateTime ReservedAt);

public record StockInsufficient(
    Guid OrderId,
    string Reason);

public record PaymentProcessed(
    Guid OrderId,
    string TransactionId,
    DateTime ProcessedAt);

public record PaymentFailed(
    Guid OrderId,
    string Reason);
