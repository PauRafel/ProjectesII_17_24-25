using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    [Header("Pause Menu Configuration")]
    public GameObject pauseMenuUI;

    public static bool IsPaused = false;

    private const float NORMAL_TIME_SCALE = 1f;
    private const float PAUSED_TIME_SCALE = 0f;
    private const string MAIN_MENU_SCENE = "MainMenu";
    private const string LEVEL_SELECTOR_SCENE = "LevelSelector";
    private const string LEVEL_PREFIX = "Level_";

    private void Update()
    {
        HandlePauseInput();
    }

    public void Resume()
    {
        SetPauseMenuVisibility(false);
        SetTimeScale(NORMAL_TIME_SCALE);
        SetPauseState(false);
    }

    public void Pause()
    {
        SetPauseMenuVisibility(true);
        SetTimeScale(PAUSED_TIME_SCALE);
        SetPauseState(true);
    }

    public void RestartLevel()
    {
        ResetTimeScale();
        LoadCurrentScene();
    }

    public void LoadMainMenu()
    {
        ResetTimeScale();
        LoadScene(MAIN_MENU_SCENE);
    }

    public void LoadLevelSelector()
    {
        ResetTimeScale();
        LoadScene(LEVEL_SELECTOR_SCENE);
    }

    public void NextLevel()
    {
        ResetTimeScale();
        LoadNextLevelOrMainMenu();
    }

    private void HandlePauseInput()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }
    }

    private void TogglePause()
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

    private void SetPauseMenuVisibility(bool isVisible)
    {
        pauseMenuUI.SetActive(isVisible);
    }

    private void SetTimeScale(float timeScale)
    {
        Time.timeScale = timeScale;
    }

    private void SetPauseState(bool isPaused)
    {
        IsPaused = isPaused;
    }

    private void ResetTimeScale()
    {
        SetTimeScale(NORMAL_TIME_SCALE);
    }

    private void LoadCurrentScene()
    {
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        SceneManager.LoadScene(currentSceneIndex);
    }

    private void LoadScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    private void LoadNextLevelOrMainMenu()
    {
        string nextLevelName = GetNextLevelName();

        if (CanLoadLevel(nextLevelName))
        {
            LoadScene(nextLevelName);
        }
        else
        {
            LoadScene(MAIN_MENU_SCENE);
        }
    }

    private string GetNextLevelName()
    {
        int nextLevelIndex = SceneManager.GetActiveScene().buildIndex + 1;
        return LEVEL_PREFIX + nextLevelIndex;
    }

    private bool CanLoadLevel(string levelName)
    {
        return Application.CanStreamedLevelBeLoaded(levelName);
    }
}