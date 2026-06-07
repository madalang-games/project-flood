using Microsoft.AspNetCore.Mvc;
using ProjectFlood.Application.Currency;
using ProjectFlood.Contracts.Currency;

namespace ProjectFlood.API.Controllers;

[ApiController]
[Route("api/currency")]
public sealed class CurrencyController : ControllerBaseEx
{
    private readonly CurrencyService _currency;

    public CurrencyController(CurrencyService currency)
    {
        _currency = currency;
    }

    [HttpGet]
    public Task<CurrencySnapshot> Get(CancellationToken ct)
        => _currency.GetAsync(PlayerId, ct);

    [HttpPost("spend")]
    public Task<CurrencySnapshot> Spend([FromBody] SpendSoftRequest request, CancellationToken ct)
        => _currency.SpendSoftAsync(PlayerId, request.Amount, request.Reason, CorrelationId, ct);
}
