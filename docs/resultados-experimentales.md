# Resultados Experimentales en Modo Replay — Radar de Ofertas Multinúcleo
**Fecha de generación:** 2026-11-17 06:49:38  
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
| **Secuencial** | 443,313 | 440,712 – 443,950 | 1,00 | — | — | — |
| **1** | 443,570 | 440,370 – 444,390 | 1,00 | 1,00 | 1,00 | — |
| **2** | 221,510 | 220,290 – 222,430 | 2,00 | 2,00 | 1,00 | -0,0012 |
| **4** | 111,650 | 111,220 – 112,020 | 3,97 | 3,97 | 0,99 | 0,0023 |
| **8** | 55,280 | 55,233 – 55,364 | 8,02 | 8,02 | 1,00 | -0,0004 |
| **16** | 27,730 | 27,658 – 27,777 | 15,99 | 16,00 | 1,00 | 0,0000 |
| **32** | 13,864 | 13,764 – 13,975 | 31,98 | 31,99 | 1,00 | 0,0000 |
| **64** | 6,941 | 6,931 – 6,988 | 63,87 | 63,91 | 1,00 | 0,0000 |

## 3. Barrido de Workers en Etapa 3: Emparejamiento en Paralelo (`Parallel.ForEach`)
| Workers (p) | Mediana (s) | Mín – máx (s) | S_abs(p) | S_rel(p) | E(p) | e (Karp–Flatt) |
| :--- | :---: | :---: | :---: | :---: | :---: | :---: |
| **Secuencial** | 2,938 | 2,905 – 3,482 | 1,00 | — | — | — |
| **1** | 2,948 | 2,914 – 3,008 | 1,00 | 1,00 | 1,00 | — |
| **2** | 2,263 | 2,250 – 2,277 | 1,30 | 1,30 | 0,65 | 0,5349 |
| **4** | 1,766 | 1,762 – 1,774 | 1,66 | 1,67 | 0,42 | 0,4652 |
| **6** | 1,715 | 1,698 – 1,748 | 1,71 | 1,72 | 0,29 | 0,4978 |
| **12** | 1,661 | 1,631 – 1,825 | 1,77 | 1,77 | 0,15 | 0,5238 |
| **24** | 1,657 | 1,642 – 1,705 | 1,77 | 1,78 | 0,07 | 0,5431 |

## 4. Extra (+2 pts): Comparación de Mecanismos en Etapa 3 (`PLINQ`)
| Workers (p) | Mediana (s) | Mín – máx (s) | S_abs(p) | S_rel(p) | E(p) | e (Karp–Flatt) |
| :--- | :---: | :---: | :---: | :---: | :---: | :---: |
| **Secuencial** | 2,922 | 2,909 – 2,932 | 1,00 | — | — | — |
| **1** | 2,943 | 2,920 – 2,956 | 0,99 | 1,00 | 1,00 | — |
| **2** | 2,272 | 2,249 – 2,287 | 1,29 | 1,30 | 0,65 | 0,5440 |
| **4** | 1,791 | 1,780 – 1,817 | 1,63 | 1,64 | 0,41 | 0,4778 |
| **6** | 1,703 | 1,699 – 1,733 | 1,72 | 1,73 | 0,29 | 0,4944 |
| **12** | 1,624 | 1,614 – 1,762 | 1,80 | 1,81 | 0,15 | 0,5111 |
| **24** | 1,704 | 1,700 – 1,762 | 1,71 | 1,73 | 0,07 | 0,5607 |

## 5. Comparación de los 3 Modos del Pipeline Completo (Sección 7)
| Modo de Ejecución | Etapa 1: Descarga (s) | Etapa 3: Emparejar (s) | Tiempo Total Pipeline (s) | S_abs Total |
| :--- | :---: | :---: | :---: | :---: |
| **1. Secuencial (1 hilo)** | 420,00 | 13,91 | **433,91** | **1,00x** |
| **2. Concurrente (W_io=32, W_cpu=1)** | 13,90 | 13,91 | **13,91** | **31,19x** |
| **3. Paralelo Completo (W_io=32, W_cpu=12)** | 13,87 | 13,88 | **13,88** | **31,27x** |

## 6. Experimento Obligatorio de Contención (Sección 8 — 2.000.000 de actualizaciones)
| Mecanismo de Sincronización | Tiempo (ms) | Total Esperado | Total Obtenido | Integridad |
| :--- | :---: | :---: | :---: | :--- |
| **1. Sin Sincronización (Race Condition)** | 86 ms | 2 000 000 | 811 630 | Condición de Carrera (-1 188 370) |
| **2. Lock Global** | 177 ms | 2 000 000 | 2 000 000 | Correcto (Sin pérdida) |
| **3. ConcurrentDictionary** | 217 ms | 2 000 000 | 2 000 000 | Correcto (Sin pérdida) |
| **4. ThreadLocal<T> + Reducción Final** | 10 ms | 2 000 000 | 2 000 000 | Correcto (Sin pérdida) |

## 7. Validación del Umbral de Emparejamiento Jaro–Winkler (64 Pares Manuales — Sección 6)
| Umbral | VP (TP) | FP | VN (TN) | FN | Precisión | Exhaustividad (Recall) | F1-Score |
| :---: | :---: | :---: | :---: | :---: | :---: | :---: | :---: |
| 0,75 | 32 | 22 | 10 | 0 | 59,3% | 100,0% | 0,744 |
| 0,80 | 32 | 21 | 11 | 0 | 60,4% | 100,0% | 0,753 |
| 0,85 | 32 | 19 | 13 | 0 | 62,7% | 100,0% | 0,771 |
| **0,88** | 32 | 14 | 18 | 0 | **69,6%** | **100,0%** | **0,821** |
| 0,92 | 31 | 7 | 25 | 1 | 81,6% | 96,9% | 0,886 |
| 0,95 | 27 | 3 | 29 | 5 | 90,0% | 84,4% | 0,871 |

