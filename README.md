# SmartWeather

SmartWeather is a modern .NET 10 backend project that showcases core skills for C#/.NET roles: RESTful APIs, EF Core persistence, background workers, resilient HTTP calls with Polly, and OpenAPI documentation via Scalar UI. It integrates with WeatherAPI.com to fetch live weather data and is designed to add an AI-powered “smart forecast” summary layer.

## Tech Stack

- .NET 10 ASP.NET Core Web API
- EF Core + SQL Server LocalDB (`(localdb)\MSSQLLocalDB`)
- Background worker (`BackgroundService`) for scheduled WeatherAPI polling
- Resilient HTTP calls with Polly (retry + circuit breaker) via `IHttpClientFactory` [web:182][web:194]
- OpenAPI + Scalar.AspNetCore for interactive API documentation [web:61][web:63]
- WeatherAPI.com for current weather data [web:112][web:172]

## Current Features

- **WeatherForecast API**
  - Sample `WeatherForecast` endpoint demonstrating basic REST + JSON.

- **Saved Locations**
  - `SavedLocationsController` exposes CRUD endpoints:
    - `GET /api/SavedLocations`
    - `GET /api/SavedLocations/{id}`
    - `POST /api/SavedLocations`
    - `DELETE /api/SavedLocations/{id}`
  - Backed by EF Core `WeatherDbContext` and SQL Server LocalDB.

- **Background Weather Polling**
  - `WeatherPollingService` runs as a hosted background service.
  - Periodically calls WeatherAPI’s `current.json` endpoint for a configured city.
  - Persists snapshots in a `WeatherSnapshots` table (`WeatherSnapshot` entity).

- **Resilient WeatherAPI Integration**
  - Named HttpClient `"WeatherApi"` is configured with:
    - Polly retry policy (3 retries with exponential backoff).
    - Polly circuit breaker (opens after repeated transient failures) [web:182][web:185][web:194].
  - This protects the app from transient network/API issues while calling WeatherAPI.

- **Weather Snapshot API**
  - `WeatherSnapshotsController` exposes:
    - `GET /api/WeatherSnapshots` — list of snapshots (most recent first).
    - `GET /api/WeatherSnapshots/latest` — latest snapshot for the configured city.

- **API Documentation**
  - OpenAPI document: `GET /openapi/v1.json`
  - Scalar interactive UI: `GET /scalar` (lists all API endpoints and lets you call them).

## Planned AI Feature: Smart Forecast Summary

Next milestone: add an LLM-powered “smart forecast” summary endpoint, built on top of the stored weather snapshots:

- **Planned Design**
  - `ForecastSummaryService`:
    - Reads latest `WeatherSnapshot` data for a city.
    - Constructs a concise prompt with temp, conditions, and time.
    - Delegates to an `IWeatherSummaryLlmClient` interface for actual LLM calls.
  - `SmartForecastController`:
    - `GET /api/SmartForecast/{city}` returns human-friendly text like:
      > “Today in Boston: 23°C, partly cloudy, light breeze. Good day for a walk, but bring a light jacket in the evening.”
- **LLM Provider**
  - Intended to be provider-agnostic (OpenAI, Azure OpenAI, etc.).
  - API keys and model settings stored via .NET user secrets and environment variables, never in source control [web:128][web:216][web:220].

This design demonstrates:
- Clean separation between data collection (WeatherAPI + worker), storage (EF Core), and AI summarization logic.
- Ability to wrap external APIs as tools for LLMs and generate natural language from structured data [web:227][web:231][web:239].

## Getting Started

1. **Clone the repository**

   ```bash
   git clone https://github.com/your-username/SmartWeather.git
   cd SmartWeather/SmartWeather.Api
   ```

2. **Configure WeatherAPI keys (development)**

   Use .NET user secrets to store your WeatherAPI credentials:

   ```bash
   dotnet user-secrets init
   dotnet user-secrets set "WeatherApi:ApiKey" "YOUR_WEATHERAPI_KEY"
   dotnet user-secrets set "WeatherApi:BaseUrl" "https://api.weatherapi.com/v1"
   dotnet user-secrets set "WeatherApi:DefaultCity" "Boston"
   ```

3. **Run database setup**

   Either run EF Core migrations or use `EnsureCreated` (for local dev):

   ```bash
   dotnet ef migrations add InitialCreate
   dotnet ef database update
   ```

   Or ensure the database is created at startup via `WeatherDbContext.Database.EnsureCreated()`.

4. **Run the API**

   ```bash
   dotnet run
   ```

5. **Explore the API**

   - Weather forecast: `GET https://localhost:7042/WeatherForecast`
   - Saved locations: `GET https://localhost:7042/api/SavedLocations`
   - Weather snapshots:
     - `GET https://localhost:7042/api/WeatherSnapshots`
     - `GET https://localhost:7042/api/WeatherSnapshots/latest`
   - API docs UI: `GET https://localhost:7042/scalar`

## Roadmap

- Add `SmartForecastController` and `ForecastSummaryService` to generate AI-based summaries from `WeatherSnapshots`.
- Introduce a React/TypeScript frontend that:
  - Displays current conditions and history.
  - Shows the AI-generated smart forecast summary.
- Containerize the services with Docker and optionally orchestrate via .NET Aspire.
- Add unit/integration tests for:
  - EF Core data access.
  - Background worker behavior.
  - LLM summary generation (using mocked LLM client).

---

This README now reflects both what you’ve already built and the upcoming LLM feature in a way that a hiring manager or interviewer can quickly understand and appreciate [web:227][web:238].

When you paste this into `README.md`, can you walk through how you’d verbally explain the “Planned AI Feature: Smart Forecast Summary” section to an interviewer in 60–90 seconds?