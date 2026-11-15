# ADR-007 · Sitio público en React + Vite sobre GitHub Pages con actualización diaria · 2026-11-15

## Contexto
El sistema debe publicar un sitio web accesible en una URL pública (no `localhost`) que incluya filtros, ordenamiento, tarjetas de oferta, vista de detalle con gráfico de historial de precios y panel de métricas experimentales, regenerándose automáticamente mediante una tarea programada.

## Alternativas
* **Opción A — Backend web con renderizado dinámico en servidor (SSR):** Permite consultar PostgreSQL en tiempo real en cada visita, pero requiere hospedar y financiar un servidor de aplicaciones activo permanentemente.
* **Opción B — SPA en React + Vite alimentada por artefactos JSON estáticos generados por el pipeline en GitHub Pages:** Desacopla la ejecución pesada del scraper respecto a la visualización web y ofrece alojamiento gratuito de alta disponibilidad.

## Decisión
Construimos el sitio público con **React + Vite** alojado en **GitHub Pages**, consumiendo los archivos JSON estructurados (`catalog.json`, historiales y `metrics.json`) que genera la Etapa 5 del pipeline, y automatizamos su actualización y despliegue cada **24 horas** mediante **GitHub Actions**.

## Consecuencias
* **Qué se gana:** Respuesta inmediata en el navegador para los filtros y gráficos de historial, costo cero de hospedaje web y trazabilidad completa de cada actualización en GitHub Actions.
* **Qué se pierde:** Los precios mostrados reflejan la fotografía de la última ejecución diaria en lugar de consultas en vivo por clic.
* **Qué se medirá:** El tiempo de generación y despliegue de la Etapa 5 y la correcta visualización de la marca temporal de última actualización en el sitio.

## Estado
Aceptada.
