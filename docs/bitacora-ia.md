# Bitácora de Uso de Inteligencia Artificial

## Registro de Consultas y Artefactos
| Fecha | Integrante | Herramienta IA | Qué se pidió (Prompt resumido) | Qué se obtuvo | Qué se aceptó / Qué se modificó y por qué |
| :--- | :--- | :--- | :--- | :--- | :--- |
| 2026-11-15 | Equipo | Gemini | Estructuración de pasos del proyecto, plantillas de ADRs iniciales y esquema relacional PostgreSQL según especificación IC-3101. | Propuesta de pipeline con `Channel<T>`, 7 borradores de ADR y script DDL SQL para PostgreSQL. | Se aceptó la estructura de tablas y límites de colas; se excluyó a la IA del cálculo numérico de `docs/predicciones-h1.md` para cumplir la regla de predicción manual del Hito 1. |

## Entrada Obligatoria: Defecto Real de Concurrencia Generado por IA
*(Espacio reservado para documentar en la Semana 2 un caso real donde la IA genere una condición de carrera, interbloqueo o sobresuscripción de hilos en C#, detallando cómo se detectó, la causa raíz y la corrección aplicada por el equipo).*