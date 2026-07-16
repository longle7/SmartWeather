# SmartWeather

SmartWeather is a modern .NET 10 backend project that showcases core skills for C#/.NET roles: RESTful APIs, EF Core persistence, background workers, resilient HTTP calls with Polly, in-memory caching, rate limiting, and OpenAPI documentation via Scalar UI. It integrates with WeatherAPI.com to fetch live weather data and uses Google Gemini to generate AI-powered “smart forecast” summaries.

## Tech Stack

- .NET 10 ASP.NET Core Web API
- EF Core + SQL Server LocalDB (`(localdb)\MSSQLLocalDB`)
- Background worker (`BackgroundService`) for scheduled WeatherAPI polling
- Resilient HTTP calls with Polly (retry + circuit breaker) via `IHttpClientFactory` [web:182][web:194]
- OpenAPI + Scalar.AspNetCore for interactive API documentation [web:61][web:63]
- WeatherAPI.com for current weather data [web:112][web:172]
- Google Gemini API (Flash / Flash-Lite models) for natural-language summaries [web:242][web:243][web:247]

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
    - Sends a `generateContent` request to Gemini (e.g., 1.5 Flash / 2.0 Flash-Lite).
    - Extracts the summary text from the first response candidate and returns it [web:243][web:247].

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
- Configuration values are accessed via `IConfiguration` (e.g., in `Program.cs` and `GeminiWeatherSummaryClient`), so keys can be injected securely in different environments [web:128][web:216][web:220].

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

## Architecture Overview

SmartWeather is structured as a layered backend application:

- **Web API layer**
  - Controllers (`WeatherForecastController`, `SavedLocationsController`, `WeatherSnapshotsController`, `SmartForecastController`) expose REST endpoints.
  - Attribute routing (`api/[controller]`) and OpenAPI/Scalar for discoverability.

- **Data access layer**
  - `WeatherDbContext` (EF Core) manages:
    - `SavedLocations` table.
    - `WeatherSnapshots` table.
  - Uses SQL Server LocalDB in development; tests use the EF Core InMemory provider for isolation and speed [web:311][web:312].

- **Background processing**
  - `WeatherPollingService` as a hosted `BackgroundService`:
    - Pulls current conditions from WeatherAPI using a resilient HttpClient (Polly retry + circuit breaker).
    - Writes snapshots to the database on a schedule.

- **AI summarization layer**
  - `ForecastSummaryService` orchestrates:
    - Reading snapshots.
    - Building LLM prompts.
    - Calling `IWeatherSummaryClient`.
    - Managing in-memory caching per city.
  - `GeminiWeatherSummaryClient` encapsulates:
    - Gemini REST API calls.
    - JSON parsing from `candidates[0].content.parts[0].text` into plain text summaries [web:243][web:247][web:255].

This architecture demonstrates separation of concerns between HTTP, persistence, background jobs, and AI integration, and uses modern .NET patterns (DI, HttpClientFactory, Polly, IMemoryCache, rate limiting) [web:329][web:335].

## Testing

SmartWeather includes unit tests to keep the AI orchestration and Gemini integration safe to evolve:

- **ForecastSummaryService tests**
  - Use EF Core InMemory database seeded with `WeatherSnapshot` data.
  - Mock `IWeatherSummaryClient` to avoid real Gemini calls.
  - Verify:
    - Returns `null` when no snapshot exists.
    - Calls LLM once on cache miss and stores the result.
    - Serves cached summaries without re-calling the LLM.

- **GeminiWeatherSummaryClient tests**
  - Use a custom `HttpMessageHandler` to simulate Gemini responses.
  - Construct a JSON payload matching Gemini’s `candidates/content/parts/text` schema.
  - Assert that `SummarizeAsync` correctly parses and returns the expected summary string [web:319][web:325][web:328].

These tests provide regression coverage across prompt-building, caching, and JSON parsing, allowing safe iteration on the AI layer over time [web:308][web:311].

## Containerization

- **Docker**
  - Add a `Dockerfile` for `SmartWeather.Api` targeting .NET 10.
  - Use multi-stage builds (restore, build, publish) to keep images small.
  - Expose the HTTP port and configure environment variables for WeatherAPI and Gemini keys.

- **Docker Compose / .NET Aspire**
  - Optionally add a `docker-compose.yml` or .NET Aspire AppHost to:
    - Run the API + a SQL Server/SQL Edge container together.
    - Make local setup a single command (e.g. `docker compose up`) [web:329][web:343][web:341].

