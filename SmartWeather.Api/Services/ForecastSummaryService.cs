// Uses WeatherDbContext to fetch snapshot(s).
// Uses an injected IWeatherSummaryLlmClient (interface), so you can swap providers later.

// Background worker → WeatherAPI → WeatherSnapshots table.
// ForecastSummaryService reads latest snapshot → builds prompt.
// Next is GeminiWeatherSummaryClient

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SmartWeather.Api.Data;
using SmartWeather.Api.Models;

namespace SmartWeather.Api.Services;

// ForecastSummaryService:
//  - Orchestrates AI "smart forecast" summaries.
//  - Reads the latest WeatherSnapshot from EF Core for a given city.
//  - Builds a structured prompt with temp/condition/time.
//  - Delegates to IWeatherSummaryClient (e.g., GeminiWeatherSummaryClient) to turn that prompt
//    into a human-friendly paragraph.
//  - Returns just the final summary string to the controller.
public class ForecastSummaryService
{
    private readonly WeatherDbContext _db;
    private readonly IWeatherSummaryClient _llmClient;
    private readonly IMemoryCache _cache;

    // ForecastSummaryService:
    //  - Reads latest WeatherSnapshot for a city.
    //  - Uses IMemoryCache to cache Gemini summaries per city for a short period.
    //  - Avoids hitting Gemini on every request while still keeping summaries fresh.
    public ForecastSummaryService(WeatherDbContext db, IWeatherSummaryClient llmClient, IMemoryCache cache)
    {
        _db = db;
        _llmClient = llmClient;
        _cache = cache;
    }

    public class SmartForecastResult
    {
        public string City { get; init; } = default!;
        public string Summary { get; init; } = default!;
        public double TemperatureC { get; init; }
        public DateTime RetrievedAtUtc { get; init; }
    }

    public async Task<SmartForecastResult?> GetSmartSummaryAsync(string city, CancellationToken cancellationToken = default)
    {
        // Try to get a cached summary for this city.
        var cacheKey = $"SmartForecast:{city}";

        if (_cache.TryGetValue(cacheKey, out SmartForecastResult cachedSummary))
        {
            return cachedSummary;
        }

        var normaizedCity = city.Trim().ToLowerInvariant();

        var snapshot = await _db.WeatherSnapshots
            .Where(w => w.City.ToLower() == normaizedCity)
            .OrderByDescending(w => w.RetrievedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (snapshot is null)
        {
            return null;
        }

        var prompt = BuildPrompt(snapshot);

        var summary = await _llmClient.SummarizeAsync(prompt, cancellationToken);

        var result = new SmartForecastResult
        {
            City = snapshot.City,
            Summary = summary,
            TemperatureC = snapshot.TemperatureC,
            RetrievedAtUtc = snapshot.RetrievedAtUtc
        };

        // Store the summary in cache with a short expiration window.
        var cacheOptions = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(TimeSpan.FromMinutes(5)); // 5 minutes is a good starting point [web:278][web:285]

        _cache.Set(cacheKey, summary, cacheOptions);

        return result;
    }

    private static string BuildPrompt(WeatherSnapshot snapshot)
    {
        // Simple, clear prompt; you can iterate on tone later [web:228][web:231]
        return $@"
        You are a helpful weather assistant.

        Using the structured data below, write a concise, friendly forecast summary for a user in {snapshot.City}.
        Focus on the current conditions and what they should wear or plan for the next few hours.
        Avoid repeating the raw numbers verbatim; instead, interpret them.

        Data:
        - City: {snapshot.City}
        - Temperature (C): {snapshot.TemperatureC}
        - Condition: {snapshot.ConditionText}
        - RetrievedAt (UTC): {snapshot.RetrievedAtUtc:O}

        Return a single short paragraph, no bullet points.";
    }
}