using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bomb : MonoBehaviour
{
    public GridCell linkedCell;
    private Color initialColor;
    private bool exploded = false;
    private Vector2Int gridPosition;
    private static GridManager gridMgr;

    private void Start()
    {
        if (linkedCell != null)
        {
            initialColor = linkedCell.GetCurrentColor();
            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            if (sr != null)
                sr.color = initialColor;
        }
        else
        {
            Debug.LogWarning("Bomba sin celda vinculada.");
        }
    }

    public void Initialize(Vector2Int position, GridManager manager)
    {
        gridPosition = position;
        gridMgr = manager;
        exploded = false;
    }

    public void TriggerExplosion(Color colorToUse)
    {
        if (exploded) return;  // Evitar doble explosión
        exploded = true;

        int rows = gridMgr.rows;
        int cols = gridMgr.columns;
        int r = gridPosition.x;
        int c = gridPosition.y;

        // Pinta en cruz expandida
        PaintCell(r, c, colorToUse);
        for (int d = 1; d <= 2; d++)
        {
            PaintCell(r + d, c, colorToUse);
            PaintCell(r - d, c, colorToUse);
            PaintCell(r, c + d, colorToUse);
            PaintCell(r, c - d, colorToUse);
        }

        // Desvincular la bomba y eliminarla
        linkedCell.bomb = null;
        Destroy(gameObject); 
    }

    private void PaintCell(int row, int col, Color color)
    {
        if (row < 0 || row >= gridMgr.rows || col < 0 || col >= gridMgr.columns)
            return;

        GridCell cell = gridMgr.gridCells[col, row];
        cell.SetColor(color);

        // Si hay bombas adyacentes, las hacemos explotar en cadena al instante
        if (cell.bomb != null && !cell.bomb.exploded)
        {
            cell.bomb.TriggerExplosion(color);
        }
    }
}
