using MediatR;
using Microsoft.AspNetCore.Mvc;
using PaymentService.Commands;

namespace PaymentService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentController(IMediator mediator) : ControllerBase
{
    [HttpPost("{orderId:guid}/process")]
    public async Task<IActionResult> ProcessPayment(Guid orderId)
    {
        var result = await mediator.Send(new ProcessPaymentCommand(orderId));
        return Ok(result);
    }
}
