# ADR-002 · Uso de PostgreSQL para el repositorio e historial de precios · 2026-11-15

## Contexto
El repositorio debe almacenar al menos 500 juegos en 3 o más plataformas sin estar codificados en el programa, conservar cada observación histórica de precios sin sobrescribirla y guardar las métricas de cada ejecución.

## Alternativas
* **Opción A — SQLite:** Base de datos embebida en un único archivo y fácil de transportar, pero utiliza bloqueos de escritura a nivel de base de datos que generan contención cuando múltiples workers persisten lotes simultáneamente.
* **Opción B — Firebase (Firestore):** Fácil integración en la nube, pero el modelo documental encarece las consultas analíticas de historial de precios, mínimos históricos y agregaciones de más de 3.500 observaciones por corrida.
* **Opción C — PostgreSQL:** Motor relacional con control de concurrencia multiversión (MVCC), soporte eficiente para inserciones masivas por lotes (`batch insert` / `COPY`) e índices compuestos para consultas históricas rápidas.

## Decisión
Adoptamos **PostgreSQL** como sistema gestor de base de datos relacional para el catálogo de juegos, el historial de observaciones de precios y las métricas del pipeline.

## Consecuencias
* **Qué se gana:** Escrituras concurrentes sin bloqueos globales de archivo, integridad referencial estricta y consultas rápidas para calcular el mínimo histórico por juego y tienda.
* **Qué se pierde:** Requiere administrar una instancia de PostgreSQL activa en el entorno de pruebas y en la tarea programada.
* **Qué se medirá:** El tiempo total de la Etapa 4 (Persistir) para confirmar que la escritura por lotes no se convierta en el cuello de botella del pipeline.

## Estado
Aceptada.