# Radar de Ofertas Multinúcleo — IC-3101 Arquitectura de Computadores

Sistema concurrente y paralelo en **C# (.NET 10.0)** con persistencia relacional en **PostgreSQL (Neon)** y sitio web estático en **React + Vite** desplegado automáticamente cada 24 horas en **GitHub Pages**.

* 🌐 **Sitio Web Público (GitHub Pages):** [https://esqui0a0i07.github.io/Ultimo-proyecto-Arquitectura/](https://esqui0a0i07.github.io/Ultimo-proyecto-Arquitectura/)
* 📘 **Guía de Comandos y Referencia de la CLI:** [`docs/guia-comandos.md`](docs/guia-comandos.md)
* 📄 **Informe Técnico Final (8 Preguntas + Análisis):** [`docs/informe-tecnico.md`](docs/informe-tecnico.md)
* 📊 **Tablas Experimentales (Formato Anexo 2):** [`docs/resultados-experimentales.md`](docs/resultados-experimentales.md)
* 🏛️ **Arquitectura del Pipeline y ADRs (001 a 007):** [`docs/arquitectura-y-adrs.md`](docs/arquitectura-y-adrs.md)
* 🤖 **Bitácora de IA y Defectos Reales Corregidos:** [`docs/bitacora-ia.md`](docs/bitacora-ia.md)

---

## Equipo de Desarrollo (TEC · Campus San Carlos)
* **Isaac Esquivel Alfaro** — Arquitectura del Pipeline, Concurrencia E/S y Sincronización
* **Fabián Campos** — Rendimiento Multinúcleo, Experimentación y Procesamiento Paralelo de Imágenes
* **Johel Fernández** — Modelo de Datos PostgreSQL, Scraping en Vivo (`--live`) e Interfaz Web React

---

## Inicio Rápido

```powershell
cd src

# 1. Compilar solución en modo Release (0 errores, 0 advertencias)
dotnet build -c Release

# 2. Poblar catálogo limpio de 520 juegos reales por plataforma en PostgreSQL
dotnet run --project RadarOfertas.Cli -c Release -- --discover --games 520

# 3. Extraer precios reales en vivo (--live) de Steam, Xbox, eShop y PlayStation/DekuDeals + exportar JSON web
dotnet run --project RadarOfertas.Cli -c Release -- --parallel --live --use-db --publish-web --games 520 --io-workers 12 --cpu-workers 12

# 4. Ejecutar suite experimental completa en Modo Replay 
dotnet run --project RadarOfertas.Cli -c Release -- --benchmark --reps 5 --games 500 --latency 120 --candidates 2500

# 5. Ejecutar barrido de procesamiento paralelo de portadas 
dotnet run --project RadarOfertas.Cli -c Release -- --images --reps 5 --use-db --publish-web --games 500

# 6. Ejecutar predicción matemática en vivo 
dotnet run --project RadarOfertas.Cli -c Release -- --predict --io-workers 20 --cpu-workers 8 --latency 90 --games 500
