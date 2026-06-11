using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectFlood.Contracts.Account;
using ProjectFlood.Contracts.Common;
using ProjectFlood.Domain.Utilities;
using ProjectFlood.Infrastructure.Generated;
using StackExchange.Redis;

namespace ProjectFlood.API.Controllers;

[ApiController]
[Route("api/auth")]
[AllowAnonymous]
public sealed class AuthController : ControllerBase
{
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };
    private static readonly TimeSpan CacheTtl = TimeSpan.FromDays(30);

    private readonly ProjectFloodConfiguration _config;
    private readonly IHttpClientFactory _httpFactory;
    private readonly AppDbContext _db;
    private readonly IConnectionMultiplexer _redis;
    private readonly NicknameGenerator _nicknameGenerator;

    public AuthController(
        ProjectFloodConfiguration config,
        IHttpClientFactory httpFactory,
        AppDbContext db,
        IConnectionMultiplexer redis,
        NicknameGenerator nicknameGenerator)
    {
        _config = config;
        _httpFactory = httpFactory;
        _db = db;
        _redis = redis;
        _nicknameGenerator = nicknameGenerator;
    }

    [HttpPost("guest")]
    public Task<IActionResult> Guest(CancellationToken ct)
        => ProxyAndTransform("auth/guest", ct);

    [HttpPost("google")]
    public Task<IActionResult> Google(CancellationToken ct)
        => ProxyAndTransform("auth/google", ct);

    [HttpPost("refresh")]
    public Task<IActionResult> Refresh(CancellationToken ct)
        => ProxyAndTransform("auth/refresh", ct);

    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        var authority = _config.Auth.JwtAuthority.TrimEnd('/');
        var http = _httpFactory.CreateClient();
        
        using var reader = new StreamReader(Request.Body, leaveOpen: true);
        var requestBody = await reader.ReadToEndAsync(ct);
        using var content = new StringContent(requestBody, Encoding.UTF8, "application/json");
        
        var response = await http.PostAsync($"{authority}/auth/logout", content, ct);
        if (response.IsSuccessStatusCode)
            return NoContent();
            
        var errorBody = await response.Content.ReadAsStringAsync(ct);
        return StatusCode((int)response.StatusCode, errorBody);
    }

    [HttpPost("link-oauth")]
    public async Task<IActionResult> LinkOauth([FromBody] LinkAccountRequest request, CancellationToken ct)
    {
        var authority = _config.Auth.JwtAuthority.TrimEnd('/');
        var http = _httpFactory.CreateClient();

        var payload = new
        {
            provider = request.Provider,
            idToken = request.IdToken,
            clientId = "game_client",
            guestRefreshToken = request.GuestRefreshToken
        };

        using var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        var response = await http.PostAsync($"{authority}/auth/{request.Provider}", content, ct);
        var responseBody = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
            return StatusCode((int)response.StatusCode, responseBody);

        return await ProcessPlatformSessionAsync(responseBody, ct);
    }

    [HttpPost("resolve-conflict")]
    public async Task<IActionResult> ResolveConflict([FromBody] ResolveConflictRequest request, CancellationToken ct)
    {
        var db = _redis.GetDatabase();
        var cached = await db.StringGetAsync($"pending_conflict:{request.ConflictToken}");
        if (!cached.HasValue)
            return BadRequest(new ErrorResponse { Code = "CONFLICT_EXPIRED", Message = "Conflict resolution session expired or not found." });

        var conflict = JsonSerializer.Deserialize<PendingConflictDetails>(cached!, JsonOpts);
        if (conflict is null)
            return BadRequest(new ErrorResponse { Code = "INVALID_CONFLICT", Message = "Invalid conflict details." });

        await db.KeyDeleteAsync($"pending_conflict:{request.ConflictToken}");

        if (request.Selection == "local")
        {
            // Keep Guest Save: delete cloud rows, update guest rows to cloud user ID and PID
            await using var transaction = await _db.Database.BeginTransactionAsync(ct);
            try
            {
                // Delete cloud player
                await _db.Database.ExecuteSqlRawAsync("DELETE FROM players WHERE user_id = {0}", conflict.CloudUserId);

                // Update guest player to cloud user ID and PID
                await _db.Database.ExecuteSqlRawAsync(
                    "UPDATE players SET user_id = {0}, platform_pid = {1} WHERE user_id = {2}",
                    conflict.CloudUserId, conflict.CloudPid, conflict.GuestUserId);

                var tables = new[] {
                    "user_stage_progress", "user_currency", "user_inventory", 
                    "user_stamina_state", "user_ranking_totals", "user_tutorial_progress", 
                    "sessions", "event_logs", "ad_reward_transactions"
                };

                foreach (var table in tables)
                {
                    await _db.Database.ExecuteSqlRawAsync($"DELETE FROM {table} WHERE user_id = {{0}}", conflict.CloudUserId);
                    await _db.Database.ExecuteSqlRawAsync($"UPDATE {table} SET user_id = {{0}} WHERE user_id = {{1}}", conflict.CloudUserId, conflict.GuestUserId);
                }

                await transaction.CommitAsync(ct);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(ct);
                return StatusCode(500, $"Failed to migrate local progress: {ex.Message}");
            }
        }
        else if (request.Selection == "cloud")
        {
            // Keep Cloud Save: delete guest rows
            await using var transaction = await _db.Database.BeginTransactionAsync(ct);
            try
            {
                await _db.Database.ExecuteSqlRawAsync("DELETE FROM players WHERE user_id = {0}", conflict.GuestUserId);

                var tables = new[] {
                    "user_stage_progress", "user_currency", "user_inventory", 
                    "user_stamina_state", "user_ranking_totals", "user_tutorial_progress", 
                    "sessions", "event_logs", "ad_reward_transactions"
                };

                foreach (var table in tables)
                {
                    await _db.Database.ExecuteSqlRawAsync($"DELETE FROM {table} WHERE user_id = {{0}}", conflict.GuestUserId);
                }

                await transaction.CommitAsync(ct);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(ct);
                return StatusCode(500, $"Failed to delete guest progress: {ex.Message}");
            }
        }
        else
        {
            return BadRequest(new ErrorResponse { Code = "INVALID_SELECTION", Message = "Selection must be 'local' or 'cloud'." });
        }

        // Return AuthResponse using saved PlatformSession
        var session = JsonSerializer.Deserialize<PlatformSession>(conflict.PlatformSession, JsonOpts);
        if (session is null)
            return StatusCode(500, "Failed to restore platform session.");

        var player = await _db.Players.FindAsync(conflict.CloudUserId, ct);
        var authRes = new AuthResponse
        {
            AccessToken = session.Tokens.AccessToken,
            RefreshToken = session.Tokens.RefreshToken,
            ExpiresAt = session.Tokens.AccessTokenExpiresAt,
            Profile = new AccountMeResponse
            {
                UserId = session.UserId.ToString(),
                Pid = session.Pid,
                DisplayName = player?.DisplayName ?? "Player",
                IsGuest = false,
                LinkedProviders = new List<string> { session.AccountType },
                AvatarId = player?.AvatarId ?? 1,
                CreatedAt = (player?.AccountCreatedAt ?? DateTimeOffset.UtcNow).ToString("O")
            }
        };

        return Ok(new ResolveConflictResponse { Success = true, Auth = authRes });
    }

    private async Task<IActionResult> ProxyAndTransform(string platformPath, CancellationToken ct)
    {
        var authority = _config.Auth.JwtAuthority.TrimEnd('/');
        var http = _httpFactory.CreateClient();
        
        using var reader = new StreamReader(Request.Body, leaveOpen: true);
        var requestBody = await reader.ReadToEndAsync(ct);
        using var content = new StringContent(requestBody, Encoding.UTF8, "application/json");
        
        var response = await http.PostAsync($"{authority}/{platformPath}", content, ct);
        var responseBody = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
            return StatusCode((int)response.StatusCode, responseBody);

        return await ProcessPlatformSessionAsync(responseBody, ct);
    }

    private async Task<IActionResult> ProcessPlatformSessionAsync(string responseBody, CancellationToken ct)
    {
        try
        {
            var session = JsonSerializer.Deserialize<PlatformSession>(responseBody, JsonOpts);
            if (session is null || string.IsNullOrWhiteSpace(session.Pid))
                return StatusCode(500, "Invalid platform session.");

            // Check if client is currently authenticated as guest (linking flow)
            long? guestUserId = null;
            var uidClaim = HttpContext.User.FindFirst(UserClaims.UserId);
            if (uidClaim != null && long.TryParse(uidClaim.Value, out var uid))
            {
                guestUserId = uid;
            }

            // Conflict condition: authenticated as guest, but the linked social account userId is different
            if (guestUserId.HasValue && session.UserId != guestUserId.Value)
            {
                var localSave = await GetSaveSnapshotAsync(guestUserId.Value, ct);
                var cloudSave = await GetSaveSnapshotAsync(session.UserId, ct);
                var conflictToken = Guid.NewGuid().ToString("N");

                var conflictDetails = new PendingConflictDetails
                {
                    GuestUserId = guestUserId.Value,
                    CloudUserId = session.UserId,
                    CloudPid = session.Pid,
                    PlatformSession = responseBody
                };

                await _redis.GetDatabase().StringSetAsync(
                    $"pending_conflict:{conflictToken}",
                    JsonSerializer.Serialize(conflictDetails, JsonOpts),
                    TimeSpan.FromMinutes(10));

                return Ok(new LinkAccountResponse
                {
                    Success = false,
                    Conflict = true,
                    LocalSave = localSave,
                    CloudSave = cloudSave,
                    ConflictToken = conflictToken
                });
            }

            var now = DateTimeOffset.UtcNow;
            var existing = await _db.Players.Query()
                .Where(p => p.PlatformPid == session.Pid)
                .FirstOrDefaultAsync(ct);

            string displayName;
            if (existing is null)
            {
                displayName = _nicknameGenerator.Generate();
                await _db.Players.InsertIgnoreAsync(new PlayersRow
                {
                    UserId = session.UserId,
                    PlatformPid = session.Pid,
                    DisplayName = displayName,
                    AvatarId = 1,
                    AccountCreatedAt = now,
                    LastLoginAt = now,
                }, ct);
            }
            else
            {
                displayName = existing.DisplayName;
                existing.LastLoginAt = now;
            }
            await _db.SaveAsync(ct);

            var cacheKey = $"user_id:{session.Pid}";
            await _redis.GetDatabase().StringSetAsync(cacheKey, session.UserId.ToString(), CacheTtl);

            var result = new AuthResponse
            {
                AccessToken = session.Tokens.AccessToken,
                RefreshToken = session.Tokens.RefreshToken,
                ExpiresAt = session.Tokens.AccessTokenExpiresAt,
                Profile = new AccountMeResponse
                {
                    UserId = session.UserId.ToString(),
                    Pid = session.Pid,
                    DisplayName = displayName,
                    IsGuest = session.AccountType == "guest",
                    LinkedProviders = session.AccountType is not null and not "guest"
                        ? new List<string> { session.AccountType }
                        : new List<string>(),
                    AvatarId = existing?.AvatarId ?? 1,
                    CreatedAt = (existing?.AccountCreatedAt ?? now).ToString("O")
                }
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal error resolving player: {ex.Message}");
        }
    }

    private async Task<SaveSnapshotDto> GetSaveSnapshotAsync(long userId, CancellationToken ct)
    {
        var maxStage = await _db.UserRankingTotals.Query()
            .Where(x => x.UserId == userId)
            .Select(x => x.MaxClearedStageId)
            .FirstOrDefaultAsync(ct);

        var gold = await _db.UserCurrency.Query()
            .Where(x => x.UserId == userId)
            .Select(x => x.SoftAmount)
            .FirstOrDefaultAsync(ct);

        var stars = await _db.UserRankingTotals.Query()
            .Where(x => x.UserId == userId)
            .Select(x => x.TotalEarnedStars)
            .FirstOrDefaultAsync(ct);

        var items = await _db.UserInventory.Query()
            .Where(x => x.UserId == userId)
            .SumAsync(x => x.Count, ct);

        return new SaveSnapshotDto
        {
            MaxStageId = maxStage,
            Gold = gold,
            TotalStars = stars,
            TotalItems = items
        };
    }

    private sealed class PendingConflictDetails
    {
        public long GuestUserId { get; set; }
        public long CloudUserId { get; set; }
        public string CloudPid { get; set; } = string.Empty;
        public string PlatformSession { get; set; } = string.Empty;
    }

    private sealed class PlatformSession
    {
        public long UserId { get; set; }
        public string Pid { get; set; } = string.Empty;
        public long SessionId { get; set; }
        public string AccountType { get; set; } = string.Empty;
        public PlatformTokens Tokens { get; set; } = new();
    }

    private sealed class PlatformTokens
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public string AccessTokenExpiresAt { get; set; } = string.Empty;
    }
}
