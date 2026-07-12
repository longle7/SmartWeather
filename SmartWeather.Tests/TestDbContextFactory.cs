using Microsoft.EntityFrameworkCore;
using SmartWeather.Api.Data;

namespace SmartWeather.Tests;

// Helper class for creating a fresh in-memory database for tests

public static class TestDbContextFactory
{
    public static WeatherDbContext CreateInMemoryDbContext()
    {
        // Creates a new WeatherDbContext backed by an in-memory database
        // Each call uses a unique database name so tests don't share data
        var options = new DbContextOptionsBuilder<WeatherDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new WeatherDbContext(options);
    }
}