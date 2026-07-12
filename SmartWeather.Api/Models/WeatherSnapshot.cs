namespace SmartWeather.Api.Models;

public class WeatherSnapshot
{
    public int Id { get; set; }

    public string City { get; set; } = string.Empty;
    public DateTime RetrievedAtUtc { get; set; }

    public double TemperatureC { get; set; }

    public double TemperatureF => TemperatureC * 9 / 5 + 32;
    public string ConditionText { get; set; } = string.Empty;
}