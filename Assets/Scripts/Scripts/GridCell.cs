using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridCell : MonoBehaviour
{
    [Header("Cell Configuration")]
    public Color cellColor = Color.white;
    public AudioClip propagationSound;

    [Header("Connected Components")]
    public Portal linkedPortal;
    public Bomb bomb;

    private SpriteRenderer spriteRenderer;
    private AudioSource audioSource;

    private const float NEIGHBOR_DETECTION_DISTANCE = 0.5f;
    private const float CELL_SCALE_MULTIPLIER = 1.25f;
    private const float VICTORY_SCALE_MULTIPLIER = 1.2f;
    private const float ANIMATION_DURATION = 0.17f;
    private const float VICTORY_ANIMATION_DURATION = 0.3f;
    private const float INITIAL_DELAY = 0.2f;
    private const float DELAY_MULTIPLIER = 0.88f;
    private const float MIN_DELAY = 0.02f;
    private const float PITCH_INCREMENT = 0.05f;
    private const float MAX_PITCH = 100.0f;
    private const float INITIAL_PITCH = 1.0f;

    void Start()
    {
        InitializeComponents();
        ApplyInitialColor();
    }

    void OnMouseDown()
    {
        if (ShouldIgnoreInput()) return;

        GridManager gridManager = FindObjectOfType<GridManager>();
        if (CanProcessClick(gridManager))
        {
            ProcessCellClick(gridManager);
        }
    }

    public void SetColor(Color newColor)
    {
        cellColor = newColor;
        spriteRenderer.color = newColor;
    }

    public void ChangeColor(Color newColor)
    {
        UpdateCellColor(newColor);
        NotifyLinkedPortal(newColor);
        StartCoroutine(AnimateCell());
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
        return cellColor;
    }

    public IEnumerator PropagateColorGradually(Color newColor)
    {
        GridManager gridManager = FindObjectOfType<GridManager>();
        if (gridManager != null)
        {
            gridManager.StartPropagation();
        }

        PropagationData propagationData = InitializePropagationData(newColor);

        try
        {
            yield return StartCoroutine(ExecutePropagation(propagationData, gridManager));
        }
        finally
        {
            if (gridManager != null)
            {
                gridManager.EndPropagation();
            }
        }
    }

    public IEnumerator AnimateVictoryEffect()
    {
        Vector3 originalScale = transform.localScale;
        Vector3 targetScale = originalScale * VICTORY_SCALE_MULTIPLIER;

        yield return StartCoroutine(ScaleToTarget(originalScale, targetScale, VICTORY_ANIMATION_DURATION));
        yield return StartCoroutine(ScaleToTarget(targetScale, originalScale, VICTORY_ANIMATION_DURATION));
    }

    private void InitializeComponents()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        audioSource = gameObject.AddComponent<AudioSource>();

        if (spriteRenderer == null)
        {
            Debug.LogError($"No se encontró SpriteRenderer en {name}");
        }
    }

    private void ApplyInitialColor()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = cellColor;
        }
    }

    private bool ShouldIgnoreInput()
    {
        return TutorialManager.isTutorialActive || !GameManager.Instance.IsColorSelected();
    }

    private bool CanProcessClick(GridManager gridManager)
    {
        return gridManager != null && gridManager.remainingAttempts > 0;
    }

    private void ProcessCellClick(GridManager gridManager)
    {
        Color selectedColor = GameManager.Instance.GetSelectedColor();

        if (IsAlreadySelectedColor(selectedColor)) return;

        gridManager.UseAttempt();
        StartCoroutine(PropagateColorGradually(selectedColor));
    }

    private bool IsAlreadySelectedColor(Color selectedColor)
    {
        return cellColor == selectedColor;
    }

    private void UpdateCellColor(Color newColor)
    {
        cellColor = newColor;
        if (spriteRenderer != null)
        {
            spriteRenderer.color = cellColor;
        }
    }

    private void NotifyLinkedPortal(Color newColor)
    {
        if (linkedPortal != null)
        {
            linkedPortal.OnCellColorChanged(newColor);
        }
    }

    private IEnumerator AnimateCell()
    {
        Vector3 originalScale = transform.localScale;
        Vector3 enlargedScale = originalScale * CELL_SCALE_MULTIPLIER;

        yield return StartCoroutine(ScaleToTarget(originalScale, enlargedScale, ANIMATION_DURATION));
        yield return StartCoroutine(ScaleToTarget(enlargedScale, originalScale, ANIMATION_DURATION));
    }

    private IEnumerator ScaleToTarget(Vector3 fromScale, Vector3 toScale, float duration)
    {
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            float progress = elapsedTime / duration;
            transform.localScale = Vector3.Lerp(fromScale, toScale, progress);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        transform.localScale = toScale;
    }

    private PropagationData InitializePropagationData(Color newColor)
    {
        return new PropagationData
        {
            cellsToProcess = new Queue<GridCell>(),
            processedCells = new HashSet<GridCell>(),
            originalColor = cellColor,
            newColor = newColor,
            delay = INITIAL_DELAY,
            pitch = INITIAL_PITCH
        };
    }

    private IEnumerator ExecutePropagation(PropagationData data, GridManager gridManager)
    {
        data.cellsToProcess.Enqueue(this);

        while (data.cellsToProcess.Count > 0)
        {
            GridCell currentCell = data.cellsToProcess.Dequeue();

            if (ShouldSkipCell(currentCell, data)) continue;

            ProcessCurrentCell(currentCell, data, gridManager);
            QueueNeighboringCells(currentCell, data);

            yield return new WaitForSeconds(data.delay);
            UpdatePropagationParameters(data);
        }
    }

    private bool ShouldSkipCell(GridCell currentCell, PropagationData data)
    {
        return currentCell.cellColor != data.originalColor || data.processedCells.Contains(currentCell);
    }

    private void ProcessCurrentCell(GridCell currentCell, PropagationData data, GridManager gridManager)
    {
        currentCell.ChangeColor(data.newColor);
        data.processedCells.Add(currentCell);

        if (currentCell.linkedPortal != null)
        {
            currentCell.linkedPortal.OnCellColorChanged(data.newColor);
        }

        TriggerBombsInAdjacentCells(currentCell, data.newColor, gridManager);
        PlayPropagationSound(data.pitch);
    }

    private void TriggerBombsInAdjacentCells(GridCell currentCell, Color newColor, GridManager gridManager)
    {
        Vector2Int coord = currentCell.GetCellCoordinates();
        int x = coord.x;
        int y = coord.y;

        Vector2Int[] directions = {
            new Vector2Int(0, 1),   // Arriba
            new Vector2Int(0, -1),  // Abajo
            new Vector2Int(-1, 0),  // Izquierda
            new Vector2Int(1, 0)    // Derecha
        };

        foreach (Vector2Int direction in directions)
        {
            int newX = x + direction.x;
            int newY = y + direction.y;

            if (gridManager.IsWithinBounds(newX, newY))
            {
                Bomb adjacentBomb = gridManager.gridCells[newX, newY].bomb;
                if (adjacentBomb != null)
                {
                    adjacentBomb.TriggerExplosion(newColor);
                }
            }
        }
    }

    private void PlayPropagationSound(float pitch)
    {
        if (audioSource != null && propagationSound != null)
        {
            audioSource.pitch = pitch;
            audioSource.PlayOneShot(propagationSound);
        }
    }

    private void QueueNeighboringCells(GridCell currentCell, PropagationData data)
    {
        foreach (GridCell neighbor in GetNeighbors(currentCell))
        {
            if (ShouldQueueNeighbor(neighbor, data))
            {
                data.cellsToProcess.Enqueue(neighbor);
            }
        }
    }

    private bool ShouldQueueNeighbor(GridCell neighbor, PropagationData data)
    {
        return neighbor.cellColor == data.originalColor && !data.processedCells.Contains(neighbor);
    }

    private void UpdatePropagationParameters(PropagationData data)
    {
        data.delay *= DELAY_MULTIPLIER;
        data.delay = Mathf.Max(MIN_DELAY, data.delay);

        data.pitch += PITCH_INCREMENT;
        data.pitch = Mathf.Min(MAX_PITCH, data.pitch);
    }

    private List<GridCell> GetNeighbors(GridCell cell)
    {
        List<GridCell> neighbors = new List<GridCell>();
        Collider2D[] colliders = Physics2D.OverlapCircleAll(
            cell.transform.position,
            NEIGHBOR_DETECTION_DISTANCE,
            LayerMask.GetMask("GridCell")
        );

        foreach (Collider2D collider in colliders)
        {
            GridCell neighbor = collider.GetComponent<GridCell>();
            if (IsValidNeighbor(neighbor))
            {
                neighbors.Add(neighbor);
            }
        }

        return neighbors;
    }

    private bool IsValidNeighbor(GridCell neighbor)
    {
        return neighbor != null && neighbor != this;
    }

    private class PropagationData
    {
        public Queue<GridCell> cellsToProcess;
        public HashSet<GridCell> processedCells;
        public Color originalColor;
        public Color newColor;
        public float delay;
        public float pitch;
    }
}