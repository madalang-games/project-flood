using Microsoft.AspNetCore.Mvc;
using ProjectFlood.Application.Inventory;
using ProjectFlood.Contracts.Inventory;

namespace ProjectFlood.API.Controllers;

[ApiController]
[Route("api/inventory")]
public sealed class InventoryController : ControllerBaseEx
{
    private readonly InventoryService _inventory;

    public InventoryController(InventoryService inventory)
    {
        _inventory = inventory;
    }

    [HttpGet]
    public Task<InventorySnapshot> Get(CancellationToken ct)
        => _inventory.GetInventoryAsync(PlayerId, ct);

    [HttpPost("spend")]
    public Task<InventorySnapshot> Spend([FromBody] SpendItemRequest request, CancellationToken ct)
        => _inventory.SpendItemAsync(PlayerId, request.ItemId, request.Amount, request.Reason, CorrelationId, ct);
}
