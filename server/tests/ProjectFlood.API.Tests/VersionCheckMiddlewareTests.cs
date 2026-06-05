using Microsoft.AspNetCore.Http;
using ProjectFlood.API;
using ProjectFlood.API.Middleware;
using Xunit;

namespace ProjectFlood.API.Tests;

public sealed class VersionCheckMiddlewareTests
{
    [Fact]
    public async Task InvokeAsyncRejectsMissingClientVersion()
    {
        var middleware = new VersionCheckMiddleware(_ => Task.CompletedTask, CreateConfig());
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status426UpgradeRequired, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsyncAllowsMatchingVersions()
    {
        var called = false;
        var middleware = new VersionCheckMiddleware(_ =>
        {
            called = true;
            return Task.CompletedTask;
        }, CreateConfig());

        var context = new DefaultHttpContext();
        context.Request.Headers["X-Client-Version"] = "1.0.0";
        context.Request.Headers["X-Protocol-Version"] = "1";

        await middleware.InvokeAsync(context);

        Assert.True(called);
    }

    private static ProjectFloodConfiguration CreateConfig()
        => new()
        {
            GameEnvironment = "test",
            LogLevel = "Information",
            Database = new ProjectFloodConfiguration.DatabaseOptions
            {
                Host = "localhost",
                Port = 3306,
                Name = "projectflood_test",
                User = "test",
                Password = "test",
            },
            Redis = new ProjectFloodConfiguration.RedisOptions
            {
                Host = "localhost",
                Port = 6379,
            },
            Auth = new ProjectFloodConfiguration.AuthOptions
            {
                JwtAuthority = "http://platform-auth",
                JwtIssuer = "http://platform-auth",
                JwtAudience = "platform-games",
            },
            App = new ProjectFloodConfiguration.AppOptions
            {
                ClientId = "project-flood",
                AllowedClientVersion = "1.0.0",
                RequiredClientVersion = "1.0.0",
                AllowedProtocolVersion = "1",
            },
            RateLimit = new ProjectFloodConfiguration.RateLimitOptions
            {
                StageStartPerHour = 720,
            },
        };
}
