using Microsoft.AspNetCore.Mvc;
using SmartWeather.Api.Services;

namespace SmartWeather.Api.Controllers;

[ApiController]
[Route("api/[controller]")]

// Accepts city, calls ForecastSummaryService, returns the summary string or object.
// Cals ForecastSummaryService
// Returns the summary as a string

// This controller gives you an API endpoint your frontend (or Scalar) can call to get the AI-generated summary.

// SmartForecastController exposes GET /api/SmartForecast/{city}.

// SmartForecastController:
//  - Defines API endpoints for AI-generated "smart forecast" summaries.
//  - For GET /api/SmartForecast/{city}:
//    - Calls ForecastSummaryService.GetSmartSummaryAsync(city).
//    - That service reads WeatherSnapshots, builds a prompt, calls GeminiWeatherSummaryClient,
//      and returns a short human-friendly forecast paragraph.
//    - If no snapshot exists for the city, returns 404; otherwise returns 200 OK with the summary.
public class SmartForecastController : ControllerBase
{
    private readonly ForecastSummaryService _summaryService;

    public SmartForecastController(ForecastSummaryService summaryService)
    {
        _summaryService = summaryService;
    }

    // GET: api/SmartForecast/{city}
    [HttpGet("{city}")]
    public async Task<ActionResult<string>> Get(string city, CancellationToken cancellationToken)
    {
        var summary = await _summaryService.GetSmartSummaryAsync(city, cancellationToken);

        if (summary is null)
        {
            return NotFound($"No weather snapshot found for city '{city}'.");
        }

        return Ok(summary);
    }
}