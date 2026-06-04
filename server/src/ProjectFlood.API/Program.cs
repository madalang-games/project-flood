using Microsoft.EntityFrameworkCore;
using ProjectFlood.API;
using ProjectFlood.API.Middleware;
using ProjectFlood.Application.Rewards;
using ProjectFlood.Application.Stage;
using ProjectFlood.Application.Stamina;
using ProjectFlood.Infrastructure.Generated;
using Serilog;
using Serilog.Events;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);
var config    = builder.Configuration;
var appConfig = ProjectFloodConfiguration.Load(config);

if (Enum.TryParse<LogEventLevel>(appConfig.LogLevel, ignoreCase: true, out var minLevel))
{
    config["Serilog:MinimumLevel:Default"] = minLevel.ToString();
    config["Serilog:MinimumLevel:Override:Microsoft.EntityFrameworkCore.Database.Command"] = "Warning";
}

builder.Host.UseSerilog((context, services, loggerConfiguration) => loggerConfiguration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .Enrich.With<ShortSourceContextEnricher>());

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// 1. DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(appConfig.Database.ConnectionString, ServerVersion.AutoDetect(appConfig.Database.ConnectionString)));

// 2. Redis
builder.Services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(appConfig.Redis.ConnectionString));

// 3. Application services
builder.Services.AddSingleton<StaminaConfigProvider>();
builder.Services.AddScoped<StaminaService>();
builder.Services.AddScoped<StageAttemptService>();
builder.Services.AddScoped<RewardService>();
builder.Services.AddScoped<AdRewardService>();
builder.Services.AddSingleton<IAdRewardVerifier, DevelopmentAdRewardVerifier>();

// 4. Configuration
builder.Services.AddSingleton(appConfig);

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseSerilogRequestLogging(options =>
{
    options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
    options.EnrichDiagnosticContext = (diagnosticContext, context) =>
    {
        if (context.Items["CorrelationId"] is string correlationId)
            diagnosticContext.Set("CorrelationId", correlationId);
    };
});
app.UseMiddleware<ApiExceptionMiddleware>();

app.MapControllers();
app.Run();
