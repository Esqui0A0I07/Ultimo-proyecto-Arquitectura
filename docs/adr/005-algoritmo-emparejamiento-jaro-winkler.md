# ADR-005 · Algoritmo Jaro–Winkler propio para el emparejamiento de títulos en CPU · 2026-11-15

## Contexto
Cada tienda publica los títulos con variaciones de formato, puntuación, números romanos y sufijos de edición o plataforma. El sistema necesita una etapa intensiva en CPU que compare cada juego del catálogo contra todos los candidatos de las tiendas mediante un algoritmo implementado por el equipo.

## Alternativas
* **Opción A — Distancia de Levenshtein normalizada:** Calcula el número mínimo de ediciones de caracteres, pero penaliza desproporcionadamente los títulos cuando una tienda añade subtítulos largos o nombres de ediciones al final.
* **Opción B — Similitud Jaccard por tokens:** Evalúa la intersección de palabras sin importar el orden, pero pierde sensibilidad ante abreviaturas o diferencias ortográficas menores.
* **Opción C — Similitud de Jaro–Winkler con preprocesamiento de cadenas:** Pondera favorablemente las coincidencias en el prefijo del título (donde se ubica el nombre principal de la franquicia) y entrega un puntaje continuo en $[0, 1]$.

## Decisión
Implementamos desde cero en C# el algoritmo de **Jaro–Winkler** (con factor de prefijo $p = 0.1$ hasta 4 caracteres) precedido por una normalización que pasa a minúsculas, elimina signos de puntuación, convierte números romanos a arábigos y filtra nombres de plataforma. Se define un umbral inicial de aceptación de $0.88$, marcando como **casos dudosos** los puntajes en $[0.78, 0.88)$ o aquellos candidatos que contengan tokens de edición especial, bundle o DLC.

## Consecuencias
* **Qué se gana:** Alta precisión al distinguir juegos base frente a remasterizaciones, secuelas o DLCs, y una carga computacional ideal para medir `Parallel.ForEach` frente a `PLINQ`.
* **Qué se pierde:** Comparar cada juego contra miles de candidatos incrementa las operaciones de CPU por corrida.
* **Qué se medirá:** La precisión y exhaustividad sobre el conjunto de validación manual de al menos 60 pares (incluyendo los 50 casos difíciles) y el speedup en el barrido de núcleos.

## Estado
Aceptada.