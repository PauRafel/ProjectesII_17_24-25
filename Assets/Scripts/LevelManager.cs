using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelManager : MonoBehaviour
{
    public string menuSceneName = "MainMenu"; // Nombre de la escena del men� principal
    public string levelPrefix = "Level_";    // Prefijo com�n en los nombres de niveles
    private int currentLevelIndex;           // �ndice actual del nivel

    void Start()
    {
        // Obtener el �ndice del nivel actual a partir del nombre de la escena
        string currentSceneName = SceneManager.GetActiveScene().name;

        if (currentSceneName.StartsWith(levelPrefix))
        {
            int.TryParse(currentSceneName.Replace(levelPrefix, ""), out currentLevelIndex);
        }
    }

    public void CompleteLevel()
    {
        // Intentar cargar el siguiente nivel
        string nextLevelName = $"{levelPrefix}{currentLevelIndex + 1}";

        if (Application.CanStreamedLevelBeLoaded(nextLevelName))
        {
            SceneManager.LoadScene(nextLevelName);
        }
        else
        {
            // Si no hay m�s niveles, volver al men� principal
            SceneManager.LoadScene(menuSceneName);
        }
    }
}
