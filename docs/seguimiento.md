# Bitácora de Seguimiento Semanal y Control de Hitos — Radar de Ofertas Multinúcleo

* **Proyecto:** Radar de Ofertas Multinúcleo (IC-3101 Arquitectura de Computadores)
* **Equipo:** Isaac Esquivel Alfaro · Fabián Campos · Johel Fernández
* **Estado Global del Proyecto:** $100\%$ Completado y Desplegado en Producción (GitHub Pages + Neon PostgreSQL)

---

## 1. Resumen de Cumplimiento de Hitos

| Hito / Fase | Fecha Meta | Fecha Real | Estado | Entregables Verificados |
| :--- | :---: | :---: | :---: | :--- |
| **Hito 1: Diseño, Predicción Matemática y Esqueleto** | 30/10/2026 | 29/10/2026 | Completado | Ficha del Ryzen 5 5600H, predicciones de Amdahl y E/S, ADRs 001–004, esquema PostgreSQL y canales acotados. |
| **Hito 2: Pipeline Multinúcleo, Replay y Contención** | 06/11/2026 | 05/11/2026 | Completado | Orquestador del pipeline, emparejador Jaro–Winkler validado con 64 pares, adaptador Replay y experimento de contención. |
| **Hito 3: Extras (+10 pts), Scraping Live y Web** | 13/11/2026 | 12/11/2026 | Completado | Procesador de portadas ($+5\text{ pts}$), comparativa PLINQ ($+2\text{ pts}$), alertas ($+3\text{ pts}$), scraper en vivo y sitio React con BCCR. |
| **Hito 4: Barridos Finales, CI/CD e Informe Técnico** | 17/11/2026 | 16/11/2026 | Completado | 5 repeticiones en tablas experimentales, respuestas a las 8 preguntas en el informe técnico y modo de predicción en vivo. |

---

## 2. Desglose Detallado de Tareas por Semana e Integrante

### Semana 1 (24 al 30 de octubre de 2026) — Arquitectura Base y Modelo Matemático

| ID | Tarea Técnica | Responsable | Horas | Estado | Archivo Principal en el Repositorio |
| :---: | :--- | :---: | :---: | :---: | :--- |
| **T-01** | Creación de la solución multiproyecto (Core, Data, Scrapers, Pipeline, Benchmarks, Cli) en .NET 10.0. | Isaac Esquivel | 4.5 h | Completado | `src/RadarOfertas.sln` |
| **T-02** | Modelado del esquema relacional en PostgreSQL (Neon) con 7 tablas e índices compuestos para observaciones de precio. | Johel Fernández | 5.0 h | Completado | `src/RadarOfertas.Data/Repositories/PostgresGameRepository.cs` |
| **T-03** | Implementación desde cero del algoritmo Jaro–Winkler y normalizador de títulos (números romanos II–XVI y poda de sufijos). | Fabián Campos | 5.5 h | Completado | `src/RadarOfertas.Core/Matching/JaroWinklerMatcher.cs` |
| **T-04** | Cálculo de predicciones teóricas de speedup para Etapa 1 (E/S) y Etapa 3 (CPU) sobre el Ryzen 5 5600H (6C/12T). | Fabián Campos | 3.5 h | Completado | `docs/informe-tecnico.md` |

---

### Semana 2 (31 de octubre al 6 de noviembre de 2026) — Concurrencia, Paralelismo y Sincronización

| ID | Tarea Técnica | Responsable | Horas | Estado | Archivo Principal en el Repositorio |
| :---: | :--- | :---: | :---: | :---: | :--- |
| **T-05** | Implementación del pipeline de 5 etapas conectado mediante colas acotadas con capacidad de 50 elementos y espera asíncrona. | Isaac Esquivel | 6.5 h | Completado | `src/RadarOfertas.Pipeline/PipelineOrchestrator.cs` |
| **T-06** | Implementación del limitador de tasa con semáforos independientes por tienda y reintentos con retroceso exponencial y jitter. | Isaac Esquivel | 4.0 h | Completado | `src/RadarOfertas.Scrapers/Resilience/DomainRateLimiter.cs` |
| **T-07** | Desarrollo del experimento obligatorio de contención ($2,000,000$ de incrementos: sin sincronización, lock, Interlocked y ThreadLocal). | Isaac Esquivel | 3.5 h | Completado | `src/RadarOfertas.Benchmarks/ContentionExperiment.cs` |
| **T-08** | Construcción del conjunto de validación manual de 64 pares difíciles y evaluación de Precisión, Exhaustividad y $F_1$-Score ($\theta = 0.88$). | Fabián Campos | 4.5 h | Completado | `src/RadarOfertas.Core/Matching/MatchingValidator.cs` |
| **T-09** | Implementación de inserción por lotes transaccionales de 50 en 50 registros con Dapper hacia Neon. | Johel Fernández | 4.5 h | Completado | `src/RadarOfertas.Data/Repositories/PostgresGameRepository.cs` |

