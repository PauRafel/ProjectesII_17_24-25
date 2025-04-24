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
                case 3: // Yellow --> Saffron
                    targetColor = new Color(0.9608f, 0.7725f, 0.0941f);
                    break;
                case 4: // Red --> IndianRed 
                    targetColor = new Color(0.8510f, 0.3255f, 0.3098f);
                    break;
                case 5: // Green --> Emerald 
                    targetColor = new Color(0.4253f, 0.7490f, 0.5176f);
                    break;
                case 6: // Blue --> TuftsBlue
                    targetColor = new Color(0.2902f, 0.5647f, 0.8863f);
                    break;
                case 7: // Blue --> TuftsBlue
                    targetColor = new Color(0.2902f, 0.5647f, 0.8863f);
                    break;
                case 8: // Yellow --> Saffron
                    targetColor = new Color(0.9608f, 0.7725f, 0.0941f);
                    break;
                case 9: // Green --> Emerald 
                    targetColor = new Color(0.4253f, 0.7490f, 0.5176f);
                    break;
                case 10: // Green --> Emerald 
                    targetColor = new Color(0.4253f, 0.7490f, 0.5176f);
                    break;
                case 11: // Yellow --> Saffron
                    targetColor = new Color(0.9608f, 0.7725f, 0.0941f);
                    break;
                case 12: // Blue --> TuftsBlue
                    targetColor = new Color(0.2902f, 0.5647f, 0.8863f);
                    break;
                case 13: // Red --> IndianRed 
                    targetColor = new Color(0.8510f, 0.3255f, 0.3098f);
                    break;
                case 14: // Red --> IndianRed 
                    targetColor = new Color(0.8510f, 0.3255f, 0.3098f);
                    break;
                case 15: // Blue --> TuftsBlue
                    targetColor = new Color(0.2902f, 0.5647f, 0.8863f);
                    break;
                case 16: // Green --> Emerald 
                    targetColor = new Color(0.4253f, 0.7490f, 0.5176f);
                    break;
                case 17: // Purple --> CyberGrape
                    targetColor = new Color(0.3176f, 0.2275f, 0.4078f);
                    break;
                case 18: // Blue --> MaximumBlue
                    targetColor = new Color(0.3529f, 0.6549f, 0.7255f);
                    break;
                case 19: // Purple --> CyberGrape
                    targetColor = new Color(0.3176f, 0.2275f, 0.4078f);
                    break;
                case 20: // Blue --> MaximumBlue
                    targetColor = new Color(0.3529f, 0.6549f, 0.7255f);
                    break;
                case 21: // Green --> GreenLizard 
                    targetColor = new Color(0.7451f, 0.9804f, 0.3098f);
                    break;
                case 22: // Fuchsia --> FashionFuchsia
                    targetColor = new Color(0.9098f, 0.2000f, 0.6000f);
                    break;
                case 23: // Purple --> CyberGrape
                    targetColor = new Color(0.3176f, 0.2275f, 0.4078f);
                    break;
                case 24: // Fuchsia --> FashionFuchsia
                    targetColor = new Color(0.9098f, 0.2000f, 0.6000f);
                    break;
                case 25: // Fuchsia --> FashionFuchsia
                    targetColor = new Color(0.9098f, 0.2000f, 0.6000f);
                    break;
                case 26: // Blue --> MaximumBlue
                    targetColor = new Color(0.3529f, 0.6549f, 0.7255f);
                    break;
                case 27: // Green --> GreenLizard 
                    targetColor = new Color(0.7451f, 0.9804f, 0.3098f);
                    break;
                case 28: // Green --> GreenLizard 
                    targetColor = new Color(0.7451f, 0.9804f, 0.3098f);
                    break;
                case 29: // Purple --> CyberGrape
                    targetColor = new Color(0.3176f, 0.2275f, 0.4078f);
                    break;
                case 30: // Blue --> MaximumBlue
                    targetColor = new Color(0.3529f, 0.6549f, 0.7255f);
                    break;
                case 31: // Green --> GreenLizard 
                    targetColor = new Color(0.7451f, 0.9804f, 0.3098f);
                    break;
                case 32: // Fuchsia --> FashionFuchsia
                    targetColor = new Color(0.9098f, 0.2000f, 0.6000f);
                    break;
                case 33: // Green --> GreenNCS
                    targetColor = new Color(0.1333f, 0.6353f, 0.4392f);
                    break;
                case 34: // White --> DutchWhite
                    targetColor = new Color(0.9255f, 0.8863f, 0.7765f);
                    break;
                case 35: // Purple --> Amethyst
                    targetColor = new Color(0.6000f, 0.4000f, 0.8000f);
                    break;
                case 36: // Orange --> YellowOrangeColorWheel
                    targetColor = new Color(1.0000f, 0.5686f, 0.0000f);
                    break;
                case 37: // Purple --> Amethyst
                    targetColor = new Color(0.6000f, 0.4000f, 0.8000f);
                    break;
                case 38: // Purple --> Amethyst
                    targetColor = new Color(0.6000f, 0.4000f, 0.8000f);
                    break;
                case 39: // Orange --> YellowOrangeColorWheel
                    targetColor = new Color(1.0000f, 0.5686f, 0.0000f);
                    break;
                case 40: // Green --> GreenNCS
                    targetColor = new Color(0.1333f, 0.6353f, 0.4392f);
                    break;
                case 41: // Green --> GreenNCS
                    targetColor = new Color(0.1333f, 0.6353f, 0.4392f);
                    break;
                case 42: // White --> DutchWhite
                    targetColor = new Color(0.9255f, 0.8863f, 0.7765f);
                    break;
                case 43: // Purple --> Amethyst
                    targetColor = new Color(0.6000f, 0.4000f, 0.8000f);
                    break;
                case 44: // White --> DutchWhite
                    targetColor = new Color(0.9255f, 0.8863f, 0.7765f);
                    break;
                case 45: // Orange --> YellowOrangeColorWheel
                    targetColor = new Color(1.0000f, 0.5686f, 0.0000f);
                    break;
                case 46: // White --> DutchWhite
                    targetColor = new Color(0.9255f, 0.8863f, 0.7765f);
                    break;
                case 47: // Orange --> YellowOrangeColorWheel
                    targetColor = new Color(1.0000f, 0.5686f, 0.0000f);
                    break;
                case 48: // Green --> GreenNCS
                    targetColor = new Color(0.1333f, 0.6353f, 0.4392f);
                    break;
                case 49: // Black --> OxfordBlue
                    targetColor = new Color(0.2549f, 0.2745f, 0.2392f);
                    break;
                case 50: // Purple --> MediumPurple
                    targetColor = new Color(0.6157f, 0.5529f, 0.9451f);
                    break;
                case 51: // Yellow --> MaizeCrayola
                    targetColor = new Color(0.9529f, 0.7882f, 0.3843f);
                    break;
                case 52: // Yellow --> MaizeCrayola
                    targetColor = new Color(0.9529f, 0.7882f, 0.3843f);
                    break;
                case 53: // Yellow --> MaizeCrayola
                    targetColor = new Color(0.9529f, 0.7882f, 0.3843f);
                    break;
                case 54: // Purple --> MediumPurple
                    targetColor = new Color(0.6157f, 0.5529f, 0.9451f);
                    break;
                case 55: // Black --> OxfordBlue
                    targetColor = new Color(0.2549f, 0.2745f, 0.2392f);
                    break;
                case 56: // Red --> VividBurgundy
                    targetColor = new Color(0.6392f, 0.0431f, 0.2157f);
                    break;
                case 57: // Purple --> MediumPurple
                    targetColor = new Color(0.6157f, 0.5529f, 0.9451f);
                    break;
                case 58: // Purple --> MediumPurple
                    targetColor = new Color(0.6157f, 0.5529f, 0.9451f);
                    break;
                case 59: // Black --> OxfordBlue
                    targetColor = new Color(0.2549f, 0.2745f, 0.2392f);
                    break;
                case 60: // Yellow --> MaizeCrayola
                    targetColor = new Color(0.9529f, 0.7882f, 0.3843f);
                    break;
                case 61: // Red --> VividBurgundy
                    targetColor = new Color(0.6392f, 0.0431f, 0.2157f);
                    break;
                case 62: // Red --> VividBurgundy
                    targetColor = new Color(0.6392f, 0.0431f, 0.2157f);
                    break;
                case 63: // Black --> OxfordBlue
                    targetColor = new Color(0.2549f, 0.2745f, 0.2392f);
                    break;
                case 64: // Red --> VividBurgundy
                    targetColor = new Color(0.6392f, 0.0431f, 0.2157f);
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

    private IEnumerator AnimateVictory()
    {
        GridManager gridManager = FindObjectOfType<GridManager>();
        if (gridManager != null)
        {
            yield return StartCoroutine(gridManager.AnimateVictory()); // Espera a que termine la animación
        }

        StartCoroutine(ShowVictoryPanel()); // Luego muestra el panel de victoria
    }


    public void CompleteLevel()
    {
        Debug.Log("¡Nivel completado!");

        if (victorySound != null && audioSource != null)
        {
            audioSource.PlayOneShot(victorySound); // Reproduce el sonido de victoria
        }

        LevelSoundManager.instance.PlayWinSound();

        // Iniciar la animación de victoria antes de mostrar el panel
        StartCoroutine(AnimateVictory());
    }


    private IEnumerator ShowVictoryPanel()
    {
        yield return new WaitForSeconds(0.1f); // Espera 1 segundos
        levelCompletePanel.SetActive(true); // Activa el panel de victoria
    }
}
