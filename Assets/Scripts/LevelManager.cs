using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq; // Para usar LINQ y simplificar verificaciones

public class LevelManager : MonoBehaviour
{
    public string menuSceneName = "MainMenu"; // Nombre de la escena del menú principal
    public string levelPrefix = "Level_";    // Prefijo común en los nombres de niveles
    private int currentLevelIndex;           // Índice actual del nivel

    public LevelCompleteUI levelCompleteUI;

    public Color targetColor; // El color que debe alcanzar toda la cuadrícula para ganar

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
                    targetColor = Color.blue;
                    break;
                case 4:
                    targetColor = Color.blue;
                    break;
                case 5:
                    targetColor = Color.red;
                    break;
                case 6:
                    targetColor = Color.blue;
                    break;
                case 7:
                    targetColor = Color.yellow;
                    break;
                case 8:
                    targetColor = Color.green;
                    break;
                case 9:
                    targetColor = Color.magenta;
                    break;
                case 10:
                    targetColor = Color.blue;
                    break;
                default:
                    break;
            }
        }
    }


    public bool CheckVictoryCondition()
    {
        // Obtener todas las celdas en la cuadrícula
        GridCell[] allCells = FindObjectsOfType<GridCell>();

        // Comprobar si todas las celdas tienen el mismo color que `targetColor`
        bool allCellsMatch = allCells.All(cell => cell.cellColor == targetColor);

        if (allCellsMatch)
        {
            Debug.Log("¡Nivel completado!");
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
        Debug.Log("¡Nivel completado!");
        FindObjectOfType<LevelTimer>().StopTimer();
        levelCompleteUI.ShowLevelCompletePanel();
    }

}