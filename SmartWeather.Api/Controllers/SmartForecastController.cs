using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
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

    public record SmartForecastDto(
        string City,
        string Summary,
        double TemperatureC,
        DateTime RetrievedAtUtc
    );

    // GET: api/SmartForecast/{city}
    [HttpGet("{city}")]
    [EnableRateLimiting("smartForecastLimiter")] // Apply limiter to this controller [web:272][web:275]
    public async Task<ActionResult<string>> Get(string city, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _summaryService.GetSmartSummaryAsync(city, cancellationToken);

            if (result is null)
            {
                return NotFound($"No weather snapshot found for city '{city}'.");
            }

            var dto = new SmartForecastDto(
                City: result.City,
                Summary: result.Summary,
                TemperatureC: result.TemperatureC,
                RetrievedAtUtc: result.RetrievedAtUtc
            );

            return Ok(result);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Gemini API"))
        {
            // Map Gemini failures to a 503 with a clearer payload
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                error = "Gemini is currently unavailable. Please try again later."
            });
        }
    }
}