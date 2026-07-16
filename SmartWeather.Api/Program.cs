using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using SmartWeather.Api.Data;
using SmartWeather.Api.Services;
using Polly;
using Polly.Extensions.Http;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;


var builder = WebApplication.CreateBuilder(args);

var rawFlag = builder.Configuration["UseInMemoryDb"];
Console.WriteLine($"UseInMemoryDb (raw) = {rawFlag}");

var useInMemory = rawFlag == "true";
Console.WriteLine($"useInMemory: {useInMemory}");

// Add services to the container.
// AddControllers() registers MVC controller support and scans for classes that inherit from ControllerBase (like WeatherForecastController).
builder.Services.AddControllers();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddMemoryCache();
builder.Services.AddScoped<ForecastSummaryService>();

// This tells the DI container: “Whenever a controller asks for WeatherDbContext, build one using SQL Server and this connection string”.\
// EF Core
var connectionString = builder.Configuration.GetConnectionString("WeatherDatabase")
    ?? "Server=(localdb)\\MSSQLLocalDB;Database=SmartWeatherDb;Trusted_Connection=True;";

if (useInMemory)
{
    // In Docker / non-Windows, use an in-memory database for demo
    builder.Services.AddDbContext<WeatherDbContext>(options =>
        options.UseInMemoryDatabase("SmartWeatherDbContainer"));
}
else
{
    // On your dev machine (Windows), use SQL Server LocalDB
    builder.Services.AddDbContext<WeatherDbContext>(options =>
        options.UseSqlServer(connectionString));
}

// WeatherAPI options
builder.Services.Configure<WeatherApiOptions>(
    builder.Configuration.GetSection("WeatherApi"));

// HttpClient for WeatherAPI
builder.Services.AddHttpClient("WeatherApi", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["WeatherApi:BaseUrl"]!);
})
.AddPolicyHandler(GetRetryPolicy())
.AddPolicyHandler(GetCircuitBreakerPolicy());

// Gemini client
//builder.Services.AddHttpClient<GeminiWeatherSummaryClient>(client =>
builder.Services.AddHttpClient<IWeatherSummaryClient, GeminiWeatherSummaryClient>(client =>

{
    client.BaseAddress = new Uri("https://generativelanguage.googleapis.com/");
});

// Registering
builder.Services.AddScoped<ForecastSummaryService>();


// Background worker
// AddHostedService<T>() registers this as a SINGLETON internally —
// one instance is created at startup and lives for the app's entire lifetime.
builder.Services.AddHostedService<WeatherPollingService>();

builder.Services.AddRateLimiter(options =>
{
    // Simple fixed window limiter named "smartForecastLimiter"
    options.AddFixedWindowLimiter("smartForecastLimiter", limiterOptions =>
    {
        limiterOptions.PermitLimit = 10; // Allow 10 requests
        limiterOptions.Window = TimeSpan.FromMinutes(1); // Per 1 minute
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = 0; // No queued requests
    });

    options.RejectionStatusCode = 429;
});

var app = builder.Build();

app.UseRateLimiter();

// Configure the HTTP request pipeline.
// MapOpenApi() exposes the OpenAPI JSON.
app.MapOpenApi();

// UseSwaggerUI serves the UI at /swagger by default and needs to know where the JSON lives (/openapi/v1.json).
app.MapScalarApiReference();   // UI at /scalar

app.UseHttpsRedirection();

app.UseAuthorization();

// app.MapControllers() adds route mappings based on attributes like [ApiController] and [Route("[controller]")] on those classes.
app.MapControllers();

app.Run();

static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
{
    // If that third attempt still fails, the retry policy gives up and surfaces the failure to the caller (your worker), where you log the error.
    return HttpPolicyExtensions
        .HandleTransientHttpError() // 5xx, 408, network failures
        .WaitAndRetryAsync(
            retryCount: 3,
            retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))  // 2s, 4s, 8s
        );
}

static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
{
    // Once there have been 5 consecutive transient failures (across multiple worker cycles, not just one),
    // the circuit breaker opens, meaning further calls immediately fail without even hitting WeatherAPI for 30 seconds.
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .CircuitBreakerAsync(
            handledEventsAllowedBeforeBreaking: 5,       // break after 5 consecutive failures
            durationOfBreak: TimeSpan.FromSeconds(30)    // stay open for 30s before trying again
        );
}
