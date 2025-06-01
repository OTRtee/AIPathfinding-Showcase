using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // For Button + Slider
using TMPro;




public static class Vec2Extensions
{
    public static Vector3 WithZ(this Vector2 v, float z) => new Vector3(v.x, v.y, z);
}

public class GridManager : MonoBehaviour
{
    //————————————————————————————————————————————
    // Singleton
    //————————————————————————————————————————————
    public static GridManager Instance;
    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    //————————————————————————————————————————————
    // Types
    //————————————————————————————————————————————
    public enum SearchMode { BFS, AStar }

    //————————————————————————————————————————————
    // Algorithm Selection
    //————————————————————————————————————————————
    [Header("Algorithm Selection")]
    public SearchMode currentMode = SearchMode.BFS;
    public Button Btn_ToggleMode;
    public Text Txt_ModeLabel;

    //————————————————————————————————————————————
    // Grid Settings
    //————————————————————————————————————————————
    [Header("Grid Settings")]
    [SerializeField] int width = 10;
    [SerializeField] int height = 10;
    [SerializeField] Tile tilePrefab;
    [SerializeField] Transform tileContainer;

    //————————————————————————————————————————————
    // Grid Lines
    //————————————————————————————————————————————
    [Header("Grid Lines")]
    [SerializeField] Color gridLineColor = new Color(0, 0, 0, 0.2f);
    [SerializeField] float lineWidth = 0.005f;
    GameObject _gridLinesContainer;

    //————————————————————————————————————————————
    // Agent
    //————————————————————————————————————————————
    [Header("Agent Settings")]
    [SerializeField] Transform agentPrefab;
    Transform agentInstance;

    //————————————————————————————————————————————
    // Selection
    //————————————————————————————————————————————
    [Header("Selection Mode")]
    public bool selectingStart, selectingEnd;

    //————————————————————————————————————————————
    // Timing
    //————————————————————————————————————————————
    [Header("BFS/A* Timing")]
    public float waveStepDelay = 0.05f;
    public float pathStepDelay = 0.10f;

    //————————————————————————————————————————————
    // UI Buttons
    //————————————————————————————————————————————
    [Header("UI Buttons")]
    public Button Btn_SelectStart;
    public Button Btn_SelectEnd;
    public Button Btn_ResetGrid;
    public Button Btn_SoftReset;
    public Button Btn_RunSearch;

    //————————————————————————————————————————————
    // Sliders & Labels
    //————————————————————————————————————————————
    [Header("Interactive Sliders")]
    public Slider Slider_GridWidth;
    public Slider Slider_GridHeight;
    public Slider Slider_WaveDelay;
    public Slider Slider_PathDelay;

    [Header("Interactive Labels")]
    public TMP_Text Label_GridWidth;
    public TMP_Text Label_GridHeight;
    public TMP_Text Label_WaveDelay;
    public TMP_Text Label_PathDelay;

    //————————————————————————————————————————————
    // Internal State
    //————————————————————————————————————————————
    Tile[,] tiles;
    Tile startTile, endTile;


    // When true, we are mid‐search and must not rebuild grid or allow width/height changes.
    private bool isSearching = false;


    //————————————————————————————————————————————
    // Startup
    //————————————————————————————————————————————
    void Start()
    {
        BuildGrid();
        HookUpUI();
    }

    //————————————————————————————————————————————
    // Build / Rebuild
    //————————————————————————————————————————————
    void BuildGrid()
    {
        // clear old
        if (_gridLinesContainer != null) Destroy(_gridLinesContainer);
        foreach (Transform c in tileContainer) Destroy(c.gameObject);

        GenerateTiles();
        DrawGridLines();
        FrameCamera();

        UpdateWidthLabel(width);
        UpdateHeightLabel(height);
        UpdateWaveDelayLabel(waveStepDelay);
        UpdatePathDelayLabel(pathStepDelay);
    }

    void GenerateTiles()
    {
        tiles = new Tile[width, height];
        var offset = new Vector2(-width / 2f + .5f, -height / 2f + .5f);

        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
            {
                var pos = new Vector2(x, y) + offset;
                var t = Instantiate(tilePrefab, pos, Quaternion.identity, tileContainer);
                t.name = $"Tile_{x}_{y}";
                tiles[x, y] = t;
            }
    }

