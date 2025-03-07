using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System;
using System.Linq;
using Unity.VisualScripting;

public class GridManager : MonoBehaviour
{
    public GameObject cellPrefab;

    public GameObject bombPrefab; // Asigna el prefab de la bomba en el Inspector
    public int bombX;
    public int bombY;
    private Bomb placedBomb; // Referencia a la bomba colocada
    public Color[] explosionColors; // Colores posibles para la explosión

    public int rows = 9;
    public int columns = 7;
    public float cellSpacing = -0.3f;
    public Color[] levelColors; // Colores personalizados para el nivel actual

    public int maxAttempts = 10; // Número máximo de intentos por nivel
    public int remainingAttempts;
    public TextMeshProUGUI attemptsText; // Referencia al texto del contador de intentos
    private int activePropagations = 0; // Contador de propagaciones activas
    private bool isCheckingRestart = false;
    public bool finishedPropagations;
    public GameObject levelFailedPanel; // Panel de Derrota

    void Start()
    {
        string levelName = SceneManager.GetActiveScene().name; // Obtiene el nombre de la escena activa
        SetLevelColors(levelName); // Asigna los colores en función del nombre del nivel
        GenerateGrid(); // Genera la cuadrícula

        remainingAttempts = maxAttempts; // Inicializar los intentos restantes
        UpdateAttemptsUI(); // Actualizar el texto del UI

        // Solo coloca la bomba si el prefab está asignado
        if (bombPrefab != null)
        {
            PlaceBomb(bombX, bombY);
        }
    }
    public void UseAttempt()
    {
        if (remainingAttempts > 0)
        {
            remainingAttempts--;
            UpdateAttemptsUI();


            if (placedBomb != null)
            {
                StartCoroutine(ReduceBombCountdownAfterPropagation());
            }

            if (remainingAttempts <= 0)
            {
                StartCoroutine(WaitForPropagations());
            }
        }
    }

    private IEnumerator ReduceBombCountdownAfterPropagation()
    {
        while (activePropagations > 0)
        {
            yield return new WaitForSeconds(0.5f);
        }

        placedBomb.ReduceCountdown();
    }

    public GridCell GetCell(int x, int y)
    {
        string cellName = $"Cell_{x}_{y}";
        Transform cellTransform = transform.Find(cellName);

        if (cellTransform != null)
        {
            return cellTransform.GetComponent<GridCell>();
        }
        return null;
    }


    private IEnumerator WaitForPropagations()
    {
        finishedPropagations = false;
        while (activePropagations > 0)
        {
            yield return new WaitForSeconds(0.5f);
        }
        finishedPropagations = true;
        Debug.Log("Todas las propagaciones han terminado.");
    }

    public void StartPropagation()
    {
        activePropagations++;
        finishedPropagations = false;
        Debug.Log($"Propagación iniciada. Total activas: {activePropagations}");
    }

    public void EndPropagation()
    {
        activePropagations--;
        Debug.Log($"Propagación finalizada. Total activas: {activePropagations}");

        if (activePropagations < 0)
        {
            activePropagations = 0;
            Debug.LogWarning("activePropagations fue menor que 0. Asegúrate de que EndPropagation se llama correctamente.");
        }

        if (activePropagations == 0 && remainingAttempts <= 0)
        {
            StartCoroutine(CheckAndRestartLevel());
        }
    }

    IEnumerator AnimateCellsAppearance(List<GameObject> cells)
    {
        float totalDuration = 1.0f; // Duración total de la animación
        float delayBetweenCells = totalDuration / cells.Count; // Tiempo entre cada celda
        System.Random rnd = new System.Random();

        foreach (GameObject cell in cells)
        {
            StartCoroutine(ScaleUpCell(cell, rnd.Next(5, 13) / 100f)); // Variabilidad en el tiempo de escala
            yield return new WaitForSeconds(delayBetweenCells * UnityEngine.Random.Range(0.25f, 0.75f)); // Variabilidad en aparición
        }
    }

    IEnumerator ScaleUpCell(GameObject cell, float duration)
    {
        Vector3 originalScale = new Vector3(0.06f, 0.06f, 1);
        Vector3 startScale = Vector3.zero;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            cell.transform.localScale = Vector3.Lerp(startScale, originalScale, elapsedTime / duration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        cell.transform.localScale = originalScale; // Asegurar tamaño final
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

        List<GameObject> cells = new List<GameObject>();

        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < columns; x++)
            {
                Vector3 position = new Vector3(x - offsetX + 1.06f + (x * cellSpacing), -(y - offsetY + 1.5f) - (y * cellSpacing), 0);
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
                // Inicialmente la escala de la celda será 0(invisible)
            cell.transform.localScale = Vector3.zero;
                cells.Add(cell);
            }
        }
        // Revolver aleatoriamente el orden de aparición de las celdas
        System.Random rnd = new System.Random();
        cells = cells.OrderBy(c => rnd.Next()).ToList();

