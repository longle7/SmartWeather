using Microsoft.Extensions.Caching.Memory;
using Moq;
using SmartWeather.Api.Data;
using SmartWeather.Api.Models;
using SmartWeather.Api.Services;
using Xunit;

namespace SmartWeather.Tests.Services;

// Tests for ForecastSummaryService, covering caching and LLM call behavior
public class ForecastSummaryServiceTests
{
    // Builds a ForecastSummaryService with test dependencies
    // Uses a real MemoryCache unless one is passed in
    private static ForecastSummaryService CreateService(
        WeatherDbContext dbContext,
        Mock<IWeatherSummaryClient> llmMock,
        IMemoryCache? cache = null)
    {
        cache ??= new MemoryCache(new MemoryCacheOptions());
        return new ForecastSummaryService(dbContext, llmMock.Object, cache);
    }

    // If there's no weather snapshot for the city, the service should return null
    // and never call the LLM
    [Fact]
    public async Task GetSmartSummaryAsync_ReturnsNull_WhenNoSnapshot()
    {
        // Arrange
        using var db = TestDbContextFactory.CreateInMemoryDbContext();
        var llmMock = new Mock<IWeatherSummaryClient>();
        var service = CreateService(db, llmMock);

        // Act
        var result = await service.GetSmartSummaryAsync("Boston");

        // Assert
        Assert.Null(result);
        llmMock.Verify(x => x.SummarizeAsync(It.IsAny<string>(), default), Times.Never);
    }

    // On the first request with a valid snapshot, the service should
    // call the LLM once and store the result in the cache
    [Fact]
    public async Task GetSmartSummaryAsync_CallsLlmAndCaches_OnFirstRequest()
    {
        // Arrange
        using var db = TestDbContextFactory.CreateInMemoryDbContext();

        // Seed a weather snapshot for Boston
        db.WeatherSnapshots.Add(new WeatherSnapshot
        {
            City = "Boston",
            RetrievedAtUtc = DateTime.UtcNow,
            TemperatureC = 22.5,
            ConditionText = "Partly cloudy"
        });
        await db.SaveChangesAsync();

        // Set up the mock LLM to return a canned summary
        var llmMock = new Mock<IWeatherSummaryClient>();
        llmMock
            .Setup(x => x.SummarizeAsync(It.IsAny<string>(), default))
            .ReturnsAsync("It is a mild, partly cloudy day in Boston.");

        var cache = new MemoryCache(new MemoryCacheOptions());
        var service = CreateService(db, llmMock, cache);

        // Act
        var result = await service.GetSmartSummaryAsync("Boston");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("It is a mild, partly cloudy day in Boston.", result);

        // LLM should be called exactly once
        llmMock.Verify(x => x.SummarizeAsync(It.IsAny<string>(), default), Times.Once);

        // Result should now be stored in the cache under the expected key
        var cacheKey = "SmartForecast:Boston";
        Assert.True(cache.TryGetValue(cacheKey, out var cached));
        Assert.Equal("It is a mild, partly cloudy day in Boston.", cached as string);
    }

    // If a summary is already cached, the service should return the cached
    // value instead of calling the LLM again
    [Fact]
    public async Task GetSmartSummaryAsync_UsesCache_AndDoesNotReCallLlm()
    {
        // Arrange
        using var db = TestDbContextFactory.CreateInMemoryDbContext();

        // Seed a weather snapshot for Boston
        db.WeatherSnapshots.Add(new WeatherSnapshot
        {
            City = "Boston",
            RetrievedAtUtc = DateTime.UtcNow,
            TemperatureC = 22.5,
            ConditionText = "Partly cloudy"
        });
        await db.SaveChangesAsync();

        var llmMock = new Mock<IWeatherSummaryClient>();
        llmMock
            .Setup(x => x.SummarizeAsync(It.IsAny<string>(), default))
            .ReturnsAsync("Cached summary for Boston.");

        var cache = new MemoryCache(new MemoryCacheOptions());
        var service = CreateService(db, llmMock, cache);

        // First call: populates the cache and calls the LLM once
        var first = await service.GetSmartSummaryAsync("Boston");
        Assert.Equal("Cached summary for Boston.", first);

        // Reset mock call history so we can check the second call cleanly
        llmMock.Invocations.Clear();

        // Act - second call should hit the cache instead of the LLM
        var second = await service.GetSmartSummaryAsync("Boston");

        // Assert
        Assert.Equal("Cached summary for Boston.", second);
        llmMock.Verify(x => x.SummarizeAsync(It.IsAny<string>(), default), Times.Never);
    }

    // If the LLM call throws, the service should propagate the exception
    // rather than silently returning a bad or cached result
    [Fact]
    public async Task GetSmartSummaryAsync_ThrowsException_WhenLlmCallFails()
    {
        //Arrange
        using var db = TestDbContextFactory.CreateInMemoryDbContext();

        db.WeatherSnapshots.Add(new WeatherSnapshot
        {
            City = "Boston",
            RetrievedAtUtc = DateTime.UtcNow,
            TemperatureC = 22.5,
            ConditionText = "Partly cloudy"
        });

        await db.SaveChangesAsync();

        var llmMock = new Mock<IWeatherSummaryClient>();
        llmMock
            .Setup(x => x.SummarizeAsync(It.IsAny<string>(), default))
            .ThrowsAsync(new InvalidOperationException("LLM service unavailable"));

        var cache = new MemoryCache(new MemoryCacheOptions());
        var service = CreateService(db, llmMock, cache);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.GetSmartSummaryAsync("Boston"));

        // Nothing should have been cached since the call failed
        var cacheKey = "SmartForecast:Boston";
        Assert.False(cache.TryGetValue(cacheKey, out _));
    }
}