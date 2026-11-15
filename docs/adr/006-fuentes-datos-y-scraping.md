# ADR-006 · Selección de fuentes de datos, scraping HTML y mecanismos de respaldo · 2026-11-15

## Contexto
El proyecto exige poblar automáticamente al menos 500 juegos desde un listado público paginado y consultar para cada juego un mínimo de 4 tiendas (al menos 2 de plataforma y 1 minorista), 2 fuentes de calificación (Metacritic obligatoria), 1 de duración (How Long To Beat obligatoria) y 1 de portadas, obteniendo al menos 3 fuentes (incluidas 2 tiendas) mediante scraping de HTML.

## Alternativas
* **Opción A — Depender exclusivamente de APIs públicas:** Simplifica la extracción de datos estructurados, pero incumple la restricción obligatoria de realizar scraping de HTML en al menos 3 fuentes y 2 tiendas.
* **Opción B — Esquema híbrido con scraping HTML directo, caché local y fuentes de respaldo:** Cumple los mínimos de parseo de HTML por tipo de fuente e incorpora tolerancia a fallos ante bloqueos o caídas parciales.

## Decisión
Seleccionamos **OpenCritic** para el descubrimiento paginado (Etapa 0) y como segunda fuente de calificación; **Steam**, **Xbox / Microsoft Store** y **Nintendo eShop** como tiendas de plataforma; **GameStop** como tienda minorista; **Metacritic** para calificación; **How Long To Beat** para duración (con **IGDB** como respaldo documentado); y extracción directa de portadas desde las tiendas. El scraping de HTML se realiza sobre Steam, Xbox Store, GameStop y Metacritic identificando el proyecto en el `User-Agent` y respetando `robots.txt`.

## Consecuencias
* **Qué se gana:** Cobertura de 4 plataformas (PC, PlayStation, Xbox y Switch), cumplimiento estricto de los mínimos de scraping HTML y continuidad operativa si una fuente falla.
* **Qué se pierde:** Mayor fragilidad ante cambios en el DOM de las tiendas y necesidad de mantener parsers específicos por sitio.
* **Qué se medirá:** El conteo de solicitudes exitosas, reintentadas y enviadas a la cola de fallidos por cada sitio en el panel de métricas.

## Estado
Aceptada.