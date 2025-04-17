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

    [Tooltip("Coordenadas (x,y) de las celdas que tendrán Portales en este nivel (solo niveles 17-32).")]
    public List<Vector2Int> portalPositions;
    public GameObject portalPrefab;
    public GridCell[,] gridCells;

    public GameObject bombPrefab;                // Prefab de la Bomba (asignado vía Inspector)
    public List<Vector2Int> bombPositions;       // Lista de coordenadas (columna,fila) donde colocar bombas en este nivel

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

        InstantiatePortals();

        foreach (Vector2Int bombPos in bombPositions)
        {
            // Instanciar el prefab de Bomba en la posición correspondiente del mundo
            GridCell cell = gridCells[bombPos.x, bombPos.y];
            Vector3 worldPosition = cell.transform.position;  // posición mundial de la celda
            Bomb newBomb = Instantiate(bombPrefab, worldPosition, Quaternion.identity).GetComponent<Bomb>();
            // Inicializar la bomba con su posición de grid y referencia al GridManager
            newBomb.Initialize(bombPos, this);
            // Vincular la bomba a la celda en la que está colocada
            cell.bomb = newBomb;
        }
    }
    public void UseAttempt()
    {
        if (remainingAttempts > 0)
        {
            remainingAttempts--;
            UpdateAttemptsUI();

            if (remainingAttempts <= 0)
            {
                StartCoroutine(WaitForPropagations());
            }
        }
    }

    public GridCell GetCellAt(int x, int y)
    {
        if (x < 0 || x >= columns || y < 0 || y >= rows)
        {
            return null;
        }

        return gridCells[x, y];
    }

    private void InstantiatePortals()
    {
        // Verificar si este nivel requiere portales (por ejemplo, nivel 17-32)
        // Si tienes una variable de nivel actual, podrías hacer algo como:
        // if (currentLevel < 17 || currentLevel > 32) return;
        // (Asumiendo que portalPositions está vacía en niveles que no aplican, también valdría con comprobar la lista vacía)

        if (portalPositions == null || portalPositions.Count == 0)
            return; // No hay portales para instanciar en este nivel

        foreach (Vector2Int coord in portalPositions)
        {
            // Obtener la celda de esa coordenada (asumiendo que tienes un método o matriz de celdas)
            GridCell cell = GetCellAt(coord.x, coord.y);
            if (cell == null)
            {
                Debug.LogWarning("Coordenada de Portal fuera de rango: " + coord);
                continue;
            }

            // Instanciar el prefab del Portal sobre la posición de la celda
            Vector3 worldPos = cell.transform.position;
            GameObject portalGO = Instantiate(portalPrefab, worldPos, Quaternion.identity);
            // Asegurar que el Portal aparece por encima de la celda (se puede ajustar la posición z o el sorting layer del sprite)
            portalGO.transform.SetParent(this.transform);  // opcional: hacer hijo del GridManager para organización

            // Configurar el Portal instanciado
            Portal portal = portalGO.GetComponent<Portal>();
            if (portal != null)
            {
                portal.linkedCell = cell;       // Vincular el Portal con su celda
                // Registrar en la celda que tiene un portal (para notificar cambios de color)
                cell.linkedPortal = portal;
            }
            else
            {
                Debug.LogError("El prefab de Portal no tiene el script Portal adjunto.");
            }
        }
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
        gridCells = new GridCell[columns, rows];

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
                Vector3 position = new Vector3(x - offsetX + 1.06f + (x * cellSpacing), -(y - offsetY + 1.74f) - (y * cellSpacing), 0);
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
                    gridCells[x, y] = gridCell;
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

    private IEnumerator ShowFailPanel()
    {
        yield return new WaitForSeconds(0.5f); // Espera 0.5 segundos
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
                LevelSoundManager.instance.PlayLoseSound();
            }
        }

        isCheckingRestart = false;
    }
    public IEnumerator AnimateVictory()
    {
        yield return new WaitForSeconds(1f); // Esperar 1 segundo antes de iniciar la animación

        foreach (GridCell cell in gridCells)
        {
            if (cell != null)
            {
                StartCoroutine(cell.AnimateVictoryEffect());
            }
        }

        yield return new WaitForSeconds(1.5f); // Esperar la animación antes de continuar
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
                new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.8510f, 0.3255f, 0.3098f),
                new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.8510f, 0.3255f, 0.3098f),
                new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.8510f, 0.3255f, 0.3098f),
                new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.8510f, 0.3255f, 0.3098f),
                new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.8510f, 0.3255f, 0.3098f),
                new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.8510f, 0.3255f, 0.3098f),
                new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.8510f, 0.3255f, 0.3098f),
                new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.8510f, 0.3255f, 0.3098f),
                };
                break;

            case "Level_2":
                levelColors = new Color[]
                {
                new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.8510f, 0.3255f, 0.3098f),
                new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.8510f, 0.3255f, 0.3098f),
                new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.8510f, 0.3255f, 0.3098f),
                new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.8510f, 0.3255f, 0.3098f),
                new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.8510f, 0.3255f, 0.3098f),
                new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.8510f, 0.3255f, 0.3098f),
                new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.8510f, 0.3255f, 0.3098f),
                new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.8510f, 0.3255f, 0.3098f),
                new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.8510f, 0.3255f, 0.3098f),
                };
                break;       

                case "Level_3":
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

                case "Level_4":
                    levelColors = new Color[]
                    {
                new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.8510f, 0.3255f, 0.3098f),
                new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.8510f, 0.3255f, 0.3098f),
                new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f),
                new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f),
                new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f),
                new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f),
                new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f),
                new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.8510f, 0.3255f, 0.3098f),
                new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.8510f, 0.3255f, 0.3098f),
                    };
                    break;

             case "Level_5":
                levelColors = new Color[]
                {
            new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.2902f, 0.5647f, 0.8863f),
            new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.2902f, 0.5647f, 0.8863f),
            new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.2902f, 0.5647f, 0.8863f),
            new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.2902f, 0.5647f, 0.8863f),
            new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.8510f, 0.3255f, 0.3098f),
            new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.8510f, 0.3255f, 0.3098f),
            new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.8510f, 0.3255f, 0.3098f),
            new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.8510f, 0.3255f, 0.3098f),
            new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.8510f, 0.3255f, 0.3098f),
                };
                break;

                case "Level_6":
                    levelColors = new Color[]
                    {
                new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.2902f, 0.5647f, 0.8863f),
                new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.9608f, 0.7725f, 0.0941f),
                new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.6431f, 0.4275f, 0.6784f), new Color(0.6431f, 0.4275f, 0.6784f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.6431f, 0.4275f, 0.6784f), new Color(0.6431f, 0.4275f, 0.6784f), new Color(0.9608f, 0.7725f, 0.0941f),
                new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.6431f, 0.4275f, 0.6784f), new Color(0.6431f, 0.4275f, 0.6784f), new Color(0.6431f, 0.4275f, 0.6784f), new Color(0.6431f, 0.4275f, 0.6784f), new Color(0.6431f, 0.4275f, 0.6784f), new Color(0.9608f, 0.7725f, 0.0941f),
                new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.6431f, 0.4275f, 0.6784f), new Color(0.6431f, 0.4275f, 0.6784f), new Color(0.6431f, 0.4275f, 0.6784f), new Color(0.6431f, 0.4275f, 0.6784f), new Color(0.6431f, 0.4275f, 0.6784f), new Color(0.9608f, 0.7725f, 0.0941f),
                new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.6431f, 0.4275f, 0.6784f), new Color(0.6431f, 0.4275f, 0.6784f), new Color(0.6431f, 0.4275f, 0.6784f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.9608f, 0.7725f, 0.0941f),
                new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.6431f, 0.4275f, 0.6784f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.2902f, 0.5647f, 0.8863f),
                new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.2902f, 0.5647f, 0.8863f),
                new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.2902f, 0.5647f, 0.8863f),
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
                new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f),
                new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f),
                new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f),
                new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.6431f, 0.4275f, 0.6784f), new Color(0.6431f, 0.4275f, 0.6784f), new Color(0.6431f, 0.4275f, 0.6784f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f),
                new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.6431f, 0.4275f, 0.6784f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.6431f, 0.4275f, 0.6784f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.9608f, 0.7725f, 0.0941f),
                new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.6431f, 0.4275f, 0.6784f), new Color(0.6431f, 0.4275f, 0.6784f), new Color(0.6431f, 0.4275f, 0.6784f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f),
                new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f),
                new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f),
                new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f),
                    };
                    break;

                case "Level_9":
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

                case "Level_10":
                    levelColors = new Color[]
                    {
                new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f),
                new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.4253f, 0.7490f, 0.5176f),
                new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.4253f, 0.7490f, 0.5176f),
                new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.4253f, 0.7490f, 0.5176f),
                new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f),
                new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.4253f, 0.7490f, 0.5176f),
                new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.4253f, 0.7490f, 0.5176f),
                new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.4253f, 0.7490f, 0.5176f),
                new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f),
                    };
                    break;

                case "Level_11":
                    levelColors = new Color[]
                    {
                new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.8510f, 0.3255f, 0.3098f),
                new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.9608f, 0.7725f, 0.0941f),
                new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.9608f, 0.7725f, 0.0941f),
                new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.9608f, 0.7725f, 0.0941f),
                new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.2902f, 0.5647f, 0.8863f),
                new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f),
                new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.9608f, 0.7725f, 0.0941f),
                new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.2902f, 0.5647f, 0.8863f),
                new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.9608f, 0.7725f, 0.0941f),
                    };
                    break;

                case "Level_12":
                    levelColors = new Color[]
                    {
                new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.2902f, 0.5647f, 0.8863f),
                new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.2902f, 0.5647f, 0.8863f),
                new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.6431f, 0.4275f, 0.6784f), new Color(0.6431f, 0.4275f, 0.6784f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.2902f, 0.5647f, 0.8863f),
                new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.6431f, 0.4275f, 0.6784f), new Color(0.6431f, 0.4275f, 0.6784f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.2902f, 0.5647f, 0.8863f),
                new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.6431f, 0.4275f, 0.6784f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.9608f, 0.7725f, 0.0941f),
                new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.6431f, 0.4275f, 0.6784f), new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.9608f, 0.7725f, 0.0941f),
                new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.8510f, 0.3255f, 0.3098f),
                new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.8510f, 0.3255f, 0.3098f),
                new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.8510f, 0.3255f, 0.3098f),
                    };
                    break;

                case "Level_13":
                    levelColors = new Color[]
                    {
                new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.4253f, 0.7490f, 0.5176f),
                new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.4253f, 0.7490f, 0.5176f),
                new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.9608f, 0.7725f, 0.0941f),
                new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.2902f, 0.5647f, 0.8863f),
                new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.2902f, 0.5647f, 0.8863f),
                new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.2902f, 0.5647f, 0.8863f),
                new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.9608f, 0.7725f, 0.0941f),
                new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.8510f, 0.3255f, 0.3098f),
                new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.8510f, 0.3255f, 0.3098f),
                    };
                    break;

                case "Level_14":
                    levelColors = new Color[]
                    {
                new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.8510f, 0.3255f, 0.3098f),
                new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.8510f, 0.3255f, 0.3098f),
                new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.6431f, 0.4275f, 0.6784f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.8510f, 0.3255f, 0.3098f),
                new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.6431f, 0.4275f, 0.6784f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.8510f, 0.3255f, 0.3098f),
                new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.6431f, 0.4275f, 0.6784f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.9608f, 0.7725f, 0.0941f),
                new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.6431f, 0.4275f, 0.6784f), new Color(0.6431f, 0.4275f, 0.6784f), new Color(0.6431f, 0.4275f, 0.6784f), new Color(0.6431f, 0.4275f, 0.6784f), new Color(0.6431f, 0.4275f, 0.6784f), new Color(0.6431f, 0.4275f, 0.6784f),
                new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.6431f, 0.4275f, 0.6784f),
                new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.6431f, 0.4275f, 0.6784f), new Color(0.6431f, 0.4275f, 0.6784f),
                new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.6431f, 0.4275f, 0.6784f), new Color(0.6431f, 0.4275f, 0.6784f), new Color(0.9608f, 0.7725f, 0.0941f),
                    };
                    break;

            case "Level_15":
                levelColors = new Color[]
                {
                new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.9608f, 0.7725f, 0.0941f),
                new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.8510f, 0.3255f, 0.3098f),
                new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.8510f, 0.3255f, 0.3098f),
                new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.8510f, 0.3255f, 0.3098f),
                new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f),
                new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.9608f, 0.7725f, 0.0941f),
                new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f),
                new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.8510f, 0.3255f, 0.3098f),
                new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.8510f, 0.3255f, 0.3098f),
               };
                break;

            case "Level_16":
                    levelColors = new Color[]
                    {
                new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.4253f, 0.7490f, 0.5176f),
                new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.8510f, 0.3255f, 0.3098f),
                new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f),
                new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.2902f, 0.5647f, 0.8863f),
                new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.2902f, 0.5647f, 0.8863f),
                new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.8510f, 0.3255f, 0.3098f),
                new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.8510f, 0.3255f, 0.3098f),
                new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.8510f, 0.3255f, 0.3098f),
                new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.8510f, 0.3255f, 0.3098f), new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.2902f, 0.5647f, 0.8863f), new Color(0.4253f, 0.7490f, 0.5176f), new Color(0.9608f, 0.7725f, 0.0941f), new Color(0.9608f, 0.7725f, 0.0941f),
                    };
                    break;

                case "Level_17":
                    levelColors = new Color[]
                    {
                new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f),
                new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f),
                new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.7451f, 0.9804f, 0.3098f), new Color(0.7451f, 0.9804f, 0.3098f), new Color(0.7451f, 0.9804f, 0.3098f), new Color(0.7451f, 0.9804f, 0.3098f), new Color(0.7451f, 0.9804f, 0.3098f), new Color(0.7451f, 0.9804f, 0.3098f),
                new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f),
                new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f),
                new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f),
                new Color(0.7451f, 0.9804f, 0.3098f), new Color(0.7451f, 0.9804f, 0.3098f), new Color(0.7451f, 0.9804f, 0.3098f), new Color(0.7451f, 0.9804f, 0.3098f), new Color(0.7451f, 0.9804f, 0.3098f), new Color(0.7451f, 0.9804f, 0.3098f), new Color(0.3176f, 0.2275f, 0.4078f),
                new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f),
                new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f),
                    };
                    break;

                case "Level_18":
                    levelColors = new Color[]
                    {
                new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f),
                new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f),
                new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f),
                new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f),
                new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f),
                new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.3176f, 0.2275f, 0.4078f),
                new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.3529f, 0.6549f, 0.7255f),
                new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3529f, 0.6549f, 0.7255f),
                new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f),
                    };
                    break;

                case "Level_19":
                    levelColors = new Color[]
                    {
                new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f),
                new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f),
                new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f),
                new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.9098f, 0.2000f, 0.6000f), new Color(0.9098f, 0.2000f, 0.6000f), new Color(0.9098f, 0.2000f, 0.6000f), new Color(0.9098f, 0.2000f, 0.6000f), new Color(0.9098f, 0.2000f, 0.6000f),
                new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.9098f, 0.2000f, 0.6000f),
                new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.9098f, 0.2000f, 0.6000f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.9098f, 0.2000f, 0.6000f),
                new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.9098f, 0.2000f, 0.6000f), new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.9098f, 0.2000f, 0.6000f),
                new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.9098f, 0.2000f, 0.6000f), new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.9098f, 0.2000f, 0.6000f), new Color(0.9098f, 0.2000f, 0.6000f), new Color(0.9098f, 0.2000f, 0.6000f),
                new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f),
                    };
                    break;

                case "Level_20":
                    levelColors = new Color[]
                    {
                new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.7451f, 0.9804f, 0.3098f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f),
                new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.7451f, 0.9804f, 0.3098f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f),
                new Color(0.7451f, 0.9804f, 0.3098f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.7451f, 0.9804f, 0.3098f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.7451f, 0.9804f, 0.3098f),
                new Color(0.7451f, 0.9804f, 0.3098f), new Color(0.7451f, 0.9804f, 0.3098f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.7451f, 0.9804f, 0.3098f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.7451f, 0.9804f, 0.3098f), new Color(0.7451f, 0.9804f, 0.3098f),
                new Color(0.7451f, 0.9804f, 0.3098f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.7451f, 0.9804f, 0.3098f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.7451f, 0.9804f, 0.3098f),
                new Color(0.7451f, 0.9804f, 0.3098f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.7451f, 0.9804f, 0.3098f),
                new Color(0.7451f, 0.9804f, 0.3098f), new Color(0.7451f, 0.9804f, 0.3098f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.7451f, 0.9804f, 0.3098f), new Color(0.7451f, 0.9804f, 0.3098f),
                new Color(0.7451f, 0.9804f, 0.3098f), new Color(0.7451f, 0.9804f, 0.3098f), new Color(0.7451f, 0.9804f, 0.3098f), new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.7451f, 0.9804f, 0.3098f), new Color(0.7451f, 0.9804f, 0.3098f), new Color(0.7451f, 0.9804f, 0.3098f),
                new Color(0.7451f, 0.9804f, 0.3098f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.7451f, 0.9804f, 0.3098f),
                    };
                    break;

                case "Level_21":
                    levelColors = new Color[]
                    {
                new Color(0.8235f, 0.1529f, 0.1882f), new Color(0.8235f, 0.1529f, 0.1882f), new Color(0.8235f, 0.1529f, 0.1882f), new Color(0.8235f, 0.1529f, 0.1882f), new Color(0.8235f, 0.1529f, 0.1882f), new Color(0.8235f, 0.1529f, 0.1882f), new Color(0.8235f, 0.1529f, 0.1882f),
                new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.8235f, 0.1529f, 0.1882f),
                new Color(0.8235f, 0.1529f, 0.1882f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.7451f, 0.9804f, 0.3098f), new Color(0.7451f, 0.9804f, 0.3098f), new Color(0.7451f, 0.9804f, 0.3098f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.8235f, 0.1529f, 0.1882f),
                new Color(0.8235f, 0.1529f, 0.1882f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.7451f, 0.9804f, 0.3098f), new Color(0.8235f, 0.1529f, 0.1882f), new Color(0.7451f, 0.9804f, 0.3098f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.8235f, 0.1529f, 0.1882f),
                new Color(0.8235f, 0.1529f, 0.1882f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.7451f, 0.9804f, 0.3098f), new Color(0.8235f, 0.1529f, 0.1882f), new Color(0.7451f, 0.9804f, 0.3098f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.8235f, 0.1529f, 0.1882f),
                new Color(0.8235f, 0.1529f, 0.1882f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.7451f, 0.9804f, 0.3098f), new Color(0.8235f, 0.1529f, 0.1882f), new Color(0.7451f, 0.9804f, 0.3098f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.8235f, 0.1529f, 0.1882f),
                new Color(0.8235f, 0.1529f, 0.1882f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.7451f, 0.9804f, 0.3098f), new Color(0.7451f, 0.9804f, 0.3098f), new Color(0.7451f, 0.9804f, 0.3098f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.8235f, 0.1529f, 0.1882f),
                new Color(0.8235f, 0.1529f, 0.1882f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f),
                new Color(0.8235f, 0.1529f, 0.1882f), new Color(0.8235f, 0.1529f, 0.1882f), new Color(0.8235f, 0.1529f, 0.1882f), new Color(0.8235f, 0.1529f, 0.1882f), new Color(0.8235f, 0.1529f, 0.1882f), new Color(0.8235f, 0.1529f, 0.1882f), new Color(0.8235f, 0.1529f, 0.1882f),
                    };
                    break;

                case "Level_22":
                    levelColors = new Color[]
                    {
                new Color(0.9098f, 0.2000f, 0.6000f), new Color(0.9098f, 0.2000f, 0.6000f), new Color(0.9098f, 0.2000f, 0.6000f), new Color(0.9098f, 0.2000f, 0.6000f), new Color(0.9098f, 0.2000f, 0.6000f), new Color(0.9098f, 0.2000f, 0.6000f), new Color(0.9098f, 0.2000f, 0.6000f),
                new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.7451f, 0.9804f, 0.3098f), new Color(0.7451f, 0.9804f, 0.3098f), new Color(0.7451f, 0.9804f, 0.3098f), new Color(0.7451f, 0.9804f, 0.3098f), new Color(0.7451f, 0.9804f, 0.3098f), new Color(0.3176f, 0.2275f, 0.4078f),
                new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.7451f, 0.9804f, 0.3098f), new Color(0.7451f, 0.9804f, 0.3098f), new Color(0.7451f, 0.9804f, 0.3098f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f),
                new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.7451f, 0.9804f, 0.3098f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f),
                new Color(0.9098f, 0.2000f, 0.6000f), new Color(0.9098f, 0.2000f, 0.6000f), new Color(0.9098f, 0.2000f, 0.6000f), new Color(0.9098f, 0.2000f, 0.6000f), new Color(0.9098f, 0.2000f, 0.6000f), new Color(0.9098f, 0.2000f, 0.6000f), new Color(0.9098f, 0.2000f, 0.6000f),
                new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f),
                new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f),
                new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f),
                new Color(0.9098f, 0.2000f, 0.6000f), new Color(0.9098f, 0.2000f, 0.6000f), new Color(0.9098f, 0.2000f, 0.6000f), new Color(0.9098f, 0.2000f, 0.6000f), new Color(0.9098f, 0.2000f, 0.6000f), new Color(0.9098f, 0.2000f, 0.6000f), new Color(0.9098f, 0.2000f, 0.6000f),
                    };
                    break;

                case "Level_23":
                    levelColors = new Color[]
                    {
                new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.9098f, 0.2000f, 0.6000f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3529f, 0.6549f, 0.7255f),
                new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.9098f, 0.2000f, 0.6000f), new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.3529f, 0.6549f, 0.7255f),
                new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.9098f, 0.2000f, 0.6000f), new Color(0.9098f, 0.2000f, 0.6000f), new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.3176f, 0.2275f, 0.4078f),
                new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.9098f, 0.2000f, 0.6000f), new Color(0.9098f, 0.2000f, 0.6000f), new Color(0.3176f, 0.2275f, 0.4078f),
                new Color(0.9098f, 0.2000f, 0.6000f), new Color(0.9098f, 0.2000f, 0.6000f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.9098f, 0.2000f, 0.6000f), new Color(0.9098f, 0.2000f, 0.6000f),
                new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.9098f, 0.2000f, 0.6000f), new Color(0.9098f, 0.2000f, 0.6000f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.3176f, 0.2275f, 0.4078f),
                new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.9098f, 0.2000f, 0.6000f), new Color(0.9098f, 0.2000f, 0.6000f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.3176f, 0.2275f, 0.4078f),
                new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.9098f, 0.2000f, 0.6000f), new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.9098f, 0.2000f, 0.6000f),
                new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.9098f, 0.2000f, 0.6000f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.9098f, 0.2000f, 0.6000f),
                    };
                    break;

                case "Level_24":
                    levelColors = new Color[]
                    {
                new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.7451f, 0.9804f, 0.3098f), new Color(0.9098f, 0.2000f, 0.6000f), new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.7451f, 0.9804f, 0.3098f),
                new Color(0.7451f, 0.9804f, 0.3098f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.9098f, 0.2000f, 0.6000f),
                new Color(0.7451f, 0.9804f, 0.3098f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f),
                new Color(0.7451f, 0.9804f, 0.3098f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.7451f, 0.9804f, 0.3098f), new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.7451f, 0.9804f, 0.3098f), new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.3176f, 0.2275f, 0.4078f),
                new Color(0.9098f, 0.2000f, 0.6000f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.7451f, 0.9804f, 0.3098f), new Color(0.3176f, 0.2275f, 0.4078f),
                new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.9098f, 0.2000f, 0.6000f), new Color(0.9098f, 0.2000f, 0.6000f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.7451f, 0.9804f, 0.3098f), new Color(0.3176f, 0.2275f, 0.4078f),
                new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.9098f, 0.2000f, 0.6000f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.3176f, 0.2275f, 0.4078f),
                new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.7451f, 0.9804f, 0.3098f), new Color(0.7451f, 0.9804f, 0.3098f), new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.3176f, 0.2275f, 0.4078f),
                new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f),
                    };
                    break;

            case "Level_25":
               levelColors = new Color[]
               {
                   new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.7451f, 0.9804f, 0.3098f), new Color(0.7451f, 0.9804f, 0.3098f), new Color(0.7451f, 0.9804f, 0.3098f), new Color(0.7451f, 0.9804f, 0.3098f), new Color(0.7451f, 0.9804f, 0.3098f), new Color(0.3529f, 0.6549f, 0.7255f),
                   new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.3529f, 0.6549f, 0.7255f),
                   new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f),
                   new Color(0.7451f, 0.9804f, 0.3098f), new Color(0.9098f, 0.2000f, 0.6000f), new Color(0.7451f, 0.9804f, 0.3098f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.7451f, 0.9804f, 0.3098f), new Color(0.9098f, 0.2000f, 0.6000f), new Color(0.7451f, 0.9804f, 0.3098f),
                   new Color(0.7451f, 0.9804f, 0.3098f), new Color(0.9098f, 0.2000f, 0.6000f), new Color(0.7451f, 0.9804f, 0.3098f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.7451f, 0.9804f, 0.3098f), new Color(0.9098f, 0.2000f, 0.6000f), new Color(0.7451f, 0.9804f, 0.3098f),
                   new Color(0.7451f, 0.9804f, 0.3098f), new Color(0.9098f, 0.2000f, 0.6000f), new Color(0.7451f, 0.9804f, 0.3098f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.7451f, 0.9804f, 0.3098f), new Color(0.9098f, 0.2000f, 0.6000f), new Color(0.7451f, 0.9804f, 0.3098f),
                   new Color(0.9098f, 0.2000f, 0.6000f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.9098f, 0.2000f, 0.6000f),
                   new Color(0.9098f, 0.2000f, 0.6000f), new Color(0.9098f, 0.2000f, 0.6000f), new Color(0.9098f, 0.2000f, 0.6000f), new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.9098f, 0.2000f, 0.6000f), new Color(0.9098f, 0.2000f, 0.6000f), new Color(0.9098f, 0.2000f, 0.6000f),
                   new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f),
               };
               break;

            case "Level_26":
               levelColors = new Color[]
               {
                   new Color(0.7451f, 0.9804f, 0.3098f), new Color(0.7451f, 0.9804f, 0.3098f), new Color(0.7451f, 0.9804f, 0.3098f), new Color(0.9098f, 0.2000f, 0.6000f), new Color(0.9098f, 0.2000f, 0.6000f), new Color(0.9098f, 0.2000f, 0.6000f), new Color(0.9098f, 0.2000f, 0.6000f),
                   new Color(0.7451f, 0.9804f, 0.3098f), new Color(0.9098f, 0.2000f, 0.6000f), new Color(0.9098f, 0.2000f, 0.6000f), new Color(0.9098f, 0.2000f, 0.6000f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.7451f, 0.9804f, 0.3098f),
                   new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.9098f, 0.2000f, 0.6000f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.7451f, 0.9804f, 0.3098f),
                   new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.7451f, 0.9804f, 0.3098f), new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.7451f, 0.9804f, 0.3098f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.7451f, 0.9804f, 0.3098f),
                   new Color(0.7451f, 0.9804f, 0.3098f), new Color(0.7451f, 0.9804f, 0.3098f), new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.7451f, 0.9804f, 0.3098f), new Color(0.7451f, 0.9804f, 0.3098f), new Color(0.7451f, 0.9804f, 0.3098f),
                   new Color(0.7451f, 0.9804f, 0.3098f), new Color(0.9098f, 0.2000f, 0.6000f), new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.7451f, 0.9804f, 0.3098f), new Color(0.3176f, 0.2275f, 0.4078f),
                   new Color(0.7451f, 0.9804f, 0.3098f), new Color(0.9098f, 0.2000f, 0.6000f), new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f),
                   new Color(0.7451f, 0.9804f, 0.3098f), new Color(0.9098f, 0.2000f, 0.6000f), new Color(0.9098f, 0.2000f, 0.6000f), new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.3176f, 0.2275f, 0.4078f),
                   new Color(0.7451f, 0.9804f, 0.3098f), new Color(0.7451f, 0.9804f, 0.3098f), new Color(0.9098f, 0.2000f, 0.6000f), new Color(0.9098f, 0.2000f, 0.6000f), new Color(0.9098f, 0.2000f, 0.6000f), new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.3529f, 0.6549f, 0.7255f),
               };
               break;

            case "Level_27":
               levelColors = new Color[]
               {
                   new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f),
                   new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.9098f, 0.2000f, 0.6000f), new Color(0.9098f, 0.2000f, 0.6000f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.7451f, 0.9804f, 0.3098f), new Color(0.7451f, 0.9804f, 0.3098f), new Color(0.3176f, 0.2275f, 0.4078f),
                   new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.9098f, 0.2000f, 0.6000f), new Color(0.9098f, 0.2000f, 0.6000f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.7451f, 0.9804f, 0.3098f), new Color(0.7451f, 0.9804f, 0.3098f), new Color(0.3176f, 0.2275f, 0.4078f),
                   new Color(0.9098f, 0.2000f, 0.6000f), new Color(0.9098f, 0.2000f, 0.6000f), new Color(0.9098f, 0.2000f, 0.6000f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f),
                   new Color(0.9098f, 0.2000f, 0.6000f), new Color(0.9098f, 0.2000f, 0.6000f), new Color(0.9098f, 0.2000f, 0.6000f), new Color(0.9098f, 0.2000f, 0.6000f), new Color(0.9098f, 0.2000f, 0.6000f), new Color(0.9098f, 0.2000f, 0.6000f), new Color(0.9098f, 0.2000f, 0.6000f),
                   new Color(0.7451f, 0.9804f, 0.3098f), new Color(0.7451f, 0.9804f, 0.3098f), new Color(0.7451f, 0.9804f, 0.3098f), new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.3529f, 0.6549f, 0.7255f),
                   new Color(0.7451f, 0.9804f, 0.3098f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.7451f, 0.9804f, 0.3098f), new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.9098f, 0.2000f, 0.6000f), new Color(0.9098f, 0.2000f, 0.6000f), new Color(0.3529f, 0.6549f, 0.7255f),
                   new Color(0.7451f, 0.9804f, 0.3098f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.7451f, 0.9804f, 0.3098f), new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.9098f, 0.2000f, 0.6000f), new Color(0.9098f, 0.2000f, 0.6000f), new Color(0.3529f, 0.6549f, 0.7255f),
                   new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.3529f, 0.6549f, 0.7255f),
               };
               break;

            case "Level_28":
               levelColors = new Color[]
               {
                   new Color(0.7451f, 0.9804f, 0.3098f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.7451f, 0.9804f, 0.3098f), new Color(0.9098f, 0.2000f, 0.6000f), new Color(0.7451f, 0.9804f, 0.3098f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.7451f, 0.9804f, 0.3098f),
                   new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.9098f, 0.2000f, 0.6000f), new Color(0.7451f, 0.9804f, 0.3098f), new Color(0.9098f, 0.2000f, 0.6000f), new Color(0.7451f, 0.9804f, 0.3098f), new Color(0.9098f, 0.2000f, 0.6000f), new Color(0.3176f, 0.2275f, 0.4078f),
                   new Color(0.7451f, 0.9804f, 0.3098f), new Color(0.7451f, 0.9804f, 0.3098f), new Color(0.9098f, 0.2000f, 0.6000f), new Color(0.9098f, 0.2000f, 0.6000f), new Color(0.9098f, 0.2000f, 0.6000f), new Color(0.7451f, 0.9804f, 0.3098f), new Color(0.7451f, 0.9804f, 0.3098f),
                   new Color(0.9098f, 0.2000f, 0.6000f), new Color(0.9098f, 0.2000f, 0.6000f), new Color(0.9098f, 0.2000f, 0.6000f), new Color(0.7451f, 0.9804f, 0.3098f), new Color(0.9098f, 0.2000f, 0.6000f), new Color(0.9098f, 0.2000f, 0.6000f), new Color(0.9098f, 0.2000f, 0.6000f),
                   new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.7451f, 0.9804f, 0.3098f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f),
                   new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.7451f, 0.9804f, 0.3098f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.7451f, 0.9804f, 0.3098f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.7451f, 0.9804f, 0.3098f), new Color(0.3529f, 0.6549f, 0.7255f),
                   new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.7451f, 0.9804f, 0.3098f), new Color(0.7451f, 0.9804f, 0.3098f), new Color(0.7451f, 0.9804f, 0.3098f), new Color(0.7451f, 0.9804f, 0.3098f), new Color(0.7451f, 0.9804f, 0.3098f), new Color(0.3529f, 0.6549f, 0.7255f),
                   new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.7451f, 0.9804f, 0.3098f), new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.3529f, 0.6549f, 0.7255f),
                   new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.7451f, 0.9804f, 0.3098f), new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.7451f, 0.9804f, 0.3098f), new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.7451f, 0.9804f, 0.3098f), new Color(0.3176f, 0.2275f, 0.4078f),
               };
               break;

            case "Level_29":
               levelColors = new Color[]
               {
                   new Color(0.9098f, 0.2000f, 0.6000f), new Color(0.9098f, 0.2000f, 0.6000f), new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.7451f, 0.9804f, 0.3098f), new Color(0.3176f, 0.2275f, 0.4078f),
                   new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.9098f, 0.2000f, 0.6000f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.7451f, 0.9804f, 0.3098f), new Color(0.3176f, 0.2275f, 0.4078f),
                   new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.9098f, 0.2000f, 0.6000f), new Color(0.9098f, 0.2000f, 0.6000f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.3529f, 0.6549f, 0.7255f),
                   new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.3176f, 0.2275f, 0.4078f),
                   new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.9098f, 0.2000f, 0.6000f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f),
                   new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.3529f, 0.6549f, 0.7255f),
                   new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.9098f, 0.2000f, 0.6000f), new Color(0.9098f, 0.2000f, 0.6000f), new Color(0.3176f, 0.2275f, 0.4078f),
                   new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.7451f, 0.9804f, 0.3098f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.9098f, 0.2000f, 0.6000f), new Color(0.3176f, 0.2275f, 0.4078f),
                   new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.7451f, 0.9804f, 0.3098f), new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.9098f, 0.2000f, 0.6000f), new Color(0.9098f, 0.2000f, 0.6000f),
               };
               break;

            case "Level_30":
               levelColors = new Color[]
               {
                   new Color(0.9098f, 0.2000f, 0.6000f), new Color(0.9098f, 0.2000f, 0.6000f), new Color(0.9098f, 0.2000f, 0.6000f), new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.3176f, 0.2275f, 0.4078f),
                   new Color(0.9098f, 0.2000f, 0.6000f), new Color(0.9098f, 0.2000f, 0.6000f), new Color(0.9098f, 0.2000f, 0.6000f), new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.7451f, 0.9804f, 0.3098f), new Color(0.7451f, 0.9804f, 0.3098f), new Color(0.3529f, 0.6549f, 0.7255f),
                   new Color(0.9098f, 0.2000f, 0.6000f), new Color(0.9098f, 0.2000f, 0.6000f), new Color(0.9098f, 0.2000f, 0.6000f), new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.7451f, 0.9804f, 0.3098f), new Color(0.7451f, 0.9804f, 0.3098f), new Color(0.3529f, 0.6549f, 0.7255f),
                   new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.3529f, 0.6549f, 0.7255f),
                   new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f),
                   new Color(0.7451f, 0.9804f, 0.3098f), new Color(0.7451f, 0.9804f, 0.3098f), new Color(0.7451f, 0.9804f, 0.3098f), new Color(0.9098f, 0.2000f, 0.6000f), new Color(0.9098f, 0.2000f, 0.6000f), new Color(0.9098f, 0.2000f, 0.6000f), new Color(0.9098f, 0.2000f, 0.6000f),
                   new Color(0.7451f, 0.9804f, 0.3098f), new Color(0.7451f, 0.9804f, 0.3098f), new Color(0.7451f, 0.9804f, 0.3098f), new Color(0.9098f, 0.2000f, 0.6000f), new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.9098f, 0.2000f, 0.6000f),
                   new Color(0.7451f, 0.9804f, 0.3098f), new Color(0.7451f, 0.9804f, 0.3098f), new Color(0.7451f, 0.9804f, 0.3098f), new Color(0.9098f, 0.2000f, 0.6000f), new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.9098f, 0.2000f, 0.6000f),
                   new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.9098f, 0.2000f, 0.6000f), new Color(0.9098f, 0.2000f, 0.6000f), new Color(0.9098f, 0.2000f, 0.6000f), new Color(0.9098f, 0.2000f, 0.6000f),
               };
               break;

             case "Level_31":
               levelColors = new Color[]
               {
                   new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.9098f, 0.2000f, 0.6000f), new Color(0.9098f, 0.2000f, 0.6000f), new Color(0.9098f, 0.2000f, 0.6000f), new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.9098f, 0.2000f, 0.6000f),
                   new Color(0.9098f, 0.2000f, 0.6000f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.9098f, 0.2000f, 0.6000f), new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.7451f, 0.9804f, 0.3098f),
                   new Color(0.7451f, 0.9804f, 0.3098f), new Color(0.9098f, 0.2000f, 0.6000f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.9098f, 0.2000f, 0.6000f), new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.7451f, 0.9804f, 0.3098f), new Color(0.7451f, 0.9804f, 0.3098f),
                   new Color(0.7451f, 0.9804f, 0.3098f), new Color(0.9098f, 0.2000f, 0.6000f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.7451f, 0.9804f, 0.3098f),
                   new Color(0.9098f, 0.2000f, 0.6000f), new Color(0.7451f, 0.9804f, 0.3098f), new Color(0.9098f, 0.2000f, 0.6000f), new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f),
                   new Color(0.9098f, 0.2000f, 0.6000f), new Color(0.7451f, 0.9804f, 0.3098f), new Color(0.7451f, 0.9804f, 0.3098f), new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.9098f, 0.2000f, 0.6000f), new Color(0.9098f, 0.2000f, 0.6000f), new Color(0.9098f, 0.2000f, 0.6000f),
                   new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.9098f, 0.2000f, 0.6000f), new Color(0.9098f, 0.2000f, 0.6000f), new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.7451f, 0.9804f, 0.3098f), new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.3529f, 0.6549f, 0.7255f),
                   new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.7451f, 0.9804f, 0.3098f), new Color(0.7451f, 0.9804f, 0.3098f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.7451f, 0.9804f, 0.3098f),
                   new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f),
               };
               break;

            case "Level_32":
               levelColors = new Color[]
               {
                   new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.7451f, 0.9804f, 0.3098f), new Color(0.7451f, 0.9804f, 0.3098f), new Color(0.8235f, 0.1529f, 0.1882f), new Color(0.7451f, 0.9804f, 0.3098f), new Color(0.7451f, 0.9804f, 0.3098f),
                   new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.8235f, 0.1529f, 0.1882f), new Color(0.8235f, 0.1529f, 0.1882f), new Color(0.8235f, 0.1529f, 0.1882f), new Color(0.8235f, 0.1529f, 0.1882f), new Color(0.8235f, 0.1529f, 0.1882f), new Color(0.8235f, 0.1529f, 0.1882f),
                   new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f),
                   new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.8235f, 0.1529f, 0.1882f), new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.3529f, 0.6549f, 0.7255f),
                   new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.8235f, 0.1529f, 0.1882f), new Color(0.8235f, 0.1529f, 0.1882f), new Color(0.8235f, 0.1529f, 0.1882f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3529f, 0.6549f, 0.7255f),
                   new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.8235f, 0.1529f, 0.1882f), new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3529f, 0.6549f, 0.7255f),
                   new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.3529f, 0.6549f, 0.7255f),
                   new Color(0.9098f, 0.2000f, 0.6000f), new Color(0.8235f, 0.1529f, 0.1882f), new Color(0.9098f, 0.2000f, 0.6000f), new Color(0.9098f, 0.2000f, 0.6000f), new Color(0.9098f, 0.2000f, 0.6000f), new Color(0.8235f, 0.1529f, 0.1882f), new Color(0.9098f, 0.2000f, 0.6000f),
                   new Color(0.8235f, 0.1529f, 0.1882f), new Color(0.8235f, 0.1529f, 0.1882f), new Color(0.8235f, 0.1529f, 0.1882f), new Color(0.9098f, 0.2000f, 0.6000f), new Color(0.8235f, 0.1529f, 0.1882f), new Color(0.8235f, 0.1529f, 0.1882f), new Color(0.8235f, 0.1529f, 0.1882f),
               };
               break;

            case "Level_33":
                levelColors = new Color[]
                 {
                   new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(1.0000f, 0.5686f, 0.0000f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.1333f, 0.6353f, 0.4392f),
                   new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(1.0000f, 0.5686f, 0.0000f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.1333f, 0.6353f, 0.4392f),
                   new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(1.0000f, 0.5686f, 0.0000f), new Color(1.0000f, 0.5686f, 0.0000f), new Color(0.1333f, 0.6353f, 0.4392f),
                   new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(1.0000f, 0.5686f, 0.0000f), new Color(0.1333f, 0.6353f, 0.4392f),
                   new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(1.0000f, 0.5686f, 0.0000f), new Color(1.0000f, 0.5686f, 0.0000f), new Color(0.1333f, 0.6353f, 0.4392f),
                   new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(1.0000f, 0.5686f, 0.0000f), new Color(0.1333f, 0.6353f, 0.4392f),
                   new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(1.0000f, 0.5686f, 0.0000f), new Color(1.0000f, 0.5686f, 0.0000f), new Color(0.1333f, 0.6353f, 0.4392f),
                   new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(1.0000f, 0.5686f, 0.0000f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.1333f, 0.6353f, 0.4392f),
                   new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(1.0000f, 0.5686f, 0.0000f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.1333f, 0.6353f, 0.4392f),
                 };
                break;

            case "Level_34":
                levelColors = new Color[]
                 {
                   new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(1.0000f, 0.5686f, 0.0000f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(1.0000f, 0.5686f, 0.0000f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.1333f, 0.6353f, 0.4392f),
                   new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.1333f, 0.6353f, 0.4392f),
                   new Color(1.0000f, 0.5686f, 0.0000f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(1.0000f, 0.5686f, 0.0000f),
                   new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.9255f, 0.8863f, 0.7765f),
                   new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.6000f, 0.4000f, 0.8000f),
                   new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.9255f, 0.8863f, 0.7765f),
                   new Color(1.0000f, 0.5686f, 0.0000f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(1.0000f, 0.5686f, 0.0000f),
                   new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.1333f, 0.6353f, 0.4392f),
                   new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(1.0000f, 0.5686f, 0.0000f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(1.0000f, 0.5686f, 0.0000f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.1333f, 0.6353f, 0.4392f),
                 };
                break;

            case "Level_35":
                levelColors = new Color[]
                 {
                   new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.6000f, 0.4000f, 0.8000f),
                   new Color(0.6000f, 0.4000f, 0.8000f), new Color(1.0000f, 0.5686f, 0.0000f), new Color(1.0000f, 0.5686f, 0.0000f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.9255f, 0.8863f, 0.7765f),
                   new Color(0.9255f, 0.8863f, 0.7765f), new Color(1.0000f, 0.5686f, 0.0000f), new Color(1.0000f, 0.5686f, 0.0000f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.9255f, 0.8863f, 0.7765f),
                   new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.9255f, 0.8863f, 0.7765f),
                   new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.6000f, 0.4000f, 0.8000f),
                   new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.9255f, 0.8863f, 0.7765f),
                   new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(1.0000f, 0.5686f, 0.0000f), new Color(1.0000f, 0.5686f, 0.0000f), new Color(0.9255f, 0.8863f, 0.7765f),
                   new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(1.0000f, 0.5686f, 0.0000f), new Color(1.0000f, 0.5686f, 0.0000f), new Color(0.6000f, 0.4000f, 0.8000f),
                   new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.6000f, 0.4000f, 0.8000f),
                 };
                break;

            case "Level_36":
                levelColors = new Color[]
                 {
                   new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(1.0000f, 0.5686f, 0.0000f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.9255f, 0.8863f, 0.7765f),
                   new Color(0.9255f, 0.8863f, 0.7765f), new Color(1.0000f, 0.5686f, 0.0000f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(1.0000f, 0.5686f, 0.0000f), new Color(0.9255f, 0.8863f, 0.7765f),
                   new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.9255f, 0.8863f, 0.7765f),
                   new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(1.0000f, 0.5686f, 0.0000f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.1333f, 0.6353f, 0.4392f),
                   new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.3961f, 0.6863f, 1.0000f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.3961f, 0.6863f, 1.0000f), new Color(0.1333f, 0.6353f, 0.4392f),
                   new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.1333f, 0.6353f, 0.4392f),
                   new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.6000f, 0.4000f, 0.8000f),
                   new Color(0.6000f, 0.4000f, 0.8000f), new Color(1.0000f, 0.5686f, 0.0000f), new Color(1.0000f, 0.5686f, 0.0000f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(1.0000f, 0.5686f, 0.0000f), new Color(1.0000f, 0.5686f, 0.0000f), new Color(0.6000f, 0.4000f, 0.8000f),
                   new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.6000f, 0.4000f, 0.8000f),
                 };
                break;

            case "Level_37":
                levelColors = new Color[]
                 {
                   new Color(1.0000f, 0.5686f, 0.0000f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(1.0000f, 0.5686f, 0.0000f),
                   new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.3961f, 0.6863f, 1.0000f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.9255f, 0.8863f, 0.7765f),
                   new Color(1.0000f, 0.5686f, 0.0000f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(1.0000f, 0.5686f, 0.0000f),
                   new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.3961f, 0.6863f, 1.0000f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.3961f, 0.6863f, 1.0000f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.9255f, 0.8863f, 0.7765f),
                   new Color(1.0000f, 0.5686f, 0.0000f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(1.0000f, 0.5686f, 0.0000f),
                   new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.3961f, 0.6863f, 1.0000f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.3961f, 0.6863f, 1.0000f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.9255f, 0.8863f, 0.7765f),
                   new Color(1.0000f, 0.5686f, 0.0000f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(1.0000f, 0.5686f, 0.0000f),
                   new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.3961f, 0.6863f, 1.0000f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.9255f, 0.8863f, 0.7765f),
                   new Color(1.0000f, 0.5686f, 0.0000f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(1.0000f, 0.5686f, 0.0000f),
                 };
                break;

            case "Level_38":
                levelColors = new Color[]
                 {
                   new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(1.0000f, 0.5686f, 0.0000f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.1333f, 0.6353f, 0.4392f),
                   new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(1.0000f, 0.5686f, 0.0000f), new Color(0.1333f, 0.6353f, 0.4392f),
                   new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(1.0000f, 0.5686f, 0.0000f), new Color(1.0000f, 0.5686f, 0.0000f), new Color(0.6000f, 0.4000f, 0.8000f),
                   new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(1.0000f, 0.5686f, 0.0000f), new Color(1.0000f, 0.5686f, 0.0000f),
                   new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(1.0000f, 0.5686f, 0.0000f), new Color(1.0000f, 0.5686f, 0.0000f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(1.0000f, 0.5686f, 0.0000f), new Color(0.9255f, 0.8863f, 0.7765f),
                   new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(1.0000f, 0.5686f, 0.0000f), new Color(1.0000f, 0.5686f, 0.0000f), new Color(1.0000f, 0.5686f, 0.0000f), new Color(0.6000f, 0.4000f, 0.8000f),
                   new Color(1.0000f, 0.5686f, 0.0000f), new Color(1.0000f, 0.5686f, 0.0000f), new Color(1.0000f, 0.5686f, 0.0000f), new Color(1.0000f, 0.5686f, 0.0000f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(1.0000f, 0.5686f, 0.0000f), new Color(1.0000f, 0.5686f, 0.0000f),
                   new Color(1.0000f, 0.5686f, 0.0000f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(1.0000f, 0.5686f, 0.0000f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(1.0000f, 0.5686f, 0.0000f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(1.0000f, 0.5686f, 0.0000f),
                   new Color(0.1333f, 0.6353f, 0.4392f), new Color(1.0000f, 0.5686f, 0.0000f), new Color(1.0000f, 0.5686f, 0.0000f), new Color(1.0000f, 0.5686f, 0.0000f), new Color(1.0000f, 0.5686f, 0.0000f), new Color(1.0000f, 0.5686f, 0.0000f), new Color(0.6000f, 0.4000f, 0.8000f),
                 };
                break;

            case "Level_39":
                levelColors = new Color[]
                 {
                   new Color(1.0000f, 0.5686f, 0.0000f), new Color(1.0000f, 0.5686f, 0.0000f), new Color(1.0000f, 0.5686f, 0.0000f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(1.0000f, 0.5686f, 0.0000f),
                   new Color(1.0000f, 0.5686f, 0.0000f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(1.0000f, 0.5686f, 0.0000f), new Color(1.0000f, 0.5686f, 0.0000f),
                   new Color(1.0000f, 0.5686f, 0.0000f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(1.0000f, 0.5686f, 0.0000f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(1.0000f, 0.5686f, 0.0000f), new Color(1.0000f, 0.5686f, 0.0000f), new Color(0.6000f, 0.4000f, 0.8000f),
                   new Color(0.9255f, 0.8863f, 0.7765f), new Color(1.0000f, 0.5686f, 0.0000f), new Color(1.0000f, 0.5686f, 0.0000f), new Color(1.0000f, 0.5686f, 0.0000f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(1.0000f, 0.5686f, 0.0000f), new Color(0.6000f, 0.4000f, 0.8000f),
                   new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(1.0000f, 0.5686f, 0.0000f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(1.0000f, 0.5686f, 0.0000f), new Color(0.6000f, 0.4000f, 0.8000f),
                   new Color(0.9255f, 0.8863f, 0.7765f), new Color(1.0000f, 0.5686f, 0.0000f), new Color(1.0000f, 0.5686f, 0.0000f), new Color(1.0000f, 0.5686f, 0.0000f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.6000f, 0.4000f, 0.8000f),
                   new Color(1.0000f, 0.5686f, 0.0000f), new Color(1.0000f, 0.5686f, 0.0000f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(1.0000f, 0.5686f, 0.0000f), new Color(1.0000f, 0.5686f, 0.0000f), new Color(0.6000f, 0.4000f, 0.8000f),
                   new Color(1.0000f, 0.5686f, 0.0000f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(1.0000f, 0.5686f, 0.0000f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(1.0000f, 0.5686f, 0.0000f), new Color(1.0000f, 0.5686f, 0.0000f),
                   new Color(1.0000f, 0.5686f, 0.0000f), new Color(1.0000f, 0.5686f, 0.0000f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(1.0000f, 0.5686f, 0.0000f),
                 };
                break;

            case "Level_40":
                levelColors = new Color[]
                 {
                   new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.1333f, 0.6353f, 0.4392f),
                   new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.6000f, 0.4000f, 0.8000f),
                   new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.1333f, 0.6353f, 0.4392f),
                   new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.9255f, 0.8863f, 0.7765f),
                   new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.1333f, 0.6353f, 0.4392f),
                   new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.1333f, 0.6353f, 0.4392f),
                   new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.1333f, 0.6353f, 0.4392f),
                   new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.9255f, 0.8863f, 0.7765f),
                   new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.1333f, 0.6353f, 0.4392f),
                 };
                break;

            case "Level_41":
                levelColors = new Color[]
                 {
                   new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(1.0000f, 0.5686f, 0.0000f),
                   new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(1.0000f, 0.5686f, 0.0000f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(1.0000f, 0.5686f, 0.0000f),
                   new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(1.0000f, 0.5686f, 0.0000f), new Color(1.0000f, 0.5686f, 0.0000f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(1.0000f, 0.5686f, 0.0000f),
                   new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(1.0000f, 0.5686f, 0.0000f), new Color(1.0000f, 0.5686f, 0.0000f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(1.0000f, 0.5686f, 0.0000f),
                   new Color(0.9255f, 0.8863f, 0.7765f), new Color(1.0000f, 0.5686f, 0.0000f), new Color(1.0000f, 0.5686f, 0.0000f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(1.0000f, 0.5686f, 0.0000f), new Color(0.9255f, 0.8863f, 0.7765f),
                   new Color(0.9255f, 0.8863f, 0.7765f), new Color(1.0000f, 0.5686f, 0.0000f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(1.0000f, 0.5686f, 0.0000f), new Color(0.9255f, 0.8863f, 0.7765f),
                   new Color(1.0000f, 0.5686f, 0.0000f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(1.0000f, 0.5686f, 0.0000f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.9255f, 0.8863f, 0.7765f),
                   new Color(0.6000f, 0.4000f, 0.8000f), new Color(1.0000f, 0.5686f, 0.0000f), new Color(1.0000f, 0.5686f, 0.0000f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.6000f, 0.4000f, 0.8000f),
                   new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.9255f, 0.8863f, 0.7765f),
                 };
                break;

            case "Level_42":
                levelColors = new Color[]
                 {
                   new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.6000f, 0.4000f, 0.8000f),
                   new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.6000f, 0.4000f, 0.8000f),
                   new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(1.0000f, 0.5686f, 0.0000f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.6000f, 0.4000f, 0.8000f),
                   new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.6000f, 0.4000f, 0.8000f),
                   new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(1.0000f, 0.5686f, 0.0000f), new Color(0.3961f, 0.6863f, 1.0000f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(1.0000f, 0.5686f, 0.0000f), new Color(0.6000f, 0.4000f, 0.8000f),
                   new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.9255f, 0.8863f, 0.7765f),
                   new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(1.0000f, 0.5686f, 0.0000f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.6000f, 0.4000f, 0.8000f),
                   new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.6000f, 0.4000f, 0.8000f),
                   new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.9255f, 0.8863f, 0.7765f),
                 };
                break;

            case "Level_43":
                levelColors = new Color[]
                 {
                   new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(1.0000f, 0.5686f, 0.0000f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.1333f, 0.6353f, 0.4392f),
                   new Color(1.0000f, 0.5686f, 0.0000f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(1.0000f, 0.5686f, 0.0000f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(1.0000f, 0.5686f, 0.0000f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(1.0000f, 0.5686f, 0.0000f),
                   new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(1.0000f, 0.5686f, 0.0000f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(1.0000f, 0.5686f, 0.0000f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.9255f, 0.8863f, 0.7765f),
                   new Color(1.0000f, 0.5686f, 0.0000f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(1.0000f, 0.5686f, 0.0000f), new Color(1.0000f, 0.5686f, 0.0000f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(1.0000f, 0.5686f, 0.0000f),
                   new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(1.0000f, 0.5686f, 0.0000f), new Color(1.0000f, 0.5686f, 0.0000f), new Color(1.0000f, 0.5686f, 0.0000f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.6000f, 0.4000f, 0.8000f),
                   new Color(0.1333f, 0.6353f, 0.4392f), new Color(1.0000f, 0.5686f, 0.0000f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(1.0000f, 0.5686f, 0.0000f), new Color(1.0000f, 0.5686f, 0.0000f), new Color(0.9255f, 0.8863f, 0.7765f),
                   new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(1.0000f, 0.5686f, 0.0000f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.1333f, 0.6353f, 0.4392f),
                   new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.9255f, 0.8863f, 0.7765f),
                   new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(1.0000f, 0.5686f, 0.0000f), new Color(0.9255f, 0.8863f, 0.7765f),
                 };
                break;

            case "Level_44":
                levelColors = new Color[]
                 {
                   new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.1333f, 0.6353f, 0.4392f),
                   new Color(1.0000f, 0.5686f, 0.0000f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.6000f, 0.4000f, 0.8000f),
                   new Color(1.0000f, 0.5686f, 0.0000f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(1.0000f, 0.5686f, 0.0000f),
                   new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(1.0000f, 0.5686f, 0.0000f), new Color(0.1333f, 0.6353f, 0.4392f),
                   new Color(1.0000f, 0.5686f, 0.0000f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.6000f, 0.4000f, 0.8000f),
                   new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(1.0000f, 0.5686f, 0.0000f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.1333f, 0.6353f, 0.4392f),
                   new Color(1.0000f, 0.5686f, 0.0000f), new Color(1.0000f, 0.5686f, 0.0000f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(1.0000f, 0.5686f, 0.0000f),
                   new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(1.0000f, 0.5686f, 0.0000f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.1333f, 0.6353f, 0.4392f),
                   new Color(1.0000f, 0.5686f, 0.0000f), new Color(1.0000f, 0.5686f, 0.0000f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(1.0000f, 0.5686f, 0.0000f), new Color(0.1333f, 0.6353f, 0.4392f),
                 };
                break;

            case "Level_45":
                levelColors = new Color[]
                 {
                   new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(1.0000f, 0.5686f, 0.0000f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.6000f, 0.4000f, 0.8000f),
                   new Color(1.0000f, 0.5686f, 0.0000f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(1.0000f, 0.5686f, 0.0000f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.1333f, 0.6353f, 0.4392f),
                   new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(1.0000f, 0.5686f, 0.0000f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.1333f, 0.6353f, 0.4392f),
                   new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.6000f, 0.4000f, 0.8000f),
                   new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.9255f, 0.8863f, 0.7765f),
                   new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(1.0000f, 0.5686f, 0.0000f),
                   new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(1.0000f, 0.5686f, 0.0000f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(1.0000f, 0.5686f, 0.0000f), new Color(0.6000f, 0.4000f, 0.8000f),
                   new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(1.0000f, 0.5686f, 0.0000f), new Color(1.0000f, 0.5686f, 0.0000f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.9255f, 0.8863f, 0.7765f),
                   new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(1.0000f, 0.5686f, 0.0000f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.6000f, 0.4000f, 0.8000f),
                 };
                break;

            case "Level_46":
                levelColors = new Color[]
                 {
                   new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.6000f, 0.4000f, 0.8000f),
                   new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(1.0000f, 0.5686f, 0.0000f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.1333f, 0.6353f, 0.4392f),
                   new Color(0.1333f, 0.6353f, 0.4392f), new Color(1.0000f, 0.5686f, 0.0000f), new Color(1.0000f, 0.5686f, 0.0000f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(1.0000f, 0.5686f, 0.0000f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(1.0000f, 0.5686f, 0.0000f),
                   new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(1.0000f, 0.5686f, 0.0000f), new Color(0.1333f, 0.6353f, 0.4392f),
                   new Color(1.0000f, 0.5686f, 0.0000f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(1.0000f, 0.5686f, 0.0000f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(1.0000f, 0.5686f, 0.0000f), new Color(0.1333f, 0.6353f, 0.4392f),
                   new Color(1.0000f, 0.5686f, 0.0000f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(1.0000f, 0.5686f, 0.0000f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(1.0000f, 0.5686f, 0.0000f), new Color(1.0000f, 0.5686f, 0.0000f),
                   new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(1.0000f, 0.5686f, 0.0000f), new Color(1.0000f, 0.5686f, 0.0000f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.1333f, 0.6353f, 0.4392f),
                   new Color(0.1333f, 0.6353f, 0.4392f), new Color(1.0000f, 0.5686f, 0.0000f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(1.0000f, 0.5686f, 0.0000f), new Color(1.0000f, 0.5686f, 0.0000f), new Color(0.9255f, 0.8863f, 0.7765f),
                   new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(1.0000f, 0.5686f, 0.0000f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.9255f, 0.8863f, 0.7765f),
                 };
                break;

            case "Level_47":
                levelColors = new Color[]
                 {
                   new Color(0.6000f, 0.4000f, 0.8000f), new Color(1.0000f, 0.5686f, 0.0000f), new Color(1.0000f, 0.5686f, 0.0000f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.1333f, 0.6353f, 0.4392f),
                   new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(1.0000f, 0.5686f, 0.0000f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.6000f, 0.4000f, 0.8000f),
                   new Color(1.0000f, 0.5686f, 0.0000f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(1.0000f, 0.5686f, 0.0000f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.9255f, 0.8863f, 0.7765f),
                   new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(1.0000f, 0.5686f, 0.0000f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.9255f, 0.8863f, 0.7765f),
                   new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(1.0000f, 0.5686f, 0.0000f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(1.0000f, 0.5686f, 0.0000f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(1.0000f, 0.5686f, 0.0000f),
                   new Color(1.0000f, 0.5686f, 0.0000f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.1333f, 0.6353f, 0.4392f),
                   new Color(1.0000f, 0.5686f, 0.0000f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.1333f, 0.6353f, 0.4392f),
                   new Color(0.9255f, 0.8863f, 0.7765f), new Color(1.0000f, 0.5686f, 0.0000f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.1333f, 0.6353f, 0.4392f),
                   new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(1.0000f, 0.5686f, 0.0000f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.1333f, 0.6353f, 0.4392f),
                 };
                break;

            case "Level_48":
                levelColors = new Color[]
                 {
                   new Color(0.1333f, 0.6353f, 0.4392f), new Color(1.0000f, 0.5686f, 0.0000f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.9255f, 0.8863f, 0.7765f),
                   new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(1.0000f, 0.5686f, 0.0000f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(1.0000f, 0.5686f, 0.0000f),
                   new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(1.0000f, 0.5686f, 0.0000f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.9255f, 0.8863f, 0.7765f),
                   new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(1.0000f, 0.5686f, 0.0000f), new Color(0.9255f, 0.8863f, 0.7765f),
                   new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.9255f, 0.8863f, 0.7765f),
                   new Color(0.9255f, 0.8863f, 0.7765f), new Color(1.0000f, 0.5686f, 0.0000f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(1.0000f, 0.5686f, 0.0000f), new Color(0.6000f, 0.4000f, 0.8000f),
                   new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(1.0000f, 0.5686f, 0.0000f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.6000f, 0.4000f, 0.8000f),
                   new Color(0.9255f, 0.8863f, 0.7765f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.6000f, 0.4000f, 0.8000f),
                   new Color(1.0000f, 0.5686f, 0.0000f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.1333f, 0.6353f, 0.4392f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(0.6000f, 0.4000f, 0.8000f), new Color(1.0000f, 0.5686f, 0.0000f), new Color(0.1333f, 0.6353f, 0.4392f),
                 };
                break;

            case "Level_49":
                levelColors = new Color[]
                 {
                   new Color(0.3255f, 0.2314f, 0.3020f), new Color(0.3255f, 0.2314f, 0.3020f), new Color(0.3255f, 0.2314f, 0.3020f), new Color(0.3255f, 0.2314f, 0.3020f), new Color(0.3255f, 0.2314f, 0.3020f), new Color(0.3255f, 0.2314f, 0.3020f), new Color(0.3255f, 0.2314f, 0.3020f),
                   new Color(0.4000f, 1.0000f, 0.4000f), new Color(0.4000f, 1.0000f, 0.4000f), new Color(0.4000f, 1.0000f, 0.4000f), new Color(0.3255f, 0.2314f, 0.3020f), new Color(0.3255f, 0.2314f, 0.3020f), new Color(0.3255f, 0.2314f, 0.3020f), new Color(0.3255f, 0.2314f, 0.3020f),
                   new Color(0.4000f, 1.0000f, 0.4000f), new Color(0.4000f, 1.0000f, 0.4000f), new Color(0.6078f, 0.4941f, 0.8706f), new Color(0.6078f, 0.4941f, 0.8706f), new Color(0.6078f, 0.4941f, 0.8706f), new Color(0.6078f, 0.4941f, 0.8706f), new Color(0.6078f, 0.4941f, 0.8706f),
                   new Color(0.4000f, 1.0000f, 0.4000f), new Color(0.4000f, 1.0000f, 0.4000f), new Color(0.4000f, 1.0000f, 0.4000f), new Color(0.3255f, 0.2314f, 0.3020f), new Color(0.3255f, 0.2314f, 0.3020f), new Color(0.3255f, 0.2314f, 0.3020f), new Color(0.3255f, 0.2314f, 0.3020f),
                   new Color(0.3255f, 0.2314f, 0.3020f), new Color(0.3255f, 0.2314f, 0.3020f), new Color(0.3255f, 0.2314f, 0.3020f), new Color(0.3255f, 0.2314f, 0.3020f), new Color(0.3255f, 0.2314f, 0.3020f), new Color(0.3255f, 0.2314f, 0.3020f), new Color(0.3255f, 0.2314f, 0.3020f),
                   new Color(0.3255f, 0.2314f, 0.3020f), new Color(0.3255f, 0.2314f, 0.3020f), new Color(0.3255f, 0.2314f, 0.3020f), new Color(0.3255f, 0.2314f, 0.3020f), new Color(0.9922f, 0.9608f, 0.7490f), new Color(0.9922f, 0.9608f, 0.7490f), new Color(0.9922f, 0.9608f, 0.7490f),
                   new Color(0.6078f, 0.4941f, 0.8706f), new Color(0.6078f, 0.4941f, 0.8706f), new Color(0.6078f, 0.4941f, 0.8706f), new Color(0.6078f, 0.4941f, 0.8706f), new Color(0.6078f, 0.4941f, 0.8706f), new Color(0.9922f, 0.9608f, 0.7490f), new Color(0.9922f, 0.9608f, 0.7490f),
                   new Color(0.3255f, 0.2314f, 0.3020f), new Color(0.3255f, 0.2314f, 0.3020f), new Color(0.3255f, 0.2314f, 0.3020f), new Color(0.3255f, 0.2314f, 0.3020f), new Color(0.9922f, 0.9608f, 0.7490f), new Color(0.9922f, 0.9608f, 0.7490f), new Color(0.9922f, 0.9608f, 0.7490f),
                   new Color(0.3255f, 0.2314f, 0.3020f), new Color(0.3255f, 0.2314f, 0.3020f), new Color(0.3255f, 0.2314f, 0.3020f), new Color(0.3255f, 0.2314f, 0.3020f), new Color(0.3255f, 0.2314f, 0.3020f), new Color(0.3255f, 0.2314f, 0.3020f), new Color(0.3255f, 0.2314f, 0.3020f),
                 };
                break;

            case "Level_50":
                levelColors = new Color[]
                 {
                   new Color(0.3255f, 0.2314f, 0.3020f), new Color(0.3255f, 0.2314f, 0.3020f), new Color(0.3255f, 0.2314f, 0.3020f), new Color(0.3255f, 0.2314f, 0.3020f), new Color(0.3255f, 0.2314f, 0.3020f), new Color(0.3255f, 0.2314f, 0.3020f), new Color(0.3255f, 0.2314f, 0.3020f),
                   new Color(0.3255f, 0.2314f, 0.3020f), new Color(0.3255f, 0.2314f, 0.3020f), new Color(0.3255f, 0.2314f, 0.3020f), new Color(0.3255f, 0.2314f, 0.3020f), new Color(0.4000f, 1.0000f, 0.4000f), new Color(0.4000f, 1.0000f, 0.4000f), new Color(0.4000f, 1.0000f, 0.4000f),
                   new Color(1.0000f, 0.2157f, 0.3725f), new Color(1.0000f, 0.2157f, 0.3725f), new Color(1.0000f, 0.2157f, 0.3725f), new Color(1.0000f, 0.2157f, 0.3725f), new Color(1.0000f, 0.2157f, 0.3725f), new Color(0.4000f, 1.0000f, 0.4000f), new Color(0.6078f, 0.4941f, 0.8706f),
                   new Color(0.3255f, 0.2314f, 0.3020f), new Color(0.3255f, 0.2314f, 0.3020f), new Color(0.3255f, 0.2314f, 0.3020f), new Color(0.3255f, 0.2314f, 0.3020f), new Color(0.4000f, 1.0000f, 0.4000f), new Color(0.4000f, 1.0000f, 0.4000f), new Color(0.4000f, 1.0000f, 0.4000f),
                   new Color(0.3255f, 0.2314f, 0.3020f), new Color(0.3255f, 0.2314f, 0.3020f), new Color(0.3255f, 0.2314f, 0.3020f), new Color(0.3255f, 0.2314f, 0.3020f), new Color(0.3255f, 0.2314f, 0.3020f), new Color(0.3255f, 0.2314f, 0.3020f), new Color(0.3255f, 0.2314f, 0.3020f),
                   new Color(0.3255f, 0.2314f, 0.3020f), new Color(0.3255f, 0.2314f, 0.3020f), new Color(0.3255f, 0.2314f, 0.3020f), new Color(0.3255f, 0.2314f, 0.3020f), new Color(0.9922f, 0.9608f, 0.7490f), new Color(0.9922f, 0.9608f, 0.7490f), new Color(0.9922f, 0.9608f, 0.7490f),
                   new Color(0.6078f, 0.4941f, 0.8706f), new Color(0.6078f, 0.4941f, 0.8706f), new Color(0.6078f, 0.4941f, 0.8706f), new Color(0.6078f, 0.4941f, 0.8706f), new Color(0.6078f, 0.4941f, 0.8706f), new Color(0.9922f, 0.9608f, 0.7490f), new Color(0.9922f, 0.9608f, 0.7490f),
                   new Color(0.3255f, 0.2314f, 0.3020f), new Color(0.3255f, 0.2314f, 0.3020f), new Color(0.3255f, 0.2314f, 0.3020f), new Color(0.3255f, 0.2314f, 0.3020f), new Color(0.9922f, 0.9608f, 0.7490f), new Color(0.9922f, 0.9608f, 0.7490f), new Color(0.9922f, 0.9608f, 0.7490f),
                   new Color(0.3255f, 0.2314f, 0.3020f), new Color(0.3255f, 0.2314f, 0.3020f), new Color(0.3255f, 0.2314f, 0.3020f), new Color(0.3255f, 0.2314f, 0.3020f), new Color(0.3255f, 0.2314f, 0.3020f), new Color(0.3255f, 0.2314f, 0.3020f), new Color(0.3255f, 0.2314f, 0.3020f),
                 };
                break;

            default:
                Debug.LogError("Nivel no definido.");
                break;
        }

    }
}