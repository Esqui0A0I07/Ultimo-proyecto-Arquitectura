# Acta de Constitución, Organización y Acuerdos del Equipo — Proyecto IC-3101

* **Curso:** IC-3101 Arquitectura de Computadores
* **Institución:** Tecnológico de Costa Rica (TEC) — Campus Tecnológico Local San Carlos
* **Semestre:** II Semestre 2026
* **Proyecto:** Radar de Ofertas Multinúcleo (Agregador Concurrente y Paralelo de Videojuegos)
* **Repositorio Oficial:** https://github.com/Esqui0A0I07/Ultimo-proyecto-Arquitectura
* **Sitio Web Público:** https://esqui0a0i07.github.io/Ultimo-proyecto-Arquitectura/

---

## 1. Integrantes del Equipo y Asignación de Roles

| Nombre Completo | Carné | Rol Principal Asignado | Responsabilidades Técnicas Específicas |
| :--- | :---: | :--- | :--- |
| **Isaac Esquivel Alfaro** | 2025104892 | **Líder de Arquitectura, Concurrencia y Sincronización** | Diseño del pipeline productor-consumidor de 5 etapas con canales acotados, control de concurrencia en E/S (semáforo global y por dominio), resiliencia HTTP con retroceso exponencial, experimento de contención y automatización en GitHub Actions. |
| **Fabián Campos** | 2025118340 | **Responsable de Paralelismo en CPU y Experimentación** | Implementación desde cero del algoritmo Jaro–Winkler y normalización de títulos, paralelización de Etapa 3, módulo extra de procesamiento paralelo de portadas (color dominante y hash perceptual de 64 bits) y ejecución de los barridos del Anexo 2. |
| **Johel Fernández** | 2025096415 | **Responsable de Datos, Scraping en Vivo y Producto Web** | Diseño del esquema relacional en PostgreSQL (Neon), persistencia transaccional por lotes con Dapper, módulo de extracción real en vivo en Steam, Xbox, Nintendo eShop y PlayStation/DekuDeals, exportador estático e interfaz en React + Vite con tipo de cambio del BCCR. |

---

## 2. Registro de Sesiones y Acuerdos de Trabajo

### Sesión 1 — Definición de Arquitectura y Hito 1 (26 de octubre de 2026)
* **Hora y Modalidad:** 14:00 – 16:30 · Presencial (Laboratorio de Computación, TEC San Carlos).
* **Asistentes:** Isaac Esquivel Alfaro, Fabián Campos, Johel Fernández (Asistencia $100\%$).
* **Temas Tratados:**
  1. Selección de la pila tecnológica: C# sobre .NET 10.0 para el backend multinúcleo, PostgreSQL alojado en Neon (us-east-1) para la base de datos relacional y React + Vite para el sitio estático en GitHub Pages.
  2. Levantamiento de la ficha técnica del equipo de medición base: procesador **AMD Ryzen 5 5600H** ($P_{fis} = 6$ núcleos físicos, $P_{log} = 12$ hilos lógicos con SMT activo y 8 GB de RAM DDR4).
  3. Formulación de las predicciones matemáticas previas a la medición (Ley de Amdahl con $f = 0.05$ en CPU y modelo de solapamiento de latencia $\ell = 120\text{ ms}$ en E/S).
* **Acuerdos y Compromisos:**
  * Isaac implementará la estructura de proyectos de la solución y los canales acotados con capacidad de 50 elementos.
  * Fabián programará el algoritmo Jaro–Winkler sin librerías externas y construirá el lote de 64 pares manuales de validación.
  * Johel configurará la instancia de Neon PostgreSQL y escribirá el DDL de las 7 tablas relacionales.

---

### Sesión 2 — Sincronización, Modo Replay y Detección de Defecto en E/S (2 de noviembre de 2026)
* **Hora y Modalidad:** 19:00 – 21:30 · Virtual (Google Meet / Discord).
* **Asistentes:** Isaac Esquivel Alfaro, Fabián Campos, Johel Fernández (Asistencia $100\%$).
* **Temas Tratados:**
  1. Revisión del primer barrido experimental en Modo Replay. Se detectó que con 4 trabajadores de E/S la Etapa 1 finalizaba en $15.8\text{ s}$ en vez de los $\approx 105\text{ s}$ teóricos.
  2. Diagnóstico conjunto: se identificó que el bucle paralelo por juego lanzaba internamente 7 tareas simultáneas por tienda, multiplicando la concurrencia real a $4 \times 7 = 28$ solicitudes simultáneas.
  3. Revisión de resultados del experimento de contención sobre $2,000,000$ de actualizaciones en los 12 hilos lógicos.
* **Acuerdos y Compromisos:**
  * Isaac corregirá el defecto en el orquestador del pipeline inyectando un semáforo global para toda la Etapa 1 y documentará el hallazgo en la bitácora de IA.
  * Fabián integrará la comparación entre `Parallel.ForEach` y `PLINQ` ($+2\text{ pts}$ extra) y el procesador de imágenes ($+5\text{ pts}$ extra) desactivando el paralelismo interno de ImageSharp.
  * Johel implementará el exportador estático para generar los archivos JSON consumidos por la web.

---

### Sesión 3 — Extracción en Vivo, Integración BCCR y Despliegue (10 de noviembre de 2026)
* **Hora y Modalidad:** 15:00 – 18:00 · Presencial (TEC Campus San Carlos).
* **Asistentes:** Isaac Esquivel Alfaro, Fabián Campos, Johel Fernández (Asistencia $100\%$).
* **Temas Tratados:**
  1. Sustitución de precios sintéticos e imágenes de prueba por extracción $100\%$ real en modo en vivo desde Steam Store, Xbox DisplayCatalog, Nintendo eShop (Algolia US) y PlayStation / DekuDeals.
  2. Curación del catálogo para garantizar 520 juegos reales únicos, sin sufijos artificiales y respetando estrictamente las plataformas exclusivas (Nintendo Switch, PlayStation, Xbox y PC).
  3. Resolución del conflicto de licencia comercial de ImageSharp 4.1.3 fijando la versión 3.1.5 y desactivando la auditoría de avisos en `Directory.Build.props`.
* **Acuerdos y Compromisos:**
  * Johel agregará en la interfaz web el selector de plataforma por tarjeta, la paginación superior/inferior con tamaño configurable ($12, 24, 48, 96$) y la conversión automática de precios a colones costarricenses consumiendo el indicador oficial del Banco Central de Costa Rica (BCCR).
  * Fabián ejecutará la suite final de benchmarks con 5 repeticiones por punto y consolidará las tablas experimentales.
  * Isaac dejará activo el flujo de trabajo de GitHub Actions para el despliegue diario en GitHub Pages.

---

### Sesión 4 — Cierre de Documentación y Simulacro de Defensa Oral (15 de noviembre de 2026)
* **Hora y Modalidad:** 10:00 – 12:30 · Virtual.
* **Asistentes:** Isaac Esquivel Alfaro, Fabián Campos, Johel Fernández (Asistencia $100\%$).
* **Temas Tratados:**
  1. Verificación del comando interactivo de predicción para el reto en vivo de 5 minutos de la defensa oral (Sección 18).
  2. Revisión cruzada de las 8 preguntas experimentales en el informe técnico para asegurar que los tres integrantes dominen tanto la arquitectura como las fórmulas de Amdahl, eficiencia y Karp–Flatt.
* **Conformidad:** Los tres integrantes aprueban por unanimidad los entregables finales y declaran una distribución equitativa del trabajo ($33.3\%$ por integrante).