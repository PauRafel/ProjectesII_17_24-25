using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    public Color selectedColor = Color.clear;

    private const string LEVEL_PREFIX = "Level_";
    private const char LEVEL_SEPARATOR = '_';
    private const int EXPECTED_SCENE_NAME_PARTS = 2;

    private void Awake()
    {
        InitializeSingleton();
    }

    public Color GetSelectedColor()
    {
        return selectedColor;
    }

    public void SetSelectedColor(Color newColor)
    {
        selectedColor = newColor;
    }

    public bool IsColorSelected()
    {
        return !IsColorClear();
    }

    public void ClearSelectedColor()
    {
        ResetSelectedColor();
    }

    public void LoadNextLevel()
    {
        string currentSceneName = GetCurrentSceneName();
        int currentLevelNumber = ExtractLevelNumber(currentSceneName);

        if (IsValidLevelNumber(currentLevelNumber))
        {
            LoadLevel(currentLevelNumber + 1);
        }
        else
        {
            LogInvalidSceneNameError();
        }
    }

    private void InitializeSingleton()
    {
        if (IsSingletonUninitialized())
        {
            SetupSingleton();
        }
        else
        {
            DestroyDuplicate();
        }
    }

    private bool IsSingletonUninitialized()
    {
        return Instance == null;
    }

    private void SetupSingleton()
    {
        Instance = this;
        MakePersistent();
        RegisterSceneLoadCallback();
    }

    private void MakePersistent()
    {
        DontDestroyOnLoad(gameObject);
    }

    private void RegisterSceneLoadCallback()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void DestroyDuplicate()
    {
        Destroy(gameObject);
    }

    private bool IsColorClear()
    {
        return selectedColor == Color.clear;
    }

    private void ResetSelectedColor()
    {
        selectedColor = Color.clear;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        HandleSceneLoad();
    }

    private void HandleSceneLoad()
    {
        ClearSelectedColor();
    }

    private string GetCurrentSceneName()
    {
        return SceneManager.GetActiveScene().name;
    }

    private int ExtractLevelNumber(string sceneName)
    {
        string[] sceneNameParts = SplitSceneName(sceneName);

        if (IsValidSceneNameFormat(sceneNameParts))
        {
            return ParseLevelNumber(sceneNameParts[1]);
        }

        return -1;
    }

    private string[] SplitSceneName(string sceneName)
    {
        return sceneName.Split(LEVEL_SEPARATOR);
    }

    private bool IsValidSceneNameFormat(string[] parts)
    {
        return parts.Length == EXPECTED_SCENE_NAME_PARTS;
    }

    private int ParseLevelNumber(string levelNumberString)
    {
        return int.TryParse(levelNumberString, out int levelNumber) ? levelNumber : -1;
    }

    private bool IsValidLevelNumber(int levelNumber)
    {
        return levelNumber > 0;
    }

    private void LoadLevel(int levelNumber)
    {
        string nextLevelName = BuildLevelName(levelNumber);

        if (CanLoadLevel(nextLevelName))
        {
            LoadScene(nextLevelName);
        }
        else
        {
            LogGameCompletedMessage();
        }
    }

    private string BuildLevelName(int levelNumber)
    {
        return $"{LEVEL_PREFIX}{levelNumber}";
    }

    private bool CanLoadLevel(string levelName)
    {
        return Application.CanStreamedLevelBeLoaded(levelName);
    }

    private void LoadScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    private void LogGameCompletedMessage()
    {
        Debug.Log("No hay más niveles disponibles. ¡Juego completado!");
    }

    private void LogInvalidSceneNameError()
    {
        Debug.LogError("El nombre del nivel actual no sigue el formato esperado.");
    }
}