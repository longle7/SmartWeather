using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SmartWeather.Api.Data;
using SmartWeather.Api.Models;

namespace SmartWeather.Api.Services;

// This uses IHttpClientFactory (recommended pattern for HttpClient usage) and parses just the fields you care about from WeatherAPI’s JSON
public class WeatherApiOptions
{
    public string BaseUrl { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string DefaultCity { get; set; } = "Boston";
}

public class WeatherPollingService : BackgroundService
{
    private readonly IHttpClientFactory _httpClientFactory;

    // WeatherDbContext is meant to be short-lived: normally .NET creates one per web request and throws it away when the request finishes.
    // WeatherPollingService is the opposite: it's created once when the app starts and lives the entire time the app runs.
    // Creates issue where this long lived service will hold onto one instance of the short lived database context - never designed to do that.

    // The fix is to have the background service ask for a "temporary workspace" (a scope) each time it needs the database, use it,
    // then throw it away — so each poll cycle gets its own fresh, safe database context.

    // Instead of injecting WeatherDbContext directly, inject IServiceScopeFactory and create a new scope each time you need the database,
    // then resolve the context from that scope. This ensures a fresh, properly-scoped WeatherDbContext for every polling cycle and disposes it cleanly afterward.

    // this itself is always a singleton, so it's safe to inject
    private readonly IServiceScopeFactory _scopeFactory;

    private readonly WeatherApiOptions _options;
    private readonly ILogger<WeatherPollingService> _logger;

    public WeatherPollingService(
        IHttpClientFactory httpClientFactory,
        IServiceScopeFactory scopeFactory,
        IOptions<WeatherApiOptions> options,
        ILogger<WeatherPollingService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Simple loop: poll every 15 minutes
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PollAndStoreAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error polling WeatherAPI");
            }

            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }

    private async Task PollAndStoreAsync(CancellationToken cancellationToken)
    {
        // client and response - WeatherAPI calls with Polly
        // Up to 3 retries with exponential backoff on transient failures.
        // A circuit breaker that opens after 5 consecutive failures and pauses further calls for 30 seconds.
        // Policies are codded in program.cs
        var client = _httpClientFactory.CreateClient("WeatherApi");

        var url = $"{_options.BaseUrl}/current.json?key={_options.ApiKey}&q={_options.DefaultCity}";

        var response = await client.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);

        // You can define a DTO that matches WeatherAPI's JSON structure.
        var jsonDoc = await System.Text.Json.JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        var root = jsonDoc.RootElement;

        var current = root.GetProperty("current");
        var condition = current.GetProperty("condition");

        var tempC = current.GetProperty("temp_c").GetDouble();

        var snapshot = new WeatherSnapshot
        {
            City = _options.DefaultCity,
            RetrievedAtUtc = DateTime.UtcNow,
            TemperatureC = tempC,
            ConditionText = condition.GetProperty("text").GetString() ?? string.Empty
        };

        // Create a short-lived "scope" just for this poll cycle, grab a fresh
        // WeatherDbContext from it, and let it get disposed automatically when
        // we're done. This avoids sharing one DbContext across the whole app's life.

        // Each time the worker runs PollAndStoreAsync, it gets a fresh WeatherDbContext
        // instance from DI, uses it, and then that instance is disposed. That’s important
        // for connection pooling and avoiding leaks.
        using (var scope = _scopeFactory.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<WeatherDbContext>();
            db.WeatherSnapshots.Add(snapshot);
            await db.SaveChangesAsync(cancellationToken);
        }
            

        _logger.LogInformation("Stored weather snapshot for {City} at {Time}", snapshot.City, snapshot.RetrievedAtUtc);
    }
}