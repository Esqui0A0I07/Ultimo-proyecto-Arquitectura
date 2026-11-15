# Resultados Experimentales en Modo Replay — Radar de Ofertas Multinúcleo
**Fecha de generación:** 2026-11-15 14:51:16  
**Repeticiones por configuración:** 5 corridas (reportando mediana y rango mínimo–máximo según Sección 12)  

## 1. Ficha del Equipo de Medición
* **Procesador:** AMD Ryzen 5 5600H (Zen 3)
* **Núcleos físicos ($P_{fis}$):** 6
* **Núcleos lógicos ($P_{log}$):** 12 (SMT Activo)
* **Memoria RAM:** 8 GB DDR4
* **Sistema Operativo y Runtime:** Microsoft Windows NT 10.0.26300.0 · .NET 10.0.14
* **Parámetros Replay:** 500 juegos · 3500 solicitudes HTTP simuladas a 120 ms · 2500 candidatos Jaro–Winkler por juego

## 2. Barrido de Workers en Etapa 1: Descarga Concurrente (E/S)
| Workers (p) | Mediana (s) | Mín – máx (s) | S_abs(p) | S_rel(p) | E(p) | e (Karp–Flatt) |
| :--- | :---: | :---: | :---: | :---: | :---: | :---: |
| **Secuencial** | 441,988 | 441,438 – 443,562 | 1,00 | — | — | — |
| **1** | 442,730 | 441,890 – 443,690 | 1,00 | 1,00 | 1,00 | — |
| **2** | 225,560 | 222,620 – 226,420 | 1,96 | 1,96 | 0,98 | 0,0190 |
| **4** | 113,430 | 113,100 – 114,410 | 3,90 | 3,90 | 0,98 | 0,0083 |
| **8** | 56,172 | 55,644 – 56,575 | 7,87 | 7,88 | 0,99 | 0,0021 |
| **16** | 27,708 | 27,608 – 27,782 | 15,95 | 15,98 | 1,00 | 0,0001 |
| **32** | 13,917 | 13,830 – 13,998 | 31,76 | 31,81 | 0,99 | 0,0002 |
| **64** | 6,972 | 6,948 – 7,011 | 63,39 | 63,50 | 0,99 | 0,0001 |

## 3. Barrido de Workers en Etapa 3: Emparejamiento en Paralelo (`Parallel.ForEach`)
| Workers (p) | Mediana (s) | Mín – máx (s) | S_abs(p) | S_rel(p) | E(p) | e (Karp–Flatt) |
| :--- | :---: | :---: | :---: | :---: | :---: | :---: |
| **Secuencial** | 3,935 | 3,874 – 4,552 | 1,00 | — | — | — |
| **1** | 3,893 | 3,867 – 3,977 | 1,01 | 1,00 | 1,00 | — |
| **2** | 2,649 | 2,617 – 2,725 | 1,49 | 1,47 | 0,73 | 0,3609 |
| **4** | 1,922 | 1,914 – 2,044 | 2,05 | 2,03 | 0,51 | 0,3250 |
| **6** | 1,832 | 1,803 – 1,877 | 2,15 | 2,12 | 0,35 | 0,3647 |
| **12** | 1,759 | 1,743 – 1,789 | 2,24 | 2,21 | 0,18 | 0,4021 |
| **24** | 1,806 | 1,791 – 1,816 | 2,18 | 2,16 | 0,09 | 0,4407 |

## 4. Extra (+2 pts): Comparación de Mecanismos en Etapa 3 (`PLINQ`)
| Workers (p) | Mediana (s) | Mín – máx (s) | S_abs(p) | S_rel(p) | E(p) | e (Karp–Flatt) |
| :--- | :---: | :---: | :---: | :---: | :---: | :---: |
| **Secuencial** | 4,069 | 4,000 – 4,202 | 1,00 | — | — | — |
| **1** | 4,568 | 4,189 – 4,699 | 0,89 | 1,00 | 1,00 | — |
| **2** | 3,752 | 3,702 – 3,830 | 1,08 | 1,22 | 0,61 | 0,6427 |
| **4** | 2,587 | 2,526 – 2,714 | 1,57 | 1,77 | 0,44 | 0,4217 |
| **6** | 2,104 | 2,064 – 2,195 | 1,93 | 2,17 | 0,36 | 0,3528 |
| **12** | 1,783 | 1,749 – 1,818 | 2,28 | 2,56 | 0,21 | 0,3350 |
| **24** | 1,796 | 1,783 – 1,857 | 2,27 | 2,54 | 0,11 | 0,3667 |

## 5. Comparación de los 3 Modos del Pipeline Completo (Sección 7)
| Modo de Ejecución | Etapa 1: Descarga (s) | Etapa 3: Emparejar (s) | Tiempo Total Pipeline (s) | S_abs Total |
| :--- | :---: | :---: | :---: | :---: |
| **1. Secuencial (1 hilo)** | 420,00 | 13,92 | **433,92** | **1,00x** |
| **2. Concurrente (W_io=32, W_cpu=1)** | 13,90 | 13,92 | **13,92** | **31,17x** |
| **3. Paralelo Completo (W_io=32, W_cpu=12)** | 13,86 | 13,87 | **13,87** | **31,28x** |

## 6. Experimento Obligatorio de Contención (Sección 8 — 2.000.000 de actualizaciones)
| Mecanismo de Sincronización | Tiempo (ms) | Total Esperado | Total Obtenido | Integridad |
| :--- | :---: | :---: | :---: | :--- |
| **1. Sin Sincronización (Race Condition)** | 94 ms | 2 000 000 | 862 078 | Condición de Carrera (-1 137 922) |
| **2. Lock Global** | 158 ms | 2 000 000 | 2 000 000 | Correcto (Sin pérdida) |
| **3. ConcurrentDictionary** | 194 ms | 2 000 000 | 2 000 000 | Correcto (Sin pérdida) |
| **4. ThreadLocal<T> + Reducción Final** | 39 ms | 2 000 000 | 2 000 000 | Correcto (Sin pérdida) |
