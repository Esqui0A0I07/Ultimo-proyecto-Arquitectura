# Informe Técnico Final — Radar de Ofertas Multinúcleo (IC-3101)

* **Curso:** IC-3101 Arquitectura de Computadores — Tecnológico de Costa Rica (Campus Tecnológico Local San Carlos)
* **Integrantes:** Isaac Esquivel Alfaro · Fabián Campos · Johel Fernández
* **Plataforma y Entorno:** C# (.NET 10.0 `Release` con `TieredPGO`), PostgreSQL Serverless (Neon con respaldo automático en memoria `OfflineFallback`), React + Vite en GitHub Pages (`https://esqui0a0i07.github.io/Ultimo-proyecto-Arquitectura/`)
* **Equipo de Medición:** AMD Ryzen 5 5600H (Microarquitectura Zen 3, 7 nm · $P_{fis} = 6$ núcleos físicos a 3.30–4.20 GHz · $P_{log} = 12$ hilos lógicos SMT · Caché L1d: 32 KB/núcleo, L2: 512 KB/núcleo, L3: 16 MB compartida · 8 GB DDR4 RAM · Windows 11 Home 64-bit)

---

## 1. Resumen Arquitectónico y Flujo de 6 Etapas

El sistema **Radar de Ofertas Multinúcleo** resuelve el problema de agregación, normalización, emparejamiento difuso y monitoreo histórico de precios de videojuegos a través de múltiples tiendas digitales y minoristas físicos (Steam Store, Microsoft Xbox Store, Nintendo eShop y PlayStation / GameStop vía DekuDeals), integrando conversión monetaria en tiempo real con el tipo de cambio oficial del **Banco Central de Costa Rica (BCCR)** y alertas reales vía **API de Telegram** y **Google Apps Script (Correo)**.

Para maximizar el aprovechamiento de la arquitectura multinúcleo sin agotar los 8 GB de memoria RAM del sistema, el motor se estructuró como un **pipeline productor–consumidor segmentado en 6 etapas**, desacopladas mediante colas acotadas asíncronas (`System.Threading.Channels.Channel<T>` con capacidad máxima de 50 elementos y política `BoundedChannelFullMode.Wait` para aplicar contrapresión automática):

1. **Etapa 0 — Descubrimiento (`--discover`):** Construye y sanea el catálogo base de **520 videojuegos reales únicos** con sus plataformas válidas (`PC`, `PlayStation`, `Xbox`, `Nintendo Switch`) e identifica **380 casos difíciles** (títulos con numeración romana, subtítulos extensos, ediciones *Deluxe/GOTY* o franquicias con entregas anuales).
2. **Etapa 1 — Descarga Concurrente de E/S:** Ejecuta las peticiones HTTP de forma asíncrona no bloqueante (`async/await`) gobernadas por dos niveles de semáforos: un semáforo global de etapa (`SemaphoreSlim(W_io)`) que garantiza exactamente $W_{io}$ operaciones en vuelo, y un limitador por dominio (`DomainRateLimiter`) con retroceso exponencial (*exponential backoff*) y *jitter* aleatorio. Soporta **Modo Replay determinista** (`ReplayHttpMessageHandler` a $\ell = 120\text{ ms}$) y **Modo Live real (`--live`)**.
3. **Etapa 2 — Parseo Estructural:** Consume los paquetes crudos desde el primer canal acotado y extrae árboles DOM HTML (`AngleSharp`) y documentos JSON (`System.Text.Json`) para transformar las respuestas de cada tienda en estructuras homogéneas `StoreCandidate`.
4. **Etapa 3 — Emparejamiento Difuso en CPU y Procesamiento de Portadas:** Paraleliza en los núcleos del procesador (`Parallel.ForEach` y `PLINQ` con `MaxDegreeOfParallelism = W_cpu`) la normalización de cadenas, el cálculo de similitud **Jaro–Winkler** implementado desde cero (umbral validado $\theta = 0,88$, banda dudosa $[0,78,\ 0,88)$) y el filtro anti-DLCs. Incluye el módulo extra (`--images`) con `SixLabors.ImageSharp` para redimensionado bicúbico a `400x225` JPEG, cuantización de color dominante RGB y hash perceptual `dHash` de 64 bits con distancia de Hamming.
5. **Etapa 4 — Persistencia Transaccional Resiliente:** Agrupa los registros evaluados en lotes de 50 juegos y los inserta dentro de una única transacción SQL con `Dapper` hacia PostgreSQL en Neon, actualizando precios mínimos históricos y la URL exacta de la carátula (`cover_image_url`). Si un firewall bloquea el puerto saliente `5432` o el nodo serverless interrumpe el *handshake* TLS, conmuta sin pérdida de datos al almacén concurrente en memoria (`OfflineFallback`).
6. **Etapa 5 — Publicación Estática (`--publish-web`):** Genera `web/public/data/catalog.json` y `web/public/data/metrics.json`, consumidos por el frontend en React desplegado mediante GitHub Actions.

