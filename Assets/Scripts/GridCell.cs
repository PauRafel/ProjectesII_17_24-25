using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridCell : MonoBehaviour
{
    public Color cellColor = Color.white; // Color inicial
    private SpriteRenderer spriteRenderer;

    private const float neighborDetectionDistance = 0.5f; 

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer == null)
        {
            Debug.LogError($"No se encontró SpriteRenderer en {name}");
            return;
        }

        spriteRenderer.color = cellColor; // Aplicar el color inicial
    }

    public void SetColor(Color newColor)
    {
        cellColor = newColor;
        GetComponent<SpriteRenderer>().color = newColor;
    }

    void OnMouseDown()
    {
        // Verificar si quedan intentos
        GridManager gridManager = FindObjectOfType<GridManager>();
        if (gridManager != null && gridManager.remainingAttempts > 0)
        {
            // Obtener el color seleccionado del GameManager
            Color selectedColor = GameManager.Instance.GetSelectedColor();

            // Si ya es del color seleccionado, no hacemos nada
            if (cellColor == selectedColor)
            {
                return;
            }

            // Resta un intento
            gridManager.UseAttempt();

            // Comienza la propagación con retraso
            StartCoroutine(PropagateColorGradually(selectedColor));
        }
    }

    public void ChangeColor(Color newColor)
    {
        cellColor = newColor;
        spriteRenderer.color = cellColor;

        // Ejecutar animación de escala
        StartCoroutine(AnimateCell());
    }



    private IEnumerator AnimateCell()
    {
        Vector3 originalScale = transform.localScale;
        Vector3 enlargedScale = originalScale * 1.25f; // Agranda la celda un 20%

        float duration = 0.17f; // Duración de la animación
        float elapsedTime = 0f;

        // Expandir
        while (elapsedTime < duration)
        {
            transform.localScale = Vector3.Lerp(originalScale, enlargedScale, elapsedTime / duration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        transform.localScale = enlargedScale; // Asegurar que llegue al tamaño final

        elapsedTime = 0f;

        // Volver a tamaño original
        while (elapsedTime < duration)
        {
            transform.localScale = Vector3.Lerp(enlargedScale, originalScale, elapsedTime / duration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        transform.localScale = originalScale; // Asegurar que vuelva al tamaño original
    }

    IEnumerator PropagateColorGradually(Color newColor)
    {
        GridManager gridManager = FindObjectOfType<GridManager>();
        if (gridManager != null)
        {
            gridManager.StartPropagation(); // Iniciar propagación
        }

        Queue<GridCell> cellsToProcess = new Queue<GridCell>();
        HashSet<GridCell> processedCells = new HashSet<GridCell>();
        cellsToProcess.Enqueue(this);

        Color originalColor = cellColor;
        float delay = 0.2f; // Tiempo inicial de espera

        try
        {
            while (cellsToProcess.Count > 0)
            {
                GridCell currentCell = cellsToProcess.Dequeue();

                if (currentCell.cellColor != originalColor || processedCells.Contains(currentCell))
                {
                    continue;
                }

                currentCell.ChangeColor(newColor);
                processedCells.Add(currentCell);

                foreach (GridCell neighbor in GetNeighbors(currentCell))
                {
                    if (neighbor.cellColor == originalColor && !processedCells.Contains(neighbor))
                    {
                        cellsToProcess.Enqueue(neighbor);
                    }
                }

                yield return new WaitForSeconds(delay);

                // Reducimos progresivamente el delay, para que la propagación se acelere
                delay *= 0.88f; // Disminuye el tiempo en un 15% en cada paso
                delay = Mathf.Max(0.02f, delay); // Evita que sea menor a 0.02s
            }
        }
        finally
        {
            if (gridManager != null)
            {
                gridManager.EndPropagation(); // Finalizar propagación
            }
        }
    }

    List<GridCell> GetNeighbors(GridCell cell)
    {
        List<GridCell> neighbors = new List<GridCell>();

        // Buscar vecinos por proximidad
        Collider2D[] colliders = Physics2D.OverlapCircleAll(cell.transform.position, neighborDetectionDistance, LayerMask.GetMask("GridCell"));

        foreach (Collider2D collider in colliders)
        {
            GridCell neighbor = collider.GetComponent<GridCell>();

            // Evitar agregar la celda actual como vecina
            if (neighbor != null && neighbor != this)
            {
                neighbors.Add(neighbor);
            }
        }

        return neighbors;
    }
}