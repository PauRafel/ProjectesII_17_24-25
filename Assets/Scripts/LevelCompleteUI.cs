using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelCompleteUI : MonoBehaviour
{
    public GameObject levelCompletePanel; // Panel de victoria
    public StarDisplay starDisplay; // Referencia a StarDisplay
    public int minMovesRequired; // Movimientos mínimos necesarios para completar el nivel
    public int extraMovesAllowed = 2; // Intentos extra permitidos

    private int totalMovesUsed;

    void Start()
    {
            if (starDisplay == null)
            {
            levelCompletePanel.SetActive(false); // Asegura que el panel no aparezca al inicio
            starDisplay = FindObjectOfType<StarDisplay>();
                if (starDisplay == null)
                {
                    Debug.LogError("StarDisplay sigue sin encontrarse en la escena.");
                }
                else
                {
                    Debug.Log("StarDisplay asignado correctamente.");
                }
            }
    }

    public void ShowLevelCompletePanel(int movesUsed)
    {
        levelCompletePanel.SetActive(true); // Activar el panel

        if (starDisplay == null)
        {
            starDisplay = FindObjectOfType<StarDisplay>();
            if (starDisplay == null)
            {
                Debug.LogError("StarDisplay no encontrado después de activar el panel.");
                return; // Salimos si no hay starDisplay
            }
        }

        // Verificar cuántos movimientos sobraron
        int movesRemaining = (extraMovesAllowed + minMovesRequired) - movesUsed;

        // Asegurar que movesRemaining no sea negativo
        movesRemaining = Mathf.Max(movesRemaining, 0);

        // Calcular las estrellas
        int starsEarned = 1; // Por defecto, al menos 1 estrella

        if (movesRemaining >= 2)
        {
            starsEarned = 3; // 3 estrellas si quedan 2 intentos o más
        }
        else if (movesRemaining == 1)
        {
            starsEarned = 2; // 2 estrellas si queda 1 intento
        }

        Debug.Log($"Movimientos usados: {movesUsed}, Movimientos restantes: {movesRemaining}, Estrellas ganadas: {starsEarned}");

        // Aplicamos el resultado al StarDisplay
        starDisplay.SetStars(starsEarned);
    }





    private int CalculateStars(int movesUsed)
    {
        int maxMoves = minMovesRequired + extraMovesAllowed;

        if (movesUsed <= minMovesRequired) return 3;
        if (movesUsed == minMovesRequired + 1) return 2;
        if (movesUsed >= maxMoves) return 1;

        return 1; // Valor de seguridad
    }

    public void NextLevel()
    {
        Time.timeScale = 1f;
        string nextLevelName = "Level_" + (SceneManager.GetActiveScene().buildIndex + 1);
        if (Application.CanStreamedLevelBeLoaded(nextLevelName))
        {
            SceneManager.LoadScene(nextLevelName);
        }
        else
        {
            SceneManager.LoadScene("MainMenu");
        }
    }

    public void RestartLevel()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void LoadMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }

    public void LoadLevelSelector()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("LevelSelector");
    }
}
