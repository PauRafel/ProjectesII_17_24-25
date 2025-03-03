using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bomb : MonoBehaviour
{
    // Colores posibles para la explosi�n (definidos por el developer en Inspector)
    public Color[] explosionColors;
   
    // Prefab de la explosi�n (puede ser un sprite animado o un sistema de part�culas)
    //public GameObject explosionPrefab;
    
    // Sonido de la explosi�n
    //public AudioClip explosionSound;
    
    // Retardo antes de explotar (en intentos del jugador)
    public int explosionCountdown = 3;

    // Posici�n de la bomba en la cuadr�cula
    [HideInInspector] public int gridX;
    [HideInInspector] public int gridY;
    
    public GridManager gridManager; // Referencia al GridManager

    private Color chosenColor; // Color elegido aleatoriamente
    private TextMesh countdownText; // Texto del contador sobre la bomba

    void Start()
    {
        // Elegir color aleatorio de la lista proporcionada
        if (explosionColors != null && explosionColors.Length > 0)
        {
            int idx = Random.Range(0, explosionColors.Length);
            chosenColor = explosionColors[idx];
        }
        else
        {
            chosenColor = Color.white; // Color por defecto si no hay lista
        }

        // Aplicar el color elegido a la celda donde est� la bomba
        if (gridManager != null)
        {
            gridManager.SetCellColor(gridX, gridY, chosenColor);
        }

        // Configurar el contador de intentos
        GameObject textObj = new GameObject("CountdownText");
        textObj.transform.SetParent(transform);
        textObj.transform.localPosition = new Vector3(0, 0.5f, 0);

        countdownText = textObj.AddComponent<TextMesh>();
        countdownText.text = explosionCountdown.ToString();
        countdownText.color = chosenColor;
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
        /* 1. Crear el efecto visual de explosi�n
        if (explosionPrefab != null)
        {
            GameObject explosionObj = Instantiate(explosionPrefab, transform.position, Quaternion.identity);
            ParticleSystem ps = explosionObj.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                var mainModule = ps.main;
                mainModule.startColor = chosenColor;
                ps.Play();
            }
        }*/

        /* 2. Reproducir sonido de explosi�n
        if (explosionSound != null)
        {
            AudioSource.PlayClipAtPoint(explosionSound, transform.position);
        }*/

        // 3. Aplicar efectos en la cuadr�cula (destruir/afectar casillas en el radio de la explosi�n)
        int radius = 2;
        for (int dx = -radius; dx <= radius; dx++)
        {
            for (int dy = -radius; dy <= radius; dy++)
            {
                int nx = gridX + dx;
                int ny = gridY + dy;
                // Verificar que (nx, ny) est� dentro de los l�mites del grid
                if (gridManager != null && gridManager.IsWithinBounds(nx, ny))
                {
                    gridManager.DestroyCell(nx, ny, chosenColor);
                }
            }
        }

        // 4. Destruir la bomba
        Destroy(gameObject);
    }
}