        // Iniciar la animación de aparición
        StartCoroutine(AnimateCellsAppearance(cells));
    }

    public Vector3 GridToWorldPosition(int x, int y)
    {
        float offsetX = (columns - 1) / 2f;
        float offsetY = (rows - 1) / 2f;

        // Calcular la posición en el mundo basándose en el espaciado de las celdas
        return new Vector3(x - offsetX + 1.06f + (x * cellSpacing), -(y - offsetY + 1.5f) - (y * cellSpacing), 0);
    }

    public bool IsWithinBounds(int x, int y)
    {
        return x >= 0 && x < columns && y >= 0 && y < rows;
    }

    public void SetCellColor(int x, int y, Color color)
    {
        string cellName = $"Cell_{x}_{y}";
        Transform cellTransform = transform.Find(cellName);

        if (cellTransform != null)
        {
            GridCell gridCell = cellTransform.GetComponent<GridCell>();
            if (gridCell != null)
            {
                gridCell.SetColor(color); // Asigna el color a la celda
            }
        }
    }

    public void DestroyCell(int x, int y, Color explosionColor)
    {
        string cellName = $"Cell_{x}_{y}";
        Transform cellTransform = transform.Find(cellName);

        if (cellTransform != null)
        {
            GridCell gridCell = cellTransform.GetComponent<GridCell>();
            if (gridCell != null)
            {
                gridCell.SetColor(explosionColor); // Asigna el color de la explosión
            }
        }
    }

    public void PlaceBomb(int x, int y)
    {
        if (bombPrefab == null)
        {
            Debug.LogWarning("No hay bomba en este nivel, PlaceBomb() no hará nada.");
            return; // Salimos de la función para evitar el error
        }

        Vector3 bombPosition = GridToWorldPosition(x, y);
        GameObject bombObj = Instantiate(bombPrefab, bombPosition, Quaternion.identity);
        placedBomb = bombObj.GetComponent<Bomb>();

        if (placedBomb != null)
        {
            placedBomb.gridX = x;
            placedBomb.gridY = y;
            placedBomb.gridManager = this;
            placedBomb.explosionColors = explosionColors; // Asigna los colores configurados
        }
    }

    private IEnumerator ShowFailPanel()
    {
        yield return new WaitForSeconds(1f); // Espera 1 segundo
        levelFailedPanel.SetActive(true); // Activa el panel de derrota
    }

    IEnumerator CheckAndRestartLevel()
    {
        if (isCheckingRestart)
        {
            yield break; // Evitar múltiples llamadas simultáneas
        }

        isCheckingRestart = true;

        // Esperar a que todas las propagaciones terminen
        yield return StartCoroutine(WaitForPropagations());

        LevelManager levelManager = FindObjectOfType<LevelManager>();
        if (levelManager != null)
        {
            bool victory = levelManager.CheckVictoryCondition();

            if (victory)
            {
                Debug.Log("¡Nivel completado!");
                levelManager.CompleteLevel(); // Pasar al siguiente nivel
                isCheckingRestart = false;
                yield break; //  Salimos de la corrutina para evitar que siga ejecutándose y muestre la derrota
            }

            if (remainingAttempts <= 0 && finishedPropagations)
            {
                Debug.Log("Sin intentos restantes. Mostrando panel de derrota...");
                StartCoroutine(ShowFailPanel());
            }
        }

        isCheckingRestart = false;
    }


    // Actualiza el texto del contador de intentos
    void UpdateAttemptsUI()
    {
        if (attemptsText != null)
        {
            attemptsText.text = $"{remainingAttempts}";
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
                new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.8510f, 0.3255f, 0.3098f),
                new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.8510f, 0.3255f, 0.3098f),
                new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.8510f, 0.3255f, 0.3098f),
                new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.8510f, 0.3255f, 0.3098f),
                new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.8510f, 0.3255f, 0.3098f),
                new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.8510f, 0.3255f, 0.3098f),
                new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.8510f, 0.3255f, 0.3098f),
                new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.8510f, 0.3255f, 0.3098f),
                new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.8510f, 0.3255f, 0.3098f),
                    };
                    break;

                case "Level_2":
                    levelColors = new Color[]
                    {
                new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.8510f, 0.3255f, 0.3098f),
                new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.8510f, 0.3255f, 0.3098f),
                new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.8510f, 0.3255f, 0.3098f),
                new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.8510f, 0.3255f, 0.3098f),
                new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.8510f, 0.3255f, 0.3098f),
                new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.8510f, 0.3255f, 0.3098f),
                new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.8510f, 0.3255f, 0.3098f),
                new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.8510f, 0.3255f, 0.3098f),
                new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.8510f, 0.3255f, 0.3098f),
                    };
                    break;

                case "Level_3":
                    levelColors = new Color[]
                    {
                new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.2902f, 0.5647f, 0.8863f),
                new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.2902f, 0.5647f, 0.8863f),
                new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.2902f, 0.5647f, 0.8863f),
                new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.2902f, 0.5647f, 0.8863f),
                new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.2902f, 0.5647f, 0.8863f),
                new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.2902f, 0.5647f, 0.8863f),
                new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.2902f, 0.5647f, 0.8863f),
                new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.2902f, 0.5647f, 0.8863f),
                new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.2902f, 0.5647f, 0.8863f),
                    };
                    break;

                case "Level_4":
                    levelColors = new Color[]
                    {
                new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.9608f, 0.7725f, 0.0941f),
                new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.2902f, 0.5647f, 0.8863f),
                new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.9608f, 0.7725f, 0.0941f),
                new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.9608f, 0.7725f, 0.0941f),
                new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.2902f, 0.5647f, 0.8863f),
                new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.9608f, 0.7725f, 0.0941f),
                new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.9608f, 0.7725f, 0.0941f),
                new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.2902f, 0.5647f, 0.8863f),
                new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.9608f, 0.7725f, 0.0941f),
                    };
                    break;

                case "Level_5":
                    levelColors = new Color[]
                    {
                new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f),
                new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.8510f, 0.3255f, 0.3098f),
                new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.8510f, 0.3255f, 0.3098f),
                new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.8510f, 0.3255f, 0.3098f),
                new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.8510f, 0.3255f, 0.3098f),
                new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.8510f, 0.3255f, 0.3098f),
                new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.8510f, 0.3255f, 0.3098f),
                new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.8510f, 0.3255f, 0.3098f),
                new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.8510f, 0.3255f, 0.3098f),
                    };
                    break;

                case "Level_6":
                    levelColors = new Color[]
                    {
                new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f),
                new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.4253f, 0.7490f, 0.5176f),
                new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.4253f, 0.7490f, 0.5176f),
                new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.4253f, 0.7490f, 0.5176f),
                new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f),
                new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f),
                new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f),
                new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f),
                new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.2902f, 0.5647f, 0.8863f),
                    };
                    break;

                case "Level_7":
                    levelColors = new Color[]
                    {
                new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.9608f, 0.7725f, 0.0941f),
                new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.2902f, 0.5647f, 0.8863f),
                new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.2902f, 0.5647f, 0.8863f),
                new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.2902f, 0.5647f, 0.8863f),
                new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.2902f, 0.5647f, 0.8863f),
                 new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.2902f, 0.5647f, 0.8863f),
                new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.2902f, 0.5647f, 0.8863f),
                new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.2902f, 0.5647f, 0.8863f),
                new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.9608f, 0.7725f, 0.0941f),
                    };
                    break;

                case "Level_8":
                    levelColors = new Color[]
                    {
                new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.9608f, 0.7725f, 0.0941f),
                new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.2902f, 0.5647f, 0.8863f),
                new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.2902f, 0.5647f, 0.8863f),
                new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.2902f, 0.5647f, 0.8863f),
                new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.2902f, 0.5647f, 0.8863f),
                new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.2902f, 0.5647f, 0.8863f),
                new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.2902f, 0.5647f, 0.8863f),
                new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.2902f, 0.5647f, 0.8863f),
                new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.9608f, 0.7725f, 0.0941f),
                    };
                    break;

                case "Level_9":
                    levelColors = new Color[]
                    {
                new Color(0.6667f, 0.3922f, 0.3529f), new Color(0.6667f, 0.3922f, 0.3529f), new Color(0.5098f, 0.4784f, 0.2941f), new Color(0.8431f, 0.7451f, 0.6078f), new Color(0.5098f, 0.4784f, 0.2941f), new Color(0.9137f, 0.6471f, 0.4000f), new Color(0.9137f, 0.6471f, 0.4000f),
                new Color(0.5098f, 0.4784f, 0.2941f), new Color(0.6667f, 0.3922f, 0.3529f), new Color(0.5098f, 0.4784f, 0.2941f), new Color(0.8431f, 0.7451f, 0.6078f), new Color(0.5098f, 0.4784f, 0.2941f), new Color(0.9137f, 0.6471f, 0.4000f), new Color(0.5098f, 0.4784f, 0.2941f),
                new Color(0.6667f, 0.3922f, 0.3529f), new Color(0.6667f, 0.3922f, 0.3529f), new Color(0.5098f, 0.4784f, 0.2941f), new Color(0.8431f, 0.7451f, 0.6078f), new Color(0.5098f, 0.4784f, 0.2941f), new Color(0.9137f, 0.6471f, 0.4000f), new Color(0.9137f, 0.6471f, 0.4000f),
                new Color(0.6667f, 0.3922f, 0.3529f), new Color(0.5098f, 0.4784f, 0.2941f), new Color(0.5098f, 0.4784f, 0.2941f), new Color(0.8431f, 0.7451f, 0.6078f), new Color(0.5098f, 0.4784f, 0.2941f), new Color(0.5098f, 0.4784f, 0.2941f), new Color(0.9137f, 0.6471f, 0.4000f),
                new Color(0.6667f, 0.3922f, 0.3529f), new Color(0.6667f, 0.3922f, 0.3529f), new Color(0.5098f, 0.4784f, 0.2941f), new Color(0.8431f, 0.7451f, 0.6078f), new Color(0.5098f, 0.4784f, 0.2941f), new Color(0.9137f, 0.6471f, 0.4000f), new Color(0.9137f, 0.6471f, 0.4000f),
                new Color(0.5098f, 0.4784f, 0.2941f), new Color(0.6667f, 0.3922f, 0.3529f), new Color(0.5098f, 0.4784f, 0.2941f), new Color(0.8431f, 0.7451f, 0.6078f), new Color(0.5098f, 0.4784f, 0.2941f), new Color(0.9137f, 0.6471f, 0.4000f), new Color(0.5098f, 0.4784f, 0.2941f),
                new Color(0.6667f, 0.3922f, 0.3529f), new Color(0.6667f, 0.3922f, 0.3529f), new Color(0.5098f, 0.4784f, 0.2941f), new Color(0.8431f, 0.7451f, 0.6078f), new Color(0.5098f, 0.4784f, 0.2941f), new Color(0.9137f, 0.6471f, 0.4000f), new Color(0.9137f, 0.6471f, 0.4000f),
                new Color(0.6667f, 0.3922f, 0.3529f), new Color(0.5098f, 0.4784f, 0.2941f), new Color(0.5098f, 0.4784f, 0.2941f), new Color(0.8431f, 0.7451f, 0.6078f), new Color(0.5098f, 0.4784f, 0.2941f), new Color(0.5098f, 0.4784f, 0.2941f), new Color(0.9137f, 0.6471f, 0.4000f),
                new Color(0.6667f, 0.3922f, 0.3529f), new Color(0.6667f, 0.3922f, 0.3529f), new Color(0.6667f, 0.3922f, 0.3529f), new Color(0.8431f, 0.7451f, 0.6078f), new Color(0.9137f, 0.6471f, 0.4000f), new Color(0.9137f, 0.6471f, 0.4000f), new Color(0.9137f, 0.6471f, 0.4000f),
                    };
                    break;

                case "Level_10":
                    levelColors = new Color[]
                    {
                new Color(0.9137f, 0.6471f, 0.4000f), new Color(0.5098f, 0.4784f, 0.2941f), new Color(0.5098f, 0.4784f, 0.2941f), new Color(0.5098f, 0.4784f, 0.2941f), new Color(0.5098f, 0.4784f, 0.2941f), new Color(0.5098f, 0.4784f, 0.2941f), new Color(0.9137f, 0.6471f, 0.4000f),
                new Color(0.9137f, 0.6471f, 0.4000f), new Color(0.5098f, 0.4784f, 0.2941f), new Color(0.9137f, 0.6471f, 0.4000f), new Color(0.9137f, 0.6471f, 0.4000f), new Color(0.9137f, 0.6471f, 0.4000f), new Color(0.5098f, 0.4784f, 0.2941f), new Color(0.9137f, 0.6471f, 0.4000f),
                new Color(0.9137f, 0.6471f, 0.4000f), new Color(0.5098f, 0.4784f, 0.2941f), new Color(0.9137f, 0.6471f, 0.4000f), new Color(0.5098f, 0.4784f, 0.2941f), new Color(0.9137f, 0.6471f, 0.4000f), new Color(0.5098f, 0.4784f, 0.2941f), new Color(0.9137f, 0.6471f, 0.4000f),
                new Color(0.9137f, 0.6471f, 0.4000f), new Color(0.5098f, 0.4784f, 0.2941f), new Color(0.9137f, 0.6471f, 0.4000f), new Color(0.5098f, 0.4784f, 0.2941f), new Color(0.9137f, 0.6471f, 0.4000f), new Color(0.5098f, 0.4784f, 0.2941f), new Color(0.9137f, 0.6471f, 0.4000f),
                new Color(0.9137f, 0.6471f, 0.4000f), new Color(0.5098f, 0.4784f, 0.2941f), new Color(0.9137f, 0.6471f, 0.4000f), new Color(0.5098f, 0.4784f, 0.2941f), new Color(0.9137f, 0.6471f, 0.4000f), new Color(0.5098f, 0.4784f, 0.2941f), new Color(0.9137f, 0.6471f, 0.4000f),
                new Color(0.9137f, 0.6471f, 0.4000f), new Color(0.5098f, 0.4784f, 0.2941f), new Color(0.9137f, 0.6471f, 0.4000f), new Color(0.5098f, 0.4784f, 0.2941f), new Color(0.9137f, 0.6471f, 0.4000f), new Color(0.5098f, 0.4784f, 0.2941f), new Color(0.9137f, 0.6471f, 0.4000f),
                new Color(0.9137f, 0.6471f, 0.4000f), new Color(0.5098f, 0.4784f, 0.2941f), new Color(0.9137f, 0.6471f, 0.4000f), new Color(0.5098f, 0.4784f, 0.2941f), new Color(0.9137f, 0.6471f, 0.4000f), new Color(0.5098f, 0.4784f, 0.2941f), new Color(0.9137f, 0.6471f, 0.4000f),
                new Color(0.9137f, 0.6471f, 0.4000f), new Color(0.9137f, 0.6471f, 0.4000f), new Color(0.9137f, 0.6471f, 0.4000f), new Color(0.9137f, 0.6471f, 0.4000f), new Color(0.9137f, 0.6471f, 0.4000f), new Color(0.9137f, 0.6471f, 0.4000f), new Color(0.9137f, 0.6471f, 0.4000f),
                new Color(0.8431f, 0.7451f, 0.6078f), new Color(0.8431f, 0.7451f, 0.6078f), new Color(0.8431f, 0.7451f, 0.6078f), new Color(0.8431f, 0.7451f, 0.6078f), new Color(0.8431f, 0.7451f, 0.6078f), new Color(0.8431f, 0.7451f, 0.6078f), new Color(0.8431f, 0.7451f, 0.6078f),
                    };
                    break;

                case "Level_11":
                    levelColors = new Color[]
                    {
                new Color(0.5098f, 0.4784f, 0.2941f), new Color(0.5098f, 0.4784f, 0.2941f), new Color(0.5098f, 0.4784f, 0.2941f), new Color(0.5098f, 0.4784f, 0.2941f), new Color(0.5098f, 0.4784f, 0.2941f), new Color(0.5098f, 0.4784f, 0.2941f), new Color(0.5098f, 0.4784f, 0.2941f),
                new Color(0.5098f, 0.4784f, 0.2941f), new Color(0.8431f, 0.7451f, 0.6078f), new Color(0.8431f, 0.7451f, 0.6078f), new Color(0.5098f, 0.4784f, 0.2941f), new Color(0.8431f, 0.7451f, 0.6078f), new Color(0.8431f, 0.7451f, 0.6078f), new Color(0.5098f, 0.4784f, 0.2941f),
                new Color(0.8431f, 0.7451f, 0.6078f), new Color(0.8431f, 0.7451f, 0.6078f), new Color(0.8431f, 0.7451f, 0.6078f), new Color(0.8431f, 0.7451f, 0.6078f), new Color(0.8431f, 0.7451f, 0.6078f), new Color(0.8431f, 0.7451f, 0.6078f), new Color(0.8431f, 0.7451f, 0.6078f),
                new Color(0.8431f, 0.7451f, 0.6078f), new Color(0.9137f, 0.6471f, 0.4000f), new Color(0.9137f, 0.6471f, 0.4000f), new Color(0.8431f, 0.7451f, 0.6078f), new Color(0.9137f, 0.6471f, 0.4000f), new Color(0.9137f, 0.6471f, 0.4000f), new Color(0.8431f, 0.7451f, 0.6078f),
                new Color(0.8431f, 0.7451f, 0.6078f), new Color(0.9137f, 0.6471f, 0.4000f), new Color(0.9137f, 0.6471f, 0.4000f), new Color(0.9137f, 0.6471f, 0.4000f), new Color(0.9137f, 0.6471f, 0.4000f), new Color(0.9137f, 0.6471f, 0.4000f), new Color(0.8431f, 0.7451f, 0.6078f),
                new Color(0.8431f, 0.7451f, 0.6078f), new Color(0.8431f, 0.7451f, 0.6078f), new Color(0.9137f, 0.6471f, 0.4000f), new Color(0.9137f, 0.6471f, 0.4000f), new Color(0.9137f, 0.6471f, 0.4000f), new Color(0.8431f, 0.7451f, 0.6078f), new Color(0.8431f, 0.7451f, 0.6078f),
                new Color(0.5098f, 0.4784f, 0.2941f), new Color(0.8431f, 0.7451f, 0.6078f), new Color(0.8431f, 0.7451f, 0.6078f), new Color(0.9137f, 0.6471f, 0.4000f), new Color(0.8431f, 0.7451f, 0.6078f), new Color(0.8431f, 0.7451f, 0.6078f), new Color(0.5098f, 0.4784f, 0.2941f),
                new Color(0.5098f, 0.4784f, 0.2941f), new Color(0.5098f, 0.4784f, 0.2941f), new Color(0.8431f, 0.7451f, 0.6078f), new Color(0.8431f, 0.7451f, 0.6078f), new Color(0.8431f, 0.7451f, 0.6078f), new Color(0.5098f, 0.4784f, 0.2941f), new Color(0.5098f, 0.4784f, 0.2941f),
                new Color(0.5098f, 0.4784f, 0.2941f), new Color(0.5098f, 0.4784f, 0.2941f), new Color(0.5098f, 0.4784f, 0.2941f), new Color(0.8431f, 0.7451f, 0.6078f), new Color(0.5098f, 0.4784f, 0.2941f), new Color(0.5098f, 0.4784f, 0.2941f), new Color(0.5098f, 0.4784f, 0.2941f),
                    };
                    break;

                case "Level_12":
                    levelColors = new Color[]
                    {
                new Color(0.5098f, 0.4784f, 0.2941f), new Color(0.5098f, 0.4784f, 0.2941f), new Color(0.5098f, 0.4784f, 0.2941f), new Color(0.5098f, 0.4784f, 0.2941f), new Color(0.5098f, 0.4784f, 0.2941f), new Color(0.5098f, 0.4784f, 0.2941f), new Color(0.5098f, 0.4784f, 0.2941f),
                new Color(0.5098f, 0.4784f, 0.2941f), new Color(0.9137f, 0.6471f, 0.4000f), new Color(0.9137f, 0.6471f, 0.4000f), new Color(0.9137f, 0.6471f, 0.4000f), new Color(0.9137f, 0.6471f, 0.4000f), new Color(0.9137f, 0.6471f, 0.4000f), new Color(0.5098f, 0.4784f, 0.2941f),
                new Color(0.9137f, 0.6471f, 0.4000f), new Color(0.9137f, 0.6471f, 0.4000f), new Color(0.8431f, 0.7451f, 0.6078f), new Color(0.8431f, 0.7451f, 0.6078f), new Color(0.8431f, 0.7451f, 0.6078f), new Color(0.9137f, 0.6471f, 0.4000f), new Color(0.9137f, 0.6471f, 0.4000f),
                new Color(0.9137f, 0.6471f, 0.4000f), new Color(0.8431f, 0.7451f, 0.6078f), new Color(0.6667f, 0.3922f, 0.3529f), new Color(0.8431f, 0.7451f, 0.6078f), new Color(0.6667f, 0.3922f, 0.3529f), new Color(0.8431f, 0.7451f, 0.6078f), new Color(0.9137f, 0.6471f, 0.4000f),
                new Color(0.9137f, 0.6471f, 0.4000f), new Color(0.8431f, 0.7451f, 0.6078f), new Color(0.8431f, 0.7451f, 0.6078f), new Color(0.8431f, 0.7451f, 0.6078f), new Color(0.8431f, 0.7451f, 0.6078f), new Color(0.8431f, 0.7451f, 0.6078f), new Color(0.9137f, 0.6471f, 0.4000f),
                new Color(0.9137f, 0.6471f, 0.4000f), new Color(0.9137f, 0.6471f, 0.4000f), new Color(0.9137f, 0.6471f, 0.4000f), new Color(0.9137f, 0.6471f, 0.4000f), new Color(0.9137f, 0.6471f, 0.4000f), new Color(0.9137f, 0.6471f, 0.4000f), new Color(0.9137f, 0.6471f, 0.4000f),
                new Color(0.9137f, 0.6471f, 0.4000f), new Color(0.9137f, 0.6471f, 0.4000f), new Color(0.8431f, 0.7451f, 0.6078f), new Color(0.8431f, 0.7451f, 0.6078f), new Color(0.8431f, 0.7451f, 0.6078f), new Color(0.9137f, 0.6471f, 0.4000f), new Color(0.9137f, 0.6471f, 0.4000f),
                new Color(0.5098f, 0.4784f, 0.2941f), new Color(0.9137f, 0.6471f, 0.4000f), new Color(0.9137f, 0.6471f, 0.4000f), new Color(0.9137f, 0.6471f, 0.4000f), new Color(0.9137f, 0.6471f, 0.4000f), new Color(0.9137f, 0.6471f, 0.4000f), new Color(0.5098f, 0.4784f, 0.2941f),
                new Color(0.5098f, 0.4784f, 0.2941f), new Color(0.5098f, 0.4784f, 0.2941f), new Color(0.5098f, 0.4784f, 0.2941f), new Color(0.5098f, 0.4784f, 0.2941f), new Color(0.5098f, 0.4784f, 0.2941f), new Color(0.5098f, 0.4784f, 0.2941f), new Color(0.5098f, 0.4784f, 0.2941f),
                    };
                    break;

                case "Level_13":
                    levelColors = new Color[]
                    {
                new Color(0.9137f, 0.6471f, 0.4000f), new Color(0.6667f, 0.3922f, 0.3529f), new Color(0.8431f, 0.7451f, 0.6078f), new Color(0.9137f, 0.6471f, 0.4000f), new Color(0.9137f, 0.6471f, 0.4000f), new Color(0.8431f, 0.7451f, 0.6078f), new Color(0.9137f, 0.6471f, 0.4000f),
                new Color(0.9137f, 0.6471f, 0.4000f), new Color(0.6667f, 0.3922f, 0.3529f), new Color(0.8431f, 0.7451f, 0.6078f), new Color(0.9137f, 0.6471f, 0.4000f), new Color(0.9137f, 0.6471f, 0.4000f), new Color(0.8431f, 0.7451f, 0.6078f), new Color(0.9137f, 0.6471f, 0.4000f),
                new Color(0.5098f, 0.4784f, 0.2941f), new Color(0.5098f, 0.4784f, 0.2941f), new Color(0.5098f, 0.4784f, 0.2941f), new Color(0.5098f, 0.4784f, 0.2941f), new Color(0.5098f, 0.4784f, 0.2941f), new Color(0.5098f, 0.4784f, 0.2941f), new Color(0.5098f, 0.4784f, 0.2941f),
                new Color(0.9137f, 0.6471f, 0.4000f), new Color(0.6667f, 0.3922f, 0.3529f), new Color(0.8431f, 0.7451f, 0.6078f), new Color(0.5098f, 0.4784f, 0.2941f), new Color(0.5098f, 0.4784f, 0.2941f), new Color(0.8431f, 0.7451f, 0.6078f), new Color(0.9137f, 0.6471f, 0.4000f),
                new Color(0.9137f, 0.6471f, 0.4000f), new Color(0.6667f, 0.3922f, 0.3529f), new Color(0.8431f, 0.7451f, 0.6078f), new Color(0.5098f, 0.4784f, 0.2941f), new Color(0.5098f, 0.4784f, 0.2941f), new Color(0.8431f, 0.7451f, 0.6078f), new Color(0.9137f, 0.6471f, 0.4000f),
                new Color(0.9137f, 0.6471f, 0.4000f), new Color(0.6667f, 0.3922f, 0.3529f), new Color(0.8431f, 0.7451f, 0.6078f), new Color(0.9137f, 0.6471f, 0.4000f), new Color(0.5098f, 0.4784f, 0.2941f), new Color(0.8431f, 0.7451f, 0.6078f), new Color(0.9137f, 0.6471f, 0.4000f),
                new Color(0.9137f, 0.6471f, 0.4000f), new Color(0.6667f, 0.3922f, 0.3529f), new Color(0.8431f, 0.7451f, 0.6078f), new Color(0.9137f, 0.6471f, 0.4000f), new Color(0.9137f, 0.6471f, 0.4000f), new Color(0.8431f, 0.7451f, 0.6078f), new Color(0.9137f, 0.6471f, 0.4000f),
                new Color(0.9137f, 0.6471f, 0.4000f), new Color(0.6667f, 0.3922f, 0.3529f), new Color(0.8431f, 0.7451f, 0.6078f), new Color(0.9137f, 0.6471f, 0.4000f), new Color(0.9137f, 0.6471f, 0.4000f), new Color(0.8431f, 0.7451f, 0.6078f), new Color(0.9137f, 0.6471f, 0.4000f),
                new Color(0.9137f, 0.6471f, 0.4000f), new Color(0.6667f, 0.3922f, 0.3529f), new Color(0.8431f, 0.7451f, 0.6078f), new Color(0.9137f, 0.6471f, 0.4000f), new Color(0.9137f, 0.6471f, 0.4000f), new Color(0.8431f, 0.7451f, 0.6078f), new Color(0.9137f, 0.6471f, 0.4000f),
                    };
                    break;

                case "Level_14":
                    levelColors = new Color[]
                    {
                new Color(0.8431f, 0.7451f, 0.6078f), new Color(0.8431f, 0.7451f, 0.6078f), new Color(0.5098f, 0.4784f, 0.2941f), new Color(0.9137f, 0.6471f, 0.4000f), new Color(0.6667f, 0.3922f, 0.3529f), new Color(0.6667f, 0.3922f, 0.3529f), new Color(0.9137f, 0.6471f, 0.4000f),
                new Color(0.5098f, 0.4784f, 0.2941f), new Color(0.8431f, 0.7451f, 0.6078f), new Color(0.6667f, 0.3922f, 0.3529f), new Color(0.8431f, 0.7451f, 0.6078f), new Color(0.8431f, 0.7451f, 0.6078f), new Color(0.8431f, 0.7451f, 0.6078f), new Color(0.9137f, 0.6471f, 0.4000f),
                new Color(0.5098f, 0.4784f, 0.2941f), new Color(0.8431f, 0.7451f, 0.6078f), new Color(0.6667f, 0.3922f, 0.3529f), new Color(0.8431f, 0.7451f, 0.6078f), new Color(0.8431f, 0.7451f, 0.6078f), new Color(0.8431f, 0.7451f, 0.6078f), new Color(0.8431f, 0.7451f, 0.6078f),
                new Color(0.5098f, 0.4784f, 0.2941f), new Color(0.8431f, 0.7451f, 0.6078f), new Color(0.5098f, 0.4784f, 0.2941f), new Color(0.6667f, 0.3922f, 0.3529f), new Color(0.5098f, 0.4784f, 0.2941f), new Color(0.6667f, 0.3922f, 0.3529f), new Color(0.8431f, 0.7451f, 0.6078f),
                new Color(0.9137f, 0.6471f, 0.4000f), new Color(0.8431f, 0.7451f, 0.6078f), new Color(0.8431f, 0.7451f, 0.6078f), new Color(0.8431f, 0.7451f, 0.6078f), new Color(0.8431f, 0.7451f, 0.6078f), new Color(0.5098f, 0.4784f, 0.2941f), new Color(0.8431f, 0.7451f, 0.6078f),
                new Color(0.6667f, 0.3922f, 0.3529f), new Color(0.9137f, 0.6471f, 0.4000f), new Color(0.9137f, 0.6471f, 0.4000f), new Color(0.8431f, 0.7451f, 0.6078f), new Color(0.8431f, 0.7451f, 0.6078f), new Color(0.5098f, 0.4784f, 0.2941f), new Color(0.8431f, 0.7451f, 0.6078f),
                new Color(0.8431f, 0.7451f, 0.6078f), new Color(0.8431f, 0.7451f, 0.6078f), new Color(0.9137f, 0.6471f, 0.4000f), new Color(0.8431f, 0.7451f, 0.6078f), new Color(0.8431f, 0.7451f, 0.6078f), new Color(0.6667f, 0.3922f, 0.3529f), new Color(0.8431f, 0.7451f, 0.6078f),
                new Color(0.8431f, 0.7451f, 0.6078f), new Color(0.8431f, 0.7451f, 0.6078f), new Color(0.6667f, 0.3922f, 0.3529f), new Color(0.5098f, 0.4784f, 0.2941f), new Color(0.5098f, 0.4784f, 0.2941f), new Color(0.6667f, 0.3922f, 0.3529f), new Color(0.8431f, 0.7451f, 0.6078f),
                new Color(0.8431f, 0.7451f, 0.6078f), new Color(0.8431f, 0.7451f, 0.6078f), new Color(0.8431f, 0.7451f, 0.6078f), new Color(0.8431f, 0.7451f, 0.6078f), new Color(0.8431f, 0.7451f, 0.6078f), new Color(0.8431f, 0.7451f, 0.6078f), new Color(0.8431f, 0.7451f, 0.6078f),
                    };
                    break;

                case "Level_15":
                    levelColors = new Color[]
                    {
                new Color(0.5098f, 0.4784f, 0.2941f), new Color(0.9137f, 0.6471f, 0.4000f), new Color(0.9137f, 0.6471f, 0.4000f), new Color(0.6667f, 0.3922f, 0.3529f), new Color(0.6667f, 0.3922f, 0.3529f), new Color(0.8431f, 0.7451f, 0.6078f), new Color(0.6667f, 0.3922f, 0.3529f),
                new Color(0.5098f, 0.4784f, 0.2941f), new Color(0.8431f, 0.7451f, 0.6078f), new Color(0.8431f, 0.7451f, 0.6078f), new Color(0.8431f, 0.7451f, 0.6078f), new Color(0.8431f, 0.7451f, 0.6078f), new Color(0.8431f, 0.7451f, 0.6078f), new Color(0.9137f, 0.6471f, 0.4000f),
                new Color(0.5098f, 0.4784f, 0.2941f), new Color(0.8431f, 0.7451f, 0.6078f), new Color(0.9137f, 0.6471f, 0.4000f), new Color(0.6667f, 0.3922f, 0.3529f), new Color(0.6667f, 0.3922f, 0.3529f), new Color(0.8431f, 0.7451f, 0.6078f), new Color(0.9137f, 0.6471f, 0.4000f),
                new Color(0.5098f, 0.4784f, 0.2941f), new Color(0.8431f, 0.7451f, 0.6078f), new Color(0.9137f, 0.6471f, 0.4000f), new Color(0.9137f, 0.6471f, 0.4000f), new Color(0.6667f, 0.3922f, 0.3529f), new Color(0.8431f, 0.7451f, 0.6078f), new Color(0.9137f, 0.6471f, 0.4000f),
                new Color(0.5098f, 0.4784f, 0.2941f), new Color(0.5098f, 0.4784f, 0.2941f), new Color(0.5098f, 0.4784f, 0.2941f), new Color(0.5098f, 0.4784f, 0.2941f), new Color(0.6667f, 0.3922f, 0.3529f), new Color(0.8431f, 0.7451f, 0.6078f), new Color(0.8431f, 0.7451f, 0.6078f),
                new Color(0.9137f, 0.6471f, 0.4000f), new Color(0.8431f, 0.7451f, 0.6078f), new Color(0.9137f, 0.6471f, 0.4000f), new Color(0.5098f, 0.4784f, 0.2941f), new Color(0.5098f, 0.4784f, 0.2941f), new Color(0.5098f, 0.4784f, 0.2941f), new Color(0.5098f, 0.4784f, 0.2941f),
                new Color(0.6667f, 0.3922f, 0.3529f), new Color(0.8431f, 0.7451f, 0.6078f), new Color(0.6667f, 0.3922f, 0.3529f), new Color(0.6667f, 0.3922f, 0.3529f), new Color(0.6667f, 0.3922f, 0.3529f), new Color(0.9137f, 0.6471f, 0.4000f), new Color(0.9137f, 0.6471f, 0.4000f),
                new Color(0.6667f, 0.3922f, 0.3529f), new Color(0.8431f, 0.7451f, 0.6078f), new Color(0.8431f, 0.7451f, 0.6078f), new Color(0.8431f, 0.7451f, 0.6078f), new Color(0.8431f, 0.7451f, 0.6078f), new Color(0.8431f, 0.7451f, 0.6078f), new Color(0.8431f, 0.7451f, 0.6078f),
                new Color(0.6667f, 0.3922f, 0.3529f), new Color(0.9137f, 0.6471f, 0.4000f), new Color(0.9137f, 0.6471f, 0.4000f), new Color(0.9137f, 0.6471f, 0.4000f), new Color(0.9137f, 0.6471f, 0.4000f), new Color(0.9137f, 0.6471f, 0.4000f), new Color(0.9137f, 0.6471f, 0.4000f),
                    };
                    break;

                case "Level_16":
                    levelColors = new Color[]
                    {
                new Color(0.8431f, 0.7451f, 0.6078f), new Color(0.6667f, 0.3922f, 0.3529f), new Color(0.6667f, 0.3922f, 0.3529f), new Color(0.9137f, 0.6471f, 0.4000f), new Color(0.6667f, 0.3922f, 0.3529f), new Color(0.6667f, 0.3922f, 0.3529f), new Color(0.8431f, 0.7451f, 0.6078f),
                new Color(0.8431f, 0.7451f, 0.6078f), new Color(0.8431f, 0.7451f, 0.6078f), new Color(0.8431f, 0.7451f, 0.6078f), new Color(0.9137f, 0.6471f, 0.4000f), new Color(0.8431f, 0.7451f, 0.6078f), new Color(0.8431f, 0.7451f, 0.6078f), new Color(0.8431f, 0.7451f, 0.6078f),
                new Color(0.6667f, 0.3922f, 0.3529f), new Color(0.8431f, 0.7451f, 0.6078f), new Color(0.6667f, 0.3922f, 0.3529f), new Color(0.9137f, 0.6471f, 0.4000f), new Color(0.6667f, 0.3922f, 0.3529f), new Color(0.8431f, 0.7451f, 0.6078f), new Color(0.6667f, 0.3922f, 0.3529f),
                new Color(0.6667f, 0.3922f, 0.3529f), new Color(0.8431f, 0.7451f, 0.6078f), new Color(0.6667f, 0.3922f, 0.3529f), new Color(0.9137f, 0.6471f, 0.4000f), new Color(0.9137f, 0.6471f, 0.4000f), new Color(0.8431f, 0.7451f, 0.6078f), new Color(0.6667f, 0.3922f, 0.3529f),
                new Color(0.9137f, 0.6471f, 0.4000f), new Color(0.9137f, 0.6471f, 0.4000f), new Color(0.9137f, 0.6471f, 0.4000f), new Color(0.8431f, 0.7451f, 0.6078f), new Color(0.9137f, 0.6471f, 0.4000f), new Color(0.9137f, 0.6471f, 0.4000f), new Color(0.9137f, 0.6471f, 0.4000f),
                new Color(0.6667f, 0.3922f, 0.3529f), new Color(0.8431f, 0.7451f, 0.6078f), new Color(0.9137f, 0.6471f, 0.4000f), new Color(0.9137f, 0.6471f, 0.4000f), new Color(0.6667f, 0.3922f, 0.3529f), new Color(0.8431f, 0.7451f, 0.6078f), new Color(0.6667f, 0.3922f, 0.3529f),
                new Color(0.6667f, 0.3922f, 0.3529f), new Color(0.8431f, 0.7451f, 0.6078f), new Color(0.6667f, 0.3922f, 0.3529f), new Color(0.9137f, 0.6471f, 0.4000f), new Color(0.6667f, 0.3922f, 0.3529f), new Color(0.8431f, 0.7451f, 0.6078f), new Color(0.6667f, 0.3922f, 0.3529f),
                new Color(0.8431f, 0.7451f, 0.6078f), new Color(0.8431f, 0.7451f, 0.6078f), new Color(0.8431f, 0.7451f, 0.6078f), new Color(0.9137f, 0.6471f, 0.4000f), new Color(0.8431f, 0.7451f, 0.6078f), new Color(0.8431f, 0.7451f, 0.6078f), new Color(0.8431f, 0.7451f, 0.6078f),
                new Color(0.8431f, 0.7451f, 0.6078f), new Color(0.6667f, 0.3922f, 0.3529f), new Color(0.6667f, 0.3922f, 0.3529f), new Color(0.6667f, 0.3922f, 0.3529f), new Color(0.6667f, 0.3922f, 0.3529f), new Color(0.6667f, 0.3922f, 0.3529f), new Color(0.8431f, 0.7451f, 0.6078f),
                    };
                    break;

                case "Level_17":
                    levelColors = new Color[]
                    {
                new Color(0.6275f, 0.6275f, 0.6275f), new Color(0.7725f, 0.8431f, 0.9686f), new Color(0.6275f, 0.6275f, 0.6275f), new Color(0.7725f, 0.8431f, 0.9686f), new Color(0.7725f, 0.8431f, 0.9686f), new Color(0.7725f, 0.8431f, 0.9686f), new Color(0.6392f, 0.5059f, 0.4078f),
                new Color(0.6275f, 0.6275f, 0.6275f), new Color(0.7725f, 0.8431f, 0.9686f), new Color(0.6275f, 0.6275f, 0.6275f), new Color(0.6275f, 0.6275f, 0.6275f), new Color(0.1882f, 0.4196f, 0.6745f), new Color(0.7725f, 0.8431f, 0.9686f), new Color(0.6392f, 0.5059f, 0.4078f),
                new Color(0.6275f, 0.6275f, 0.6275f), new Color(0.7725f, 0.8431f, 0.9686f), new Color(0.6275f, 0.6275f, 0.6275f), new Color(0.7725f, 0.8431f, 0.9686f), new Color(0.1882f, 0.4196f, 0.6745f), new Color(0.7725f, 0.8431f, 0.9686f), new Color(0.6392f, 0.5059f, 0.4078f),
                new Color(0.6275f, 0.6275f, 0.6275f), new Color(0.7725f, 0.8431f, 0.9686f), new Color(0.6275f, 0.6275f, 0.6275f), new Color(0.7725f, 0.8431f, 0.9686f), new Color(0.1882f, 0.4196f, 0.6745f), new Color(0.7725f, 0.8431f, 0.9686f), new Color(0.6392f, 0.5059f, 0.4078f),
                new Color(0.1882f, 0.4196f, 0.6745f), new Color(0.6275f, 0.6275f, 0.6275f), new Color(0.7725f, 0.8431f, 0.9686f), new Color(0.7725f, 0.8431f, 0.9686f), new Color(0.1882f, 0.4196f, 0.6745f), new Color(0.7725f, 0.8431f, 0.9686f), new Color(0.1882f, 0.4196f, 0.6745f),
                new Color(0.1882f, 0.4196f, 0.6745f), new Color(0.6275f, 0.6275f, 0.6275f), new Color(0.6275f, 0.6275f, 0.6275f), new Color(0.7725f, 0.8431f, 0.9686f), new Color(0.7725f, 0.8431f, 0.9686f), new Color(0.7725f, 0.8431f, 0.9686f), new Color(0.1882f, 0.4196f, 0.6745f),
                new Color(0.1882f, 0.4196f, 0.6745f), new Color(0.6392f, 0.5059f, 0.4078f), new Color(0.6275f, 0.6275f, 0.6275f), new Color(0.7725f, 0.8431f, 0.9686f), new Color(0.1882f, 0.4196f, 0.6745f), new Color(0.7725f, 0.8431f, 0.9686f), new Color(0.1882f, 0.4196f, 0.6745f),
                new Color(0.1882f, 0.4196f, 0.6745f), new Color(0.6392f, 0.5059f, 0.4078f), new Color(0.6275f, 0.6275f, 0.6275f), new Color(0.6275f, 0.6275f, 0.6275f), new Color(0.1882f, 0.4196f, 0.6745f), new Color(0.7725f, 0.8431f, 0.9686f), new Color(0.1882f, 0.4196f, 0.6745f),
                new Color(0.1882f, 0.4196f, 0.6745f), new Color(0.6392f, 0.5059f, 0.4078f), new Color(0.6275f, 0.6275f, 0.6275f), new Color(0.7725f, 0.8431f, 0.9686f), new Color(0.7725f, 0.8431f, 0.9686f), new Color(0.7725f, 0.8431f, 0.9686f), new Color(0.1882f, 0.4196f, 0.6745f),
                    };
                    break;

                case "Level_18":
                    levelColors = new Color[]
                    {
                new Color(0.7725f, 0.8431f, 0.9686f), new Color(0.6275f, 0.6275f, 0.6275f), new Color(0.6275f, 0.6275f, 0.6275f), new Color(0.1882f, 0.4196f, 0.6745f), new Color(0.7725f, 0.8431f, 0.9686f), new Color(0.6392f, 0.5059f, 0.4078f), new Color(0.7725f, 0.8431f, 0.9686f),
                new Color(0.7725f, 0.8431f, 0.9686f), new Color(0.6392f, 0.5059f, 0.4078f), new Color(0.6275f, 0.6275f, 0.6275f), new Color(0.1882f, 0.4196f, 0.6745f), new Color(0.7725f, 0.8431f, 0.9686f), new Color(0.6392f, 0.5059f, 0.4078f), new Color(0.6392f, 0.5059f, 0.4078f),
                new Color(0.1882f, 0.4196f, 0.6745f), new Color(0.1882f, 0.4196f, 0.6745f), new Color(0.6275f, 0.6275f, 0.6275f), new Color(0.1882f, 0.4196f, 0.6745f), new Color(0.1882f, 0.4196f, 0.6745f), new Color(0.7725f, 0.8431f, 0.9686f), new Color(0.7725f, 0.8431f, 0.9686f),
                new Color(0.1882f, 0.4196f, 0.6745f), new Color(0.7725f, 0.8431f, 0.9686f), new Color(0.6275f, 0.6275f, 0.6275f), new Color(0.6275f, 0.6275f, 0.6275f), new Color(0.1882f, 0.4196f, 0.6745f), new Color(0.1882f, 0.4196f, 0.6745f), new Color(0.1882f, 0.4196f, 0.6745f),
                new Color(0.1882f, 0.4196f, 0.6745f), new Color(0.7725f, 0.8431f, 0.9686f), new Color(0.7725f, 0.8431f, 0.9686f), new Color(0.6275f, 0.6275f, 0.6275f), new Color(0.6392f, 0.5059f, 0.4078f), new Color(0.6392f, 0.5059f, 0.4078f), new Color(0.1882f, 0.4196f, 0.6745f),
                new Color(0.1882f, 0.4196f, 0.6745f), new Color(0.1882f, 0.4196f, 0.6745f), new Color(0.7725f, 0.8431f, 0.9686f), new Color(0.6275f, 0.6275f, 0.6275f), new Color(0.6275f, 0.6275f, 0.6275f), new Color(0.6392f, 0.5059f, 0.4078f), new Color(0.6392f, 0.5059f, 0.4078f),
                new Color(0.6392f, 0.5059f, 0.4078f), new Color(0.1882f, 0.4196f, 0.6745f), new Color(0.7725f, 0.8431f, 0.9686f), new Color(0.7725f, 0.8431f, 0.9686f), new Color(0.6275f, 0.6275f, 0.6275f), new Color(0.6275f, 0.6275f, 0.6275f), new Color(0.6392f, 0.5059f, 0.4078f),
                new Color(0.6275f, 0.6275f, 0.6275f), new Color(0.1882f, 0.4196f, 0.6745f), new Color(0.1882f, 0.4196f, 0.6745f), new Color(0.7725f, 0.8431f, 0.9686f), new Color(0.7725f, 0.8431f, 0.9686f), new Color(0.6275f, 0.6275f, 0.6275f), new Color(0.6392f, 0.5059f, 0.4078f),
                new Color(0.6275f, 0.6275f, 0.6275f), new Color(0.6392f, 0.5059f, 0.4078f), new Color(0.1882f, 0.4196f, 0.6745f), new Color(0.1882f, 0.4196f, 0.6745f), new Color(0.7725f, 0.8431f, 0.9686f), new Color(0.6275f, 0.6275f, 0.6275f), new Color(0.6275f, 0.6275f, 0.6275f),
                    };
                    break;

                case "Level_19":
                    levelColors = new Color[]
                    {
                new Color(0.6275f, 0.6275f, 0.6275f), new Color(0.7725f, 0.8431f, 0.9686f), new Color(0.1882f, 0.4196f, 0.6745f), new Color(0.6392f, 0.5059f, 0.4078f), new Color(0.1882f, 0.4196f, 0.6745f), new Color(0.7725f, 0.8431f, 0.9686f), new Color(0.6275f, 0.6275f, 0.6275f),
                new Color(0.6275f, 0.6275f, 0.6275f), new Color(0.7725f, 0.8431f, 0.9686f), new Color(0.1882f, 0.4196f, 0.6745f), new Color(0.6392f, 0.5059f, 0.4078f), new Color(0.1882f, 0.4196f, 0.6745f), new Color(0.7725f, 0.8431f, 0.9686f), new Color(0.6275f, 0.6275f, 0.6275f),
                new Color(0.7725f, 0.8431f, 0.9686f), new Color(0.7725f, 0.8431f, 0.9686f), new Color(0.1882f, 0.4196f, 0.6745f), new Color(0.6392f, 0.5059f, 0.4078f), new Color(0.1882f, 0.4196f, 0.6745f), new Color(0.7725f, 0.8431f, 0.9686f), new Color(0.7725f, 0.8431f, 0.9686f),
                new Color(0.1882f, 0.4196f, 0.6745f), new Color(0.1882f, 0.4196f, 0.6745f), new Color(0.6392f, 0.5059f, 0.4078f), new Color(0.6392f, 0.5059f, 0.4078f), new Color(0.6392f, 0.5059f, 0.4078f), new Color(0.1882f, 0.4196f, 0.6745f), new Color(0.1882f, 0.4196f, 0.6745f),
                new Color(0.6392f, 0.5059f, 0.4078f), new Color(0.6392f, 0.5059f, 0.4078f), new Color(0.6392f, 0.5059f, 0.4078f), new Color(0.6275f, 0.6275f, 0.6275f), new Color(0.6392f, 0.5059f, 0.4078f), new Color(0.6392f, 0.5059f, 0.4078f), new Color(0.6392f, 0.5059f, 0.4078f),
                new Color(0.1882f, 0.4196f, 0.6745f), new Color(0.1882f, 0.4196f, 0.6745f), new Color(0.6392f, 0.5059f, 0.4078f), new Color(0.6392f, 0.5059f, 0.4078f), new Color(0.6392f, 0.5059f, 0.4078f), new Color(0.1882f, 0.4196f, 0.6745f), new Color(0.1882f, 0.4196f, 0.6745f),
                new Color(0.7725f, 0.8431f, 0.9686f), new Color(0.7725f, 0.8431f, 0.9686f), new Color(0.1882f, 0.4196f, 0.6745f), new Color(0.6392f, 0.5059f, 0.4078f), new Color(0.1882f, 0.4196f, 0.6745f), new Color(0.7725f, 0.8431f, 0.9686f), new Color(0.7725f, 0.8431f, 0.9686f),
                new Color(0.6275f, 0.6275f, 0.6275f), new Color(0.7725f, 0.8431f, 0.9686f), new Color(0.1882f, 0.4196f, 0.6745f), new Color(0.6392f, 0.5059f, 0.4078f), new Color(0.1882f, 0.4196f, 0.6745f), new Color(0.7725f, 0.8431f, 0.9686f), new Color(0.6275f, 0.6275f, 0.6275f),
                new Color(0.6275f, 0.6275f, 0.6275f), new Color(0.7725f, 0.8431f, 0.9686f), new Color(0.1882f, 0.4196f, 0.6745f), new Color(0.6392f, 0.5059f, 0.4078f), new Color(0.1882f, 0.4196f, 0.6745f), new Color(0.7725f, 0.8431f, 0.9686f), new Color(0.6275f, 0.6275f, 0.6275f),
                    };
                    break;

                case "Level_20":
                    levelColors = new Color[]
                    {
                new Color(0.6275f, 0.6275f, 0.6275f), new Color(0.6275f, 0.6275f, 0.6275f), new Color(0.6275f, 0.6275f, 0.6275f), new Color(0.6275f, 0.6275f, 0.6275f), new Color(0.6275f, 0.6275f, 0.6275f), new Color(0.7725f, 0.8431f, 0.9686f), new Color(0.7725f, 0.8431f, 0.9686f),
                new Color(0.6275f, 0.6275f, 0.6275f), new Color(0.7725f, 0.8431f, 0.9686f), new Color(0.7725f, 0.8431f, 0.9686f), new Color(0.7725f, 0.8431f, 0.9686f), new Color(0.6275f, 0.6275f, 0.6275f), new Color(0.7725f, 0.8431f, 0.9686f), new Color(0.7725f, 0.8431f, 0.9686f),
                new Color(0.6275f, 0.6275f, 0.6275f), new Color(0.7725f, 0.8431f, 0.9686f), new Color(0.7725f, 0.8431f, 0.9686f), new Color(0.7725f, 0.8431f, 0.9686f), new Color(0.6275f, 0.6275f, 0.6275f), new Color(0.7725f, 0.8431f, 0.9686f), new Color(0.7725f, 0.8431f, 0.9686f),
                new Color(0.6275f, 0.6275f, 0.6275f), new Color(0.7725f, 0.8431f, 0.9686f), new Color(0.1882f, 0.4196f, 0.6745f), new Color(0.1882f, 0.4196f, 0.6745f), new Color(0.1882f, 0.4196f, 0.6745f), new Color(0.1882f, 0.4196f, 0.6745f), new Color(0.1882f, 0.4196f, 0.6745f),
                new Color(0.6275f, 0.6275f, 0.6275f), new Color(0.6275f, 0.6275f, 0.6275f), new Color(0.6275f, 0.6275f, 0.6275f), new Color(0.6275f, 0.6275f, 0.6275f), new Color(0.6275f, 0.6275f, 0.6275f), new Color(0.7725f, 0.8431f, 0.9686f), new Color(0.1882f, 0.4196f, 0.6745f),
                new Color(0.7725f, 0.8431f, 0.9686f), new Color(0.7725f, 0.8431f, 0.9686f), new Color(0.1882f, 0.4196f, 0.6745f), new Color(0.7725f, 0.8431f, 0.9686f), new Color(0.7725f, 0.8431f, 0.9686f), new Color(0.7725f, 0.8431f, 0.9686f), new Color(0.1882f, 0.4196f, 0.6745f),
                new Color(0.7725f, 0.8431f, 0.9686f), new Color(0.6392f, 0.5059f, 0.4078f), new Color(0.1882f, 0.4196f, 0.6745f), new Color(0.6392f, 0.5059f, 0.4078f), new Color(0.7725f, 0.8431f, 0.9686f), new Color(0.7725f, 0.8431f, 0.9686f), new Color(0.1882f, 0.4196f, 0.6745f),
                new Color(0.7725f, 0.8431f, 0.9686f), new Color(0.6392f, 0.5059f, 0.4078f), new Color(0.1882f, 0.4196f, 0.6745f), new Color(0.6392f, 0.5059f, 0.4078f), new Color(0.1882f, 0.4196f, 0.6745f), new Color(0.1882f, 0.4196f, 0.6745f), new Color(0.1882f, 0.4196f, 0.6745f),
                new Color(0.7725f, 0.8431f, 0.9686f), new Color(0.6392f, 0.5059f, 0.4078f), new Color(0.6392f, 0.5059f, 0.4078f), new Color(0.6392f, 0.5059f, 0.4078f), new Color(0.7725f, 0.8431f, 0.9686f), new Color(0.7725f, 0.8431f, 0.9686f), new Color(0.7725f, 0.8431f, 0.9686f),
                    };
                    break;

                case "Level_21":
                    levelColors = new Color[]
                    {
                new Color(0.1882f, 0.4196f, 0.6745f), new Color(0.1882f, 0.4196f, 0.6745f), new Color(0.1882f, 0.4196f, 0.6745f), new Color(0.6392f, 0.5059f, 0.4078f), new Color(0.1882f, 0.4196f, 0.6745f), new Color(0.1882f, 0.4196f, 0.6745f), new Color(0.1882f, 0.4196f, 0.6745f),
                new Color(0.1882f, 0.4196f, 0.6745f), new Color(0.1882f, 0.4196f, 0.6745f), new Color(0.1882f, 0.4196f, 0.6745f), new Color(0.6275f, 0.6275f, 0.6275f), new Color(0.1882f, 0.4196f, 0.6745f), new Color(0.1882f, 0.4196f, 0.6745f), new Color(0.1882f, 0.4196f, 0.6745f),
                new Color(0.1882f, 0.4196f, 0.6745f), new Color(0.1882f, 0.4196f, 0.6745f), new Color(0.1882f, 0.4196f, 0.6745f), new Color(0.1882f, 0.4196f, 0.6745f), new Color(0.1882f, 0.4196f, 0.6745f), new Color(0.1882f, 0.4196f, 0.6745f), new Color(0.1882f, 0.4196f, 0.6745f),
                new Color(0.1882f, 0.4196f, 0.6745f), new Color(0.1882f, 0.4196f, 0.6745f), new Color(0.1882f, 0.4196f, 0.6745f), new Color(0.7725f, 0.8431f, 0.9686f), new Color(0.1882f, 0.4196f, 0.6745f), new Color(0.1882f, 0.4196f, 0.6745f), new Color(0.1882f, 0.4196f, 0.6745f),
                new Color(0.6275f, 0.6275f, 0.6275f), new Color(0.1882f, 0.4196f, 0.6745f), new Color(0.7725f, 0.8431f, 0.9686f), new Color(0.6275f, 0.6275f, 0.6275f), new Color(0.6392f, 0.5059f, 0.4078f), new Color(0.6275f, 0.6275f, 0.6275f), new Color(0.6275f, 0.6275f, 0.6275f),
                new Color(0.1882f, 0.4196f, 0.6745f), new Color(0.1882f, 0.4196f, 0.6745f), new Color(0.1882f, 0.4196f, 0.6745f), new Color(0.6392f, 0.5059f, 0.4078f), new Color(0.1882f, 0.4196f, 0.6745f), new Color(0.1882f, 0.4196f, 0.6745f), new Color(0.1882f, 0.4196f, 0.6745f),
                new Color(0.1882f, 0.4196f, 0.6745f), new Color(0.1882f, 0.4196f, 0.6745f), new Color(0.1882f, 0.4196f, 0.6745f), new Color(0.1882f, 0.4196f, 0.6745f), new Color(0.1882f, 0.4196f, 0.6745f), new Color(0.1882f, 0.4196f, 0.6745f), new Color(0.1882f, 0.4196f, 0.6745f),
                new Color(0.1882f, 0.4196f, 0.6745f), new Color(0.1882f, 0.4196f, 0.6745f), new Color(0.1882f, 0.4196f, 0.6745f), new Color(0.6275f, 0.6275f, 0.6275f), new Color(0.1882f, 0.4196f, 0.6745f), new Color(0.1882f, 0.4196f, 0.6745f), new Color(0.1882f, 0.4196f, 0.6745f),
                new Color(0.1882f, 0.4196f, 0.6745f), new Color(0.1882f, 0.4196f, 0.6745f), new Color(0.1882f, 0.4196f, 0.6745f), new Color(0.7725f, 0.8431f, 0.9686f), new Color(0.1882f, 0.4196f, 0.6745f), new Color(0.1882f, 0.4196f, 0.6745f), new Color(0.1882f, 0.4196f, 0.6745f),
                    };
                    break;

                case "Level_22":
                    levelColors = new Color[]
                    {
                new Color(0.7725f, 0.8431f, 0.9686f), new Color(0.7725f, 0.8431f, 0.9686f), new Color(0.7725f, 0.8431f, 0.9686f), new Color(0.1882f, 0.4196f, 0.6745f), new Color(0.1882f, 0.4196f, 0.6745f), new Color(0.1882f, 0.4196f, 0.6745f), new Color(0.7725f, 0.8431f, 0.9686f),
                new Color(0.7725f, 0.8431f, 0.9686f), new Color(0.1882f, 0.4196f, 0.6745f), new Color(0.1882f, 0.4196f, 0.6745f), new Color(0.1882f, 0.4196f, 0.6745f), new Color(0.1882f, 0.4196f, 0.6745f), new Color(0.7725f, 0.8431f, 0.9686f), new Color(0.7725f, 0.8431f, 0.9686f),
                new Color(0.7725f, 0.8431f, 0.9686f), new Color(0.7725f, 0.8431f, 0.9686f), new Color(0.1882f, 0.4196f, 0.6745f), new Color(0.7725f, 0.8431f, 0.9686f), new Color(0.7725f, 0.8431f, 0.9686f), new Color(0.7725f, 0.8431f, 0.9686f), new Color(0.6275f, 0.6275f, 0.6275f),
                new Color(0.6392f, 0.5059f, 0.4078f), new Color(0.7725f, 0.8431f, 0.9686f), new Color(0.7725f, 0.8431f, 0.9686f), new Color(0.7725f, 0.8431f, 0.9686f), new Color(0.6275f, 0.6275f, 0.6275f), new Color(0.7725f, 0.8431f, 0.9686f), new Color(0.6275f, 0.6275f, 0.6275f),
                new Color(0.6392f, 0.5059f, 0.4078f), new Color(0.6392f, 0.5059f, 0.4078f), new Color(0.7725f, 0.8431f, 0.9686f), new Color(0.6275f, 0.6275f, 0.6275f), new Color(0.6275f, 0.6275f, 0.6275f), new Color(0.7725f, 0.8431f, 0.9686f), new Color(0.6275f, 0.6275f, 0.6275f),
                new Color(0.6392f, 0.5059f, 0.4078f), new Color(0.7725f, 0.8431f, 0.9686f), new Color(0.7725f, 0.8431f, 0.9686f), new Color(0.7725f, 0.8431f, 0.9686f), new Color(0.6275f, 0.6275f, 0.6275f), new Color(0.6275f, 0.6275f, 0.6275f), new Color(0.6275f, 0.6275f, 0.6275f),
                new Color(0.7725f, 0.8431f, 0.9686f), new Color(0.7725f, 0.8431f, 0.9686f), new Color(0.6392f, 0.5059f, 0.4078f), new Color(0.7725f, 0.8431f, 0.9686f), new Color(0.7725f, 0.8431f, 0.9686f), new Color(0.7725f, 0.8431f, 0.9686f), new Color(0.6275f, 0.6275f, 0.6275f),
                new Color(0.7725f, 0.8431f, 0.9686f), new Color(0.6392f, 0.5059f, 0.4078f), new Color(0.6392f, 0.5059f, 0.4078f), new Color(0.7725f, 0.8431f, 0.9686f), new Color(0.6392f, 0.5059f, 0.4078f), new Color(0.7725f, 0.8431f, 0.9686f), new Color(0.7725f, 0.8431f, 0.9686f),
                new Color(0.7725f, 0.8431f, 0.9686f), new Color(0.7725f, 0.8431f, 0.9686f), new Color(0.6392f, 0.5059f, 0.4078f), new Color(0.6392f, 0.5059f, 0.4078f), new Color(0.6392f, 0.5059f, 0.4078f), new Color(0.6392f, 0.5059f, 0.4078f), new Color(0.7725f, 0.8431f, 0.9686f),
                    };
                    break;

                case "Level_23":
                    levelColors = new Color[]
                    {
                new Color(0.6275f, 0.6275f, 0.6275f), new Color(0.6275f, 0.6275f, 0.6275f), new Color(0.6275f, 0.6275f, 0.6275f), new Color(0.6275f, 0.6275f, 0.6275f), new Color(0.7725f, 0.8431f, 0.9686f), new Color(0.7725f, 0.8431f, 0.9686f), new Color(0.6392f, 0.5059f, 0.4078f),
                new Color(0.1882f, 0.4196f, 0.6745f), new Color(0.1882f, 0.4196f, 0.6745f), new Color(0.6275f, 0.6275f, 0.6275f), new Color(0.7725f, 0.8431f, 0.9686f), new Color(0.7725f, 0.8431f, 0.9686f), new Color(0.7725f, 0.8431f, 0.9686f), new Color(0.6392f, 0.5059f, 0.4078f),
                new Color(0.6392f, 0.5059f, 0.4078f), new Color(0.1882f, 0.4196f, 0.6745f), new Color(0.1882f, 0.4196f, 0.6745f), new Color(0.7725f, 0.8431f, 0.9686f), new Color(0.7725f, 0.8431f, 0.9686f), new Color(0.6275f, 0.6275f, 0.6275f), new Color(0.1882f, 0.4196f, 0.6745f),
                new Color(0.6392f, 0.5059f, 0.4078f), new Color(0.6392f, 0.5059f, 0.4078f), new Color(0.1882f, 0.4196f, 0.6745f), new Color(0.7725f, 0.8431f, 0.9686f), new Color(0.7725f, 0.8431f, 0.9686f), new Color(0.1882f, 0.4196f, 0.6745f), new Color(0.1882f, 0.4196f, 0.6745f),
                new Color(0.6392f, 0.5059f, 0.4078f), new Color(0.6392f, 0.5059f, 0.4078f), new Color(0.7725f, 0.8431f, 0.9686f), new Color(0.7725f, 0.8431f, 0.9686f), new Color(0.7725f, 0.8431f, 0.9686f), new Color(0.6392f, 0.5059f, 0.4078f), new Color(0.6392f, 0.5059f, 0.4078f),
                new Color(0.1882f, 0.4196f, 0.6745f), new Color(0.1882f, 0.4196f, 0.6745f), new Color(0.7725f, 0.8431f, 0.9686f), new Color(0.7725f, 0.8431f, 0.9686f), new Color(0.6392f, 0.5059f, 0.4078f), new Color(0.6392f, 0.5059f, 0.4078f), new Color(0.1882f, 0.4196f, 0.6745f),
                new Color(0.1882f, 0.4196f, 0.6745f), new Color(0.7725f, 0.8431f, 0.9686f), new Color(0.7725f, 0.8431f, 0.9686f), new Color(0.7725f, 0.8431f, 0.9686f), new Color(0.1882f, 0.4196f, 0.6745f), new Color(0.1882f, 0.4196f, 0.6745f), new Color(0.1882f, 0.4196f, 0.6745f),
                new Color(0.1882f, 0.4196f, 0.6745f), new Color(0.7725f, 0.8431f, 0.9686f), new Color(0.7725f, 0.8431f, 0.9686f), new Color(0.6275f, 0.6275f, 0.6275f), new Color(0.1882f, 0.4196f, 0.6745f), new Color(0.6275f, 0.6275f, 0.6275f), new Color(0.6275f, 0.6275f, 0.6275f),
                new Color(0.7725f, 0.8431f, 0.9686f), new Color(0.7725f, 0.8431f, 0.9686f), new Color(0.6275f, 0.6275f, 0.6275f), new Color(0.6275f, 0.6275f, 0.6275f), new Color(0.6275f, 0.6275f, 0.6275f), new Color(0.6275f, 0.6275f, 0.6275f), new Color(0.1882f, 0.4196f, 0.6745f),
                    };
                    break;

                case "Level_24":
                    levelColors = new Color[]
                    {
                new Color(0.6275f, 0.6275f, 0.6275f), new Color(0.6275f, 0.6275f, 0.6275f), new Color(0.6275f, 0.6275f, 0.6275f), new Color(0.6275f, 0.6275f, 0.6275f), new Color(0.6275f, 0.6275f, 0.6275f), new Color(0.6275f, 0.6275f, 0.6275f), new Color(0.6275f, 0.6275f, 0.6275f),
                new Color(0.6275f, 0.6275f, 0.6275f), new Color(0.6275f, 0.6275f, 0.6275f), new Color(0.6275f, 0.6275f, 0.6275f), new Color(0.1882f, 0.4196f, 0.6745f), new Color(0.6275f, 0.6275f, 0.6275f), new Color(0.6275f, 0.6275f, 0.6275f), new Color(0.6275f, 0.6275f, 0.6275f),
                new Color(0.6275f, 0.6275f, 0.6275f), new Color(0.6275f, 0.6275f, 0.6275f), new Color(0.1882f, 0.4196f, 0.6745f), new Color(0.7725f, 0.8431f, 0.9686f), new Color(0.1882f, 0.4196f, 0.6745f), new Color(0.6275f, 0.6275f, 0.6275f), new Color(0.6275f, 0.6275f, 0.6275f),
                new Color(0.6275f, 0.6275f, 0.6275f), new Color(0.1882f, 0.4196f, 0.6745f), new Color(0.7725f, 0.8431f, 0.9686f), new Color(0.6392f, 0.5059f, 0.4078f), new Color(0.7725f, 0.8431f, 0.9686f), new Color(0.1882f, 0.4196f, 0.6745f), new Color(0.6275f, 0.6275f, 0.6275f),
                new Color(0.1882f, 0.4196f, 0.6745f), new Color(0.7725f, 0.8431f, 0.9686f), new Color(0.6392f, 0.5059f, 0.4078f), new Color(0.6392f, 0.5059f, 0.4078f), new Color(0.6392f, 0.5059f, 0.4078f), new Color(0.7725f, 0.8431f, 0.9686f), new Color(0.1882f, 0.4196f, 0.6745f),
                new Color(0.6275f, 0.6275f, 0.6275f), new Color(0.1882f, 0.4196f, 0.6745f), new Color(0.7725f, 0.8431f, 0.9686f), new Color(0.6392f, 0.5059f, 0.4078f), new Color(0.7725f, 0.8431f, 0.9686f), new Color(0.1882f, 0.4196f, 0.6745f), new Color(0.6275f, 0.6275f, 0.6275f),
                new Color(0.6275f, 0.6275f, 0.6275f), new Color(0.6275f, 0.6275f, 0.6275f), new Color(0.1882f, 0.4196f, 0.6745f), new Color(0.7725f, 0.8431f, 0.9686f), new Color(0.1882f, 0.4196f, 0.6745f), new Color(0.6275f, 0.6275f, 0.6275f), new Color(0.6275f, 0.6275f, 0.6275f),
                new Color(0.6275f, 0.6275f, 0.6275f), new Color(0.6275f, 0.6275f, 0.6275f), new Color(0.6275f, 0.6275f, 0.6275f), new Color(0.1882f, 0.4196f, 0.6745f), new Color(0.6275f, 0.6275f, 0.6275f), new Color(0.6275f, 0.6275f, 0.6275f), new Color(0.6275f, 0.6275f, 0.6275f),
                new Color(0.6275f, 0.6275f, 0.6275f), new Color(0.6275f, 0.6275f, 0.6275f), new Color(0.6275f, 0.6275f, 0.6275f), new Color(0.6275f, 0.6275f, 0.6275f), new Color(0.6275f, 0.6275f, 0.6275f), new Color(0.6275f, 0.6275f, 0.6275f), new Color(0.6275f, 0.6275f, 0.6275f),
                    };
                    break;

            case "Level_25":
               levelColors = new Color[]
               {
                   new Color(0.7059f, 1.0f, 1.0f), new Color(0.7059f, 1.0f, 1.0f), new Color(0.7059f, 1.0f, 1.0f), new Color(0.7059f, 1.0f, 1.0f), new Color(0.7059f, 1.0f, 1.0f), new Color(0.7059f, 1.0f, 1.0f), new Color(0.7059f, 1.0f, 1.0f),
                   new Color(1.0f, 0.7176f, 0.7725f), new Color(1.0f, 0.7176f, 0.7725f), new Color(1.0f, 0.7176f, 0.7725f), new Color(1.0f, 0.7176f, 0.7725f), new Color(1.0f, 0.7176f, 0.7725f), new Color(1.0f, 0.7176f, 0.7725f), new Color(1.0f, 0.7176f, 0.7725f),
                   new Color(1.0f, 0.7176f, 0.7725f), new Color(0.9882f, 0.9059f, 0.4902f), new Color(0.9882f, 0.9059f, 0.4902f), new Color(0.9882f, 0.9059f, 0.4902f), new Color(0.9882f, 0.9059f, 0.4902f), new Color(0.9882f, 0.9059f, 0.4902f), new Color(1.0f, 0.7176f, 0.7725f),
                   new Color(1.0f, 0.7176f, 0.7725f), new Color(0.9882f, 0.9059f, 0.4902f), new Color(0.7059f, 1.0f, 1.0f), new Color(0.7059f, 1.0f, 1.0f), new Color(0.7059f, 1.0f, 1.0f), new Color(0.9882f, 0.9059f, 0.4902f), new Color(1.0f, 0.7176f, 0.7725f),
                   new Color(1.0f, 0.7176f, 0.7725f), new Color(0.9882f, 0.9059f, 0.4902f), new Color(0.7059f, 1.0f, 1.0f), new Color(1.0f, 0.7176f, 0.7725f), new Color(0.7059f, 1.0f, 1.0f), new Color(0.9882f, 0.9059f, 0.4902f), new Color(1.0f, 0.7176f, 0.7725f),
                   new Color(1.0f, 0.7176f, 0.7725f), new Color(0.9882f, 0.9059f, 0.4902f), new Color(0.7059f, 1.0f, 1.0f), new Color(0.7059f, 1.0f, 1.0f), new Color(0.7059f, 1.0f, 1.0f), new Color(0.9882f, 0.9059f, 0.4902f), new Color(1.0f, 0.7176f, 0.7725f),
                   new Color(1.0f, 0.7176f, 0.7725f), new Color(0.9882f, 0.9059f, 0.4902f), new Color(0.9882f, 0.9059f, 0.4902f), new Color(0.9882f, 0.9059f, 0.4902f), new Color(0.9882f, 0.9059f, 0.4902f), new Color(0.9882f, 0.9059f, 0.4902f), new Color(1.0f, 0.7176f, 0.7725f),
                   new Color(1.0f, 0.7176f, 0.7725f), new Color(1.0f, 0.7176f, 0.7725f), new Color(1.0f, 0.7176f, 0.7725f), new Color(1.0f, 0.7176f, 0.7725f), new Color(1.0f, 0.7176f, 0.7725f), new Color(1.0f, 0.7176f, 0.7725f), new Color(1.0f, 0.7176f, 0.7725f),
                   new Color(0.7059f, 1.0f, 1.0f), new Color(0.7059f, 1.0f, 1.0f), new Color(0.7059f, 1.0f, 1.0f), new Color(0.7059f, 1.0f, 1.0f), new Color(0.7059f, 1.0f, 1.0f), new Color(0.7059f, 1.0f, 1.0f), new Color(0.7059f, 1.0f, 1.0f),
               };
               break;

            case "Level_26":
               levelColors = new Color[]
               {
                   new Color(0.5961f, 0.9843f, 0.5961f), new Color(0.5961f, 0.9843f, 0.5961f), new Color(0.5961f, 0.9843f, 0.5961f), new Color(0.5961f, 0.9843f, 0.5961f), new Color(0.5961f, 0.9843f, 0.5961f), new Color(0.5961f, 0.9843f, 0.5961f), new Color(0.5961f, 0.9843f, 0.5961f),
                   new Color(0.5961f, 0.9843f, 0.5961f), new Color(1.0f, 0.7176f, 0.7725f), new Color(0.9882f, 0.9059f, 0.4902f), new Color(1.0f, 0.7176f, 0.7725f), new Color(0.9882f, 0.9059f, 0.4902f), new Color(1.0f, 0.7176f, 0.7725f), new Color(0.5961f, 0.9843f, 0.5961f),
                   new Color(0.5961f, 0.9843f, 0.5961f), new Color(0.9882f, 0.9059f, 0.4902f), new Color(0.5961f, 0.9843f, 0.5961f), new Color(0.9882f, 0.9059f, 0.4902f), new Color(0.5961f, 0.9843f, 0.5961f), new Color(0.9882f, 0.9059f, 0.4902f), new Color(0.5961f, 0.9843f, 0.5961f),
                   new Color(0.5961f, 0.9843f, 0.5961f), new Color(1.0f, 0.7176f, 0.7725f), new Color(0.9882f, 0.9059f, 0.4902f), new Color(1.0f, 0.7176f, 0.7725f), new Color(0.9882f, 0.9059f, 0.4902f), new Color(1.0f, 0.7176f, 0.7725f), new Color(0.5961f, 0.9843f, 0.5961f),
                   new Color(0.5961f, 0.9843f, 0.5961f), new Color(0.5961f, 0.9843f, 0.5961f), new Color(0.5961f, 0.9843f, 0.5961f), new Color(0.5961f, 0.9843f, 0.5961f), new Color(0.5961f, 0.9843f, 0.5961f), new Color(0.5961f, 0.9843f, 0.5961f), new Color(0.5961f, 0.9843f, 0.5961f),
                   new Color(0.5961f, 0.9843f, 0.5961f), new Color(1.0f, 0.7176f, 0.7725f), new Color(0.9882f, 0.9059f, 0.4902f), new Color(1.0f, 0.7176f, 0.7725f), new Color(0.9882f, 0.9059f, 0.4902f), new Color(1.0f, 0.7176f, 0.7725f), new Color(0.5961f, 0.9843f, 0.5961f),
                   new Color(0.5961f, 0.9843f, 0.5961f), new Color(0.9882f, 0.9059f, 0.4902f), new Color(0.5961f, 0.9843f, 0.5961f), new Color(0.9882f, 0.9059f, 0.4902f), new Color(0.5961f, 0.9843f, 0.5961f), new Color(0.9882f, 0.9059f, 0.4902f), new Color(0.5961f, 0.9843f, 0.5961f),
                   new Color(0.5961f, 0.9843f, 0.5961f), new Color(1.0f, 0.7176f, 0.7725f), new Color(0.9882f, 0.9059f, 0.4902f), new Color(1.0f, 0.7176f, 0.7725f), new Color(0.9882f, 0.9059f, 0.4902f), new Color(1.0f, 0.7176f, 0.7725f), new Color(0.5961f, 0.9843f, 0.5961f),
                   new Color(0.5961f, 0.9843f, 0.5961f), new Color(0.5961f, 0.9843f, 0.5961f), new Color(0.5961f, 0.9843f, 0.5961f), new Color(0.5961f, 0.9843f, 0.5961f), new Color(0.5961f, 0.9843f, 0.5961f), new Color(0.5961f, 0.9843f, 0.5961f), new Color(0.5961f, 0.9843f, 0.5961f),
               };
               break;

            case "Level_27":
               levelColors = new Color[]
               {
                   new Color(0.5961f, 0.9843f, 0.5961f), new Color(0.5961f, 0.9843f, 0.5961f), new Color(0.5961f, 0.9843f, 0.5961f), new Color(0.5961f, 0.9843f, 0.5961f), new Color(0.5961f, 0.9843f, 0.5961f), new Color(0.5961f, 0.9843f, 0.5961f), new Color(0.5961f, 0.9843f, 0.5961f),
                   new Color(0.9882f, 0.9059f, 0.4902f), new Color(0.5961f, 0.9843f, 0.5961f), new Color(0.5961f, 0.9843f, 0.5961f), new Color(1.0f, 0.7176f, 0.7725f), new Color(0.5961f, 0.9843f, 0.5961f), new Color(0.5961f, 0.9843f, 0.5961f), new Color(0.9882f, 0.9059f, 0.4902f),
                   new Color(0.9882f, 0.9059f, 0.4902f), new Color(0.7059f, 1.0f, 1.0f), new Color(0.5961f, 0.9843f, 0.5961f), new Color(0.5961f, 0.9843f, 0.5961f), new Color(0.5961f, 0.9843f, 0.5961f), new Color(0.7059f, 1.0f, 1.0f), new Color(0.9882f, 0.9059f, 0.4902f),
                   new Color(0.9882f, 0.9059f, 0.4902f), new Color(0.7059f, 1.0f, 1.0f), new Color(0.9882f, 0.9059f, 0.4902f), new Color(0.5961f, 0.9843f, 0.5961f), new Color(0.9882f, 0.9059f, 0.4902f), new Color(0.7059f, 1.0f, 1.0f), new Color(0.9882f, 0.9059f, 0.4902f),
                   new Color(0.9882f, 0.9059f, 0.4902f), new Color(0.7059f, 1.0f, 1.0f), new Color(0.9882f, 0.9059f, 0.4902f), new Color(0.5961f, 0.9843f, 0.5961f), new Color(0.9882f, 0.9059f, 0.4902f), new Color(0.7059f, 1.0f, 1.0f), new Color(0.9882f, 0.9059f, 0.4902f),
                   new Color(0.9882f, 0.9059f, 0.4902f), new Color(0.7059f, 1.0f, 1.0f), new Color(0.9882f, 0.9059f, 0.4902f), new Color(0.5961f, 0.9843f, 0.5961f), new Color(0.9882f, 0.9059f, 0.4902f), new Color(0.7059f, 1.0f, 1.0f), new Color(0.9882f, 0.9059f, 0.4902f),
                   new Color(0.9882f, 0.9059f, 0.4902f), new Color(0.7059f, 1.0f, 1.0f), new Color(0.9882f, 0.9059f, 0.4902f), new Color(0.5961f, 0.9843f, 0.5961f), new Color(0.9882f, 0.9059f, 0.4902f), new Color(0.7059f, 1.0f, 1.0f), new Color(0.9882f, 0.9059f, 0.4902f),
                   new Color(0.9882f, 0.9059f, 0.4902f), new Color(0.7059f, 1.0f, 1.0f), new Color(0.9882f, 0.9059f, 0.4902f), new Color(0.5961f, 0.9843f, 0.5961f), new Color(0.9882f, 0.9059f, 0.4902f), new Color(0.7059f, 1.0f, 1.0f), new Color(0.9882f, 0.9059f, 0.4902f),
                   new Color(0.5961f, 0.9843f, 0.5961f), new Color(0.5961f, 0.9843f, 0.5961f), new Color(0.5961f, 0.9843f, 0.5961f), new Color(0.5961f, 0.9843f, 0.5961f), new Color(0.5961f, 0.9843f, 0.5961f), new Color(0.5961f, 0.9843f, 0.5961f), new Color(0.5961f, 0.9843f, 0.5961f),
               };
               break;

            case "Level_28":
               levelColors = new Color[]
               {
                   new Color(0.7059f, 1.0f, 1.0f), new Color(0.7059f, 1.0f, 1.0f), new Color(1.0f, 0.7176f, 0.7725f), new Color(1.0f, 0.7176f, 0.7725f), new Color(1.0f, 0.7176f, 0.7725f), new Color(0.7059f, 1.0f, 1.0f), new Color(0.7059f, 1.0f, 1.0f),
                   new Color(0.7059f, 1.0f, 1.0f), new Color(0.7059f, 1.0f, 1.0f), new Color(1.0f, 0.7176f, 0.7725f), new Color(1.0f, 0.7176f, 0.7725f), new Color(1.0f, 0.7176f, 0.7725f), new Color(0.7059f, 1.0f, 1.0f), new Color(0.7059f, 1.0f, 1.0f),
                   new Color(0.7059f, 1.0f, 1.0f), new Color(0.7059f, 1.0f, 1.0f), new Color(0.7059f, 1.0f, 1.0f), new Color(1.0f, 0.7176f, 0.7725f), new Color(0.7059f, 1.0f, 1.0f), new Color(0.7059f, 1.0f, 1.0f), new Color(0.7059f, 1.0f, 1.0f),
                   new Color(0.9882f, 0.9059f, 0.4902f), new Color(0.9882f, 0.9059f, 0.4902f), new Color(0.7059f, 1.0f, 1.0f), new Color(1.0f, 0.7176f, 0.7725f), new Color(0.7059f, 1.0f, 1.0f), new Color(0.9882f, 0.9059f, 0.4902f), new Color(0.9882f, 0.9059f, 0.4902f),
                   new Color(0.9882f, 0.9059f, 0.4902f), new Color(1.0f, 0.7176f, 0.7725f), new Color(0.7059f, 1.0f, 1.0f), new Color(0.7059f, 1.0f, 1.0f), new Color(0.7059f, 1.0f, 1.0f), new Color(1.0f, 0.7176f, 0.7725f), new Color(0.9882f, 0.9059f, 0.4902f),
                   new Color(0.7059f, 1.0f, 1.0f), new Color(0.7059f, 1.0f, 1.0f), new Color(0.7059f, 1.0f, 1.0f), new Color(0.7059f, 1.0f, 1.0f), new Color(0.7059f, 1.0f, 1.0f), new Color(0.7059f, 1.0f, 1.0f), new Color(0.7059f, 1.0f, 1.0f),
                   new Color(0.7059f, 1.0f, 1.0f), new Color(0.7059f, 1.0f, 1.0f), new Color(0.5961f, 0.9843f, 0.5961f), new Color(0.5961f, 0.9843f, 0.5961f), new Color(0.5961f, 0.9843f, 0.5961f), new Color(0.7059f, 1.0f, 1.0f), new Color(0.7059f, 1.0f, 1.0f),
                   new Color(0.7059f, 1.0f, 1.0f), new Color(0.7059f, 1.0f, 1.0f), new Color(0.7059f, 1.0f, 1.0f), new Color(0.5961f, 0.9843f, 0.5961f), new Color(0.7059f, 1.0f, 1.0f), new Color(0.7059f, 1.0f, 1.0f), new Color(0.7059f, 1.0f, 1.0f),
                   new Color(0.9882f, 0.9059f, 0.4902f), new Color(0.7059f, 1.0f, 1.0f), new Color(0.7059f, 1.0f, 1.0f), new Color(0.7059f, 1.0f, 1.0f), new Color(0.7059f, 1.0f, 1.0f), new Color(0.7059f, 1.0f, 1.0f), new Color(0.9882f, 0.9059f, 0.4902f),
               };
               break;

            case "Level_29":
               levelColors = new Color[]
               {
                   new Color(0.5961f, 0.9843f, 0.5961f), new Color(0.5961f, 0.9843f, 0.5961f), new Color(0.5961f, 0.9843f, 0.5961f), new Color(0.9882f, 0.9059f, 0.4902f), new Color(0.5961f, 0.9843f, 0.5961f), new Color(0.5961f, 0.9843f, 0.5961f), new Color(0.5961f, 0.9843f, 0.5961f),
                   new Color(0.5961f, 0.9843f, 0.5961f), new Color(0.5961f, 0.9843f, 0.5961f), new Color(0.5961f, 0.9843f, 0.5961f), new Color(0.9882f, 0.9059f, 0.4902f), new Color(0.5961f, 0.9843f, 0.5961f), new Color(0.5961f, 0.9843f, 0.5961f), new Color(0.5961f, 0.9843f, 0.5961f),
                   new Color(0.5961f, 0.9843f, 0.5961f), new Color(0.5961f, 0.9843f, 0.5961f), new Color(0.5961f, 0.9843f, 0.5961f), new Color(0.9882f, 0.9059f, 0.4902f), new Color(0.5961f, 0.9843f, 0.5961f), new Color(0.5961f, 0.9843f, 0.5961f), new Color(0.5961f, 0.9843f, 0.5961f),
                   new Color(0.5961f, 0.9843f, 0.5961f), new Color(0.5961f, 0.9843f, 0.5961f), new Color(1.0f, 0.7176f, 0.7725f), new Color(1.0f, 0.7176f, 0.7725f), new Color(1.0f, 0.7176f, 0.7725f), new Color(0.5961f, 0.9843f, 0.5961f), new Color(0.5961f, 0.9843f, 0.5961f),
                   new Color(0.9882f, 0.9059f, 0.4902f), new Color(0.9882f, 0.9059f, 0.4902f), new Color(1.0f, 0.7176f, 0.7725f), new Color(0.9882f, 0.9059f, 0.4902f), new Color(1.0f, 0.7176f, 0.7725f), new Color(0.9882f, 0.9059f, 0.4902f), new Color(0.9882f, 0.9059f, 0.4902f),
                   new Color(0.5961f, 0.9843f, 0.5961f), new Color(0.5961f, 0.9843f, 0.5961f), new Color(1.0f, 0.7176f, 0.7725f), new Color(1.0f, 0.7176f, 0.7725f), new Color(1.0f, 0.7176f, 0.7725f), new Color(0.5961f, 0.9843f, 0.5961f), new Color(0.5961f, 0.9843f, 0.5961f),
                   new Color(0.5961f, 0.9843f, 0.5961f), new Color(0.5961f, 0.9843f, 0.5961f), new Color(0.5961f, 0.9843f, 0.5961f), new Color(0.9882f, 0.9059f, 0.4902f), new Color(0.5961f, 0.9843f, 0.5961f), new Color(0.5961f, 0.9843f, 0.5961f), new Color(0.5961f, 0.9843f, 0.5961f),
                   new Color(0.5961f, 0.9843f, 0.5961f), new Color(0.5961f, 0.9843f, 0.5961f), new Color(0.5961f, 0.9843f, 0.5961f), new Color(0.9882f, 0.9059f, 0.4902f), new Color(0.5961f, 0.9843f, 0.5961f), new Color(0.5961f, 0.9843f, 0.5961f), new Color(0.5961f, 0.9843f, 0.5961f),
                   new Color(0.5961f, 0.9843f, 0.5961f), new Color(0.5961f, 0.9843f, 0.5961f), new Color(0.5961f, 0.9843f, 0.5961f), new Color(0.9882f, 0.9059f, 0.4902f), new Color(0.5961f, 0.9843f, 0.5961f), new Color(0.5961f, 0.9843f, 0.5961f), new Color(0.5961f, 0.9843f, 0.5961f),
               };
               break;

            case "Level_30":
               levelColors = new Color[]
               {
                   new Color(1.0f, 0.7176f, 0.7725f), new Color(0.7059f, 1.0f, 1.0f), new Color(0.7059f, 1.0f, 1.0f), new Color(0.7059f, 1.0f, 1.0f), new Color(0.7059f, 1.0f, 1.0f), new Color(0.7059f, 1.0f, 1.0f), new Color(0.9882f, 0.9059f, 0.4902f),
                   new Color(0.7059f, 1.0f, 1.0f), new Color(0.7059f, 1.0f, 1.0f), new Color(0.9882f, 0.9059f, 0.4902f), new Color(0.9882f, 0.9059f, 0.4902f), new Color(0.7059f, 1.0f, 1.0f), new Color(0.9882f, 0.9059f, 0.4902f), new Color(0.9882f, 0.9059f, 0.4902f),
                   new Color(0.5961f, 0.9843f, 0.5961f), new Color(0.7059f, 1.0f, 1.0f), new Color(0.5961f, 0.9843f, 0.5961f), new Color(0.9882f, 0.9059f, 0.4902f), new Color(0.6431f, 0.4275f, 0.6784f), new Color(0.9882f, 0.9059f, 0.4902f), new Color(0.9882f, 0.9059f, 0.4902f),
                   new Color(0.5961f, 0.9843f, 0.5961f), new Color(0.9882f, 0.9059f, 0.4902f), new Color(0.9882f, 0.9059f, 0.4902f), new Color(0.9882f, 0.9059f, 0.4902f), new Color(0.6431f, 0.4275f, 0.6784f), new Color(0.9882f, 0.9059f, 0.4902f), new Color(1.0f, 0.7176f, 0.7725f),
                   new Color(0.7059f, 1.0f, 1.0f), new Color(0.7059f, 1.0f, 1.0f), new Color(0.9882f, 0.9059f, 0.4902f), new Color(0.5961f, 0.9843f, 0.5961f), new Color(0.6431f, 0.4275f, 0.6784f), new Color(1.0f, 0.7176f, 0.7725f), new Color(1.0f, 0.7176f, 0.7725f),
                   new Color(0.7059f, 1.0f, 1.0f), new Color(0.6431f, 0.4275f, 0.6784f), new Color(0.6431f, 0.4275f, 0.6784f), new Color(0.6431f, 0.4275f, 0.6784f), new Color(0.6431f, 0.4275f, 0.6784f), new Color(0.6431f, 0.4275f, 0.6784f), new Color(0.6431f, 0.4275f, 0.6784f),
                   new Color(0.5961f, 0.9843f, 0.5961f), new Color(0.5961f, 0.9843f, 0.5961f), new Color(0.5961f, 0.9843f, 0.5961f), new Color(1.0f, 0.7176f, 0.7725f), new Color(0.9882f, 0.9059f, 0.4902f), new Color(0.9882f, 0.9059f, 0.4902f), new Color(0.6431f, 0.4275f, 0.6784f),
                   new Color(0.5961f, 0.9843f, 0.5961f), new Color(0.5961f, 0.9843f, 0.5961f), new Color(1.0f, 0.7176f, 0.7725f), new Color(0.5961f, 0.9843f, 0.5961f), new Color(0.9882f, 0.9059f, 0.4902f), new Color(0.6431f, 0.4275f, 0.6784f), new Color(0.6431f, 0.4275f, 0.6784f),
                   new Color(0.5961f, 0.9843f, 0.5961f), new Color(1.0f, 0.7176f, 0.7725f), new Color(1.0f, 0.7176f, 0.7725f), new Color(0.5961f, 0.9843f, 0.5961f), new Color(0.6431f, 0.4275f, 0.6784f), new Color(0.6431f, 0.4275f, 0.6784f), new Color(1.0f, 0.7176f, 0.7725f),
               };
               break;

             case "Level_31":
               levelColors = new Color[]
               {
                   new Color(1.0f, 0.7176f, 0.7725f), new Color(0.9882f, 0.9059f, 0.4902f), new Color(0.9882f, 0.9059f, 0.4902f), new Color(0.9882f, 0.9059f, 0.4902f), new Color(0.9882f, 0.9059f, 0.4902f), new Color(0.9882f, 0.9059f, 0.4902f), new Color(1.0f, 0.7176f, 0.7725f),
                   new Color(1.0f, 0.7176f, 0.7725f), new Color(0.9882f, 0.9059f, 0.4902f), new Color(0.5961f, 0.9843f, 0.5961f), new Color(0.5961f, 0.9843f, 0.5961f), new Color(0.5961f, 0.9843f, 0.5961f), new Color(0.9882f, 0.9059f, 0.4902f), new Color(1.0f, 0.7176f, 0.7725f),
                   new Color(0.9882f, 0.9059f, 0.4902f), new Color(1.0f, 0.7176f, 0.7725f), new Color(0.9882f, 0.9059f, 0.4902f), new Color(0.9882f, 0.9059f, 0.4902f), new Color(0.9882f, 0.9059f, 0.4902f), new Color(0.5961f, 0.9843f, 0.5961f), new Color(0.9882f, 0.9059f, 0.4902f),
                   new Color(0.5961f, 0.9843f, 0.5961f), new Color(0.9882f, 0.9059f, 0.4902f), new Color(1.0f, 0.7176f, 0.7725f), new Color(1.0f, 0.7176f, 0.7725f), new Color(0.9882f, 0.9059f, 0.4902f), new Color(0.9882f, 0.9059f, 0.4902f), new Color(0.5961f, 0.9843f, 0.5961f),
                   new Color(0.5961f, 0.9843f, 0.5961f), new Color(0.9882f, 0.9059f, 0.4902f), new Color(0.9882f, 0.9059f, 0.4902f), new Color(0.9882f, 0.9059f, 0.4902f), new Color(0.9882f, 0.9059f, 0.4902f), new Color(0.9882f, 0.9059f, 0.4902f), new Color(0.5961f, 0.9843f, 0.5961f),
                   new Color(0.5961f, 0.9843f, 0.5961f), new Color(0.9882f, 0.9059f, 0.4902f), new Color(0.9882f, 0.9059f, 0.4902f), new Color(0.7059f, 1.0f, 1.0f), new Color(0.7059f, 1.0f, 1.0f), new Color(0.9882f, 0.9059f, 0.4902f), new Color(0.5961f, 0.9843f, 0.5961f),
                   new Color(0.9882f, 0.9059f, 0.4902f), new Color(0.5961f, 0.9843f, 0.5961f), new Color(0.9882f, 0.9059f, 0.4902f), new Color(0.9882f, 0.9059f, 0.4902f), new Color(0.9882f, 0.9059f, 0.4902f), new Color(0.7059f, 1.0f, 1.0f), new Color(0.9882f, 0.9059f, 0.4902f),
                   new Color(0.7059f, 1.0f, 1.0f), new Color(0.9882f, 0.9059f, 0.4902f), new Color(0.5961f, 0.9843f, 0.5961f), new Color(0.5961f, 0.9843f, 0.5961f), new Color(0.5961f, 0.9843f, 0.5961f), new Color(0.9882f, 0.9059f, 0.4902f), new Color(0.7059f, 1.0f, 1.0f),
                   new Color(0.7059f, 1.0f, 1.0f), new Color(0.9882f, 0.9059f, 0.4902f), new Color(0.9882f, 0.9059f, 0.4902f), new Color(0.9882f, 0.9059f, 0.4902f), new Color(0.9882f, 0.9059f, 0.4902f), new Color(0.9882f, 0.9059f, 0.4902f), new Color(0.7059f, 1.0f, 1.0f),
               };
               break;

            case "Level_32":
               levelColors = new Color[]
               {
                   new Color(0.7059f, 1.0f, 1.0f), new Color(0.9882f, 0.9059f, 0.4902f), new Color(0.9882f, 0.9059f, 0.4902f), new Color(0.5961f, 0.9843f, 0.5961f), new Color(0.9882f, 0.9059f, 0.4902f), new Color(0.9882f, 0.9059f, 0.4902f), new Color(0.7059f, 1.0f, 1.0f),
                   new Color(0.7059f, 1.0f, 1.0f), new Color(0.9882f, 0.9059f, 0.4902f), new Color(0.9882f, 0.9059f, 0.4902f), new Color(0.9882f, 0.9059f, 0.4902f), new Color(0.9882f, 0.9059f, 0.4902f), new Color(0.9882f, 0.9059f, 0.4902f), new Color(0.7059f, 1.0f, 1.0f),
                   new Color(0.7059f, 1.0f, 1.0f), new Color(0.7059f, 1.0f, 1.0f), new Color(0.9882f, 0.9059f, 0.4902f), new Color(0.9882f, 0.9059f, 0.4902f), new Color(0.9882f, 0.9059f, 0.4902f), new Color(0.7059f, 1.0f, 1.0f), new Color(0.7059f, 1.0f, 1.0f),
                   new Color(0.9882f, 0.9059f, 0.4902f), new Color(0.7059f, 1.0f, 1.0f), new Color(0.7059f, 1.0f, 1.0f), new Color(0.9882f, 0.9059f, 0.4902f), new Color(0.7059f, 1.0f, 1.0f), new Color(0.7059f, 1.0f, 1.0f), new Color(0.9882f, 0.9059f, 0.4902f),
                   new Color(1.0f, 0.7176f, 0.7725f), new Color(0.9882f, 0.9059f, 0.4902f), new Color(0.7059f, 1.0f, 1.0f), new Color(0.7059f, 1.0f, 1.0f), new Color(0.7059f, 1.0f, 1.0f), new Color(0.9882f, 0.9059f, 0.4902f), new Color(1.0f, 0.7176f, 0.7725f),
                   new Color(0.9882f, 0.9059f, 0.4902f), new Color(0.7059f, 1.0f, 1.0f), new Color(0.7059f, 1.0f, 1.0f), new Color(0.9882f, 0.9059f, 0.4902f), new Color(0.7059f, 1.0f, 1.0f), new Color(0.7059f, 1.0f, 1.0f), new Color(0.9882f, 0.9059f, 0.4902f),
                   new Color(0.7059f, 1.0f, 1.0f), new Color(0.7059f, 1.0f, 1.0f), new Color(0.9882f, 0.9059f, 0.4902f), new Color(0.9882f, 0.9059f, 0.4902f), new Color(0.9882f, 0.9059f, 0.4902f), new Color(0.7059f, 1.0f, 1.0f), new Color(0.7059f, 1.0f, 1.0f),
                   new Color(0.7059f, 1.0f, 1.0f), new Color(0.9882f, 0.9059f, 0.4902f), new Color(0.9882f, 0.9059f, 0.4902f), new Color(0.5961f, 0.9843f, 0.5961f), new Color(0.9882f, 0.9059f, 0.4902f), new Color(0.9882f, 0.9059f, 0.4902f), new Color(0.7059f, 1.0f, 1.0f),
                   new Color(0.7059f, 1.0f, 1.0f), new Color(0.9882f, 0.9059f, 0.4902f), new Color(0.5961f, 0.9843f, 0.5961f), new Color(0.5961f, 0.9843f, 0.5961f), new Color(0.5961f, 0.9843f, 0.5961f), new Color(0.9882f, 0.9059f, 0.4902f), new Color(0.7059f, 1.0f, 1.0f),
               };
               break;

            case "Level_33":
                levelColors = new Color[]
                 {
                   new Color(0.9451f, 0.8275f, 0.0078f), new Color(0.9451f, 0.8275f, 0.0078f), new Color(0.9451f, 0.8275f, 0.0078f), new Color(0.9451f, 0.8275f, 0.0078f), new Color(0.9451f, 0.8275f, 0.0078f), new Color(0.9451f, 0.8275f, 0.0078f), new Color(0.9451f, 0.8275f, 0.0078f),
                   new Color(0.9451f, 0.8275f, 0.0078f), new Color(0.9451f, 0.8275f, 0.0078f), new Color(0.9451f, 0.8275f, 0.0078f), new Color(0.9451f, 0.8275f, 0.0078f), new Color(0.9451f, 0.8275f, 0.0078f), new Color(0.9451f, 0.8275f, 0.0078f), new Color(0.9451f, 0.8275f, 0.0078f),
                   new Color(0.9451f, 0.8275f, 0.0078f), new Color(0.9451f, 0.8275f, 0.0078f), new Color(0.9451f, 0.8275f, 0.0078f), new Color(0.9451f, 0.8275f, 0.0078f), new Color(0.9451f, 0.8275f, 0.0078f), new Color(0.9451f, 0.8275f, 0.0078f), new Color(0.9451f, 0.8275f, 0.0078f),
                   new Color(0.9451f, 0.8275f, 0.0078f), new Color(0.9451f, 0.8275f, 0.0078f), new Color(0.9451f, 0.8275f, 0.0078f), new Color(0.9451f, 0.8275f, 0.0078f), new Color(0.9451f, 0.8275f, 0.0078f), new Color(0.9451f, 0.8275f, 0.0078f), new Color(0.9451f, 0.8275f, 0.0078f),
                   new Color(0.9451f, 0.8275f, 0.0078f), new Color(0.9451f, 0.8275f, 0.0078f), new Color(0.9451f, 0.8275f, 0.0078f), new Color(0.9569f, 0.3765f, 0.2039f), new Color(0.9451f, 0.8275f, 0.0078f), new Color(0.9451f, 0.8275f, 0.0078f), new Color(0.9451f, 0.8275f, 0.0078f),
                   new Color(0.9451f, 0.8275f, 0.0078f), new Color(0.9451f, 0.8275f, 0.0078f), new Color(0.9451f, 0.8275f, 0.0078f), new Color(0.9451f, 0.8275f, 0.0078f), new Color(0.9451f, 0.8275f, 0.0078f), new Color(0.9451f, 0.8275f, 0.0078f), new Color(0.9451f, 0.8275f, 0.0078f),
                   new Color(0.9451f, 0.8275f, 0.0078f), new Color(0.9451f, 0.8275f, 0.0078f), new Color(0.9451f, 0.8275f, 0.0078f), new Color(0.9451f, 0.8275f, 0.0078f), new Color(0.9451f, 0.8275f, 0.0078f), new Color(0.9451f, 0.8275f, 0.0078f), new Color(0.9451f, 0.8275f, 0.0078f),
                   new Color(0.9451f, 0.8275f, 0.0078f), new Color(0.9451f, 0.8275f, 0.0078f), new Color(0.9451f, 0.8275f, 0.0078f), new Color(0.9451f, 0.8275f, 0.0078f), new Color(0.9451f, 0.8275f, 0.0078f), new Color(0.9451f, 0.8275f, 0.0078f), new Color(0.9451f, 0.8275f, 0.0078f),
                   new Color(0.9451f, 0.8275f, 0.0078f), new Color(0.9451f, 0.8275f, 0.0078f), new Color(0.9451f, 0.8275f, 0.0078f), new Color(0.9451f, 0.8275f, 0.0078f), new Color(0.9451f, 0.8275f, 0.0078f), new Color(0.9451f, 0.8275f, 0.0078f), new Color(0.9451f, 0.8275f, 0.0078f),
                 };
                break;

            default:
                Debug.LogError("Nivel no definido.");
                break;
        }

    }
}