---

## 2. Respuestas Fundamentadas a las 8 Preguntas del Informe Técnico

### Pregunta 1: ¿Dónde usamos concurrencia (E/S) y dónde paralelismo (CPU), y por qué no son intercambiables en nuestro pipeline?

* **Concurrencia en E/S (Etapa 1 y Etapa 4):** Se emplea programación asíncrona basada en máquinas de estados (`Task`, `async/await`, `HttpClient` y `SemaphoreSlim`) durante la descarga de páginas/APIs de tiendas y la escritura por red en PostgreSQL. Durante una petición HTTP en Modo Replay ($\ell = 120\text{ ms}$) o en Modo Live, el procesador invierte menos de $0,2\text{ ms}$ emitiendo el paquete TCP/TLS y permanece el $99,8\%$ del tiempo esperando la interrupción de la tarjeta de red (IOCP en Windows). La concurrencia permite mantener $W_{io} = 32$ o $64$ descargas simultáneas en vuelo sin bloquear hilos del sistema operativo.
* **Paralelismo en CPU (Etapa 3 y Procesamiento de Imágenes):** Se emplea paralelismo de datos (`Parallel.ForEach` y `PLINQ`) sobre hilos del `ThreadPool` afinados a los núcleos del Ryzen 5 5600H ($W_{cpu} \in \{1, 2, 4, 6, 12, 24\}$). Aquí los datos ya residen en memoria caché L1/L2/L3 y las unidades aritmético-lógicas (ALU/SIMD) ejecutan millones de comparaciones de caracteres en matrices Jaro–Winkler o convoluciones bicúbicas de píxeles al $100\%$ de utilización.
* **Por qué no son intercambiables:**
  1. Si usáramos hilos bloqueantes de CPU (`Parallel.For` con `Thread.Sleep` o llamadas HTTP síncronas) para la Etapa 1 con $W_{io}=64$, bloquearíamos 64 hilos del sistema operativo (consumiendo $\approx 64\text{ MB}$ solo en pilas de usuario y provocando *thread starvation* en el `ThreadPool`) únicamente para esperar paquetes de red.
  2. Inversamente, si envolviéramos el cálculo Jaro–Winkler en `async/await` sin repartirlo en múltiples núcleos físicos, la sobrecarga de creación de máquinas de estados `IAsyncStateMachine` degradaría el tiempo secuencial y un solo núcleo ejecutaría todo el trabajo, obteniendo un *speedup* de $1,00\text{x}$.

---

### Pregunta 2: ¿Cómo evolucionan el tiempo de ejecución, el speedup ($S_{abs}, S_{rel}$), la eficiencia $E(p)$ y la métrica de Karp–Flatt $e$ al escalar trabajadores en E/S vs CPU?

Las mediciones reales (mediana de 5 repeticiones en modo `Release`) muestran dos regímenes físicos completamente distintos:

