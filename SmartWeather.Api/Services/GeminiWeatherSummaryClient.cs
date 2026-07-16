using System.Text.Json;

namespace SmartWeather.Api.Services;

// IWeatherSummaryClient:
//  - Abstraction for "LLM that can turn a weather prompt into a summary".
//  - Allows swapping Gemini for another provider without touching controllers/services.
public interface IWeatherSummaryClient
{
    Task<string> SummarizeAsync(string prompt, CancellationToken cancellationToken = default);
}

// This service wraps the Gemini REST API generateContent endpoint and hides the HTTP details behind a simple method like SummarizeAsync.

// GeminiWeatherSummaryClient calls Gemini’s generateContent endpoint → returns text.
// Next is SmartForecastController

// GeminiWeatherSummaryClient:
//  - Concrete implementation that calls Google's Gemini API.
//  - Uses HttpClient to POST a generateContent request with the prompt as text.
//  - Parses candidates[0].content.parts[0].text from the JSON response and returns it.
//  - The Gemini API key is read from configuration ("Gemini:ApiKey"), which is supplied
//    via .NET user secrets / environment variables (not stored in Git).
public class GeminiWeatherSummaryClient : IWeatherSummaryClient
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    // Go to https://generativelanguage.googleapis.com/v1beta/models?key=YOUR-API-KEY-GOES-HERE to test which models are enabled for your api key
    private const string ModelName = "models/gemini-3.5-flash";

    public GeminiWeatherSummaryClient(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _apiKey = configuration["Gemini:ApiKey"]
                  ?? throw new InvalidOperationException("Gemini:ApiKey configuration is missing.");
    }

    public async Task<string> SummarizeAsync(string prompt, CancellationToken cancellationToken = default)
    {
        // Gemini generateContent REST format [web:243]
        // 1. Shape the request body in Gemini generateContent format (contents/parts/text).
        var requestBody = new
        {
            contents = new[]
            {
                new
                {
                    parts = new[]
                    {
                        new { text = prompt }
                    }
                }
            }
        };

        // 2. Build the HTTP request to Gemini's REST endpoint with the API key header.
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"v1beta/{ModelName}:generateContent"
        );

        request.Headers.Add("x-goog-api-key", _apiKey);
        request.Content = JsonContent.Create(requestBody);

        // 3. Send the request and ensure we got a successful response.
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            if ((int)response.StatusCode == 503)
            {
                // Option A: return a fallback summary instead of throwing
                return "The AI summary service is currently experiencing high demand. Please try again in a moment.";
            }
            else
            {
                throw new InvalidOperationException(
                $"Gemini API returned {(int)response.StatusCode} {response.StatusCode}: {errorBody}");
            }
        }
        response.EnsureSuccessStatusCode();

        // 4. Parse the JSON and extract the main text from the first candidate.
        var json = await response.Content.ReadAsStringAsync(cancellationToken);

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        // Gemini responses typically have candidates[0].content.parts[0].text [web:243][web:247]
        var candidates = root.GetProperty("candidates");
        if (candidates.GetArrayLength() == 0)
        {
            throw new InvalidOperationException("Gemini response has no candidates.");
        }

        var firstCandidate = candidates[0];
        var content = firstCandidate.GetProperty("content");
        var parts = content.GetProperty("parts");

        if (parts.GetArrayLength() == 0)
        {
            throw new InvalidOperationException("Gemini response has no parts.");
        }

        var text = parts[0].GetProperty("text").GetString();
        return text ?? string.Empty;
    }
}