# Bitácora de Uso de Inteligencia Artificial

## Registro de Consultas y Artefactos
| Fecha | Integrante | Herramienta IA | Qué se pidió (Prompt resumido) | Qué se obtuvo | Qué se aceptó / Qué se modificó y por qué |
| :--- | :--- | :--- | :--- | :--- | :--- |
| 2026-11-15 | Equipo | Gemini | Estructuración de pasos del proyecto, plantillas de ADRs iniciales y esquema relacional PostgreSQL según especificación IC-3101. | Propuesta de pipeline con `Channel<T>`, 7 borradores de ADR y script DDL SQL para PostgreSQL. | Se aceptó la estructura de tablas y límites de colas; se excluyó a la IA del cálculo numérico de `docs/predicciones-h1.md` para cumplir la regla de predicción manual del Hito 1. |

## Entrada Obligatoria: Defecto Real de Concurrencia Generado por IA

* **Componente afectado:** `RadarOfertas.Pipeline/PipelineOrchestrator.cs` (Etapa 1 · Descargar E/S).
* **Defecto producido por la IA:** Sobresuscripción de tareas por anidamiento no acotado de niveles de concurrencia (Sección 7). El código inicial generado combinó `Parallel.ForEachAsync` con `MaxDegreeOfParallelism = W_io` (nivel por juego) y dentro de cada iteración invocó `Task.WhenAll` para las 7 fuentes (nivel por fuente) sin un semáforo global de etapa.
* **Cómo se detectó:** Al contrastar la predicción manual del Hito 1 para $W_{io} = 32$ con latencia de $120\text{ ms}$ ($\approx 15,63\text{ s}$) contra la primera medición en Modo Replay, la Etapa 1 finalizó en solo $2,16\text{ s}$. Al auditar las métricas se comprobó que había $32 \times 7 = 224$ solicitudes en vuelo simultáneamente ($\frac{3.500}{224} \times 0,12\text{ s} = 1,875\text{ s}$), violando el límite experimental de $W_{io} = 32$.
* **Causa raíz:** `Parallel.ForEachAsync` limitaba la cantidad de *juegos* activos a 32, pero no el total de *solicitudes HTTP* concurrentes despachadas por sus subtareas hijas.
* **Corrección aplicada:** Se introdujo un `SemaphoreSlim(config.IoWorkers, config.IoWorkers)` global a nivel de la Etapa 1 (`_stageIoGate`) que cada solicitud individual debe adquirir antes de ejecutarse, garantizando que para cualquier barrido $W_{io} \in \{1, 2, 4, 8, 16, 32, 64\}$ existan exactamente $W_{io}$ operaciones de E/S en vuelo.