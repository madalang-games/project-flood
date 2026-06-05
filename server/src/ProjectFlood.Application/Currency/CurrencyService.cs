using ProjectFlood.Application.Logging;
using ProjectFlood.Contracts.Currency;
using ProjectFlood.Infrastructure.Generated;

namespace ProjectFlood.Application.Currency;

public sealed class CurrencyService
{
    private readonly AppDbContext _db;

    public CurrencyService(AppDbContext db) => _db = db;

    public async Task<CurrencySnapshot> GrantSoftAsync(long userId, long amount, string reason, string correlationId, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var row = await _db.UserCurrency.FindAsync(userId, ct);
        if (row is null)
        {
            row = new UserCurrencyRow { UserId = userId, SoftAmount = 0, UpdatedAt = now };
            _db.UserCurrency.Insert(row);
        }

        row.SoftAmount += amount;
        row.UpdatedAt = now;

        _db.EventLogs.Insert(EventLogFactory.CurrencyChanged(userId, correlationId, amount, reason, row.SoftAmount));
        return new CurrencySnapshot { SoftAmount = row.SoftAmount };
    }

    public async Task<CurrencySnapshot> GetAsync(long userId, CancellationToken ct)
    {
        var row = await _db.UserCurrency.FindAsync(userId, ct);
        return new CurrencySnapshot { SoftAmount = row?.SoftAmount ?? 0 };
    }
}
