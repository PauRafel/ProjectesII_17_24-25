using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class GridManager : MonoBehaviour
{
    public GameObject cellPrefab;
    public int rows = 8;
    public int columns = 10;
    public float cellSpacing = 0.001f;
    public Color[] levelColors; // Colores personalizados para el nivel actual

    public int maxAttempts = 10; // Número máximo de intentos por nivel
    public int remainingAttempts;
    public TextMeshProUGUI attemptsText; // Referencia al texto del contador de intentos
    private int activePropagations = 0; // Contador de propagaciones activas

    private bool isCheckingRestart = false;

    void Start()
    {
        string levelName = SceneManager.GetActiveScene().name; // Obtiene el nombre de la escena activa
        SetLevelColors(levelName); // Asigna los colores en función del nombre del nivel
        GenerateGrid(); // Genera la cuadrícula

        remainingAttempts = maxAttempts; // Inicializar los intentos restantes
        UpdateAttemptsUI(); // Actualizar el texto del UI
    }

    public void UseAttempt()
    {
        if (remainingAttempts > 0)
        {
            remainingAttempts--;
            UpdateAttemptsUI();

            if (remainingAttempts <= 0)
            {
                StartCoroutine(CheckAndRestartLevel());
            }
        }
    }


    public IEnumerator WaitForPropagations()
    {
        // Keep checking until all propagations are done
        while (activePropagations > 0)
        {
            Debug.Log($"[WaitForPropagations] Current active propagations: {activePropagations}");
            yield return new WaitForSeconds(0.1f); // Check every 0.1 seconds
        }
        Debug.Log("[WaitForPropagations] All propagations finished!");
    }
   
    IEnumerator CheckAndRestartLevel()
    {
        if (isCheckingRestart)
        {
            yield break;
        }

        isCheckingRestart = true;

        LevelManager levelManager = FindObjectOfType<LevelManager>();
        if (levelManager != null)
        {
            bool victory = levelManager.CheckVictoryCondition();

            if (!victory)
            {
                Debug.Log("[CheckAndRestartLevel] Starting to wait for propagations");

                // Wait for all propagations using our new function
                yield return StartCoroutine(WaitForPropagations());

                
                Debug.Log("[CheckAndRestartLevel] Starting countdown for restart");
                yield return new WaitForSeconds(2f);

                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            }
            else
            {
                Debug.Log("Level completed!");
            }
        }

        isCheckingRestart = false;
    }


    // Actualiza el texto del contador de intentos
    void UpdateAttemptsUI()
    {
        if (attemptsText != null)
        {
            attemptsText.text = $"Intentos restantes: {remainingAttempts}";
        }
    }

    public void StartPropagation()
    {
        activePropagations++;
        Debug.Log($"[StartPropagation] Started new propagation. Total active: {activePropagations}");
    }

    public void EndPropagation()
    {
        activePropagations = Mathf.Max(0, activePropagations - 1);
        Debug.Log($"[EndPropagation] Ended propagation. Remaining active: {activePropagations}");
    }




    void GenerateGrid()
    {
        if (levelColors == null || levelColors.Length != rows * columns)
        {
            Debug.LogError("El array de colores no coincide con la cantidad de celdas.");
            return;
        }

        float offsetX = (columns - 1) / 2f;
        float offsetY = (rows - 1) / 2f;

        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < columns; x++)
            {
                Vector3 position = new Vector3(x - offsetX + (x * cellSpacing), -(y - offsetY) - (y * cellSpacing), 0);
                GameObject cell = Instantiate(cellPrefab, position, Quaternion.identity);
                cell.name = $"Cell_{x}_{y}";
                cell.transform.parent = transform;

                int index = y * columns + x;

                // Asignar color a la celda
                Color cellColor = levelColors[index];
                GridCell gridCell = cell.GetComponent<GridCell>();
                if (gridCell != null)
                {
                    gridCell.cellColor = cellColor; // Asignar color inicial
                }

                // Instanciar un material único para cada celda
                SpriteRenderer renderer = cell.GetComponent<SpriteRenderer>();
                if (renderer != null)
                {
                    renderer.material = new Material(renderer.material); // Clonar el material
                    renderer.color = cellColor; // Aplicar el color
                }
            }
        }
    }

    void SetLevelColors(string levelName)
    {
        // Asigna colores según el nombre del nivel
        switch (levelName)
        {
            case "Level_1":
                levelColors = new Color[]
                {
                    Color.red, Color.red, Color.red, Color.red, Color.red, Color.red, Color.red, Color.red, Color.red, Color.red,
                    Color.red, Color.yellow, Color.yellow, Color.yellow, Color.yellow, Color.yellow, Color.yellow, Color.yellow, Color.yellow, Color.red,
                    Color.red, Color.red, Color.red, Color.red, Color.red, Color.red, Color.red, Color.red, Color.yellow, Color.red,
                    Color.red, Color.yellow, Color.yellow, Color.yellow, Color.yellow, Color.yellow, Color.yellow, Color.red, Color.yellow, Color.red,
                    Color.red, Color.yellow, Color.red, Color.red, Color.red, Color.red, Color.yellow, Color.red, Color.yellow, Color.red,
                    Color.red, Color.yellow, Color.red, Color.red, Color.red, Color.red, Color.red, Color.red, Color.yellow, Color.red,
                    Color.red, Color.yellow, Color.yellow, Color.yellow, Color.yellow, Color.yellow, Color.yellow, Color.yellow, Color.yellow, Color.red,
                    Color.red, Color.red, Color.red, Color.red, Color.red, Color.red, Color.red, Color.red, Color.red, Color.red
                };
                break;

            case "Level_2":
                levelColors = new Color[]
                {
                    Color.red, Color.red, Color.red, Color.red, Color.red, Color.red, Color.red, Color.red, Color.red, Color.red,
                    Color.red, Color.yellow, Color.yellow, Color.yellow, Color.yellow, Color.yellow, Color.yellow, Color.yellow, Color.yellow, Color.red,
                    Color.red, Color.red, Color.red, Color.red, Color.red, Color.red, Color.red, Color.red, Color.yellow, Color.red,
                    Color.red, Color.yellow, Color.yellow, Color.yellow, Color.yellow, Color.yellow, Color.yellow, Color.red, Color.yellow, Color.red,
                    Color.red, Color.yellow, Color.red, Color.red, Color.red, Color.red, Color.yellow, Color.red, Color.yellow, Color.red,
                    Color.red, Color.yellow, Color.red, Color.red, Color.red, Color.red, Color.red, Color.red, Color.yellow, Color.red,
                    Color.red, Color.yellow, Color.yellow, Color.yellow, Color.yellow, Color.yellow, Color.yellow, Color.yellow, Color.yellow, Color.red,
                    Color.red, Color.red, Color.red, Color.red, Color.red, Color.red, Color.red, Color.red, Color.red, Color.red
                };
                break;

            case "Level_3":
                levelColors = new Color[]
                {
                    Color.yellow, Color.red, Color.yellow, Color.yellow, Color.yellow, Color.blue, Color.green, Color.yellow, Color.blue, Color.yellow,
                    Color.blue, Color.blue, Color.blue, Color.blue, Color.blue, Color.blue, Color.green, Color.yellow, Color.blue, Color.yellow,
                    Color.yellow, Color.red, Color.blue, Color.red, Color.red, Color.red, Color.green, Color.red, Color.blue, Color.yellow,
                    Color.yellow, Color.red, Color.blue, Color.red, Color.yellow, Color.yellow, Color.green, Color.red, Color.blue, Color.yellow,
                    Color.yellow, Color.red, Color.blue, Color.red, Color.yellow, Color.green, Color.green, Color.red, Color.blue, Color.yellow,
                    Color.yellow, Color.yellow, Color.blue, Color.yellow, Color.yellow, Color.green, Color.yellow, Color.red, Color.blue, Color.yellow,
                    Color.yellow, Color.yellow, Color.blue, Color.blue, Color.blue, Color.green, Color.blue, Color.blue, Color.blue, Color.yellow,
                    Color.green, Color.green, Color.green, Color.green, Color.green, Color.green, Color.yellow, Color.red, Color.red, Color.red
                };
                break;

            case "Level_4":
                levelColors = new Color[]
                {
                    Color.yellow, Color.red, Color.red, Color.blue, Color.yellow, Color.yellow, Color.yellow, Color.yellow, Color.blue, Color.yellow,
                    Color.yellow, Color.red, Color.red, Color.blue, Color.yellow, Color.yellow, Color.yellow, Color.yellow, Color.blue, Color.yellow,
                    Color.green, Color.green, Color.green, Color.green, Color.green, Color.green, Color.green, Color.green, Color.green, Color.green,
                    Color.yellow, Color.red, Color.red, Color.blue, Color.green, Color.green, Color.green, Color.green, Color.blue, Color.yellow,
                    Color.yellow, Color.red, Color.red, Color.blue, Color.yellow, Color.green, Color.green, Color.green, Color.blue, Color.yellow,
                    Color.yellow, Color.red, Color.red, Color.blue, Color.yellow, Color.yellow, Color.green, Color.green, Color.blue, Color.yellow,
                    Color.yellow, Color.red, Color.red, Color.blue, Color.yellow, Color.yellow, Color.yellow, Color.green, Color.blue, Color.yellow,
                    Color.yellow, Color.red, Color.red, Color.blue, Color.yellow, Color.yellow, Color.yellow, Color.yellow, Color.blue, Color.yellow
                };
                break;

            case "Level_5":
                levelColors = new Color[]
                {
                    Color.green, Color.red, Color.blue, Color.blue, Color.green, Color.green, Color.green, Color.green, Color.green, Color.green,
                    Color.green, Color.red, Color.red, Color.green, Color.green, Color.green, Color.green, Color.green, Color.green, Color.green,
                    Color.green, Color.blue, Color.green, Color.green, Color.blue, Color.yellow, Color.yellow, Color.blue, Color.blue, Color.green,
                    Color.green, Color.blue, Color.green, Color.green, Color.yellow, Color.green, Color.green, Color.green, Color.yellow, Color.green,
                    Color.green, Color.red, Color.green, Color.green, Color.blue, Color.green, Color.green, Color.green, Color.yellow, Color.green,
                    Color.green, Color.yellow, Color.blue, Color.blue, Color.yellow, Color.green, Color.red, Color.red, Color.blue, Color.green,
                    Color.green, Color.green, Color.green, Color.green, Color.green, Color.green, Color.red, Color.green, Color.green, Color.green,
                    Color.green, Color.green, Color.yellow, Color.yellow, Color.yellow, Color.red, Color.blue, Color.green, Color.green, Color.green
                };
                break;
            default:
                Debug.LogError("Nivel no definido.");
                break;
        }
    }
}