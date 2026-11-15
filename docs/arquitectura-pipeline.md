flowchart LR
    subgraph E0["0 · Descubrir (E/S)"]
        D0["OpenCritic Paginado\n(>= 500 juegos)"]
    end

    Q1(["Cola 1: Channel&lt;GameSeed&gt;\nCapacidad: 100"])

    subgraph E1["1 · Descargar (E/S Concurrente)"]
        D1["Workers: Task / Parallel.ForEachAsync\nBarrido: 1, 2, 4, 8, 16, 32, 64\nSemáforos por Dominio + Backoff\n(Modo Vivo o Modo Replay)"]
        DLQ["Cola de Fallidos (Retry Final)"]
    end

    Q2(["Cola 2: Channel&lt;RawGamePayloads&gt;\nCapacidad: 50"])

    subgraph E2["2 · Parsear (CPU Ligero)"]
        D2["Extracción HTML/JSON\n(AngleSharp / HtmlAgilityPack)\nPrecio, Oferta, Score, HLTB, URL Imagen"]
    end

    Q3(["Cola 3: Channel&lt;ParsedGameCandidates&gt;\nCapacidad: 50"])

    subgraph E3["3 · Emparejar y Procesar (CPU Intensivo)"]
        D3["Normalización + Jaro-Winkler\n(Parallel.ForEach vs PLINQ)\nBarrido: 1, 2, 4 ... P, 2P\n+ Extra: Hash Perceptual y Color de Portada"]
    end

    Q4(["Cola 4: Channel&lt;MatchedGameResult&gt;\nCapacidad: 100"])

    subgraph E4["4 · Persistir (E/S BD)"]
        D4["PostgreSQL (Batch Insert)\nHistorial de Precios + Métricas\n+ Extra: Disparo Alertas Telegram/Email"]
    end

    subgraph E5["5 · Publicar (Generación Estática)"]
        D5["Exportación JSON Estático\nSitio React + Vite (GitHub Pages)\nActualización cada 24h (GH Actions)"]
    end

    E0 --> Q1 --> E1
    E1 -.-> DLQ
    E1 --> Q2 --> E2 --> Q3 --> E3 --> Q4 --> E4 --> E5