    void DrawGridLines()
    {
        _gridLinesContainer = new GameObject("GridLines");
        _gridLinesContainer.transform.SetParent(transform, false);

        var off = new Vector2(-width / 2f, -height / 2f);

        // vertical
        for (int x = 0; x <= width; x++)
            DrawLine(off + Vector2.right * x,
                     off + Vector2.right * x + Vector2.up * height);

        // horizontal
        for (int y = 0; y <= height; y++)
            DrawLine(off + Vector2.up * y,
                     off + Vector2.up * y + Vector2.right * width);
    }

    void DrawLine(Vector2 a, Vector2 b)
    {
        var go = new GameObject("Line", typeof(LineRenderer));
        go.transform.SetParent(_gridLinesContainer.transform, false);
        var lr = go.GetComponent<LineRenderer>();
        lr.positionCount = 2;
        lr.startWidth = lr.endWidth = lineWidth;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = lr.endColor = gridLineColor;

        lr.SetPosition(0, a.WithZ(-0.1f));
        lr.SetPosition(1, b.WithZ(-0.1f));
    }

    //————————————————————————————————————————————
    // Camera
    //————————————————————————————————————————————
    void FrameCamera()
    {
        var cam = Camera.main;
        cam.orthographic = true;
        cam.transform.position = new Vector3(0, 0, -10);
        cam.orthographicSize = Mathf.Max(width, height) / 2f + 1f;
    }

    //————————————————————————————————————————————
    // UI Wiring
    //————————————————————————————————————————————
    void HookUpUI()
    {
        // mode
        Btn_ToggleMode?.onClick.AddListener(ToggleMode);
        if (Txt_ModeLabel != null) Txt_ModeLabel.text = "Mode: BFS";

        // buttons
        Btn_SelectStart?.onClick.AddListener(() => selectingStart = true);
        Btn_SelectEnd?.onClick.AddListener(() => selectingEnd = true);
        Btn_ResetGrid?.onClick.AddListener(ResetGrid);
        Btn_SoftReset?.onClick.AddListener(SoftReset);
        Btn_RunSearch?.onClick.AddListener(RunSearchVisualWrapper);

        // sliders
        Slider_GridWidth?.onValueChanged.AddListener(OnWidthSlider);
        Slider_GridHeight?.onValueChanged.AddListener(OnHeightSlider);
        Slider_WaveDelay?.onValueChanged.AddListener(v => { waveStepDelay = v; UpdateWaveDelayLabel(v); });
        Slider_PathDelay?.onValueChanged.AddListener(v => { pathStepDelay = v; UpdatePathDelayLabel(v); });

        if (Slider_GridWidth != null) Slider_GridWidth.value = width;
        if (Slider_GridHeight != null) Slider_GridHeight.value = height;
        if (Slider_WaveDelay != null) Slider_WaveDelay.value = waveStepDelay;
        if (Slider_PathDelay != null) Slider_PathDelay.value = pathStepDelay;
    }

    void ToggleMode()
    {
        currentMode = currentMode == SearchMode.BFS ? SearchMode.AStar : SearchMode.BFS;
        if (Txt_ModeLabel != null)
            Txt_ModeLabel.text = currentMode == SearchMode.BFS ? "Mode: BFS" : "Mode: A*";
    }

    //————————————————————————————————————————————
    // Slider Callbacks
    //————————————————————————————————————————————
    void OnWidthSlider(float v)
    {
        if (isSearching) return;
        width = Mathf.RoundToInt(v);
        UpdateWidthLabel(width);
        BuildGrid();
    }

    void OnHeightSlider(float v)
    {
        if (isSearching) return;
        height = Mathf.RoundToInt(v);
        UpdateHeightLabel(height);
        BuildGrid();
    }

    //————————————————————————————————————————————
    // Label Helpers
    //————————————————————————————————————————————
    void UpdateWidthLabel(float v) { if (Label_GridWidth != null) Label_GridWidth.text = $"W:{v}"; }
    void UpdateHeightLabel(float v) { if (Label_GridHeight != null) Label_GridHeight.text = $"H:{v}"; }
    void UpdateWaveDelayLabel(float v) { if (Label_WaveDelay != null) Label_WaveDelay.text = $"Wave:{v:0.00}s"; }
    void UpdatePathDelayLabel(float v) { if (Label_PathDelay != null) Label_PathDelay.text = $"Path:{v:0.00}s"; }

    //————————————————————————————————————————————
    // Grid Reset
    //————————————————————————————————————————————
    public void ResetGrid()
    {
        selectingStart = selectingEnd = false;
        startTile = endTile = null;
        foreach (var t in tiles) t.ResetState();
    }

