using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectFlood.Contracts.Account;
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

        try
        {
            var session = JsonSerializer.Deserialize<PlatformSession>(responseBody, JsonOpts);
            if (session is null || string.IsNullOrWhiteSpace(session.Pid))
                return StatusCode(500, "Invalid platform session.");

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
