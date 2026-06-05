using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using ProjectFlood.API;
using ProjectFlood.Contracts.Common;
using ProjectFlood.Infrastructure.Concurrency;

namespace ProjectFlood.API.Filters;

public sealed class UserSerializeFilter : IAsyncActionFilter
{
    private static readonly TimeSpan LockTimeout = TimeSpan.FromSeconds(10);

    private readonly UserSerializer _serializer;
    private readonly ILogger<UserSerializeFilter> _logger;

    public UserSerializeFilter(UserSerializer serializer, ILogger<UserSerializeFilter> logger)
    {
        _serializer = serializer;
        _logger = logger;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (HttpMethods.IsGet(context.HttpContext.Request.Method))
        {
            await next();
            return;
        }

        var pid = context.HttpContext.User.GetPlatformPid();
        if (pid is null)
        {
            await next();
            return;
        }

        var sw = Stopwatch.StartNew();
        IAsyncDisposable lease;
        try
        {
            lease = await _serializer.AcquireAsync(pid, LockTimeout, context.HttpContext.RequestAborted);
        }
        catch (TimeoutException)
        {
            _logger.LogWarning("Timed out acquiring user write lock for pid {Pid} after {ElapsedMs} ms", pid, sw.ElapsedMilliseconds);
            context.Result = new ObjectResult(new ErrorResponse
            {
                Code = "USER_LOCK_TIMEOUT",
                Message = "The previous request for this user is still being processed.",
            })
            {
                StatusCode = StatusCodes.Status503ServiceUnavailable,
            };
            return;
        }

        await using (lease)
        {
            await next();
        }
    }
}
