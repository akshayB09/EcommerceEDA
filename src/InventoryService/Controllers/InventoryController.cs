using InventoryService.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace InventoryService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InventoryController(IMediator mediator) : ControllerBase
{
    [HttpGet("{productId}")]
    public async Task<IActionResult> GetStock(string productId)
    {
        var product = await mediator.Send(new GetProductStockQuery(productId));
        return product is null ? NotFound() : Ok(product);
    }
}
