using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using SmartWeather.Api.Data;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// AddControllers() registers MVC controller support and scans for classes that inherit from ControllerBase (like WeatherForecastController).
builder.Services.AddControllers();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();


// This tells the DI container: “Whenever a controller asks for WeatherDbContext, build one using SQL Server and this connection string”.
var connectionString = builder.Configuration.GetConnectionString("WeatherDatabase")
    ?? "Server=(localdb)\\MSSQLLocalDB;Database=SmartWeatherDb;Trusted_Connection=True;";

builder.Services.AddDbContext<WeatherDbContext>(options =>
    options.UseSqlServer(connectionString));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // MapOpenApi() exposes the OpenAPI JSON.
    app.MapOpenApi();

    // UseSwaggerUI serves the UI at /swagger by default and needs to know where the JSON lives (/openapi/v1.json).
    app.MapScalarApiReference();   // UI at /scalar
}

app.UseHttpsRedirection();

app.UseAuthorization();

// app.MapControllers() adds route mappings based on attributes like [ApiController] and [Route("[controller]")] on those classes.
app.MapControllers();

app.Run();
