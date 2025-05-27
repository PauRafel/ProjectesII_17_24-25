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
    [Header("Grid Configuration")]
    public GameObject cellPrefab;
    public int rows = 9;
    public int columns = 7;
    public float cellSpacing = -0.3f;

    [Header("Portal System")]
    [Tooltip("Coordenadas (x,y) de las celdas que tendrán Portales en este nivel (solo niveles 17-64).")]
    public List<Vector2Int> portalPositions;
    public GameObject portalPrefab;

    [Header("Bomb System")]
    public GameObject bombPrefab;
    public List<Vector2Int> bombPositions;

    [Header("Game Management")]
    public int maxAttempts = 10;
    public TextMeshProUGUI attemptsText;
    public GameObject levelFailedPanel;

    [Header("Visual")]
    public Color[] levelColors;

    public GridCell[,] gridCells;
    public int remainingAttempts;
    private int activePropagations = 0;
    private bool isCheckingRestart = false;
    private bool finishedPropagations;

    private const float GRID_OFFSET_X = 1.06f;
    private const float GRID_OFFSET_Y = 1.74f;
    private const float ANIMATION_DURATION = 1.0f;
    private const float CELL_SCALE = 0.06f;
    private const float FAIL_PANEL_DELAY = 0.5f;

    void Start()
    {
        InitializeGame();
    }

    private void InitializeGame()
    {
        string levelName = SceneManager.GetActiveScene().name;
        SetLevelColors(levelName);
        GenerateGrid();
        InitializeAttempts();
        InstantiatePortals();
        InstantiateBombs();
    }

    private void InitializeAttempts()
    {
        remainingAttempts = maxAttempts;
        UpdateAttemptsUI();
    }

    private void InstantiateBombs()
    {
        foreach (Vector2Int bombPos in bombPositions)
        {
            CreateBombAtPosition(bombPos);
        }
    }

    private void CreateBombAtPosition(Vector2Int position)
    {
        GridCell cell = gridCells[position.x, position.y];
        Vector3 worldPosition = cell.transform.position;
        Bomb newBomb = Instantiate(bombPrefab, worldPosition, Quaternion.identity).GetComponent<Bomb>();
        newBomb.Initialize(position, this);
        cell.bomb = newBomb;
    }

    public void UseAttempt()
    {
        if (remainingAttempts <= 0) return;

        remainingAttempts--;
        UpdateAttemptsUI();

        if (remainingAttempts <= 0)
        {
            StartCoroutine(WaitForPropagations());
        }
    }

    public GridCell GetCellAt(int x, int y)
    {
        if (!IsWithinBounds(x, y)) return null;
        return gridCells[x, y];
    }

    public GridCell GetCell(int x, int y)
    {
        string cellName = GetCellName(x, y);
        Transform cellTransform = transform.Find(cellName);
        return cellTransform?.GetComponent<GridCell>();
    }

    private string GetCellName(int x, int y)
    {
        return $"Cell_{x}_{y}";
    }

    private void InstantiatePortals()
    {
        if (portalPositions == null || portalPositions.Count == 0) return;

        foreach (Vector2Int coord in portalPositions)
        {
            CreatePortalAtPosition(coord);
        }
    }

    private void CreatePortalAtPosition(Vector2Int position)
    {
        GridCell cell = GetCellAt(position.x, position.y);
        if (cell == null)
        {
            Debug.LogWarning($"Coordenada de Portal fuera de rango: {position}");
            return;
        }

        GameObject portalGO = Instantiate(portalPrefab, cell.transform.position, Quaternion.identity);
        portalGO.transform.SetParent(this.transform);

        Portal portal = portalGO.GetComponent<Portal>();
        if (portal != null)
        {
            portal.linkedCell = cell;
            cell.linkedPortal = portal;
        }
        else
        {
            Debug.LogError("El prefab de Portal no tiene el script Portal adjunto.");
        }
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

    private void GenerateGrid()
    {
        gridCells = new GridCell[columns, rows];

        if (!ValidateLevelColors()) return;

        List<GameObject> cells = CreateCells();
        cells = ShuffleCells(cells);
        StartCoroutine(AnimateCellsAppearance(cells));
    }

    private bool ValidateLevelColors()
    {
        if (levelColors == null || levelColors.Length != rows * columns)
        {
            Debug.LogError("El array de colores no coincide con la cantidad de celdas.");
            return false;
        }
        return true;
    }

    private List<GameObject> CreateCells()
    {
        List<GameObject> cells = new List<GameObject>();
        float offsetX = (columns - 1) / 2f;
        float offsetY = (rows - 1) / 2f;

        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < columns; x++)
            {
                GameObject cell = CreateCell(x, y, offsetX, offsetY);
                cells.Add(cell);
            }
        }
        return cells;
    }

    private GameObject CreateCell(int x, int y, float offsetX, float offsetY)
    {
        Vector3 position = CalculateCellPosition(x, y, offsetX, offsetY);
        GameObject cell = Instantiate(cellPrefab, position, Quaternion.identity);

        ConfigureCell(cell, x, y);
        AssignCellColor(cell, x, y);

        cell.transform.localScale = Vector3.zero;
        return cell;
    }

    private Vector3 CalculateCellPosition(int x, int y, float offsetX, float offsetY)
    {
        return new Vector3(
            x - offsetX + GRID_OFFSET_X + (x * cellSpacing),
            -(y - offsetY + GRID_OFFSET_Y) - (y * cellSpacing),
            0
        );
    }

    private void ConfigureCell(GameObject cell, int x, int y)
    {
        cell.name = GetCellName(x, y);
        cell.transform.parent = transform;

        GridCell gridCell = cell.GetComponent<GridCell>();
        if (gridCell != null)
        {
            gridCells[x, y] = gridCell;
        }
    }

    private void AssignCellColor(GameObject cell, int x, int y)
    {
        int index = y * columns + x;
        Color cellColor = levelColors[index];

        GridCell gridCell = cell.GetComponent<GridCell>();
        if (gridCell != null)
        {
            gridCell.cellColor = cellColor;
        }

        SpriteRenderer renderer = cell.GetComponent<SpriteRenderer>();
        if (renderer != null)
        {
            renderer.material = new Material(renderer.material);
            renderer.color = cellColor;
        }
    }

    private List<GameObject> ShuffleCells(List<GameObject> cells)
    {
        System.Random rnd = new System.Random();
        return cells.OrderBy(c => rnd.Next()).ToList();
    }

    private IEnumerator AnimateCellsAppearance(List<GameObject> cells)
    {
        float delayBetweenCells = ANIMATION_DURATION / cells.Count;
        System.Random rnd = new System.Random();

        foreach (GameObject cell in cells)
        {
            StartCoroutine(ScaleUpCell(cell, rnd.Next(5, 13) / 100f));
            yield return new WaitForSeconds(delayBetweenCells * UnityEngine.Random.Range(0.25f, 0.75f));
        }
    }

    private IEnumerator ScaleUpCell(GameObject cell, float duration)
    {
        Vector3 originalScale = new Vector3(CELL_SCALE, CELL_SCALE, 1);
        Vector3 startScale = Vector3.zero;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            cell.transform.localScale = Vector3.Lerp(startScale, originalScale, elapsedTime / duration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        cell.transform.localScale = originalScale;
    }

    public Vector3 GridToWorldPosition(int x, int y)
    {
        float offsetX = (columns - 1) / 2f;
        float offsetY = (rows - 1) / 2f;

        return new Vector3(
            x - offsetX + GRID_OFFSET_X + (x * cellSpacing),
            -(y - offsetY + 1.5f) - (y * cellSpacing),
            0
        );
    }

    public bool IsWithinBounds(int x, int y)
    {
        return x >= 0 && x < columns && y >= 0 && y < rows;
    }

    public void SetCellColor(int x, int y, Color color)
    {
        string cellName = GetCellName(x, y);
        Transform cellTransform = transform.Find(cellName);

        if (cellTransform != null)
        {
            GridCell gridCell = cellTransform.GetComponent<GridCell>();
            gridCell?.SetColor(color);
        }
    }

    public void DestroyCell(int x, int y, Color explosionColor)
    {
        string cellName = GetCellName(x, y);
        Transform cellTransform = transform.Find(cellName);

        if (cellTransform != null)
        {
            GridCell gridCell = cellTransform.GetComponent<GridCell>();
            gridCell?.SetColor(explosionColor);
        }
    }

    private IEnumerator CheckAndRestartLevel()
    {
        if (isCheckingRestart) yield break;

        isCheckingRestart = true;
        yield return StartCoroutine(WaitForPropagations());

        LevelManager levelManager = FindObjectOfType<LevelManager>();
        if (levelManager != null)
        {
            if (HandleVictoryCheck(levelManager))
            {
                isCheckingRestart = false;
                yield break;
            }

            HandleDefeatCheck();
        }

        isCheckingRestart = false;
    }

    private bool HandleVictoryCheck(LevelManager levelManager)
    {
        bool victory = levelManager.CheckVictoryCondition();
        if (victory)
        {
            Debug.Log("¡Nivel completado!");
            levelManager.CompleteLevel();
            return true;
        }
        return false;
    }

    private void HandleDefeatCheck()
    {
        if (remainingAttempts <= 0 && finishedPropagations)
        {
            Debug.Log("Sin intentos restantes. Mostrando panel de derrota...");
            StartCoroutine(ShowFailPanel());
            LevelSoundManager.instance.PlayLoseSound();
        }
    }

    private IEnumerator ShowFailPanel()
    {
        yield return new WaitForSeconds(FAIL_PANEL_DELAY);
        levelFailedPanel.SetActive(true);
    }

    public IEnumerator AnimateVictory()
    {
        yield return new WaitForSeconds(1f);

        foreach (GridCell cell in gridCells)
        {
            if (cell != null)
            {
                StartCoroutine(cell.AnimateVictoryEffect());
            }
        }

        yield return new WaitForSeconds(1.5f);
    }

    private void UpdateAttemptsUI()
    {
        if (attemptsText != null)
        {
            attemptsText.text = $"{remainingAttempts}";
        }
    }

    void SetLevelColors(string levelName)
    {
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
                new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.9098f, 0.2000f, 0.6000f), new Color(0.3176f, 0.2275f, 0.4078f), new Color(0.3529f, 0.6549f, 0.7255f), new Color(0.9098f, 0.2000f, 0.6000f),
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
                   new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.2549f, 0.2745f, 0.2392f),
                   new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.6157f, 0.5529f, 0.9451f),
                   new Color(0.9176f, 0.8471f, 0.7451f), new Color(0.9176f, 0.8471f, 0.7451f), new Color(0.9176f, 0.8471f, 0.7451f), new Color(0.9176f, 0.8471f, 0.7451f), new Color(0.9176f, 0.8471f, 0.7451f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.9529f, 0.7882f, 0.3843f),
                   new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.6157f, 0.5529f, 0.9451f),
                   new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.2549f, 0.2745f, 0.2392f),
                   new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.6392f, 0.0431f, 0.2157f),
                   new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.6392f, 0.0431f, 0.2157f),
                   new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.6392f, 0.0431f, 0.2157f),
                   new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.2549f, 0.2745f, 0.2392f),
                 };
                break;

            case "Level_50":
                levelColors = new Color[]
                 {
                   new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.2549f, 0.2745f, 0.2392f),
                   new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.6157f, 0.5529f, 0.9451f),
                   new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.2549f, 0.2745f, 0.2392f),
                   new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.6157f, 0.5529f, 0.9451f),
                   new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.6392f, 0.0431f, 0.2157f),
                   new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.6157f, 0.5529f, 0.9451f),
                   new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.6157f, 0.5529f, 0.9451f),
                   new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.2549f, 0.2745f, 0.2392f),
                   new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.6157f, 0.5529f, 0.9451f),
                 };
                break;

            case "Level_51":
                levelColors = new Color[]
                 {
                   new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.9529f, 0.7882f, 0.3843f),
                   new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.2549f, 0.2745f, 0.2392f),
                   new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.2549f, 0.2745f, 0.2392f),
                   new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.6157f, 0.5529f, 0.9451f),
                   new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.2549f, 0.2745f, 0.2392f),
                   new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.2549f, 0.2745f, 0.2392f),
                   new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.6392f, 0.0431f, 0.2157f),
                   new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.9529f, 0.7882f, 0.3843f),
                   new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.9529f, 0.7882f, 0.3843f),
                 };
                break;

            case "Level_52":
                levelColors = new Color[]
                 {
                   new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.2549f, 0.2745f, 0.2392f),
                   new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.2549f, 0.2745f, 0.2392f),
                   new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.2549f, 0.2745f, 0.2392f),
                   new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.6157f, 0.5529f, 0.9451f),
                   new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.9529f, 0.7882f, 0.3843f),
                   new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.6392f, 0.0431f, 0.2157f),
                   new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.2549f, 0.2745f, 0.2392f),
                   new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.2549f, 0.2745f, 0.2392f),
                   new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.2549f, 0.2745f, 0.2392f),
                 };
                break;

            case "Level_53":
                levelColors = new Color[]
                 {
                   new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.9529f, 0.7882f, 0.3843f),
                   new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.9529f, 0.7882f, 0.3843f),
                   new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.6157f, 0.5529f, 0.9451f),
                   new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.6157f, 0.5529f, 0.9451f),
                   new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.2549f, 0.2745f, 0.2392f),
                   new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.2549f, 0.2745f, 0.2392f),
                   new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.6157f, 0.5529f, 0.9451f),
                   new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.6157f, 0.5529f, 0.9451f),
                   new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.6392f, 0.0431f, 0.2157f),
                 };
                break;

            case "Level_54":
                levelColors = new Color[]
                 {
                   new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.6157f, 0.5529f, 0.9451f),
                   new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.2549f, 0.2745f, 0.2392f),
                   new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.9529f, 0.7882f, 0.3843f),
                   new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.9529f, 0.7882f, 0.3843f),
                   new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.6392f, 0.0431f, 0.2157f),
                   new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.6392f, 0.0431f, 0.2157f),
                   new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.6392f, 0.0431f, 0.2157f),
                   new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.6392f, 0.0431f, 0.2157f),
                   new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.6392f, 0.0431f, 0.2157f),
                 };
                break;

            case "Level_55":
                levelColors = new Color[]
                 {
                   new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.6157f, 0.5529f, 0.9451f),
                   new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.9529f, 0.7882f, 0.3843f),
                   new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.9529f, 0.7882f, 0.3843f),
                   new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.9529f, 0.7882f, 0.3843f),
                   new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.6392f, 0.0431f, 0.2157f),
                   new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.2549f, 0.2745f, 0.2392f),
                   new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.6157f, 0.5529f, 0.9451f),
                   new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.2549f, 0.2745f, 0.2392f),
                   new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.9529f, 0.7882f, 0.3843f),
                 };
                break;

            case "Level_56":
                levelColors = new Color[]
                 {
                   new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.6392f, 0.0431f, 0.2157f),
                   new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.6392f, 0.0431f, 0.2157f),
                   new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.6392f, 0.0431f, 0.2157f),
                   new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.9529f, 0.7882f, 0.3843f),
                   new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.9529f, 0.7882f, 0.3843f),
                   new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.9529f, 0.7882f, 0.3843f),
                   new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.6157f, 0.5529f, 0.9451f),
                   new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.6157f, 0.5529f, 0.9451f),
                   new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.9529f, 0.7882f, 0.3843f),
                 };
                break;

            case "Level_57":
                levelColors = new Color[]
                 {
                   new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.9529f, 0.7882f, 0.3843f),
                   new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.2549f, 0.2745f, 0.2392f),
                   new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.2549f, 0.2745f, 0.2392f),
                   new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.2549f, 0.2745f, 0.2392f),
                   new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.2549f, 0.2745f, 0.2392f),
                   new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.2549f, 0.2745f, 0.2392f),
                   new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.6392f, 0.0431f, 0.2157f),
                   new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.6392f, 0.0431f, 0.2157f),
                   new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.6392f, 0.0431f, 0.2157f),
                 };
                break;

            case "Level_58":
                levelColors = new Color[]
                 {
                   new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.2549f, 0.2745f, 0.2392f),
                   new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.9529f, 0.7882f, 0.3843f),
                   new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.6392f, 0.0431f, 0.2157f),
                   new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.9529f, 0.7882f, 0.3843f),
                   new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.9529f, 0.7882f, 0.3843f),
                   new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.9529f, 0.7882f, 0.3843f),
                   new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.6392f, 0.0431f, 0.2157f),
                   new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.9529f, 0.7882f, 0.3843f),
                   new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.6392f, 0.0431f, 0.2157f),
                 };
                break;

            case "Level_59":
                levelColors = new Color[]
                 {
                   new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.2549f, 0.2745f, 0.2392f),
                   new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.6157f, 0.5529f, 0.9451f),
                   new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.9529f, 0.7882f, 0.3843f),
                   new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.2549f, 0.2745f, 0.2392f),
                   new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.6392f, 0.0431f, 0.2157f),
                   new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.6157f, 0.5529f, 0.9451f),
                   new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.6157f, 0.5529f, 0.9451f),
                   new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.6157f, 0.5529f, 0.9451f),
                   new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.2549f, 0.2745f, 0.2392f),
                 };
                break;

            case "Level_60":
                levelColors = new Color[]
                 {
                   new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.9529f, 0.7882f, 0.3843f),
                   new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.9529f, 0.7882f, 0.3843f),
                   new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.9529f, 0.7882f, 0.3843f),
                   new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.9529f, 0.7882f, 0.3843f),
                   new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.9529f, 0.7882f, 0.3843f),
                   new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.2549f, 0.2745f, 0.2392f),
                   new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.6392f, 0.0431f, 0.2157f),
                   new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.6392f, 0.0431f, 0.2157f),
                   new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.2549f, 0.2745f, 0.2392f),
                 };
                break;

            case "Level_61":
                levelColors = new Color[]
                 {
                   new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.6157f, 0.5529f, 0.9451f),
                   new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.6157f, 0.5529f, 0.9451f),
                   new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.2549f, 0.2745f, 0.2392f),
                   new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.6157f, 0.5529f, 0.9451f),
                   new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.2549f, 0.2745f, 0.2392f),
                   new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.2549f, 0.2745f, 0.2392f),
                   new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.9529f, 0.7882f, 0.3843f),
                   new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.9529f, 0.7882f, 0.3843f),
                   new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.9529f, 0.7882f, 0.3843f),
                 };
                break;

            case "Level_62":
                levelColors = new Color[]
                 {
                   new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.6392f, 0.0431f, 0.2157f),
                   new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.6392f, 0.0431f, 0.2157f),
                   new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.6392f, 0.0431f, 0.2157f),
                   new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.6157f, 0.5529f, 0.9451f),
                   new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.6157f, 0.5529f, 0.9451f),
                   new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.6392f, 0.0431f, 0.2157f),
                   new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.6392f, 0.0431f, 0.2157f),
                   new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.9529f, 0.7882f, 0.3843f),
                   new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.2549f, 0.2745f, 0.2392f),
                 };
                break;

            case "Level_63":
                levelColors = new Color[]
                 {
                   new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.6157f, 0.5529f, 0.9451f),
                   new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.6392f, 0.0431f, 0.2157f),
                   new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.6392f, 0.0431f, 0.2157f),
                   new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.9529f, 0.7882f, 0.3843f),
                   new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.9529f, 0.7882f, 0.3843f),
                   new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.6157f, 0.5529f, 0.9451f),
                   new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.9529f, 0.7882f, 0.3843f),
                   new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.9529f, 0.7882f, 0.3843f),
                   new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.2549f, 0.2745f, 0.2392f),
                 };
                break;

            case "Level_64":
                levelColors = new Color[]
                 {
                   new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.6392f, 0.0431f, 0.2157f),
                   new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.2549f, 0.2745f, 0.2392f),
                   new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.9529f, 0.7882f, 0.3843f),
                   new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.9529f, 0.7882f, 0.3843f),
                   new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.6392f, 0.0431f, 0.2157f),
                   new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.6392f, 0.0431f, 0.2157f),
                   new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.2549f, 0.2745f, 0.2392f),
                   new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.2549f, 0.2745f, 0.2392f), new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.2549f, 0.2745f, 0.2392f),
                   new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.6392f, 0.0431f, 0.2157f), new Color(0.9529f, 0.7882f, 0.3843f), new Color(0.6157f, 0.5529f, 0.9451f), new Color(0.6392f, 0.0431f, 0.2157f),
                 };
                break;

            default:
                Debug.LogError("Nivel no definido.");
                break;
        }

    }
}