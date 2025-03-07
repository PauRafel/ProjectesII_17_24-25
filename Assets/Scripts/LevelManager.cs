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
                case 1: // Red --> IndianRed
                    targetColor = new Color(0.8510f, 0.3255f, 0.3098f);
                    break;
                case 2: // Yellow --> Saffron
                    targetColor = new Color(0.9608f, 0.7725f, 0.0941f);
                    break;
                case 3: // Blue --> TuftsBlue
                    targetColor = new Color(0.2902f, 0.5647f, 0.8863f);
                    break;
                case 4: // Yellow --> Saffron
                    targetColor = new Color(0.9608f, 0.7725f, 0.0941f);
                    break;
                case 5: // Red --> IndianRed 
                    targetColor = new Color(0.8510f, 0.3255f, 0.3098f);
                    break;
                case 6: // Green --> Emerald 
                    targetColor = new Color(0.4253f, 0.7490f, 0.5176f);
                    break;
                case 7: // Blue --> TuftsBlue
                    targetColor = new Color(0.2902f, 0.5647f, 0.8863f);
                    break;
                case 8: // Green --> Emerald
                    targetColor = new Color(0.4253f, 0.7490f, 0.5176f);
                    break;
                case 9: // DarkGreen --> GoldFusion 
                    targetColor = new Color(0.5098f, 0.4784f, 0.2941f);
                    break;
                case 10: // Beige --> Tan
                    targetColor = new Color(0.8431f, 0.7451f, 0.6078f);
                    break;
                case 11: // DarkGreen --> GoldFusion  
                    targetColor = new Color(0.5098f, 0.4784f, 0.2941f);
                    break;
                case 12: // Red --> Redwood
                    targetColor = new Color(0.6667f, 0.3922f, 0.3529f);
                    break;
                case 13: // Red --> Tan
                    targetColor = new Color(0.8431f, 0.7451f, 0.6078f);
                    break;
                case 14: // Orange --> Fawn
                    targetColor = new Color(0.9137f, 0.6471f, 0.4000f);
                    break;
                case 15: // Orange --> Fawn
                    targetColor = new Color(0.9137f, 0.6471f, 0.4000f);
                    break;
                case 16: // Red --> Redwood
                    targetColor = new Color(0.6667f, 0.3922f, 0.3529f);
                    break;
                case 17: // DarkBlue --> GreenBlue
                    targetColor = new Color(0.1882f, 0.4196f, 0.6745f);
                    break;
                case 18: // LightBlue --> PeriwinkleCrayola
                    targetColor = new Color(0.7725f, 0.8431f, 0.9686f);
                    break;
                case 19: // Grey --> QuickSilver
                    targetColor = new Color(0.6275f, 0.6275f, 0.6275f);
                    break;
                case 20: // LightBlue --> PeriwinkleCrayola
                    targetColor = new Color(0.7725f, 0.8431f, 0.9686f);
                    break;
                case 21: // Brown --> Beaver
                    targetColor = new Color(0.6392f, 0.5059f, 0.4078f);
                    break;
                case 22: // DarkBlue --> GreenBlue
                    targetColor = new Color(0.1882f, 0.4196f, 0.6745f);
                    break;
                case 23: // Brown --> Beaver
                    targetColor = new Color(0.6392f, 0.5059f, 0.4078f);
                    break;
                case 24: // Grey --> QuickSilver
                    targetColor = new Color(0.6275f, 0.6275f, 0.6275f);
                    break;
                case 25: // Blue --> UranianBlue
                    targetColor = new Color(0.7059f, 1.0f, 1.0f);
                    break;
                case 26: // Green --> MintGreen
                    targetColor = new Color(0.5961f, 0.9843f, 0.5961f);
                    break;
                case 27: // Pink --> CherryBlossomPink
                    targetColor = new Color(1.0f, 0.7176f, 0.7725f);
                    break;
                case 28: // Green --> MintGreen
                    targetColor = new Color(0.5961f, 0.9843f, 0.5961f);
                    break;
                case 29: // Yellow --> YellowCrayola
                    targetColor = new Color(0.9882f, 0.9059f, 0.4902f);
                    break;
                case 30: // Yellow --> YellowCrayola
                    targetColor = new Color(0.9882f, 0.9059f, 0.4902f);
                    break;
                case 31: // Blue --> UranianBlue
                    targetColor = new Color(0.7059f, 1.0f, 1.0f);
                    break;
                case 32: // Pink --> CherryBlossomPink
                    targetColor = new Color(1.0f, 0.7176f, 0.7725f);
                    break;
                case 33:
                    // targetColor ;
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
