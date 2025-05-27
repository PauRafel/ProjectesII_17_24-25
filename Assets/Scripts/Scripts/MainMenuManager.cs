using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    public void StartGame()
    {
        SceneManager.LoadScene("Level_1");
    }
    public void LevelSelector()
    {
        SceneManager.LoadScene("LevelSelector");
    }

    public void QuitGame()
    {
        Debug.Log("Quiting Game");
        Application.Quit();
    }
}