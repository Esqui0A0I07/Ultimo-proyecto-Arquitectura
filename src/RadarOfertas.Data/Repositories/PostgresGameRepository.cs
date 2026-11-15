using System.Text;
using Dapper;
using Npgsql;
using RadarOfertas.Core.Matching;
using RadarOfertas.Core.Models;

namespace RadarOfertas.Data.Repositories;

public sealed class PostgresGameRepository
{
    private readonly string _connectionString;

    private sealed class GameRowDto
    {
        public int GameId { get; set; }
        public string CanonicalTitle { get; set; } = string.Empty;
        public decimal MsrpUsd { get; set; }
        public string[] Platforms { get; set; } = [];
        public bool IsHardCase { get; set; }
    }

    private sealed class StoreRowDto
    {
        public int StoreId { get; set; }
        public string StoreCode { get; set; } = string.Empty;
    }

    private sealed class MinPriceRowDto
    {
        public int GameId { get; set; }
        public decimal? MinPrice { get; set; }
    }

    public PostgresGameRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task InitializeDatabaseAsync(CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);

        const string ddl = """
            CREATE TABLE IF NOT EXISTS games (
                game_id SERIAL PRIMARY KEY,
                canonical_title VARCHAR(255) NOT NULL UNIQUE,
                normalized_title VARCHAR(255) NOT NULL,
                release_date DATE,
                msrp_usd NUMERIC(10, 2) NOT NULL,
                platforms TEXT[] NOT NULL,
                item_type VARCHAR(30) NOT NULL DEFAULT 'game',
                is_hard_case BOOLEAN NOT NULL DEFAULT FALSE,
                cover_image_url TEXT,
                dominant_color_hex VARCHAR(7),
                perceptual_hash VARCHAR(64),
                hltb_main_hours NUMERIC(6, 2),
                hltb_extra_hours NUMERIC(6, 2),
                hltb_completionist_hours NUMERIC(6, 2),
                hltb_source VARCHAR(30),
                hltb_updated_at TIMESTAMPTZ,
                metacritic_score INT,
                metacritic_updated_at TIMESTAMPTZ,
                opencritic_score INT,
                opencritic_updated_at TIMESTAMPTZ,
                created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
            );

            CREATE TABLE IF NOT EXISTS game_aliases (
                alias_id SERIAL PRIMARY KEY,
                game_id INT NOT NULL REFERENCES games(game_id) ON DELETE CASCADE,
                alias_title VARCHAR(255) NOT NULL,
                UNIQUE(game_id, alias_title)
            );

            CREATE TABLE IF NOT EXISTS stores (
                store_id SERIAL PRIMARY KEY,
                store_code VARCHAR(50) NOT NULL UNIQUE,
                store_name VARCHAR(100) NOT NULL,
                store_category VARCHAR(30) NOT NULL,
                extraction_method VARCHAR(30) NOT NULL,
                max_concurrent_requests INT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS scrape_runs (
                run_id SERIAL PRIMARY KEY,
                execution_mode VARCHAR(30) NOT NULL,
                is_replay_mode BOOLEAN NOT NULL DEFAULT FALSE,
                io_workers INT NOT NULL,
                cpu_workers INT NOT NULL,
                simulated_latency_ms INT,
                started_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                finished_at TIMESTAMPTZ,
                total_duration_ms BIGINT,
                status VARCHAR(30) NOT NULL DEFAULT 'running'
            );

            CREATE TABLE IF NOT EXISTS price_observations (
                observation_id BIGSERIAL PRIMARY KEY,
                game_id INT NOT NULL REFERENCES games(game_id) ON DELETE CASCADE,
                store_id INT NOT NULL REFERENCES stores(store_id) ON DELETE CASCADE,
                run_id INT NOT NULL REFERENCES scrape_runs(run_id) ON DELETE CASCADE,
                matched_store_title VARCHAR(255) NOT NULL,
                similarity_score NUMERIC(5, 4) NOT NULL,
                is_dubious_match BOOLEAN NOT NULL DEFAULT FALSE,
                format VARCHAR(20) NOT NULL DEFAULT 'digital',
                current_price_usd NUMERIC(10, 2) NOT NULL,
                regular_price_usd NUMERIC(10, 2),
                discount_pct NUMERIC(5, 2) NOT NULL DEFAULT 0,
                is_on_sale BOOLEAN NOT NULL DEFAULT FALSE,
                is_historical_low BOOLEAN NOT NULL DEFAULT FALSE,
                product_url TEXT NOT NULL,
                observed_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                is_stale_fallback BOOLEAN NOT NULL DEFAULT FALSE
            );

            CREATE INDEX IF NOT EXISTS idx_price_obs_game_store_date 
                ON price_observations(game_id, store_id, observed_at DESC);

            CREATE TABLE IF NOT EXISTS stage_metrics (
                metric_id SERIAL PRIMARY KEY,
                run_id INT NOT NULL REFERENCES scrape_runs(run_id) ON DELETE CASCADE,
                stage_name VARCHAR(50) NOT NULL,
                duration_ms BIGINT NOT NULL,
                items_processed INT NOT NULL,
                successful_requests INT NOT NULL DEFAULT 0,
                retried_requests INT NOT NULL DEFAULT 0,
                failed_requests INT NOT NULL DEFAULT 0
            );

            CREATE TABLE IF NOT EXISTS unmatched_or_dubious_candidates (
                candidate_id BIGSERIAL PRIMARY KEY,
                run_id INT NOT NULL REFERENCES scrape_runs(run_id) ON DELETE CASCADE,
                game_id INT REFERENCES games(game_id) ON DELETE SET NULL,
                store_id INT NOT NULL REFERENCES stores(store_id) ON DELETE CASCADE,
                raw_candidate_title VARCHAR(255) NOT NULL,
                best_similarity_score NUMERIC(5, 4) NOT NULL,
                reason VARCHAR(100) NOT NULL,
                recorded_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
            );

            CREATE TABLE IF NOT EXISTS wishlist_subscriptions (
                subscription_id SERIAL PRIMARY KEY,
                game_id INT NOT NULL REFERENCES games(game_id) ON DELETE CASCADE,
                target_price_usd NUMERIC(10, 2) NOT NULL,
                channel VARCHAR(20) NOT NULL,
                destination VARCHAR(255) NOT NULL,
                is_active BOOLEAN NOT NULL DEFAULT TRUE,
                last_notified_at TIMESTAMPTZ,
                created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
            );

            INSERT INTO stores (store_code, store_name, store_category, extraction_method, max_concurrent_requests)
            VALUES
                ('steam', 'Steam Store', 'platform', 'html_scraping', 6),
                ('xbox', 'Xbox / Microsoft Store', 'platform', 'html_scraping', 4),
                ('eshop', 'Nintendo eShop', 'platform', 'api_or_html', 6),
                ('gamestop', 'GameStop', 'retailer', 'html_scraping', 2)
            ON CONFLICT (store_code) DO NOTHING;
            """;

