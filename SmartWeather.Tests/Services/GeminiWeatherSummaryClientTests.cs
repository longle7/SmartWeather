using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using SmartWeather.Api.Services;

namespace SmartWeather.Tests.Services;

public class GeminiWeatherSummaryClientTests
{
    private static GeminiWeatherSummaryClient CreateClient(string summaryText)
    {
        // Build a fake Gemini JSON payload that matches the shape you parse.
        // Shape: { "candidates": [ { "content": { "parts": [ { "text": "..." } ] } } ] }
        using var doc = JsonDocument.Parse($@"
        {{
          ""candidates"": [
            {{
              ""content"": {{
                ""parts"": [
                  {{ ""text"": ""{summaryText}"" }}
                ]
              }}
            }}
          ]
        }}
        ");

        var jsonString = doc.RootElement.GetRawText();

        var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(jsonString)
        };

        var handler = new FakeHttpMessageHandler(responseMessage);
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://generativelanguage.googleapis.com/")
        };

        // Fake configuration with a dummy API key
        var inMemorySettings = new Dictionary<string, string?>
        {
            { "Gemini:ApiKey", "TEST_KEY" }
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        return new GeminiWeatherSummaryClient(httpClient, configuration);
    }

    [Fact]
    public async Task SummarizeAsync_ReturnsText_FromFirstCandidate()
    {
        // Arrange
        const string expectedSummary = "It will be a mild, partly cloudy evening in Boston.";
        var client = CreateClient(expectedSummary);

        // Act
        var result = await client.SummarizeAsync("dummy prompt");

        // Assert
        Assert.Equal(expectedSummary, result);
    }
}