using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement; // Necesario para manejar escenas

public class GameManager : MonoBehaviour
{
    public static GameManager Instance; // Singleton para acceso global
    public Color selectedColor = Color.clear; // Color por defecto

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Mantener GameManager entre escenas
            SceneManager.sceneLoaded += OnSceneLoaded; // Detectar cambio de escena
        }
        else
        {
            Destroy(gameObject);
        }
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
        return selectedColor != Color.clear;
    }

    public void ClearSelectedColor()
    {
        selectedColor = Color.clear;
    }

    // Este método se llama automáticamente cuando se carga una escena nueva
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ClearSelectedColor(); // Resetear color al cargar cada nivel
    }

    // Método para cargar el siguiente nivel
    public void LoadNextLevel()
    {
        string currentSceneName = SceneManager.GetActiveScene().name;
        string[] parts = currentSceneName.Split('_');

        if (parts.Length == 2 && int.TryParse(parts[1], out int levelNumber))
        {
            string nextLevelName = $"Level_{levelNumber + 1}";
            if (Application.CanStreamedLevelBeLoaded(nextLevelName))
            {
                SceneManager.LoadScene(nextLevelName);
            }
            else
            {
                Debug.Log("No hay más niveles disponibles. ¡Juego completado!");
            }
        }
        else
        {
            Debug.LogError("El nombre del nivel actual no sigue el formato esperado.");
        }
    }
}
