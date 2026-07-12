// Uses WeatherDbContext to fetch snapshot(s).
// Uses an injected IWeatherSummaryLlmClient (interface), so you can swap providers later.

// Background worker → WeatherAPI → WeatherSnapshots table.
// ForecastSummaryService reads latest snapshot → builds prompt.
// Next is GeminiWeatherSummaryClient

using Microsoft.EntityFrameworkCore;
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

    public ForecastSummaryService(WeatherDbContext db, IWeatherSummaryClient llmClient)
    {
        _db = db;
        _llmClient = llmClient;
    }

    public async Task<string?> GetSmartSummaryAsync(string city, CancellationToken cancellationToken = default)
    {
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
        return summary;
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