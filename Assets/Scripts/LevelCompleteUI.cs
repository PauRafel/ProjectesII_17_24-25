using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelCompleteUI : MonoBehaviour
{
    public GameObject levelCompletePanel; // Panel de victoria

    void Start()
    {
        levelCompletePanel.SetActive(false); // Asegurarse de que el panel esté oculto al inicio
    }

    public void ShowLevelCompletePanel()
    {
        levelCompletePanel.SetActive(true); // Mostrar el panel al completar el nivel
        Time.timeScale = 0f; // Pausar el juego
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

