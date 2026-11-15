# ADR-003 · Definición del indicador de oferta mediante criterio combinado · 2026-11-15

## Contexto
Las tiendas digitales y minoristas reportan los descuentos de forma heterogénea: algunas incluyen el precio regular tachado, otras solo muestran el precio actual y algunas mantienen precios base permanentemente inferiores al MSRP de lanzamiento. Se requiere una regla determinista para el indicador de oferta.

## Alternativas
* **Opción A — Comparar únicamente contra el MSRP del repositorio:** Detecta cualquier rebaja frente al lanzamiento, pero clasifica erróneamente como oferta a juegos antiguos cuyo precio base oficial bajó de forma permanente.
* **Opción B — Comparar únicamente contra el precio regular tachado de la tienda:** Refleja promociones activas del comercio, pero falla cuando la tienda no publica el precio tachado en el HTML.
* **Opción C — Criterio combinado con margen mínimo de descuento:** Prioriza el precio regular tachado de la tienda cuando existe y usa el MSRP del repositorio como respaldo, exigiendo un porcentaje mínimo de rebaja para descartar diferencias de centavos.

## Decisión
Un juego se marca **en oferta** cuando su precio actual es al menos un 5 % menor que el precio regular tachado del sitio o, si el sitio no muestra precio tachado, al menos un 10 % menor que el MSRP registrado en el repositorio. Además, recibe la etiqueta de **mínimo histórico** si está en oferta y su precio actual es menor o igual al menor precio registrado previamente en todas las tiendas para ese juego.

## Consecuencias
* **Qué se gana:** Se eliminan falsos positivos por variaciones marginales de centavos y se cubren tiendas que omiten el precio anterior.
* **Qué se pierde:** Requiere recolectar y mantener el MSRP de referencia de cada juego durante el poblamiento del catálogo.
* **Qué se medirá:** La proporción de ofertas activas y mínimos históricos detectados por tienda en cada corrida del pipeline.

## Estado
Aceptada.