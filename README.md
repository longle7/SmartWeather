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

## AI-Powered Smart Forecast (Gemini)

SmartWeather includes an AI-assisted endpoint that turns raw weather data into a human-friendly forecast summary using Google Gemini.

### How it works

- **Data collection**
  - `WeatherPollingService` runs as a hosted background worker.
  - On a schedule, it calls WeatherAPI.com’s `current.json` endpoint for a configured city.
  - Each reading is stored as a `WeatherSnapshot` in SQL Server via `WeatherDbContext`.

- **AI orchestration**
  - `ForecastSummaryService`:
    - Reads the latest `WeatherSnapshot` for a given city.
    - Builds a concise prompt describing the temperature, condition, and timestamp.
    - Calls an injected `IWeatherSummaryClient` to turn that prompt into natural language.
  - `GeminiWeatherSummaryClient`:
    - Implements `IWeatherSummaryClient` using Google’s Gemini API.
    - Sends a `generateContent` request to Gemini (1.5 Flash / 2.0 Flash-Lite).
    - Extracts the summary text from the first response candidate and returns it.

- **API surface**
  - `SmartForecastController` exposes:
    - `GET /api/SmartForecast/{city}`
  - When you call this endpoint:
    1. The controller passes `{city}` to `ForecastSummaryService`.
    2. The service fetches the latest snapshot for that city and builds a prompt.
    3. Gemini generates a short, readable forecast paragraph.
    4. The controller returns that summary as plain text in the HTTP response.

Instead of just forwarding raw JSON from WeatherAPI, this endpoint demonstrates how to combine:
- Scheduled data ingestion (background worker),
- Persistence (EF Core),
- External APIs (WeatherAPI + Gemini),
- And LLM-based summarization into a cohesive feature.

### Secret management

- **WeatherAPI key**: stored in `.NET` user secrets under `WeatherApi:ApiKey`, never committed to Git.
- **Gemini API key**: stored in `.NET` user secrets under `Gemini:ApiKey`, also kept out of source control.
- Configuration values are accessed via `IConfiguration` in `Program.cs` and `GeminiWeatherSummaryClient`, so keys can be injected securely in different environments.

## SmartForecast Performance & Resilience

To keep the AI layer efficient and production-friendly, SmartWeather adds both caching and rate limiting around the SmartForecast endpoint:

- **Per-city in-memory caching**
  - `ForecastSummaryService` uses `IMemoryCache` to cache Gemini-generated summaries per city.
  - Cache key format: `SmartForecast:{city}`.
  - Cache duration: 5 minutes.
  - Behavior:
    - First `GET /api/SmartForecast/{city}`:
      - Reads the latest `WeatherSnapshot` for the city from SQL.
      - Builds a prompt and calls Gemini via `GeminiWeatherSummaryClient`.
      - Caches the returned summary.
    - Subsequent requests within 5 minutes:
      - Served directly from cache.
      - No database query and no Gemini API call.
  - This reduces latency and significantly lowers LLM/API usage while still keeping summaries fresh [web:278][web:285][web:296].

- **Rate limiting on SmartForecast**
  - ASP.NET Core rate limiting middleware is configured with a fixed window limiter named `smartForecastLimiter`.
  - `SmartForecastController` is annotated with `[EnableRateLimiting("smartForecastLimiter")]`.
  - Example policy:
    - Permit limit: 10 requests per minute.
    - Rejections return HTTP 429 (Too Many Requests).
  - This protects the AI-backed endpoint from abuse or accidental request storms [web:272][web:273][web:279].

Together, these patterns demonstrate:
- In-memory caching to avoid unnecessary external calls.
- Controlled API access with rate limiting.
- A realistic backend design for AI-assisted features.

## Testing (Planned)

To keep the codebase maintainable and interview-ready, the next steps include unit and integration tests:

- **Unit tests**
  - `ForecastSummaryService`:
    - Uses an in-memory `WeatherDbContext` (e.g., EF Core InMemory provider) to verify:
      - Returns `null` when no snapshot exists.
      - Builds prompts correctly and calls the LLM client once on cache miss.
      - Returns cached summaries without re-calling the LLM.
  - `GeminiWeatherSummaryClient`:
    - Tested with a mocked `HttpMessageHandler` / `HttpClient` to:
      - Verify request shape (endpoint, headers) and response parsing into plain text.
      - Avoid calling the real Gemini API during tests [web:287][web:289][web:291][web:297].

- **Integration tests**
  - End-to-end tests using `WebApplicationFactory` / TestServer:
    - Call `GET /api/SmartForecast/{city}` with seeded `WeatherSnapshots`.
    - Assert that the controller returns the expected summary when the LLM client is mocked.
    - Provide realistic coverage of routing, DI, caching, and controller behavior [web:298][web:292][web:294].

These tests will make the project a strong example of testable architecture (services + interfaces + controllers) in ASP.NET Core.

## Containerization (Planned)

Next iteration: run SmartWeather as a containerized backend service.

- **Docker**
  - Add a `Dockerfile` for `SmartWeather.Api` targeting .NET 10.
  - Use multi-stage builds (restore, build, publish) to keep images small.
  - Expose the HTTP port and configure environment variables for WeatherAPI and Gemini keys.

- **Docker Compose / .NET Aspire**
  - Optionally add a `docker-compose.yml` or .NET Aspire AppHost to:
    - Run the API + SQL Server LocalDB (or a full SQL Server/SQL Edge container) together.
    - Make local setup a single command (e.g. `docker compose up`).

Containerization will:
- Make the backend easy to run on any machine.
- Demonstrate familiarity with modern deployment practices.
- Let you speak about cloud-native patterns in interviews (even before adding Kubernetes).


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