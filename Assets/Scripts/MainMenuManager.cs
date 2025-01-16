using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    public void StartGame()
    {
        // Cargar el primer nivel
        SceneManager.LoadScene("Level_1");
    }
    public void LevelSelector()
    {
        SceneManager.LoadScene("LevelSelector");
    }

    public void QuitGame()
    {
        // Salir del juego (solo funciona en una build)
        Debug.Log("Quit Game");
        Application.Quit();
    }
}
