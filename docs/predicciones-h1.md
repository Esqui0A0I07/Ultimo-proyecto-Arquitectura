# Predicciones de Rendimiento — Hito 1
**Proyecto:** Radar de Ofertas Multinúcleo (IC-3101 Arquitectura de Computadoras)  
**Responsable del área:** Fabián Jesús Campos Santamaría (Rendimiento y experimentación)  
**Equipo:** Isaac Esquivel Alfaro · Fabián Jesús Campos Santamaría · Johel Alonso Fernández Madrigal  
**Fecha:** 15 de noviembre de 2026  

---

## 1. Ficha del Equipo de Medición
* **Procesador:** AMD Ryzen 5 5600H (arquitectura Zen 3, frecuencia base 3.3 GHz / boost hasta 4.2 GHz)
* **Núcleos físicos ($P_{fis}$):** 6 núcleos físicos
* **Núcleos lógicos ($P_{log}$):** 12 hilos lógicos
* **SMT (Simultaneous Multithreading):** Activo (2 hilos por núcleo físico)
* **Memoria RAM:** 8 GB DDR4 (límite crítico que justifica el acotamiento estricto de `Channel<T>` para evitar paginación a disco)
* **Sistema Operativo:** Windows 11 Home Single Language (64 bits)
* **Entorno de Ejecución:** C# sobre .NET 8.0 SDK (compilación en modo `Release`)

---

## 2. Parámetros y Supuestos del Escenario Experimental (Modo Replay)
* **Tamaño del catálogo ($N$):** 500 juegos almacenados en PostgreSQL.
* **Solicitudes por juego ($F$):** 7 fuentes por juego (Steam, Xbox Store, Nintendo eShop, GameStop, Metacritic, OpenCritic y How Long To Beat) = **3.500 solicitudes totales** por corrida.
* **Latencia simulada fija en Modo Replay ($L$):** $120\text{ ms}$ ($0,12\text{ s}$) por solicitud servida localmente.
* **Carga de trabajo en Etapa 3 (Emparejamiento CPU + Imágenes):**
  * Conjunto de candidatos en tiendas: **4.000 candidatos totales** (promedio de 1.000 por tienda capturados en replay).
  * Comparaciones Jaro–Winkler por corrida: $500\text{ juegos} \times 4.000\text{ candidatos} = \mathbf{2.000.000\text{ comparaciones}}$ de cadenas normalizadas, más el cálculo del hash perceptual y color dominante de 500 portadas.
* **Tiempos Base Secuenciales Estimados (1 solo hilo, sin andamiaje paralelo):**
  * **Etapa 1 · Descarga E/S ($T_{io\_sec}$):** $3.500 \times 0,12\text{ s} = \mathbf{420,00\text{ s}}$ ($7\text{ min}$)
  * **Etapa 3 · Emparejamiento CPU + Imágenes ($T_{cpu\_sec}$):** $\mathbf{29,00\text{ s}}$
  * **Etapas 2, 4 y 5 · Parseo HTML ($3,50\text{ s}$) + Persistencia PostgreSQL ($3,50\text{ s}$) + Publicación JSON ($1,00\text{ s}$) ($T_{serial}$):** $\mathbf{8,00\text{ s}}$
  * **Tiempo Total Secuencial del Pipeline ($T_{sec}$):** $420,00 + 29,00 + 8,00 = \mathbf{457,00\text{ s}}$ ($7\text{ min } 37\text{ s}$)
* **Costo del Andamiaje Paralelo ($T_{par}(1)$ vs. $T_{sec}$):** Se estima un sobrecosto del **3,0 %** ($\times 1,03$) al ejecutar con $p = 1$ worker respecto a la versión secuencial pura, producto de la creación de máquinas de estados `async/await`, encolamiento en `Channel<T>`, adquisición de `SemaphoreSlim` y particionamiento en `Parallel.ForEach`.

---

## 3. Fórmulas Utilizadas para la Predicción
* **Speedup Absoluto:** $S_{abs}(p) = \frac{T_{sec}}{T_{par}(p)}$
* **Speedup Relativo:** $S_{rel}(p) = \frac{T_{par}(1)}{T_{par}(p)}$
* **Eficiencia:**