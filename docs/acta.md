# Acta de Constitución del Equipo — Radar de Ofertas Multinúcleo
**Curso:** IC-3101 Arquitectura de Computadoras (II Semestre 2026)  
**Institución:** Instituto Tecnológico de Costa Rica — Campus San Carlos  

## 1. Integrantes y Roles Oficiales
Cada integrante asume la responsabilidad principal de defensa y conducción sobre su área asignada, manteniendo el compromiso de comprender y poder explicar cualquier componente del sistema completo:

| Integrante | Rol Asignado | Responsabilidades Principales |
| :--- | :--- | :--- |
| **Isaac Esquivel Alfaro** | **Arquitectura y sincronización** | Diseño del pipeline en C# (.NET 8+), colas acotadas (`Channel<T>`), límites de concurrencia (`SemaphoreSlim`), ADRs técnicos y experimento de contención (`lock` vs `ThreadLocal<T>` vs `ConcurrentDictionary`). |
| **Fabián Jesús Campos Santamaría** | **Rendimiento y experimentación** | Implementación del Modo Replay con latencia simulada, banco de pruebas, barridos de workers (E/S y CPU), cálculo de métricas (Speedup, Eficiencia, Ley de Amdahl, Karp–Flatt) e informe técnico. |
| **Johel Alonso Fernández Madrigal** | **Datos y producto** | Esquema y persistencia en PostgreSQL, descubrimiento paginado (Etapa 0), scrapers por fuente, algoritmo Jaro–Winkler y su validación, sitio público en React + Vite, panel de métricas y automatización con GitHub Actions. |

## 2. Acuerdos de Comunicación y Seguimiento
* **Canal principal:** Grupo de mensajería instantánea y servidor de Discord/Teams para sesiones de programación y revisión.
* **Reuniones de seguimiento:** Se realizarán al menos **dos reuniones formales por semana** (martes y viernes a las 7:00 p.m.), cuyos acuerdos, bloqueos y acciones quedarán registrados en `docs/seguimiento.md` (mínimo 5 líneas por sesión).
* **Flujo de trabajo en Git:**
  * Uso de ramas por funcionalidad (`feature/nombre-tarea`, `fix/nombre-bug`, `docs/nombre-doc`).
  * Ningún cambio entra a `main` sin un Pull Request revisado por al menos otro integrante.
  * Commits frecuentes y atómicos distribuidos durante las 3 semanas para reflejar el aporte continuo de los tres miembros.

## 3. Criterio para Tomar Decisiones Técnicas
* Toda decisión arquitectónica con alternativas reales se discute entre los tres integrantes y se registra en un **ADR** dentro de `docs/adr/` antes de su implementación.
* Cualquier desviación posterior respecto al diseño entregado en el Hito 1 se documentará obligatoriamente en `docs/cambios-diseno.md` justificando el motivo técnico o experimental.

## 4. Resolución de Conflictos
1. **Desacuerdos técnicos:** Se priorizará la opción que tenga respaldo empírico (una prueba rápida de concepto o medición de tiempos/memoria) o que facilite el cumplimiento estricto de la rúbrica experimental. Si persiste el empate, decide el responsable del rol asociado al componente y se deja constancia en el ADR correspondiente.
2. **Bloqueos o atrasos individuales:** El integrante que enfrente un bloqueo técnico o de tiempo debe notificarlo con al menos 24 horas de anticipación a la reunión de seguimiento para redistribuir subtareas en el tablero.