#### A. Comportamiento en Etapa 1 (Descarga E/S — 3.500 solicitudes a 120 ms en Modo Replay)
* En ejecución secuencial ($1$ solicitud a la vez), el tiempo medido es **$443,313\text{ s}$** (rango $440,712\text{--}443,950\text{ s}$), muy cercano al piso teórico $3.500 \times 0,12\text{ s} = 420,0\text{ s}$ más la sobrecarga de planificación del temporizador de Windows.
* Al incrementar $W_{io}$ de $1$ a $64$, el tiempo cae de forma inversamente proporcional:
  * $W_{io} = 2 \rightarrow 221,510\text{ s}$ ($S_{abs} = 2,00\text{x},\ E = 1,00,\ e = -0,0012$)
  * $W_{io} = 8 \rightarrow 55,280\text{ s}$ ($S_{abs} = 8,02\text{x},\ E = 1,00,\ e = -0,0004$)
  * $W_{io} = 32 \rightarrow 13,864\text{ s}$ ($S_{abs} = 31,98\text{x},\ S_{rel} = 31,99\text{x},\ E = 1,00,\ e = 0,0000$)
  * $W_{io} = 64 \rightarrow 6,941\text{ s}$ ($S_{abs} = 63,87\text{x},\ S_{rel} = 63,91\text{x},\ E = 1,00,\ e = 0,0000$)
* **Interpretación arquitectónica:** La fracción serial experimental de Karp–Flatt es prácticamente nula ($e \approx 0,0000$) porque los *workers* asíncronos no compiten por las ALUs del procesador; únicamente solapan intervalos de espera en el controlador de E/S.

#### B. Comportamiento en Etapa 3 (Emparejamiento CPU `Parallel.ForEach` — 500 juegos $\times$ 2.500 candidatos)
* El tiempo secuencial base es **$2,938\text{ s}$** y con $p=1$ trabajador en `Parallel.ForEach` es **$2,948\text{ s}$** ($S_{abs} = 1,00\text{x}$).
* Al escalar sobre los núcleos físicos ($P_{fis} = 6$) y lógicos ($P_{log} = 12$):
  * $p = 2 \rightarrow 2,263\text{ s}$ ($S_{abs} = 1,30\text{x},\ S_{rel} = 1,30\text{x},\ E = 0,65,\ e = 0,5349$)
  * $p = 4 \rightarrow 1,766\text{ s}$ ($S_{abs} = 1,66\text{x},\ S_{rel} = 1,67\text{x},\ E = 0,42,\ e = 0,4652$)
  * $p = 6\ (P_{fis}) \rightarrow 1,715\text{ s}$ ($S_{abs} = 1,71\text{x},\ S_{rel} = 1,72\text{x},\ E = 0,29,\ e = 0,4978$)
  * $p = 12\ (P_{log}) \rightarrow 1,661\text{ s}$ ($S_{abs} = 1,77\text{x},\ S_{rel} = 1,77\text{x},\ E = 0,15,\ e = 0,5238$)
  * $p = 24\ (2\times P_{log}) \rightarrow 1,657\text{ s}$ ($S_{abs} = 1,77\text{x},\ S_{rel} = 1,78\text{x},\ E = 0,07,\ e = 0,5431$)
* **Interpretación arquitectónica:** Como la carga total dura menos de $3\text{ s}$ y crea miles de objetos `EvaluatedOffer` y cadenas normalizadas en el *heap* administrado (Gen 0 de `.NET`), el ancho de banda de memoria DDR4 y las recolecciones breves del *Garbage Collector* introducen una fracción no paralelizable efectiva $e \approx 0,47\text{--}0,53$, estabilizando la ganancia máxima alrededor de $1,77\text{--}1,80\text{x}$ al llegar a los 12 hilos lógicos. En cambio, en el procesamiento de imágenes (`CoverImageProcessor`), donde cada hilo trabaja sobre buffers densos con menos presión relativa sobre el asignador de objetos pequeños, el *speedup* sube hasta **$3,85\text{x}$** en $p = 6$ ($e = 0,1116$).

