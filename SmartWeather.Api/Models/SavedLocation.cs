namespace SmartWeather.Api.Models;

public class SavedLocation
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;     // e.g. "Boston, MA"
    public string Country { get; set; } = string.Empty;  // e.g. "US"
}