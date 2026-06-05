using ProjectFlood.Application.Logging;
using ProjectFlood.Contracts.Ad;
using ProjectFlood.Generated.Data;
using ProjectFlood.Infrastructure.Generated;

namespace ProjectFlood.Application.Stage;

public sealed class AdInterstitialService
{
    private readonly AppDbContext _db;
    private readonly Lazy<AdPlacement?> _interstitialPlacement;

    public AdInterstitialService(AppDbContext db)
    {
        _db = db;
        _interstitialPlacement = new Lazy<AdPlacement?>(() =>
        {
            var path = System.IO.Path.Combine(AppContext.BaseDirectory, "generated", "data", "ad", "ad_placement.csv");
            return AdPlacementLoader.LoadAsDict(path).TryGetValue("INTERSTITIAL_POST_STAGE", out var p) ? p : null;
        });
    }

    public async Task<AdEligibilityResponse> GetEligibilityAsync(long userId, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var placement = _interstitialPlacement.Value;
        var state = await _db.UserInterstitialState.FindAsync(userId, ct);

        int cooldown = placement?.cooldown_seconds ?? 180;
        bool isEligible = state is null
            || (now - state.LastShownAt).TotalSeconds >= cooldown;
        int remaining = isEligible ? 0 : (int)(cooldown - (now - state!.LastShownAt).TotalSeconds);

        return new AdEligibilityResponse
        {
            Placements = new List<AdPlacementStatus>
            {
                new()
                {
                    PlacementId = "INTERSTITIAL_POST_STAGE",
                    IsEligible = isEligible,
                    CooldownRemainingSeconds = Math.Max(0, remaining),
                },
            },
            ServerTime = now,
        };
    }

    public async Task<AdInterstitialShownResponse> RecordShownAsync(long userId, int stageId, string correlationId, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var row = await _db.UserInterstitialState.FindAsync(userId, ct);
        if (row is null)
        {
            row = new UserInterstitialStateRow { UserId = userId, LastShownAt = now, UpdatedAt = now };
            _db.UserInterstitialState.Insert(row);
        }
        else
        {
            row.LastShownAt = now;
            row.UpdatedAt = now;
        }

        _db.EventLogs.Insert(EventLogFactory.AdInterstitialShown(userId, correlationId, stageId));
        await _db.SaveAsync(ct);

        return new AdInterstitialShownResponse { ServerTime = now };
    }
}