---

### Pregunta 3: ¿Qué diferencia observamos entre escalar hasta los núcleos físicos ($P_{fis} = 6$), hasta los núcleos lógicos SMT ($P_{log} = 12$) y al sobresuscribir ($2\times P_{log} = 24$)?

1. **De $p=1$ a $p=6$ ($P_{fis}$):** Cada hilo adicional se ejecuta en un núcleo físico Zen 3 dedicado con su propia caché L1d (32 KB) y L2 (512 KB). Aquí se obtiene la mayor ganancia marginal: en Etapa 3 el tiempo baja de $2,948\text{ s}$ a $1,715\text{ s}$, y en procesamiento de portadas baja de $1,929\text{ s}$ a **$0,501\text{ s}$** (el punto óptimo absoluto con $S_{abs} = 3,85\text{x}$).
2. **De $p=6$ ($P_{fis}$) a $p=12$ ($P_{log}$ con SMT / Hyper-Threading):** Los hilos $7$ al $12$ comparten los puertos de ejecución, las unidades vectoriales AVX2 y las cachés L1/L2 con los primeros 6 hilos.
   * En **Jaro–Winkler (Etapa 3)**, donde abundan saltos condicionales y fallos de predicción de saltos (*branch mispredictions*) al recorrer cadenas de longitud variable, SMT aprovecha los ciclos estancados del tubería (*pipeline stalls*) y logra una mejora adicional del $3,1\%$ con `Parallel.ForEach` ($1,715\text{ s} \rightarrow 1,661\text{ s}$) y del $4,6\%$ con `PLINQ` ($1,703\text{ s} \rightarrow 1,624\text{ s}$, alcanzando $S_{rel} = 1,81\text{x}$).
   * En **Procesamiento de Portadas (`ImageSharp`)**, el redimensionado bicúbico ya satura las unidades vectoriales SIMD con 6 hilos físicos; al subir a 12 hilos lógicos se produce contención por caché L2/L3 y el tiempo retrocede de $0,501\text{ s}$ a $0,545\text{ s}$ ($S_{abs}$ baja de $3,85\text{x}$ a $3,54\text{x}$).
3. **Sobresuscripción en $p=24$ ($2\times P_{log}$):** Hay el doble de hilos activos que procesadores lógicos disponibles en el hardware. El planificador de Windows debe realizar cambios de contexto (*context switches*) constantes, expulsando líneas calientes de caché L1/L2 y aumentando la métrica de Karp–Flatt a $e = 0,5607$ en `PLINQ` (donde el tiempo empeora de $1,624\text{ s}$ a $1,704\text{ s}$) y a $e = 0,2628$ en imágenes ($0,566\text{ s}$).

---

### Pregunta 4: ¿Qué demostró el experimento obligatorio de contención y qué mecanismo elegimos para las métricas del pipeline?

En `ContentionExperiment.cs` ejecutamos $N = 2.000.000$ de actualizaciones concurrentes repartidas entre $p = 12$ hilos sobre cuatro variantes de sincronización:

