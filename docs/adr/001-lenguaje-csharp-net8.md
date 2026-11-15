# ADR-001 · Elección de C# (.NET 8) como lenguaje del pipeline · 2026-11-15

## Contexto
El sistema requiere implementar un pipeline de web scraping y procesamiento en tres modos (secuencial, concurrente en E/S y paralelo en CPU) sobre un catálogo mínimo de 500 juegos, midiendo el escalamiento real en una arquitectura multinúcleo. El curso permite elegir entre C# (.NET 8 o superior) y Python 3.11 o superior.

## Alternativas
* **Opción A — Python 3.11+:** Ecosistema amplio para scraping, pero el Global Interpreter Lock (GIL) impide el paralelismo real de CPU entre hilos dentro de un mismo proceso, obligando a usar `ProcessPoolExecutor` con sobrecosto de serialización interproceso (IPC).
* **Opción B — C# (.NET 8+):** Ejecución compilada con JIT, paralelismo real en memoria compartida sin GIL mediante `Parallel.ForEach` y `PLINQ`, asincronía nativa con `Task.WhenAll` / `Parallel.ForEachAsync` y colas acotadas de alto rendimiento con `System.Threading.Channels.Channel<T>`.

## Decisión
Elegimos **C# (.NET 8)** como lenguaje para todo el backend, el pipeline multinúcleo y el banco de pruebas experimental.

## Consecuencias
* **Qué se gana:** Menor costo de andamiaje paralelo al compartir memoria entre hilos de CPU sin serialización IPC y tipado estático robusto para conectar las etapas del pipeline.
* **Qué se pierde:** Mayor verbosidad inicial en la escritura de parsers DOM frente a scripts ligeros en Python.
* **Qué se medirá:** El costo del andamiaje paralelo comparando `T_par(1)` frente a `T_sec` tanto en la etapa de descarga como en la de emparejamiento.

## Estado
Aceptada.