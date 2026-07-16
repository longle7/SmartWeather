using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using SmartWeather.Api.Services;

namespace SmartWeather.Tests.Services;

// This test class verifies that GeminiWeatherSummaryClient correctly
// parses a response from the Gemini API and extracts the summary text.
public class GeminiWeatherSummaryClientTests
{
    // Helper method that builds a GeminiWeatherSummaryClient wired up with
    // a fake HTTP handler, so we can test the client's parsing logic
    // without making a real network call to Gemini.
    private static GeminiWeatherSummaryClient CreateClient(string summaryText)
    {
        // Build a fake Gemini JSON payload that matches the shape the client parses.
        // Shape: { "candidates": [ { "content": { "parts": [ { "text": "..." } ] } } ] }
        // This mimics exactly what Gemini's real API would return.
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

        // Convert the parsed JSON back into a raw string so we can use it
        // as the body of our fake HTTP response.
        var jsonString = doc.RootElement.GetRawText();

        // Create a fake successful (200 OK) HTTP response containing our fake JSON.
        var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(jsonString)
        };

        // FakeHttpMessageHandler intercepts any HTTP call made through httpClient
        // and returns our canned responseMessage instead of hitting the network.
        var handler = new FakeHttpMessageHandler(responseMessage);
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://generativelanguage.googleapis.com/")
        };

        // Fake configuration with a dummy API key, since GeminiWeatherSummaryClient
        // expects to read "Gemini:ApiKey" from IConfiguration.
        var inMemorySettings = new Dictionary<string, string?>
        {
            { "Gemini:ApiKey", "TEST_KEY" }
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        // Return a fully constructed client using our fake HttpClient and config,
        // ready to be tested in isolation.
        return new GeminiWeatherSummaryClient(httpClient, configuration);
    }

    [Fact]
    public async Task SummarizeAsync_ReturnsText_FromFirstCandidate()
    {
        // Arrange: define what text we expect Gemini to "return",
        // and build a client pre-loaded with that fake response.
        const string expectedSummary = "It will be a mild, partly cloudy evening in Boston.";
        var client = CreateClient(expectedSummary);

        // Act: call the method under test with a placeholder prompt.
        // Since the HTTP call is faked, the prompt content doesn't matter here.
        var result = await client.SummarizeAsync("dummy prompt");

        // Assert: verify the client correctly extracted the summary text
        // from the first candidate in the fake Gemini response.
        Assert.Equal(expectedSummary, result);
    }
}