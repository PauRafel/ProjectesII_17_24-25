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
    public GameObject levelCompletePanel; // Panel de victoria

    public Color targetColor; // El color que debe alcanzar toda la cuadrícula para ganar

    public AudioClip victorySound; // Asigna el sonido desde el Inspector
    private AudioSource audioSource;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();

        string currentSceneName = SceneManager.GetActiveScene().name;

        if (currentSceneName.StartsWith(levelPrefix))
        {
            int.TryParse(currentSceneName.Replace(levelPrefix, ""), out currentLevelIndex);

            // Asignar el color objetivo basado en el nivel
            switch (currentLevelIndex)
            {
                case 1: // Red
                    targetColor = new Color(0.8510f, 0.3255f, 0.3098f);
                    break;
                case 2: // Yellow
                    targetColor = new Color(0.9608f, 0.7725f, 0.0941f);
                    break;
                case 3: // Blue
                    targetColor = new Color(0.2902f, 0.5647f, 0.8863f);
                    break;
                case 4: // Magenta
                    targetColor = new Color(0.7294f, 0.3333f, 0.8275f);
                    break;
                case 5: // Blue
                    targetColor = new Color(0.2902f, 0.5647f, 0.8863f);
                    break;
                case 6: // Red
                    targetColor = new Color(0.8510f, 0.3255f, 0.3098f); 
                    break;
                case 7:
                    targetColor = Color.yellow;
                    break;
                case 8: // Blue
                    targetColor = new Color(0.2902f, 0.5647f, 0.8863f);
                    break;
                case 9:
                    targetColor = Color.blue;
                    break;
                case 10: // Green --> RaisinBlack
                    targetColor = new Color(0.1176f, 0.1176f, 0.1412f);
                    break;
                case 11: // Green --> Keppel
                    targetColor = new Color(0.2314f, 0.6627f, 0.6118f);
                    break;
                case 12: // Red --> QueenPink
                    targetColor = new Color(0.9255f, 0.7961f, 0.8510f);
                    break;
                case 25: // Yellow --> SafetyYellow
                    targetColor = new Color(0.9451f, 0.8275f, 0.0078f);
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

        if (victorySound != null && audioSource != null)
        {
            audioSource.PlayOneShot(victorySound); // Reproduce el sonido de victoria
        }

        StartCoroutine(ShowVictoryPanel());
    }

    private IEnumerator ShowVictoryPanel()
    {
        yield return new WaitForSeconds(2f); // Espera 2 segundos
        levelCompletePanel.SetActive(true); // Activa el panel de victoria
    }
}