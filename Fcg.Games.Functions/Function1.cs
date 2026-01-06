using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace Fcg.Games.Functions;

public class Function1
{
    private readonly ILogger<Function1> _logger;
    private readonly IHttpClientFactory _httpFactory;

    private const string ExternalApi = "https://fcggamesapi-g2dcb2fafjftgzfy.chilecentral-01.azurewebsites.net/api/games/random";

    // In-memory cache: updated only by the timer trigger
    private static byte[]? _cachedContent;
    private static string? _cachedContentType;
    private static HttpStatusCode _cachedStatus = HttpStatusCode.OK;
    private static DateTimeOffset? _lastUpdated;
    private static readonly object _cacheLock = new();

    public Function1(ILogger<Function1> logger, IHttpClientFactory httpFactory)
    {
        _logger = logger;
        _httpFactory = httpFactory;
    }

    // HTTP GET endpoint available locally at: http://localhost:7071/api/games/random
    [Function("GetRandomGame")]
    public async Task<HttpResponseData> GetRandom([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "games/random")] HttpRequestData req)
    {
        // Copy cache to locals under lock to avoid holding lock during async IO
        byte[]? content;
        string? contentType;
        HttpStatusCode status;

        lock (_cacheLock)
        {
            content = _cachedContent;
            contentType = _cachedContentType;
            status = _cachedStatus;
        }

        if (content == null)
        {
            // First request: fetch immediately, update cache and return result
            var client = _httpFactory.CreateClient();
            try
            {
                var externalResp = await client.GetAsync(ExternalApi);
                var bytes = await externalResp.Content.ReadAsByteArrayAsync();

                lock (_cacheLock)
                {
                    _cachedContent = bytes;
                    _cachedContentType = externalResp.Content.Headers.ContentType?.ToString();
                    _cachedStatus = externalResp.StatusCode;
                    _lastUpdated = DateTimeOffset.UtcNow;
                }

                var initialResponse = req.CreateResponse(externalResp.StatusCode);
                if (!string.IsNullOrEmpty(_cachedContentType)) initialResponse.Headers.Add("Content-Type", _cachedContentType);
                await initialResponse.Body.WriteAsync(bytes, 0, bytes.Length);
                return initialResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching initial game from external API");
                var notReady = req.CreateResponse(HttpStatusCode.ServiceUnavailable);
                notReady.Headers.Add("Content-Type", "text/plain; charset=utf-8");
                await notReady.WriteStringAsync("Unable to load initial game. Try again later.");
                return notReady;
            }
        }

        var response = req.CreateResponse(status);
        if (!string.IsNullOrEmpty(contentType)) response.Headers.Add("Content-Type", contentType);
        await response.Body.WriteAsync(content, 0, content.Length);
        return response;
    }

    // Timer trigger that runs every 1 minute
    [Function("TimerEveryMinute")]
    public async Task RunTimer([TimerTrigger("0 */1 * * * *")] object timer)
    {
        _logger.LogInformation("Timer triggered at: {time}", DateTimeOffset.UtcNow);

        var client = _httpFactory.CreateClient();
        try
        {
            var externalResp = await client.GetAsync(ExternalApi);
            var bytes = await externalResp.Content.ReadAsByteArrayAsync();

            lock (_cacheLock)
            {
                _cachedContent = bytes;
                _cachedContentType = externalResp.Content.Headers.ContentType?.ToString();
                _cachedStatus = externalResp.StatusCode;
                _lastUpdated = DateTimeOffset.UtcNow;
            }

            _logger.LogInformation("External API responded with {status} and cache was updated at {time}", externalResp.StatusCode, _lastUpdated);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Timer error calling external API");
        }
    }
}
