using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bomb : MonoBehaviour
{
    public Color[] explosionColors; // Colores posibles para la explosi�n (definidos en Inspector)
    public int explosionCountdown = 3; // Intentos antes de explotar
    public int gridX, gridY; // Posici�n en la cuadr�cula
    public GridManager gridManager; // Referencia al GridManager

    private Color chosenColor; // Color de la explosi�n
    private TextMesh countdownText; // Texto sobre la bomba

    void Start()
    {
        // Seleccionar color aleatorio para la explosi�n
        if (explosionColors != null && explosionColors.Length > 0)
        {
            chosenColor = explosionColors[Random.Range(0, explosionColors.Length)];
        }
        else
        {
            chosenColor = Color.white;
        }
        chosenColor.a = 1.0f;

        // Pintar la casilla donde est� la bomba
        gridManager.SetCellColor(gridX, gridY, chosenColor);

        // Agregar contador visual sobre la bomba
        GameObject textObj = new GameObject("CountdownText");
        textObj.transform.SetParent(transform);
        textObj.transform.localPosition = new Vector3(0, 0.5f, 0);

        countdownText = textObj.AddComponent<TextMesh>();
        countdownText.text = explosionCountdown.ToString();
        countdownText.color = Color.black;
        countdownText.fontSize = 48;
    }

    public void ReduceCountdown()
    {
        explosionCountdown--;
        countdownText.text = explosionCountdown.ToString();

        if (explosionCountdown <= 0)
        {
            Explode();
        }
    }

    void Explode()
    {
        int[] dx = { -2, -1, -1, -1, 0, 0, 0, 0, 1, 1, 1, 2 };
        int[] dy = { 0, -1, 0, 1, -2, -1, 1, 2, -1, 0, 1, 0 };

        for (int i = 0; i < dx.Length; i++)
        {
            int nx = gridX + dx[i];
            int ny = gridY + dy[i];

            if (gridManager.IsWithinBounds(nx, ny))
            {
                GridCell cell = gridManager.GetCell(nx, ny);
                if (cell != null)
                {
                    cell.ChangeColor(chosenColor); // Cambia el color de la celda
                    StartCoroutine(cell.PropagateColorGradually(chosenColor)); // Inicia propagaci�n
                }
            }
        }

        Destroy(gameObject);
    }

}