    public void SoftReset()
    {
        selectingStart = selectingEnd = false;
        foreach (var t in tiles) t.ClearWave();
    }

    //————————————————————————————————————————————
    // Tile Click
    //————————————————————————————————————————————
    public void HandleTileClick(Tile t)
    {
        if (selectingStart)
        {
            if (startTile != null) startTile.isStart = false;
            startTile = t; startTile.SetAsStart();
            selectingStart = false;
        }
        else if (selectingEnd)
        {
            if (endTile != null) endTile.isEnd = false;
            endTile = t; endTile.SetAsEnd();
            selectingEnd = false;
        }
        else
        {
            t.ToggleBlocked();
        }
    }

    //————————————————————————————————————————————
    // Run Search Wrapper
    //————————————————————————————————————————————
    public void RunSearchVisualWrapper()
    {
        if (isSearching) return;
        if (currentMode == SearchMode.BFS) StartCoroutine(RunBFS_Visual());
        else StartCoroutine(RunAStar_Visual());
    }

    void DisableUI()
    {
        isSearching = true;
        Btn_SelectStart.interactable =
        Btn_SelectEnd.interactable =
        Btn_ResetGrid.interactable =
        Btn_SoftReset.interactable =
        Btn_RunSearch.interactable =
        Btn_ToggleMode.interactable = false;

        if (Slider_GridWidth != null) Slider_GridWidth.interactable = false;
        if (Slider_GridHeight != null) Slider_GridHeight.interactable = false;
    }

    void EnableUI()
    {
        isSearching = false;
        Btn_SelectStart.interactable =
        Btn_SelectEnd.interactable =
        Btn_ResetGrid.interactable =
        Btn_SoftReset.interactable =
        Btn_RunSearch.interactable =
        Btn_ToggleMode.interactable = true;

        if (Slider_GridWidth != null) Slider_GridWidth.interactable = true;
        if (Slider_GridHeight != null) Slider_GridHeight.interactable = true;
    }

    // BFS coroutine: waves in yellow, path in blue, then moves Agent.
    private IEnumerator RunBFS_Visual()
    {
        // clear any old wave colours
        foreach (var t in tiles) t.ClearWave();
        DisableUI();

        if (startTile == null || endTile == null)
        {
            Debug.LogWarning("Select both start and end!");
            EnableUI(); yield break;
        }

        if (!startTile.isWalkable || !endTile.isWalkable)
        {
            Debug.LogWarning("Start or end is blocked!");
            EnableUI(); yield break;
        }

        if (agentInstance == null) agentInstance = Instantiate(agentPrefab);
        agentInstance.position = startTile.transform.position + Vector3.back;

        var visited = new bool[width, height];
        var parent = new Vector2Int[width, height];
        var q = new Queue<Vector2Int>();

        Vector2Int s = FindCoords(startTile),
                   e = FindCoords(endTile);

        q.Enqueue(s);
        visited[s.x, s.y] = true;
        parent[s.x, s.y] = new Vector2Int(-1, -1);

        // BFS loop
        while (q.Count > 0)
        {
            var cur = q.Dequeue();
            var tile = tiles[cur.x, cur.y];

            // closed
            if (!tile.isStart && !tile.isEnd)
                tile.PaintWave(new Color(1f, 0.65f, 0f)); // orange
            yield return new WaitForSeconds(waveStepDelay);

            if (cur == e)
                break;

            // open
            foreach (var nb in GetNeighbors(cur))
            {
                if (!visited[nb.x, nb.y])
                {
                    visited[nb.x, nb.y] = true;
                    parent[nb.x, nb.y] = cur;
                    var openTile = tiles[nb.x, nb.y];
                    if (!openTile.isEnd)
                        openTile.PaintWave(Color.yellow);
                    q.Enqueue(nb);
                }
            }
        }

        // rebuild & paint path
        var path = new List<Vector2Int>();
        for (var p = e; p.x != -1; p = parent[p.x, p.y])
            path.Add(p);
        path.Reverse();

        foreach (var p in path)
        {
            var t = tiles[p.x, p.y];
            if (!t.isStart && !t.isEnd)
                t.PaintWave(Color.green);
            yield return new WaitForSeconds(pathStepDelay);
        }

        yield return StartCoroutine(MoveAgentAlong(path));
        EnableUI();
    }

