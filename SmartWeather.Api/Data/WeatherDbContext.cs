using Microsoft.EntityFrameworkCore;
using SmartWeather.Api.Models;

namespace SmartWeather.Api.Data;

// So WeatherDbContext is “your app’s view of the WeatherDb database”
public class WeatherDbContext : DbContext
{
    // This constructor takes configuration options (e.g., which provider, connection string).
    // In Program.cs, you pass these options when you call AddDbContext with UseSqlServer(connectionString).
    // EF Core uses those options to connect to SQL Server.
    public WeatherDbContext(DbContextOptions<WeatherDbContext> options)
        : base(options)
    {
    }

    // DbSet<T> represents a table (or collection) of T entities in the database.
    // SavedLocations is “the SavedLocations table in WeatherDb”, giving you LINQ access: SavedLocations.Add(...), SavedLocations.Where(...), etc.
    // The get/set is just the property; EF Core wires it to the underlying table at runtime based on conventions and migrations.

    // "SavedLocations is the DbSet that represents the SavedLocations table, so I use it to query and save SavedLocation entities."
    public DbSet<SavedLocation> SavedLocations { get; set; } = null!;
}