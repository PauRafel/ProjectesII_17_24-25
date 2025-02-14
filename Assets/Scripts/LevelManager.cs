using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq; // Para usar LINQ y simplificar verificaciones

public class LevelManager : MonoBehaviour
{
    public string menuSceneName = "MainMenu"; // Nombre de la escena del men� principal
    public string levelPrefix = "Level_";    // Prefijo com�n en los nombres de niveles
    private int currentLevelIndex;           // �ndice actual del nivel

    public Color targetColor; // El color que debe alcanzar toda la cuadr�cula para ganar

    void Start()
    {
        string currentSceneName = SceneManager.GetActiveScene().name;

        if (currentSceneName.StartsWith(levelPrefix))
        {
            int.TryParse(currentSceneName.Replace(levelPrefix, ""), out currentLevelIndex);

            // Asignar el color objetivo basado en el nivel
            switch (currentLevelIndex)
            {
                case 1:
                    targetColor = Color.red;
                    break;
                case 2:
                    targetColor = Color.yellow;
                    break;
                case 3:
                    targetColor = Color.yellow;
                    break;
                case 4:
                    targetColor = Color.blue;
                    break;
                case 5:
                    targetColor = Color.red;
                    break;
                case 6:
                    targetColor = Color.green;
                    break;
                case 7:
                    targetColor = Color.blue;
                    break;
                case 8:
                    targetColor = Color.green;
                    break;
                default:
                    break;
            }
        }
    }


    public bool CheckVictoryCondition()
    {
        // Obtener todas las celdas en la cuadr�cula
        GridCell[] allCells = FindObjectsOfType<GridCell>();

        // Comprobar si todas las celdas tienen el mismo color que `targetColor`
        bool allCellsMatch = allCells.All(cell => cell.cellColor == targetColor);

        if (allCellsMatch)
        {
            Debug.Log("�Nivel completado!");
            CompleteLevel();
            return true;
        }
        else
        {
            return false;
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