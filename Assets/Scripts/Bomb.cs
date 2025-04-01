using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bomb : MonoBehaviour
{
    public GridCell linkedCell;
    private bool exploded = false;
    private Vector2Int gridPosition;
    private GridManager gridMgr;

    public void Initialize(Vector2Int position, GridManager manager)
    {
        gridPosition = position;
        gridMgr = manager;
        exploded = false;
        linkedCell = gridMgr.gridCells[gridPosition.x, gridPosition.y];
    }

    public void TriggerExplosion(Color colorToUse)
    {
        if (exploded) return; // Evitar doble explosión
        exploded = true;

        // Coordenadas relativas según el patrón solicitado
        Vector2Int[] pattern = new Vector2Int[]
        {
            new Vector2Int(0,0),
            new Vector2Int(1,0), new Vector2Int(-1,0), new Vector2Int(0,1), new Vector2Int(0,-1),
            //new Vector2Int(2,0), new Vector2Int(-2,0), new Vector2Int(0,2), new Vector2Int(0,-2),
            new Vector2Int(1,1), new Vector2Int(1,-1), new Vector2Int(-1,1), new Vector2Int(-1,-1)
        };

        // Pinta las casillas siguiendo el patrón
        foreach (Vector2Int offset in pattern)
        {
            int targetX = gridPosition.x + offset.x;
            int targetY = gridPosition.y + offset.y;

            if (gridMgr.IsWithinBounds(targetY, targetX))
            {
                GridCell targetCell = gridMgr.gridCells[targetY, targetX];
                targetCell.SetColor(colorToUse);

                // Encadenamiento de bombas
                if (targetCell.bomb != null && !targetCell.bomb.exploded)
                {
                    targetCell.bomb.TriggerExplosion(colorToUse);
                }
            }
        }

        // Eliminar la bomba tras explotar
        linkedCell.bomb = null;
        Destroy(gameObject);
    }
}
