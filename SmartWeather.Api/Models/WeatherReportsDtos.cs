namespace SmartWeather.Api.Models;

public record CityTemperatureReportDto(
    string City,
    double AverageTemperatureC,
    int SnapshotCount
);

public record CitySnapshotCountDto(
    string City,
    int SnapshotCount
);