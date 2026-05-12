using MediatR;

namespace PaymentService.Commands;

public record ProcessPaymentCommand(Guid OrderId) : IRequest<ProcessPaymentResult>;

public record ProcessPaymentResult(bool Success, string? TransactionId, string? FailureReason);
