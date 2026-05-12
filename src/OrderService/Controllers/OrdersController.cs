using MediatR;
using Microsoft.AspNetCore.Mvc;
using OrderService.Commands;
using OrderService.Queries;

namespace OrderService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> PlaceOrder([FromBody] PlaceOrderRequest request)
    {
        var result = await mediator.Send(new PlaceOrderCommand(
            request.CustomerId,
            request.Items.Select(i => new PlaceOrderCommandItem(
                i.ProductId, i.ProductName, i.Quantity, i.UnitPrice)).ToList()));

        return CreatedAtAction(nameof(GetOrder), new { id = result.OrderId }, result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetOrder(Guid id)
    {
        var order = await mediator.Send(new GetOrderByIdQuery(id));
        return order is null ? NotFound() : Ok(order);
    }
}

public record PlaceOrderRequestItem(string ProductId, string ProductName, int Quantity, decimal UnitPrice);
public record PlaceOrderRequest(string CustomerId, List<PlaceOrderRequestItem> Items);
