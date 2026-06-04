using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace ProjectFlood.API.Controllers;

public abstract class ControllerBaseEx : ControllerBase
{
    protected long PlayerId
    {
        get
        {
            var value = User.FindFirstValue("player_id") ?? User.FindFirstValue("sub");
            if (long.TryParse(value, out var playerId)) return playerId;
            throw new UnauthorizedAccessException("player_id claim is required.");
        }
    }

    protected string CorrelationId
        => HttpContext.Items.TryGetValue("CorrelationId", out var value) && value is string id
            ? id
            : HttpContext.TraceIdentifier;
}
