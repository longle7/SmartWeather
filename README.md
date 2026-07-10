# SmartWeather

SmartWeather is a modern .NET Web API that demonstrates core backend skills for C#/.NET roles, including RESTful endpoints, EF Core persistence, and OpenAPI documentation with Scalar UI.

## Tech Stack

- .NET 10 ASP.NET Core Web API
- EF Core with SQL Server (LocalDB)
- OpenAPI + Scalar.AspNetCore UI
- Async/await controllers and repository-style data access

## Features

- `WeatherForecast` endpoint for sample data
- `SavedLocations` CRUD API backed by EF Core
- OpenAPI document at `/openapi/v1.json`
- Interactive API UI at `/scalar`

## Getting Started

1. Clone the repository:

   ```bash
   git clone https://github.com/your-username/SmartWeather.git
   cd SmartWeather/SmartWeather.Api
   ```

2. Restore and run:

   ```bash
   dotnet restore
   dotnet run
   ```

3. Explore:

   - Weather forecast: `https://localhost:7042/WeatherForecast`
   - Saved locations: `https://localhost:7042/api/SavedLocations`
   - API docs UI: `https://localhost:7042/scalar`

## Roadmap

Planned additions:

- Background worker that integrates with a real weather API
- Event-driven notifications and resiliency patterns
- React/TypeScript frontend
- LLM-powered “smart forecast” summaries