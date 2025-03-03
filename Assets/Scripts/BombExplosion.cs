using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bomb : MonoBehaviour
{
    // Colores posibles para la explosión (definidos por el developer en Inspector)
    public Color[] explosionColors;
   
    // Prefab de la explosión (puede ser un sprite animado o un sistema de partículas)
    //public GameObject explosionPrefab;
    
    // Sonido de la explosión
    //public AudioClip explosionSound;
    
    // Retardo antes de explotar (en intentos del jugador)
    public int explosionCountdown = 3;

    // Posición de la bomba en la cuadrícula
    [HideInInspector] public int gridX;
    [HideInInspector] public int gridY;
    
    public GridManager gridManager; // Referencia al GridManager

    private Color chosenColor; // Color elegido aleatoriamente
    private TextMesh countdownText; // Texto del contador sobre la bomba

    void Start()
    {
        // Seleccionar un color aleatorio de la lista
        if (explosionColors != null && explosionColors.Length > 0)
        {
            chosenColor = explosionColors[Random.Range(0, explosionColors.Length)];
        }
        else
        {
            chosenColor = Color.white;
        }

        // Asegurar que el color tenga opacidad total
        chosenColor.a = 1.0f;

        // Pinta la casilla donde está la bomba con el color elegido
        if (gridManager != null)
        {
            gridManager.SetCellColor(gridX, gridY, chosenColor);
        }

        // Agregar un contador visual sobre la bomba
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
        int[] dx = { -2, -1, -1, -1, 0, 0, 0, 0, 1, 1, 1, 2 }; // Coordenadas en X de la explosión
        int[] dy = { 0, -1, 0, 1, -2, -1, 1, 2, -1, 0, 1, 0 }; // Coordenadas en Y de la explosión

        for (int i = 0; i < dx.Length; i++)
        {
            int nx = gridX + dx[i];
            int ny = gridY + dy[i];

            if (gridManager.IsWithinBounds(nx, ny))
            {
                gridManager.SetCellColor(nx, ny, chosenColor);
            }
        }

        Destroy(gameObject);
    }
}