Containerization will:

- Make the backend easy to run on any machine.
- Demonstrate familiarity with modern deployment practices.
- Prepare the project for future cloud-native evolution (Kubernetes, Azure Container Apps, etc.).

## Getting Started

This solution has three main pieces:

- `SmartWeather.Api`: ASP.NET Core Web API (WeatherAPI + Gemini + EF Core)
- `SmartWeather.Tests`: xUnit tests for the backend
- `SmartWeather.React`: React + Vite frontend

### Run the API locally (LocalDB)

For local development on Windows, the API uses SQL Server LocalDB and .NET user secrets.

1. Clone and enter the API project:

   ```bash
   git clone https://github.com/your-username/SmartWeather.git
   cd SmartWeather/SmartWeather.Api
   ```

2. Configure WeatherAPI & Gemini keys (user secrets):

   ```bash
   dotnet user-secrets init
   dotnet user-secrets set "WeatherApi:ApiKey" "YOUR_WEATHERAPI_KEY"
   dotnet user-secrets set "WeatherApi:BaseUrl" "https://api.weatherapi.com/v1"
   dotnet user-secrets set "WeatherApi:DefaultCity" "Boston"
   dotnet user-secrets set "Gemini:ApiKey" "YOUR_GEMINI_API_KEY"
   ```

3. Configure LocalDB and apply migrations:

   ```text
   ConnectionStrings:WeatherDatabase = Server=(localdb)\MSSQLLocalDB;Database=SmartWeatherDb;Trusted_Connection=True;
   ```

   ```bash
   dotnet ef database update
   ```

4. Run the API:

   ```bash
   dotnet run
   ```

   Typical dev URLs:

   - Scalar UI: `https://localhost:7042/scalar` (if using the https profile)
   - Smart forecast: `https://localhost:7042/api/SmartForecast/Boston` or `http://localhost:5205/api/SmartForecast/Boston`

### Run the API in Docker (InMemory EF)

The API can also run inside a Docker container using EF Core InMemory for `WeatherDbContext` (controlled by `UseInMemoryDb=true`), which avoids LocalDB and is ideal for demos.

1. Build the Docker image from the solution root:

   ```bash
   docker build -t smartweather-api -f SmartWeather.Api/Dockerfile .
   ```

2. Run the container:

   ```bash
   docker run --name smartweather-api-dev -p 8080:8080 \
     -e UseInMemoryDb=true \
     -e WeatherApi__ApiKey="YOUR_WEATHERAPI_KEY" \
     -e WeatherApi__BaseUrl="https://api.weatherapi.com/v1" \
     -e WeatherApi__DefaultCity="Boston" \
     -e Gemini__ApiKey="YOUR_GEMINI_API_KEY" \
     smartweather-api
   ```

   API URLs in Docker:

   - Scalar UI: `http://localhost:8080/scalar`
   - Smart forecast: `http://localhost:8080/api/SmartForecast/Boston`

3. Stop and remove the container when done:

   ```bash
   docker stop smartweather-api-dev
   docker rm smartweather-api-dev
   ```

### Run the React frontend (SmartWeather.React)

The React app lives in `SmartWeather.React` and calls the API to show AI-generated summaries and saved snapshots.

1. Start the API (either locally or via Docker):

   - Local: `dotnet run` (API at `http://localhost:5205`)
   - Docker: `docker run ... -p 8080:8080` (API at `http://localhost:8080`)

2. Configure the React API base URL in `src/api/smartForecastApi.ts` and `src/api/weatherSnapshotsApi.ts`:

   ```ts
   // Example: local API
   const API_BASE_URL = "http://localhost:5205";

   // Example: Docker API
   // const API_BASE_URL = "http://localhost:8080";
   ```

3. Run the React app:

   ```bash
   cd SmartWeather/SmartWeather.React
   npm install    # first time only
   npm run dev
   ```

   Open `http://localhost:5173` in the browser.

   - **Home** tab: calls `GET /api/SmartForecast/{city}` and displays a JSON payload:

     ```json
     {
       "city": "Boston",
       "summary": "AI-generated forecast summary...",
       "temperatureC": 21.5,
       "retrievedAtUtc": "2026-07-16T16:13:13Z"
     }
     ```

   - **Saved Weather** tab: calls `GET /api/WeatherSnapshots` and lists saved snapshots.

Make sure CORS is enabled in the API to allow requests from `http://localhost:5173` during development.