| Mecanismo Evaluado | Tiempo Medido | Valor Obtenido / Esperado | Explicación desde la Arquitectura de Computadores |
| :--- | :---: | :---: | :--- |
| **1. Sin sincronización (`unsafeCounter++`)** | `6 ms` | `418.924 / 2.000.000` (**79,1% perdido**) | La operación lectura–modificación–escritura (`MOV`, `INC`, `MOV`) no es atómica. Múltiples núcleos leen el mismo valor desde su caché L1 local antes de que se propague la escritura, sobrescribiendo incrementos ajenos (**condición de carrera**). |
| **2. Exclusión mutua (`lock`)** | `84 ms` | `2.000.000 / 2.000.000` (Correcto) | Garantiza exclusión mutua mediante `Monitor.Enter/Exit`, pero serializa los 12 hilos y provoca suspensiones en el kernel (*futex/wait queues*) cuando la sección crítica dura apenas nanosegundos ($14\text{x}$ más lento). |
| **3. Operación atómica (`Interlocked.Increment`)** | `29 ms` | `2.000.000 / 2.000.000` (Correcto) | Emite la instrucción `LOCK XADD` en x86-64. Evita bloquear hilos en el SO, pero obliga al protocolo de coherencia de caché **MOESI** del Ryzen 5 5600H a invalidar constantemente la línea de caché entre los 6 núcleos (*cache line bouncing*). |
| **4. Estado local por hilo (`Parallel.For` con `localInit/localFinally`)** | **`2 ms`** | `2.000.000 / 2.000.000` (**Óptimo**) | Cada hilo acumula su subtotal en un registro o variable en su propia pila local sin tráfico de coherencia entre núcleos, y realiza **una sola** suma atómica (`Interlocked.Add`) al terminar su partición. |

* **Decisión en el pipeline:** En la Etapa 3 (`PipelineOrchestrator.cs`), cada hilo procesa su juego y construye su lista local `List<EvaluatedOffer>` sin compartir estado mutable durante el bucle Jaro–Winkler, y para los contadores globales de telemetría de baja frecuencia (3.500 peticiones) utilizamos `Interlocked.Increment`.

---

### Pregunta 5: ¿Cómo afectan los límites de concurrencia por dominio (`DomainRateLimiter`) al speedup máximo alcanzable en la Etapa 1?

En el experimento puro de Modo Replay (`--benchmark`), evaluamos el escalamiento de $W_{io}$ sin restricción por tienda para caracterizar el canal asíncrono, alcanzando $S_{abs} = 63,87\text{x}$ con $W_{io}=64$. Sin embargo, al consultar tiendas reales o activar `DomainRateLimiter`, cada fuente $i$ impone un tope de peticiones concurrentes $c_i$ para evitar bloqueos HTTP `429 Too Many Requests`:

$$\sum_{i=1}^{7} c_i = c_{\text{steam}}(6) + c_{\text{xbox}}(4) + c_{\text{eshop}}(6) + c_{\text{gamestop}}(2) + c_{\text{metacritic}}(3) + c_{\text{opencritic}}(4) + c_{\text{hltb}}(3) = 28\text{ permisos máximos}$$

Además, como cada uno de los 500 juegos requiere consultar tanto fuentes rápidas ($c_i = 6$) como la fuente más restrictiva (`gamestop` con $c_{\text{min}} = 2$), el tiempo total de la Etapa 1 queda acotado inferiormente por la tienda con menor cuota de concurrencia:

$$T_{\text{min}}(\text{Etapa 1}) \ge \frac{N_{\text{juegos}} \times \ell}{c_{\text{min}}} = \frac{500 \times 0,12\text{ s}}{2} = 30,0\text{ s} \implies S_{\max} \le \frac{420,0\text{ s}}{30,0\text{ s}} = 14,0\text{x}$$

Esto demuestra que aumentar $W_{io}$ por encima de $\sum c_i = 28$ no reduce el tiempo de descarga cuando los semáforos por dominio están activos, pues los trabajadores adicionales quedan suspendidos esperando permisos del dominio cuello de botella (`gamestop`). En nuestra corrida real **`--live`** (1.340 peticiones efectivas filtradas por las plataformas reales de cada juego y con $W_{io}=12$), el pipeline completo descargó, emparejó y persistió los 520 juegos en **$23,86\text{ s}$**.

---

### Pregunta 6: ¿Cuánto gana el sistema al pasar del Modo 1 (Secuencial) al Modo 2 (Concurrente en E/S) y al Modo 3 (Paralelo Completo con Pipeline Segmentado)?

Al comparar los tres modos de ejecución sobre el pipeline de extremo a extremo en Modo Replay:

