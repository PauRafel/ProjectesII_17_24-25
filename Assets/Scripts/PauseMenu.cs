using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    public static bool IsPaused = false; // Estado del juego
    public GameObject pauseMenuUI; // Asignar el Canvas del menú de pausa

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) // Pausa con la tecla Esc
        {
            if (IsPaused)
            {
                Resume();
            }
            else
            {
                Pause();
            }
        }
    }

    public void Resume()
    {
        pauseMenuUI.SetActive(false); // Ocultar el menú
        Time.timeScale = 1f; // Reanudar el tiempo del juego
        IsPaused = false;
    }

    public void Pause()
    {
        pauseMenuUI.SetActive(true); // Mostrar el menú
        Time.timeScale = 0f; // Detener el tiempo del juego
        IsPaused = true;
    }

    public void RestartLevel()
    {
        Time.timeScale = 1f; // Asegurarse de reanudar el tiempo
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void LoadMainMenu()
    {
        Time.timeScale = 1f; // Asegurarse de reanudar el tiempo
        SceneManager.LoadScene("MainMenu"); // Cambiar a tu escena de menú principal
    }
    public void LoadLevelSelector()
    {
        Time.timeScale = 1f; // Asegurarse de reanudar el tiempo
        SceneManager.LoadScene("LevelSelector"); // Cambiar a tu escena de menú principal
    }
}