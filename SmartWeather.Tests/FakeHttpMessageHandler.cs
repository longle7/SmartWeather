using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace SmartWeather.Tests;

public class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly HttpResponseMessage _response;

    public FakeHttpMessageHandler(HttpResponseMessage response)
    {
        _response = response;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        // Always return the preconfigured response, ignoring the request details for this test.
        return Task.FromResult(_response);
    }
}