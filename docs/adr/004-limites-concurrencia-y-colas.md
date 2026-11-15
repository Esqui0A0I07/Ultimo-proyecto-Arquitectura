# ADR-004 · Diseño de colas acotadas y límites de concurrencia por etapa y dominio · 2026-11-15

## Contexto
Con un mínimo de 500 juegos y 7 fuentes consultadas por juego (más de 3.500 solicitudes por ejecución), anidar sin control los tres niveles de paralelismo (por juego, por fuente y por tienda) saturaría la memoria RAM, agotaría los sockets locales y provocaría bloqueos HTTP 429/403 en las tiendas.

## Alternativas
* **Opción A — Colas ilimitadas y despacho libre de tareas:** Simplifica el código al encolar todas las descargas de inmediato, pero acumula miles de respuestas HTML en memoria y causa rechazos por límite de tasa en los servidores destino.
* **Opción B — Pipeline con `Channel<T>` acotados, presupuesto de workers por etapa y `SemaphoreSlim` por dominio:** Aplica contrapresión (*backpressure*) automática entre etapas y desacopla la concurrencia de red de los núcleos de CPU.

## Decisión
Conectamos las etapas con `Channel<T>` acotados en capacidades de 100 (Etapa 0→1), 50 (Etapa 1→2), 50 (Etapa 2→3) y 100 (Etapa 3→4) bajo política `BoundedChannelFullMode.Wait`. Limitamos las solicitudes simultáneas por dominio en vivo mediante `SemaphoreSlim` (Steam: 6, Nintendo eShop: 6, OpenCritic: 4, Xbox Store: 4, Metacritic: 3, HLTB: 3, GameStop: 2) con hasta 3 reintentos exponenciales con *jitter*, fijando en producción 32 workers de E/S y $P_{log}$ workers de CPU.

## Consecuencias
* **Qué se gana:** Consumo de memoria RAM acotado y cumplimiento de scraping responsable sin bloqueos de IP.
* **Qué se pierde:** El throughput de descarga en vivo queda acotado por el dominio con el semáforo más estricto (GameStop con 2 conexiones).
* **Qué se medirá:** El barrido experimental en Modo Replay (1, 2, 4, 8, 16, 32 y 64 workers en E/S; 1 hasta $2 \times P_{log}$ en CPU) y la tasa de reintentos por sitio en vivo.

## Estado
Aceptada.