1. **Modo 1 — Secuencial ($W_{io}=1, W_{cpu}=1$):** La Etapa 1 tarda $443,31\text{ s}$ y la Etapa 3 tarda $2,94\text{ s}$, sumando un tiempo total de extremo a extremo de **$\approx 446,8\text{ s}$** ($S_{abs} = 1,00\text{x}$). El $99,2\%$ del tiempo se desperdicia esperando latencia de E/S.
2. **Modo 2 — Concurrente en E/S ($W_{io}=32, W_{cpu}=1$):** La Etapa 1 colapsa de $443,31\text{ s}$ a **$13,86\text{ s}$** gracias a la concurrencia asíncrona. Como las etapas están conectadas por canales `Channel<T>`, el único hilo de CPU de la Etapa 3 ($2,94\text{ s}$ de trabajo total) procesa los juegos conforme van llegando desde la red, reduciendo el tiempo total del pipeline a **$\approx 16,8\text{ s}$** ($S_{abs} = 26,6\text{x}$).
3. **Modo 3 — Paralelo Completo ($W_{io}=32, W_{cpu}=12$):** La Etapa 1 ejecuta 32 descargas concurrentes ($13,86\text{ s}$) mientras la Etapa 3 reparte el emparejamiento Jaro–Winkler entre los 12 hilos lógicos ($1,66\text{ s}$ totalmente solapados dentro de la ventana de descarga) y la Etapa 4 persiste los lotes en paralelo. El tiempo total cae a **$14,12\text{ s}$** en Replay ($S_{abs} = 31,64\text{x}$) y a **$23,86\text{ s}$** en extracción real `--live`.

---

### Pregunta 7: ¿Cómo justificamos la elección del umbral $\theta = 0,88$ en Jaro–Winkler y cómo evitamos falsos positivos con DLCs y ediciones especiales?

Construimos un conjunto de validación etiquetado manualmente con **64 pares de títulos reales** (32 positivos verdaderos con variaciones de números romanos, signos de puntuación y sufijos de tienda, y 32 negativos verdaderos formados por secuelas distintas como *Dark Souls II* vs *Dark Souls III*, *remakes* distintos y expansiones/DLCs).

Evaluamos seis umbrales candidatos ($\theta \in \{0,75,\ 0,80,\ 0,85,\ 0,88,\ 0,92,\ 0,95\}$):
* Con $\theta = 0,80$, la exhaustividad (*Recall*) es del $100\%$, pero se cuelan $5$ falsos positivos ($\text{Precisión} = 86,5\%,\ F_1 = 0,928$), lo cual contaminaría la web mostrando el precio de un juego incorrecto.
* Con $\theta = 0,92$, la precisión es del $100\%$, pero la exhaustividad cae al $81,3\%$ ($6$ falsos negativos en títulos con subtítulos de edición).
* En **$\theta = 0,88$** logramos el máximo global con **$F_1 = 0,984$**, **Precisión del $100,0\%$ ($0$ falsos positivos)** y **Exhaustividad del $96,9\%$ ($31/32$ verdaderos positivos)**.

**Reglas de dominio deterministas (`JaroWinklerMatcher.cs`):**
1. **Estandarización de números romanos:** Un tokenizador convierte números romanos aislados (`II` a `XVI`) a arábigos (`2` a `16`) antes de calcular la matriz Jaro, evitando que `"Final Fantasy VII"` y `"Final Fantasy VIII"` superen el umbral por compartir un prefijo largo.
2. **Filtro estricto anti-DLC:** Si el título canónico del catálogo no contiene palabras clave de contenido descargable (`dlc`, `expansion`, `season pass`, `soundtrack`, `artbook`, `upgrade`, `pack`), cualquier candidato de tienda que sí las contenga es rechazado inmediatamente aunque tenga similitud $> 0,90$.
3. **Banda de auditoría $[0,78,\ 0,88)$:** Los candidatos cuyo puntaje cae en este intervalo se marcan con `IsDubious = true` y se registran para inspección en el Panel de Métricas.

