using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartWeather.Api.Data;
using SmartWeather.Api.Models;

namespace SmartWeather.Api.Controllers;

[ApiController]
[Route("api/[controller]")]

// SavedLocationsController where cites and countries can be saved and retrieved
// from the database
public class SavedLocationsController : ControllerBase
{
    private readonly WeatherDbContext _db;

    public SavedLocationsController(WeatherDbContext db)
    {
        _db = db;
    }

    // GET: api/SavedLocations
    // Attribute
    [HttpGet]

    // Method - GetAll()
    public async Task<ActionResult<IEnumerable<SavedLocation>>> GetAll()
    {
        return await _db.SavedLocations.ToListAsync();
    }


    // 1. The client sends an HTTP POST to /api/SavedLocations with JSON { "name": "...", "country": "..." }.
    // 2. ASP.NET Core matches this to SavedLocationsController.Create(CreateSavedLocationDto dto) based on [HttpPost] and the route [Route("api/[controller]")]
    // 3. Model binding deserializes the JSON body into the CreateSavedLocationDto parameter.
    // POST: api/SavedLocations
    public record CreateSavedLocationDto(string Name, string Country);

    [HttpPost]
    public async Task<ActionResult<SavedLocation>> Create(CreateSavedLocationDto dto)
    {
        // Use a DTO instead of the entity for Create: Define a CreateSavedLocationDto with only Name and Country,
        // bind to that in the controller, then map to a new SavedLocation (with Id left unset). This prevents clients from ever supplying Id on creation.
        var location = new SavedLocation
        {
            Name = dto.Name,
            Country = dto.Country
        };

        // _db.SavedLocations.Add(location); tells EF Core’s WeatherDbContext to start tracking this entity with state Added.
        // The SavedLocations DbSet acts like an in-memory “view” of the table and EF’s change tracker remembers that there’s a new entity to insert.
        _db.SavedLocations.Add(location);

        // await _db.SaveChangesAsync(); scans all tracked entities, sees the SavedLocation marked as Added, and generates an INSERT SQL command 
        // against the SmartWeatherDb database and its SavedLocations table.
        await _db.SaveChangesAsync();

        // builds an HTTP 201 Created response, sets the Location header to the URL for GET /api/SavedLocations/{id}, and returns the saved entity as JSON.
        return CreatedAtAction(nameof(GetById), new { id = location.Id }, location);
    }

    // GET: api/SavedLocations/5
    [HttpGet("{id:int}")]
    public async Task<ActionResult<SavedLocation>> GetById(int id)
    {
        var location = await _db.SavedLocations.FindAsync(id);

        if (location is null)
        {
            return NotFound();
        }

        return location;
    }

    // DELETE: api/SavedLocations/5
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var location = await _db.SavedLocations.FindAsync(id);

        if (location is null)
        {
            return NotFound();
        }

        _db.SavedLocations.Remove(location);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}