        await conn.ExecuteAsync(new CommandDefinition(ddl, cancellationToken: ct));
    }

    public async Task<int> UpsertDiscoveredGamesAsync(IEnumerable<GameTarget> games, CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);

        const string sql = """
            INSERT INTO games (canonical_title, normalized_title, msrp_usd, platforms, is_hard_case)
            VALUES (@CanonicalTitle, @NormalizedTitle, @MsrpUsd, @Platforms, @IsHardCase)
            ON CONFLICT (canonical_title) DO UPDATE
            SET msrp_usd = EXCLUDED.msrp_usd,
                platforms = EXCLUDED.platforms,
                is_hard_case = EXCLUDED.is_hard_case;
            """;

        int affected = 0;
        foreach (var g in games)
        {
            affected += await conn.ExecuteAsync(new CommandDefinition(sql, new
            {
                g.CanonicalTitle,
                NormalizedTitle = JaroWinklerMatcher.NormalizeTitle(g.CanonicalTitle),
                g.MsrpUsd,
                g.Platforms,
                g.IsHardCase
            }, transaction: tx, cancellationToken: ct));
        }

        await tx.CommitAsync(ct);
        return affected;
    }

    public async Task<IReadOnlyList<GameTarget>> GetCatalogAsync(int limit = 500, CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);

        const string sql = """
            SELECT game_id AS GameId,
                   canonical_title AS CanonicalTitle,
                   msrp_usd AS MsrpUsd,
                   platforms AS Platforms,
                   is_hard_case AS IsHardCase
            FROM games
            ORDER BY game_id
            LIMIT @Limit;
            """;

        var rows = await conn.QueryAsync<GameRowDto>(new CommandDefinition(sql, new { Limit = limit }, cancellationToken: ct));
        return rows
            .Select(r => new GameTarget(r.GameId, r.CanonicalTitle, r.MsrpUsd, r.Platforms ?? [], r.IsHardCase))
            .ToList();
    }

    public async Task<int> StartScrapeRunAsync(PipelineConfig config, CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);

        const string sql = """
            INSERT INTO scrape_runs (execution_mode, is_replay_mode, io_workers, cpu_workers, simulated_latency_ms, status)
            VALUES (@Mode, @IsReplayMode, @IoWorkers, @CpuWorkers, @SimulatedLatencyMs, 'running')
            RETURNING run_id;
            """;

        return await conn.ExecuteScalarAsync<int>(new CommandDefinition(sql, new
        {
            Mode = config.Mode.ToString(),
            config.IsReplayMode,
            config.IoWorkers,
            config.CpuWorkers,
            config.SimulatedLatencyMs
        }, cancellationToken: ct));
    }

    public async Task SaveEvaluatedBatchAsync(int runId, IReadOnlyList<EvaluatedGameRecord> batch, CancellationToken ct = default)
    {
        if (batch.Count == 0) return;

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);

        var storeRows = await conn.QueryAsync<StoreRowDto>(
            "SELECT store_id AS StoreId, store_code AS StoreCode FROM stores;", transaction: tx);
        var stores = storeRows.ToDictionary(s => s.StoreCode, s => s.StoreId, StringComparer.OrdinalIgnoreCase);

        int[] gameIds = batch.Select(b => b.Game.GameId).Distinct().ToArray();
        var minRows = await conn.QueryAsync<MinPriceRowDto>(
            "SELECT game_id AS GameId, MIN(current_price_usd) AS MinPrice FROM price_observations WHERE game_id = ANY(@GameIds) GROUP BY game_id;",
            new { GameIds = gameIds }, transaction: tx);
        var minPrices = minRows.ToDictionary(m => m.GameId, m => m.MinPrice);

        var obsSql = new StringBuilder();
        var parameters = new DynamicParameters();
        int pIdx = 0;

        foreach (var record in batch)
        {
            minPrices.TryGetValue(record.Game.GameId, out decimal? prevMin);

            foreach (var offer in record.Offers)
            {
                if (!stores.TryGetValue(offer.Candidate.StoreCode, out int storeId))
                {
                    storeId = stores.Values.First();
                }

                if (offer.IsAccepted)
                {
                    bool isHistLow = offer.IsOnSale && (!prevMin.HasValue || offer.Candidate.CurrentPriceUsd <= prevMin.Value);
                    string s = $"_{pIdx++}";

                    obsSql.AppendLine($"""
                        INSERT INTO price_observations (
                            game_id, store_id, run_id, matched_store_title, similarity_score,
                            is_dubious_match, format, current_price_usd, regular_price_usd,
                            discount_pct, is_on_sale, is_historical_low, product_url
                        ) VALUES (
                            @gid{s}, @sid{s}, @rid{s}, @title{s}, @sim{s},
                            @dub{s}, @fmt{s}, @cur{s}, @reg{s},
                            @disc{s}, @sale{s}, @low{s}, @url{s}
                        );
                        """);

                    parameters.Add($"gid{s}", record.Game.GameId);
                    parameters.Add($"sid{s}", storeId);
                    parameters.Add($"rid{s}", runId);
                    parameters.Add($"title{s}", offer.Candidate.RawTitle);
                    parameters.Add($"sim{s}", (decimal)Math.Round(offer.SimilarityScore, 4));
                    parameters.Add($"dub{s}", offer.IsDubious);
                    parameters.Add($"fmt{s}", offer.Candidate.Format);
                    parameters.Add($"cur{s}", offer.Candidate.CurrentPriceUsd);
                    parameters.Add($"reg{s}", offer.Candidate.RegularPriceUsd);
                    parameters.Add($"disc{s}", offer.DiscountPct);
                    parameters.Add($"sale{s}", offer.IsOnSale);
                    parameters.Add($"low{s}", isHistLow);
                    parameters.Add($"url{s}", offer.Candidate.ProductUrl);
                }
            }
        }

        if (obsSql.Length > 0)
        {
            await conn.ExecuteAsync(new CommandDefinition(obsSql.ToString(), parameters, transaction: tx, cancellationToken: ct));
        }

        await tx.CommitAsync(ct);
    }

    public async Task CompleteScrapeRunAsync(
        int runId,
        long downloadMs,
        long parseMs,
        long matchMs,
        long persistMs,
        long totalMs,
        int catalogCount,
        int totalRequests,
        int failedRequests,
        CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);

        const string updateRunSql = """
            UPDATE scrape_runs
            SET finished_at = NOW(),
                total_duration_ms = @TotalMs,
                status = 'completed'
            WHERE run_id = @RunId;
            """;

        await conn.ExecuteAsync(new CommandDefinition(updateRunSql, new { RunId = runId, TotalMs = totalMs }, cancellationToken: ct));

        const string stageSql = """
            INSERT INTO stage_metrics (run_id, stage_name, duration_ms, items_processed, successful_requests, failed_requests)
            VALUES (@RunId, @StageName, @DurationMs, @ItemsProcessed, @SuccessReq, @FailedReq);
            """;

        var stages = new[]
        {
            new { RunId = runId, StageName = "1_Download", DurationMs = downloadMs, ItemsProcessed = catalogCount, SuccessReq = totalRequests - failedRequests, FailedReq = failedRequests },
            new { RunId = runId, StageName = "2_Parse", DurationMs = parseMs, ItemsProcessed = catalogCount, SuccessReq = catalogCount, FailedReq = 0 },
            new { RunId = runId, StageName = "3_Match", DurationMs = matchMs, ItemsProcessed = catalogCount, SuccessReq = catalogCount, FailedReq = 0 },
            new { RunId = runId, StageName = "4_Persist", DurationMs = persistMs, ItemsProcessed = catalogCount, SuccessReq = catalogCount, FailedReq = 0 }
        };

        foreach (var s in stages)
        {
            await conn.ExecuteAsync(new CommandDefinition(stageSql, s, cancellationToken: ct));
        }
    }
}