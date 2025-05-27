using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bomb : MonoBehaviour
{
    public GridCell linkedCell;
    public bool exploded = false;
    public AudioClip propagationSound;

    private const float INITIAL_EXPLOSION_DELAY = 0.65f;
    private const float INITIAL_PROPAGATION_DELAY = 0.15f;
    private const float DELAY_MULTIPLIER = 0.9f;
    private const float MIN_DELAY = 0.02f;
    private const float PITCH_INCREMENT = 0.05f;
    private const float DESTRUCTION_DELAY = 1.0f;
    private const float INITIAL_PITCH = 1f;

    private Vector2Int gridPosition;
    private GridManager gridMgr;
    private AudioSource audioSource;
    private Animator animator;

    private void Start()
    {
        InitializeComponents();
    }

    public void Initialize(Vector2Int position, GridManager manager)
    {
        SetGridData(position, manager);
        ResetExplosionState();
        SetupLinkedCell();
        SetupAudioSource();
    }

    public void TriggerExplosion(Color colorToUse)
    {
        if (HasAlreadyExploded()) return;

        MarkAsExploded();
        PlayExplosionAnimation();
        StartExplosionPropagation(colorToUse);
    }

    private void InitializeComponents()
    {
        animator = GetComponent<Animator>();
    }

    private void SetGridData(Vector2Int position, GridManager manager)
    {
        gridPosition = position;
        gridMgr = manager;
    }

    private void ResetExplosionState()
    {
        exploded = false;
    }

    private void SetupLinkedCell()
    {
        linkedCell = gridMgr.gridCells[gridPosition.x, gridPosition.y];
    }

    private void SetupAudioSource()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
    }

    private bool HasAlreadyExploded()
    {
        return exploded;
    }

    private void MarkAsExploded()
    {
        exploded = true;
    }

    private void PlayExplosionAnimation()
    {
        animator.SetTrigger("Explosion");
    }

    private void StartExplosionPropagation(Color colorToUse)
    {
        StartCoroutine(PropagateExplosion(colorToUse));
    }

    private IEnumerator PropagateExplosion(Color colorToUse)
    {
        InitializePropagation();
        yield return WaitForExplosionDelay();

        var cellProcessor = CreateCellProcessor();
        var explosionPattern = GetExplosionPattern();

        QueueCellsInPattern(cellProcessor, explosionPattern);
        yield return ProcessQueuedCells(cellProcessor, colorToUse);

        FinalizePropagation();
    }

    private void InitializePropagation()
    {
        gridMgr.StartPropagation();
    }

    private WaitForSeconds WaitForExplosionDelay()
    {
        return new WaitForSeconds(INITIAL_EXPLOSION_DELAY);
    }

    private CellProcessor CreateCellProcessor()
    {
        return new CellProcessor();
    }

    private Vector2Int[] GetExplosionPattern()
    {
        return new Vector2Int[]
        {
            new Vector2Int(0,0),
            new Vector2Int(1,0), new Vector2Int(-1,0), new Vector2Int(0,1), new Vector2Int(0,-1),
            new Vector2Int(1,1), new Vector2Int(1,-1), new Vector2Int(-1,1), new Vector2Int(-1,-1)
        };
    }

    private void QueueCellsInPattern(CellProcessor processor, Vector2Int[] pattern)
    {
        foreach (Vector2Int offset in pattern)
        {
            Vector2Int targetPosition = CalculateTargetPosition(offset);

            if (IsPositionValid(targetPosition))
            {
                GridCell targetCell = GetCellAtPosition(targetPosition);
                processor.TryEnqueueCell(targetCell);
            }
        }
    }

    private Vector2Int CalculateTargetPosition(Vector2Int offset)
    {
        return new Vector2Int(gridPosition.x + offset.x, gridPosition.y + offset.y);
    }

    private bool IsPositionValid(Vector2Int position)
    {
        return gridMgr.IsWithinBounds(position.x, position.y);
    }

    private GridCell GetCellAtPosition(Vector2Int position)
    {
        return gridMgr.gridCells[position.x, position.y];
    }

    private IEnumerator ProcessQueuedCells(CellProcessor processor, Color colorToUse)
    {
        var audioController = new AudioController(audioSource, propagationSound);
        var delayController = new DelayController();

        while (processor.HasCellsToProcess())
        {
            GridCell currentCell = processor.GetNextCell();

            ProcessCell(currentCell, colorToUse);
            TriggerChainExplosions(currentCell, colorToUse);

            audioController.PlayPropagationSound();

            yield return delayController.GetCurrentDelay();
            delayController.UpdateDelay();
            audioController.IncreasePitch();
        }
    }

    private void ProcessCell(GridCell cell, Color colorToUse)
    {
        cell.ChangeColor(colorToUse);
        TriggerBombInCell(cell, colorToUse);
    }

    private void TriggerBombInCell(GridCell cell, Color colorToUse)
    {
        if (CellHasUnexplodedBomb(cell))
        {
            cell.bomb.TriggerExplosion(colorToUse);
        }
    }

    private void TriggerChainExplosions(GridCell currentCell, Color colorToUse)
    {
        Vector2Int currentPosition = currentCell.GetCellCoordinates();
        Vector2Int[] directions = GetCardinalDirections();

        foreach (Vector2Int direction in directions)
        {
            Vector2Int neighborPosition = currentPosition + direction;

            if (IsPositionValid(neighborPosition))
            {
                GridCell neighborCell = GetCellAtPosition(neighborPosition);
                TriggerBombInCell(neighborCell, colorToUse);
            }
        }
    }

    private Vector2Int[] GetCardinalDirections()
    {
        return new Vector2Int[]
        {
            new Vector2Int(0,1),   
            new Vector2Int(0,-1),  
            new Vector2Int(-1,0),  
            new Vector2Int(1,0)    
        };
    }

    private bool CellHasUnexplodedBomb(GridCell cell)
    {
        return cell.bomb != null && !cell.bomb.exploded;
    }

    private void FinalizePropagation()
    {
        gridMgr.EndPropagation();
        CleanupBomb();
    }

    private void CleanupBomb()
    {
        linkedCell.bomb = null;
        Destroy(gameObject, DESTRUCTION_DELAY);
    }

    private class CellProcessor
    {
        private readonly Queue<GridCell> cellsToProcess = new Queue<GridCell>();
        private readonly HashSet<GridCell> processedCells = new HashSet<GridCell>();

        public void TryEnqueueCell(GridCell cell)
        {
            if (!processedCells.Contains(cell))
            {
                cellsToProcess.Enqueue(cell);
            }
        }

        public bool HasCellsToProcess()
        {
            return cellsToProcess.Count > 0;
        }

        public GridCell GetNextCell()
        {
            GridCell cell = cellsToProcess.Dequeue();
            processedCells.Add(cell);
            return cell;
        }
    }

    private class AudioController
    {
        private readonly AudioSource audioSource;
        private readonly AudioClip soundClip;
        private float currentPitch = INITIAL_PITCH;

        public AudioController(AudioSource source, AudioClip clip)
        {
            audioSource = source;
            soundClip = clip;
        }

        public void PlayPropagationSound()
        {
            if (CanPlaySound())
            {
                audioSource.pitch = currentPitch;
                audioSource.PlayOneShot(soundClip);
            }
        }

        public void IncreasePitch()
        {
            currentPitch += PITCH_INCREMENT;
        }

        private bool CanPlaySound()
        {
            return audioSource != null && soundClip != null;
        }
    }

    private class DelayController
    {
        private float currentDelay = INITIAL_PROPAGATION_DELAY;

        public WaitForSeconds GetCurrentDelay()
        {
            return new WaitForSeconds(currentDelay);
        }

        public void UpdateDelay()
        {
            currentDelay *= DELAY_MULTIPLIER;
            currentDelay = Mathf.Max(MIN_DELAY, currentDelay);
        }
    }
}