using System.Collections.Concurrent;

namespace RadarOfertas.Scrapers.Resilience;

public sealed class DomainRateLimiter
{
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _semaphores = new(StringComparer.OrdinalIgnoreCase);

    public DomainRateLimiter()
    {
        // ADR-004: Límites máximos de conexiones simultáneas por dominio
        _semaphores["store.steampowered.com"] = new SemaphoreSlim(6, 6);
        _semaphores["www.nintendo.com"] = new SemaphoreSlim(6, 6);
        _semaphores["opencritic.com"] = new SemaphoreSlim(4, 4);
        _semaphores["www.xbox.com"] = new SemaphoreSlim(4, 4);
        _semaphores["www.metacritic.com"] = new SemaphoreSlim(3, 3);
        _semaphores["howlongtobeat.com"] = new SemaphoreSlim(3, 3);
        _semaphores["www.gamestop.com"] = new SemaphoreSlim(2, 2);
    }

    public async Task<(string Content, bool Success, int Retries, string? Error)> ExecuteWithRetryAsync(
        HttpClient client,
        string url,
        bool isReplayMode,
        CancellationToken ct,
        int maxRetries = 3)
    {
        Uri uri = new(url);
        // En Modo Replay se permite medir el escalamiento puro de W_io sin bloqueo de semáforo externo
        SemaphoreSlim? semaphore = isReplayMode ? null : _semaphores.GetOrAdd(uri.Host, _ => new SemaphoreSlim(3, 3));

        if (semaphore is not null)
        {
            await semaphore.WaitAsync(ct).ConfigureAwait(false);
        }

        try
        {
            for (int attempt = 0; attempt <= maxRetries; attempt++)
            {
                try
                {
                    using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                    cts.CancelAfter(TimeSpan.FromSeconds(12));

                    using var response = await client.GetAsync(url, cts.Token).ConfigureAwait(false);
                    response.EnsureSuccessStatusCode();

                    string content = await response.Content.ReadAsStringAsync(cts.Token).ConfigureAwait(false);
                    return (content, true, attempt, null);
                }
                catch (Exception ex) when (attempt < maxRetries && !ct.IsCancellationRequested)
                {
                    int backoffMs = (int)(Math.Pow(2, attempt) * 500) + Random.Shared.Next(50, 350);
                    await Task.Delay(backoffMs, ct).ConfigureAwait(false);
                }
                catch (Exception finalEx)
                {
                    return (string.Empty, false, attempt, finalEx.Message);
                }
            }
            return (string.Empty, false, maxRetries, "Max retries exceeded");
        }
        finally
        {
            semaphore?.Release();
        }
    }
}
