using System.Collections.Concurrent;
using System.Net;

namespace MarketData.IntegrationTests.Infrastructure;

/// <summary>Stands in for Yahoo Finance: records each request and answers with <see cref="Responder"/>.</summary>
public sealed class StubHttpMessageHandler : HttpMessageHandler
{
    private readonly ConcurrentQueue<HttpRequestMessage> _requests = new();

    public Func<HttpRequestMessage, HttpResponseMessage> Responder { get; set; } =
        _ => new HttpResponseMessage(HttpStatusCode.InternalServerError);

    public IReadOnlyCollection<HttpRequestMessage> Requests => _requests;

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        _requests.Enqueue(request);
        return Task.FromResult(Responder(request));
    }
}
