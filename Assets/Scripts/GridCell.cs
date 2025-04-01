using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridCell : MonoBehaviour
{
    public Color cellColor = Color.white; // Color inicial
    private SpriteRenderer spriteRenderer;

    private const float neighborDetectionDistance = 0.5f;

    private AudioSource audioSource;
    public AudioClip propagationSound;

    public Portal linkedPortal;

    public Bomb bomb;  // Nuevo: referencia a una bomba ubicada en esta celda (si la hay)

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        audioSource = gameObject.AddComponent<AudioSource>(); // Agrega un AudioSource dinámicamente

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
        spriteRenderer.color = newColor;
    }

    void OnMouseDown()
    {
        if (TutorialManager.isTutorialActive) return;
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

        // Asignar el color una sola vez usando spriteRenderer, que ya tienes cacheado
        if (spriteRenderer != null)
        {
            spriteRenderer.color = cellColor;
        }

        // Notificar al portal si hay uno
        if (linkedPortal != null)
        {
            linkedPortal.OnCellColorChanged(newColor);
        }

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

    public IEnumerator PropagateColorGradually(Color newColor)
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
        float pitch = 1.0f; // Pitch inicial del sonido

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

                if (currentCell.linkedPortal != null)
                {
                    currentCell.linkedPortal.OnCellColorChanged(newColor);
                }

                Vector2Int coord = currentCell.GetCellCoordinates();
                int x = coord.x;
                int y = coord.y;

                // Arriba
                if (gridManager.IsWithinBounds(x, y + 1) && gridManager.gridCells[x, y + 1].bomb != null)
                    gridManager.gridCells[x, y + 1].bomb.TriggerExplosion(newColor);

                // Abajo
                if (gridManager.IsWithinBounds(x, y - 1) && gridManager.gridCells[x, y - 1].bomb != null)
                    gridManager.gridCells[x, y - 1].bomb.TriggerExplosion(newColor);

                // Izquierda
                if (gridManager.IsWithinBounds(x - 1, y) && gridManager.gridCells[x - 1, y].bomb != null)
                    gridManager.gridCells[x - 1, y].bomb.TriggerExplosion(newColor);

                // Derecha
                if (gridManager.IsWithinBounds(x + 1, y) && gridManager.gridCells[x + 1, y].bomb != null)
                    gridManager.gridCells[x + 1, y].bomb.TriggerExplosion(newColor);



                // **Reproducir sonido con tono progresivo**
                if (audioSource != null && propagationSound != null)
                {
                    audioSource.pitch = pitch; // Aumentar tono
                    audioSource.PlayOneShot(propagationSound);
                }

                foreach (GridCell neighbor in GetNeighbors(currentCell))
                {
                    if (neighbor.cellColor == originalColor && !processedCells.Contains(neighbor))
                    {
                        cellsToProcess.Enqueue(neighbor);
                    }
                }

                yield return new WaitForSeconds(delay);

                // **Aceleración de propagación**
                delay *= 0.88f; // Disminuye el delay en un 12%
                delay = Mathf.Max(0.02f, delay); // Evita que sea menor a 0.02s

                // **Aumentar tono progresivamente**
                pitch += 0.05f; // Sube el tono en cada celda
                pitch = Mathf.Min(100.0f, pitch); // Limita el pitch máximo
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

    public Vector2Int GetCellCoordinates()
    {
        string[] parts = name.Split('_');
        int x = int.Parse(parts[1]);
        int y = int.Parse(parts[2]);
        return new Vector2Int(x, y);
    }

    public Color GetCurrentColor()
    {
        // Devuelve el color actual de la celda (por ejemplo, del SpriteRenderer o de una variable interna)
        return cellColor;
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

    public IEnumerator AnimateVictoryEffect()
    {
        Vector3 originalScale = transform.localScale;
        Vector3 targetScale = originalScale * 1.2f; // Aumenta un 20% el tamaño

        float duration = 0.3f; // Duración de la animación
        float elapsed = 0f;

        // Aumentar tamaño
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            transform.localScale = Vector3.Lerp(originalScale, targetScale, elapsed / duration);
            yield return null;
        }

        elapsed = 0f;

        // Volver al tamaño normal
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            transform.localScale = Vector3.Lerp(targetScale, originalScale, elapsed / duration);
            yield return null;
        }
    }


}