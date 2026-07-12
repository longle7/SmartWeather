using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartWeather.Api.Data;
using SmartWeather.Api.Models;

namespace SmartWeather.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WeatherSnapshotsController : ControllerBase
{
    private readonly WeatherDbContext _db;

    public WeatherSnapshotsController(WeatherDbContext db)
    {
        _db = db;
    }

    // GET: api/WeatherSnapshots/latest
    [HttpGet("latest")]
    public async Task<ActionResult<WeatherSnapshot>> GetLatest()
    {
        var snapshot = await _db.WeatherSnapshots
            .OrderByDescending(w => w.RetrievedAtUtc)
            .FirstOrDefaultAsync();

        if (snapshot is null)
        {
            return NotFound();
        }

        return snapshot;
    }

    // GET: api/WeatherSnapshots
    [HttpGet]
    public async Task<ActionResult<IEnumerable<WeatherSnapshot>>> GetAll()
    {
        return await _db.WeatherSnapshots
            .OrderByDescending(w => w.RetrievedAtUtc)
            .ToListAsync();
    }
}