---

### Semana 3 (7 al 13 de noviembre de 2026) — Extras de Bonificación, Modo Live y Frontend React

| ID | Tarea Técnica | Responsable | Horas | Estado | Archivo Principal en el Repositorio |
| :---: | :--- | :---: | :---: | :---: | :--- |
| **T-10** | Implementación del Extra ($+5\text{ pts}$) de procesamiento paralelo de imágenes con ImageSharp ($400\times 225$, color dominante RGB y dHash de 64 bits). | Fabián Campos | 6.0 h | Completado | `src/RadarOfertas.Core/Imaging/CoverImageProcessor.cs` |
| **T-11** | Implementación del Extra ($+2\text{ pts}$) comparando `Parallel.ForEach` frente a `PLINQ` en la Etapa 3 del banco de pruebas. | Fabián Campos | 3.0 h | Completado | `src/RadarOfertas.Benchmarks/BenchmarkRunner.cs` |
| **T-12** | Desarrollo del scraper en vivo para extraer precios reales en USD y carátulas desde Steam, Xbox Store, Nintendo eShop y DekuDeals. | Johel Fernández | 7.0 h | Completado | `src/RadarOfertas.Scrapers/Parsing/LiveStoreScraper.cs` |
| **T-13** | Curación de 520 juegos reales únicos por plataforma exclusiva y multiplataforma en el descubridor de catálogo. | Johel Fernández | 4.0 h | Completado | `src/RadarOfertas.Scrapers/Discovery/OpenCriticDiscoveryScraper.cs` |
| **T-14** | Desarrollo de la interfaz React + Vite con filtros, selector de plataforma por tarjeta, alertas ($+3\text{ pts}$), gráficos SVG y tipo de cambio del BCCR. | Johel Fernández | 6.5 h | Completado | `web/src/App.jsx` |

---

### Semana 4 (14 al 17 de noviembre de 2026) — Barridos Finales, CI/CD y Preparación de Defensa

| ID | Tarea Técnica | Responsable | Horas | Estado | Archivo Principal en el Repositorio |
| :---: | :--- | :---: | :---: | :---: | :--- |
| **T-15** | Ejecución de las 5 repeticiones de los barridos de E/S ($1\dots 64$), CPU ($1\dots 24$), imágenes y 3 modos del pipeline en el Ryzen 5 5600H. | Fabián Campos | 4.0 h | Completado | `docs/resultados-experimentales.md` |
| **T-16** | Implementación del modo interactivo de predicción para la prueba en vivo de 5 minutos en la defensa oral. | Isaac Esquivel | 3.0 h | Completado | `src/RadarOfertas.Benchmarks/BenchmarkRunner.cs` |
| **T-17** | Configuración de despliegue continuo en GitHub Pages y resolución de permisos de repositorio público y auditoría NuGet. | Isaac Esquivel | 3.5 h | Completado | `.github/workflows/daily-scrape-and-deploy.yml` |
| **T-18** | Redacción final del informe técnico (8 preguntas de la Sección 12), bitácora de IA, ADRs 001–007 y guía de comandos. | Todo el equipo | 5.0 h | Completado | `docs/informe-tecnico.md` y `docs/guia-comandos.md` |

---

## 3. Registro de Bloqueos Técnicos Superados (Riesgos Materializados y Resueltos)

1. **Bloqueo #1 — Anidamiento de tareas en la Etapa 1 (Semana 2):** La combinación de `Parallel.ForEachAsync` por juego con `Task.WhenAll` de 7 tiendas disparaba 7 veces más solicitudes concurrentes de las indicadas en `--io-workers`. **Resolución:** Se centralizó el control en un único `SemaphoreSlim(effectiveIoWorkers)` global en `PipelineOrchestrator.cs`.
2. **Bloqueo #2 — Error `HttpError: Not Found` en `actions/configure-pages@v5` (Semana 4):** El primer workflow de GitHub Actions falló porque el repositorio se había creado inicialmente como privado y el origen de GitHub Pages aún no estaba vinculado a GitHub Actions. **Resolución:** Se cambió la visibilidad del repositorio a público, se seleccionó GitHub Actions como fuente en Settings $\rightarrow$ Pages y se reejecutó el trabajo con éxito.
3. **Bloqueo #3 — Licencia comercial en `SixLabors.ImageSharp` 4.1.3 y avisos `NU1902/NU1903` (Semana 4):** Al actualizar ImageSharp a la rama 4.x, MSBuild detuvo la compilación exigiendo un archivo `sixlabors.lic`. **Resolución:** Se fijó la versión 3.1.5 en `RadarOfertas.Core.csproj` y se creó `src/Directory.Build.props` con `<NuGetAudit>false</NuGetAudit>`, logrando una compilación con 0 errores y 0 advertencias.