---

### Pregunta 8: ¿Cómo funciona nuestro modelo matemático de predicción en vivo (`--predict`) para la defensa oral?

Cuando se invoca `dotnet run --project RadarOfertas.Cli -c Release -- --predict --io-workers <W_io> --cpu-workers <W_cpu> --latency <L> --games <N>`, el módulo `PerformancePredictor.cs` calcula y muestra en consola la estimación **antes** de ejecutar el pipeline descomponiendo el tiempo por etapas:

1. **Predicción de la Etapa 1 (E/S Concurrente):**
   Con $R = 7 \times N$ solicitudes totales y latencia $\ell$ (en segundos):
   $$T_{\text{pred, IO}}(W_{io}, \ell) = \left\lceil \frac{7 \cdot N}{W_{io}} \right\rceil \cdot (\ell + \delta_{\text{sched}})$$
   donde $\delta_{\text{sched}} \approx 0,006\text{ s}$ modela la sobrecarga medida del planificador asíncrono de Windows (`Task.Delay` + apertura de tarea).

2. **Predicción de la Etapa 3 (CPU Paralelo por Ley de Amdahl):**
   A partir del tiempo base medido por juego y la fracción serial empírica $f$ en nuestro Ryzen 5 5600H (incluyendo una penalización $\gamma_{\text{SMT}} = 1,18$ si $W_{cpu} > P_{fis}$ y $\gamma_{\text{over}} = 1,28$ si $W_{cpu} > P_{log}$):
   $$T_{\text{pred, CPU}}(W_{cpu}) = T_{\text{CPU}}(1) \cdot \left( f + \frac{1 - f}{W_{cpu}} \right) \cdot \gamma(W_{cpu})$$

3. **Predicción del Pipeline Segmentado Completo:**
   Dado que las etapas operan en paralelo conectadas por `Channel<T>`, el tiempo total está gobernado por la etapa cuello de botella más el tiempo de llenado/vaciado de la tubería ($T_{\text{drain}} \approx 0,35\text{ s}$):
   $$T_{\text{pred, Total}} = \max\left(T_{\text{pred, IO}}(W_{io}, \ell),\ T_{\text{pred, CPU}}(W_{cpu})\right) + T_{\text{drain}}$$

Al presionar `[ENTER]`, el sistema corre el pipeline real con esos parámetros y reporta en pantalla la desviación porcentual $\left|\frac{T_{\text{real}} - T_{\text{pred}}}{T_{\text{real}}}\right| \times 100\%$, manteniéndose consistentemente por debajo del $8\%$ de error.

---

## 3. Funcionalidades de Valor Agregado e Integración en Producción

1. **Integración en Tiempo Real con el Banco Central de Costa Rica (BCCR):** El cliente React consulta al cargar el tipo de cambio oficial del dólar (compra y venta), lo muestra en la barra superior y calcula automáticamente el equivalente en colones costarricenses (`₡`) en cada tarjeta del catálogo, en la tabla comparativa multi-tienda y en el configurador de alertas.
2. **Asociación Exacta de Carátulas y Precios Reales (`--live`):** Cada tarjeta muestra exclusivamente la carátula oficial extraída de la misma tienda de su oferta (`header.jpg` o `tiny_image` con hash en Steam Store, `SuperHeroArt` en Microsoft DisplayCatalog y `assets.nintendo.com` en Nintendo eShop), garantizando correspondencia 1 a 1 con el enlace de compra.
3. **Sistema de Alertas en Vivo (Telegram Bot API + Google Apps Script + Web Notifications):** Incluye validación de formato de correo electrónico (`/^[^\s@]+@[^\s@]+\.[^\s@]{2,}$/`) y de `Chat ID`, modo de simulación de caída de precios y envío real inmediato tanto al teléfono móvil vía Telegram como a la bandeja de correo electrónico.