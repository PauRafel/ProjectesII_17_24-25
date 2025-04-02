using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bomb : MonoBehaviour
{
    public GridCell linkedCell;
    private bool exploded = false;
    private Vector2Int gridPosition;
    private GridManager gridMgr;

    public AudioClip propagationSound;
    private AudioSource audioSource;

    public void Initialize(Vector2Int position, GridManager manager)
    {
        gridPosition = position;
        gridMgr = manager;
        exploded = false;
        linkedCell = gridMgr.gridCells[gridPosition.x, gridPosition.y];
        audioSource = gameObject.AddComponent<AudioSource>();
    }

    public void TriggerExplosion(Color colorToUse)
    {
        if (exploded) return;
        exploded = true;

        StartCoroutine(PropagateExplosion(colorToUse));
    }

    private IEnumerator PropagateExplosion(Color colorToUse)
    {
        gridMgr.StartPropagation(); // <--- Inicia propagación de bomba

        Queue<GridCell> cellsToProcess = new Queue<GridCell>();
        HashSet<GridCell> processedCells = new HashSet<GridCell>();

        // Pattern de la bomba
        Vector2Int[] pattern = new Vector2Int[]
        {
            new Vector2Int(0,0),
            new Vector2Int(1,0), new Vector2Int(-1,0), new Vector2Int(0,1), new Vector2Int(0,-1),
            new Vector2Int(1,1), new Vector2Int(1,-1), new Vector2Int(-1,1), new Vector2Int(-1,-1)
        };

        float delay = 0.15f;
        float pitch = 1f;

        foreach (Vector2Int offset in pattern)
        {
            int targetX = gridPosition.x + offset.x;
            int targetY = gridPosition.y + offset.y;

            if (gridMgr.IsWithinBounds(targetX, targetY))
            {
                GridCell targetCell = gridMgr.gridCells[targetX, targetY];
                if (!processedCells.Contains(targetCell))
                {
                    cellsToProcess.Enqueue(targetCell);
                }
            }
        }

        while (cellsToProcess.Count > 0)
        {
            GridCell currentCell = cellsToProcess.Dequeue();

            // Cambiar color con animación y sonido
            currentCell.ChangeColor(colorToUse);
            processedCells.Add(currentCell);

            // Encadenamiento de bombas
            if (currentCell.bomb != null && !currentCell.bomb.exploded)
            {
                currentCell.bomb.TriggerExplosion(colorToUse);
            }

            // Sonido progresivo
            if (audioSource != null && propagationSound != null)
            {
                audioSource.pitch = pitch;
                audioSource.PlayOneShot(propagationSound);
            }

            yield return new WaitForSeconds(delay);
            delay *= 0.9f;
            delay = Mathf.Max(0.02f, delay);
            pitch += 0.05f;
        }
        gridMgr.EndPropagation(); // <--- Termina propagación de bomba

        // Eliminar la bomba tras explotar
        linkedCell.bomb = null;
        Destroy(gameObject);
    }
}
