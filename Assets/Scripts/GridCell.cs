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

        // Notificar al LevelManager para verificar la condición de victoria
        LevelManager levelManager = FindObjectOfType<LevelManager>();
        if (levelManager != null)
        {
            levelManager.CheckVictoryCondition();
        }
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

                yield return new WaitForSeconds(0.1f); // Simular propagación gradual
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