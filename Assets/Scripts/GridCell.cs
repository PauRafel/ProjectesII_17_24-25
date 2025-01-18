using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridCell : MonoBehaviour
{
    public Color cellColor = Color.white; // Color inicial
    private SpriteRenderer spriteRenderer;

    private const float neighborDetectionDistance = 0.86f; // Tamaño de celda (0.85) + Espaciado (0.001)

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

    void OnMouseDown()
    {
        // Obtener el color seleccionado del GameManager
        Color selectedColor = GameManager.Instance.GetSelectedColor();

        // Si ya es del color seleccionado, no hacemos nada
        if (cellColor == selectedColor)
        {
            return;
        }

        // Comienza la propagación con retraso
        StartCoroutine(PropagateColorGradually(selectedColor));
    }

    public void ChangeColor(Color newColor)
    {
        cellColor = newColor;
        spriteRenderer.color = cellColor;

        // Notificar al LevelManager para verificar la condición de victoria
        LevelManager levelManager = FindObjectOfType<LevelManager>();
        if (levelManager != null)
        {
            levelManager.CheckVictoryCondition();
        }
    }

    IEnumerator PropagateColorGradually(Color newColor)
    {
        // Cola para realizar la búsqueda (BFS)
        Queue<GridCell> cellsToProcess = new Queue<GridCell>();
        HashSet<GridCell> processedCells = new HashSet<GridCell>();
        cellsToProcess.Enqueue(this);

        // Color original de la celda actual
        Color originalColor = cellColor;

        while (cellsToProcess.Count > 0)
        {
            GridCell currentCell = cellsToProcess.Dequeue();

            // Evitar procesar celdas que ya fueron cambiadas
            if (currentCell.cellColor != originalColor || processedCells.Contains(currentCell))
            {
                continue;
            }

            // Cambiar el color de la celda actual
            currentCell.ChangeColor(newColor);
            processedCells.Add(currentCell);

            // Agregar vecinos a la cola
            foreach (GridCell neighbor in GetNeighbors(currentCell))
            {
                if (neighbor.cellColor == originalColor && !processedCells.Contains(neighbor))
                {
                    cellsToProcess.Enqueue(neighbor);
                }
            }

            // Esperar un pequeño retraso antes de continuar
            yield return new WaitForSeconds(0.1f);
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