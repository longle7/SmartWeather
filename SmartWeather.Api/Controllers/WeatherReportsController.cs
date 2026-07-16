using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartWeather.Api.Data;
using SmartWeather.Api.Models;

namespace SmartWeather.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WeatherReportsController : ControllerBase
{
    private readonly WeatherDbContext _db;

    public WeatherReportsController(WeatherDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Returns average temperature and snapshot count per city.
    /// GET /api/WeatherReports/average-temperatures
    /// </summary>
    [HttpGet("average-temperatures")]
    public async Task<ActionResult<IEnumerable<CityTemperatureReportDto>>> GetAverageTemperatures(CancellationToken cancellationToken)
    {
        // Step 1: server-side grouping & aggregation
        var grouped = await _db.WeatherSnapshots
            .GroupBy(ws => ws.City)
            .Select(g => new
            {
                City = g.Key,
                AverageTemperatureC = g.Average(ws => ws.TemperatureC),
                SnapshotCount = g.Count()
            })
            .ToListAsync(cancellationToken);  // materialize here

        // Step 2: client-side projection into DTOs
        var results = grouped
            .Select(x => new CityTemperatureReportDto(
                City: x.City,
                AverageTemperatureC: x.AverageTemperatureC,
                SnapshotCount: x.SnapshotCount
            ))
            .OrderByDescending(r => r.SnapshotCount)
            .ToList();

        if (results.Count == 0)
        {
            return NotFound("No weather snapshots available to build reports.");
        }

        return Ok(results);
    }

    /// <summary>
    /// Returns snapshot counts per city (simple frequency report).
    /// GET /api/WeatherReports/snapshot-counts
    /// </summary>
    [HttpGet("snapshot-counts")]
    public async Task<ActionResult<IEnumerable<CitySnapshotCountDto>>> GetSnapshotCounts(CancellationToken cancellationToken)
    {
        var grouped = await _db.WeatherSnapshots
        .GroupBy(ws => ws.City)
        .Select(g => new
        {
            City = g.Key,
            SnapshotCount = g.Count()
        })
        .ToListAsync(cancellationToken);

        var results = grouped
            .Select(x => new CitySnapshotCountDto(
                City: x.City,
                SnapshotCount: x.SnapshotCount
            ))
            .OrderByDescending(r => r.SnapshotCount)
            .ToList();

        if (results.Count == 0)
        {
            return NotFound("No weather snapshots available to build reports.");
        }

        return Ok(results);
    }
}