    // —— A* Coroutine —— 
    private IEnumerator RunAStar_Visual()
    {
        foreach (var t in tiles) t.ClearWave();
        DisableUI();

        if (startTile == null || endTile == null)
        {
            Debug.LogWarning("Select both start and end!");
            EnableUI(); yield break;
        }
        if (!startTile.isWalkable || !endTile.isWalkable)
        {
            Debug.LogWarning("Start or end is blocked!");
            EnableUI(); yield break;
        }

        if (agentInstance == null) agentInstance = Instantiate(agentPrefab);
        agentInstance.position = startTile.transform.position + Vector3.back;

        var open = new SimplePriorityQueue<Vector2Int>();
        var closed = new HashSet<Vector2Int>();
        var parent = new Dictionary<Vector2Int, Vector2Int>();
        var gScore = new Dictionary<Vector2Int, float>();
        var fScore = new Dictionary<Vector2Int, float>();

        Vector2Int s = FindCoords(startTile),
                   e = FindCoords(endTile);

        gScore[s] = 0;
        fScore[s] = Heuristic(s, e);
        open.Enqueue(s, fScore[s]);
        parent[s] = new Vector2Int(-1, -1);

        while (open.Count > 0)
        {
            var cur = open.Dequeue();
            closed.Add(cur);

            // paint closed (cyan) and show its g-score
            var closedT = tiles[cur.x, cur.y];
            if (!closedT.isStart && !closedT.isEnd)
            {
                closedT.PaintWave(Color.cyan);
                closedT.SetLabel(gScore[cur].ToString("0.0"));
            }
            yield return new WaitForSeconds(waveStepDelay);

            if (cur == e) break;

            foreach (var nb in GetNeighbors(cur))
            {
                if (closed.Contains(nb)) continue;

                float tentativeG = gScore[cur] + 1;
                if (!gScore.ContainsKey(nb) || tentativeG < gScore[nb])
                {
                    parent[nb] = cur;
                    gScore[nb] = tentativeG;
                    fScore[nb] = tentativeG + Heuristic(nb, e);

                    if (!open.Contains(nb))
                        open.Enqueue(nb, fScore[nb]);

                    // paint open (yellow)
                    var oT = tiles[nb.x, nb.y];
                    if (!oT.isEnd)
                        oT.PaintWave(Color.yellow);
                    oT.SetLabel(fScore[nb].ToString("0.0"));
                }
            }
        }

        // rebuild & paint path
        var path = new List<Vector2Int>();
        for (var p = e; p.x != -1; p = parent[p])
            path.Add(p);
        path.Reverse();

        foreach (var p in path)
        {
            var t = tiles[p.x, p.y];
            if (!t.isStart && !t.isEnd)
                t.PaintWave(Color.green);
            yield return new WaitForSeconds(pathStepDelay);
        }

        yield return StartCoroutine(MoveAgentAlong(path));
        EnableUI();
    }


    // fade any label after 1 second ← added
    private IEnumerator FadeLabel(Tile t)
    {
        yield return new WaitForSeconds(1f);
        t.ClearLabel();
    }


    private IEnumerator MoveAgentAlong(List<Vector2Int> path)
    {
        foreach (var p in path)
        {
            var tgt = tiles[p.x, p.y].transform.position + Vector3.back;
            var frm = agentInstance.position;
            float t = 0, d = 0.2f;
            while (t < d)
            {
                t += Time.deltaTime;
                agentInstance.position = Vector3.Lerp(frm, tgt, t / d);
                yield return null;
            }
            agentInstance.position = tgt;
        }
    }


    // find the tile marked
    private Vector2Int FindCoords(Tile t)
    {
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                if (tiles[x, y] == t)
                    return new Vector2Int(x, y);
        return new Vector2Int(-1, -1);
    }

    /// Returns walkable neighbors (up/down/left/right).
    private List<Vector2Int> GetNeighbors(Vector2Int c)
    {
        var list = new List<Vector2Int>();
        int x = c.x, y = c.y;
        if (y + 1 < height && tiles[x, y + 1].isWalkable) list.Add(new Vector2Int(x, y + 1));
        if (y - 1 >= 0 && tiles[x, y - 1].isWalkable) list.Add(new Vector2Int(x, y - 1));
        if (x - 1 >= 0 && tiles[x - 1, y].isWalkable) list.Add(new Vector2Int(x - 1, y));
        if (x + 1 < width && tiles[x + 1, y].isWalkable) list.Add(new Vector2Int(x + 1, y));
        return list;
    }

    private float Heuristic(Vector2Int a, Vector2Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }


    // Single Update method inside the class
    void Update()
    {
        // ...
    }
}

