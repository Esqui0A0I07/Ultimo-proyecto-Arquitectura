using System.Net;
using System.Text;
using RadarOfertas.Core.Models;

namespace RadarOfertas.Scrapers.Replay;

public sealed class ReplayHttpMessageHandler : HttpMessageHandler
{
    private readonly PipelineConfig _config;

    public ReplayHttpMessageHandler(PipelineConfig config)
    {
        _config = config;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        int delayMs = _config.SimulatedLatencyMs;
        if (_config.SimulatedJitterMs > 0)
        {
            delayMs += Random.Shared.Next(-_config.SimulatedJitterMs, _config.SimulatedJitterMs + 1);
            delayMs = Math.Max(1, delayMs);
        }

        await Task.Delay(delayMs, cancellationToken).ConfigureAwait(false);

        string host = request.RequestUri?.Host ?? "unknown";
        string safeFileName = Convert.ToHexString(
            System.Security.Cryptography.SHA256.HashData(
                Encoding.UTF8.GetBytes(request.RequestUri?.ToString() ?? "")))[..16];

        string filePath = Path.Combine(_config.ReplayCacheDirectory, host, $"{safeFileName}.html");

        string payload = File.Exists(filePath)
            ? await File.ReadAllTextAsync(filePath, cancellationToken).ConfigureAwait(false)
            : $"<html><head><title>Mock {host}</title></head><body data-price='29.99' data-regular='59.99'>Replay Data</body></html>";

        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(payload, Encoding.UTF8, "text/html")
        };
    }
}
