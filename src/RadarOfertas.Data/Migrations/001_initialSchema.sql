-- Esquema Relacional: Radar de Ofertas Multinúcleo (PostgreSQL)

CREATE TABLE games (
    game_id SERIAL PRIMARY KEY,
    canonical_title VARCHAR(255) NOT NULL UNIQUE,
    normalized_title VARCHAR(255) NOT NULL,
    release_date DATE,
    msrp_usd NUMERIC(10, 2) NOT NULL,
    platforms TEXT[] NOT NULL, -- Ej: {'PC', 'PlayStation', 'Nintendo Switch', 'Xbox'}
    item_type VARCHAR(30) NOT NULL DEFAULT 'game', -- 'game', 'game_dlc', 'dlc', 'other'
    is_hard_case BOOLEAN NOT NULL DEFAULT FALSE, -- Marca los >= 50 casos difíciles
    cover_image_url TEXT,
    dominant_color_hex VARCHAR(7), -- Extra (+5 pts): Color dominante para la card
    perceptual_hash VARCHAR(64),   -- Extra (+5 pts): Hash perceptual (dHash/pHash)
    hltb_main_hours NUMERIC(6, 2),
    hltb_extra_hours NUMERIC(6, 2),
    hltb_completionist_hours NUMERIC(6, 2),
    hltb_source VARCHAR(30),       -- 'HLTB' o 'IGDB_BACKUP'
    hltb_updated_at TIMESTAMPTZ,
    metacritic_score INT,
    metacritic_updated_at TIMESTAMPTZ,
    opencritic_score INT,
    opencritic_updated_at TIMESTAMPTZ,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE game_aliases (
    alias_id SERIAL PRIMARY KEY,
    game_id INT NOT NULL REFERENCES games(game_id) ON DELETE CASCADE,
    alias_title VARCHAR(255) NOT NULL,
    UNIQUE(game_id, alias_title)
);

CREATE TABLE stores (
    store_id SERIAL PRIMARY KEY,
    store_code VARCHAR(50) NOT NULL UNIQUE, -- 'steam', 'xbox', 'eshop', 'gamestop'
    store_name VARCHAR(100) NOT NULL,
    store_category VARCHAR(30) NOT NULL,    -- 'platform' o 'retailer'
    extraction_method VARCHAR(30) NOT NULL, -- 'html_scraping' o 'api'
    max_concurrent_requests INT NOT NULL
);

CREATE TABLE scrape_runs (
    run_id SERIAL PRIMARY KEY,
    execution_mode VARCHAR(30) NOT NULL, -- 'Sequential', 'Concurrent', 'Parallel'
    is_replay_mode BOOLEAN NOT NULL DEFAULT FALSE,
    io_workers INT NOT NULL,
    cpu_workers INT NOT NULL,
    simulated_latency_ms INT,
    started_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    finished_at TIMESTAMPTZ,
    total_duration_ms BIGINT,
    status VARCHAR(30) NOT NULL DEFAULT 'running'
);

-- Conserva cada observación de precio sin sobrescribirla (Sección 9)
CREATE TABLE price_observations (
    observation_id BIGSERIAL PRIMARY KEY,
    game_id INT NOT NULL REFERENCES games(game_id) ON DELETE CASCADE,
    store_id INT NOT NULL REFERENCES stores(store_id) ON DELETE CASCADE,
    run_id INT NOT NULL REFERENCES scrape_runs(run_id) ON DELETE CASCADE,
    matched_store_title VARCHAR(255) NOT NULL,
    similarity_score NUMERIC(5, 4) NOT NULL,
    is_dubious_match BOOLEAN NOT NULL DEFAULT FALSE,
    format VARCHAR(20) NOT NULL DEFAULT 'digital', -- 'digital' o 'physical'
    current_price_usd NUMERIC(10, 2) NOT NULL,
    regular_price_usd NUMERIC(10, 2),
    discount_pct NUMERIC(5, 2) NOT NULL DEFAULT 0,
    is_on_sale BOOLEAN NOT NULL DEFAULT FALSE,
    is_historical_low BOOLEAN NOT NULL DEFAULT FALSE,
    product_url TEXT NOT NULL,
    observed_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    is_stale_fallback BOOLEAN NOT NULL DEFAULT FALSE
);

CREATE INDEX idx_price_obs_game_store_date 
    ON price_observations(game_id, store_id, observed_at DESC);

CREATE TABLE stage_metrics (
    metric_id SERIAL PRIMARY KEY,
    run_id INT NOT NULL REFERENCES scrape_runs(run_id) ON DELETE CASCADE,
    stage_name VARCHAR(50) NOT NULL, -- '0_Discover', '1_Download', '2_Parse', '3_Match', '4_Persist', '5_Publish'
    duration_ms BIGINT NOT NULL,
    items_processed INT NOT NULL,
    successful_requests INT NOT NULL DEFAULT 0,
    retried_requests INT NOT NULL DEFAULT 0,
    failed_requests INT NOT NULL DEFAULT 0
);

CREATE TABLE unmatched_or_dubious_candidates (
    candidate_id BIGSERIAL PRIMARY KEY,
    run_id INT NOT NULL REFERENCES scrape_runs(run_id) ON DELETE CASCADE,
    game_id INT REFERENCES games(game_id) ON DELETE SET NULL,
    store_id INT NOT NULL REFERENCES stores(store_id) ON DELETE CASCADE,
    raw_candidate_title VARCHAR(255) NOT NULL,
    best_similarity_score NUMERIC(5, 4) NOT NULL,
    reason VARCHAR(100) NOT NULL, -- 'BelowThreshold', 'NearThreshold', 'SpecialEdition', 'DLC_Bundle'
    recorded_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Extra (+3 pts): Lista de deseos con alertas por correo o Telegram
CREATE TABLE wishlist_subscriptions (
    subscription_id SERIAL PRIMARY KEY,
    game_id INT NOT NULL REFERENCES games(game_id) ON DELETE CASCADE,
    target_price_usd NUMERIC(10, 2) NOT NULL,
    channel VARCHAR(20) NOT NULL, -- 'telegram' o 'email'
    destination VARCHAR(255) NOT NULL, -- Chat ID de Telegram o dirección de correo
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    last_notified_at TIMESTAMPTZ,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
