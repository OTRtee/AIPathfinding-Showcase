using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // For Button

public class GridManager : MonoBehaviour
{
    public static GridManager Instance; // Tiles can check selectingStart/selectingEnd

    public enum SearchMode {BFS,AStar }
    [Header("Algorithm Selection")]
    public SearchMode currentMode = SearchMode.BFS;
    public Text Txt_ModeLabel;

    [Header("Grid Settings")]
    [SerializeField] private int width = 10;    // Grid columns
    [SerializeField] private int height = 10;   // Grid rows
    [SerializeField] private Tile tilePrefab;   // Tile prefab


    [Header("Grid Lines")]
    [SerializeField] private Color gridLineColor = new Color(0, 0, 0, 0.2f);
    [SerializeField] private float lineWidth = 0.02f;
    private GameObject _gridLinesContainer;

    [Header("Agent Settings")]
    [SerializeField] private Transform agentPrefab;  // Agent prefab
    private Transform agentInstance;   // spawned Agent


    [Header("Selection Mode")]
    public bool selectingStart, selectingEnd;

    [Header("BFS/A* Timing")]
    public float waveStepDelay = 0.05f;
    public float pathStepDelay = 0.10f;


    [Header("UI Buttons")]
    public Button Btn_SelectStart;
    public Button Btn_SelectEnd;
    public Button Btn_ResetGrid;
    public Button Btn_SoftReset;
    public Button Btn_RunSearch;
    public Button Btn_ToggleMode;


    private Tile[,] tiles; // spawned grid
    private Tile startTile, endTile;// store the current start, end


    void Awake()
    {
        //singleton patterm
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        GenerateGrid();
        DrawGridLines();

        // wire up mode toggle
        Btn_ToggleMode.onClick.AddListener(() =>
        {
            currentMode = (currentMode == SearchMode.BFS)
                            ? SearchMode.AStar
                            : SearchMode.BFS;
            Txt_ModeLabel.text = currentMode == SearchMode.BFS
                                    ? "Mode: BFS"
                                    : "Mode: A*";
        });
        Txt_ModeLabel.text = "Mode: BFS";

        // wire up buttons
        Btn_SelectStart.onClick.AddListener(SelectStartMode);
        Btn_SelectEnd.onClick.AddListener(SelectEndMode);
        Btn_ResetGrid.onClick.AddListener(ResetGrid);
        Btn_SoftReset.onClick.AddListener(SoftReset);
        Btn_RunSearch.onClick.AddListener(RunSearchVisualWrapper);
    }

    private void DrawGridLines()
    {
       
        if (_gridLinesContainer != null) Destroy(_gridLinesContainer);
        _gridLinesContainer = new GameObject("GridLines");
        _gridLinesContainer.transform.SetParent(transform, false);

        var offset = new Vector2(-width / 2f, -height / 2f);

        // vertical lines
        for (int x = 0; x <= width; x++)
        {
            var go = new GameObject($"V-Line-{x}", typeof(LineRenderer));
            go.transform.SetParent(_gridLinesContainer.transform, false);
            var lr = go.GetComponent<LineRenderer>();
            lr.positionCount = 2;
            lr.startWidth = lr.endWidth = lineWidth;
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.startColor = lr.endColor = gridLineColor;

            lr.SetPosition(0, new Vector3(offset.x + x, offset.y, -0.1f));
            lr.SetPosition(1, new Vector3(offset.x + x, offset.y + height, -0.1f));
        }

        // horizontal lines
        for (int y = 0; y <= height; y++)
        {
            var go = new GameObject($"H-Line-{y}", typeof(LineRenderer));
            go.transform.SetParent(_gridLinesContainer.transform, false);
            var lr = go.GetComponent<LineRenderer>();
            lr.positionCount = 2;
            lr.startWidth = lr.endWidth = lineWidth;
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.startColor = lr.endColor = gridLineColor;

            lr.SetPosition(0, new Vector3(offset.x, offset.y + y, -0.1f));
            lr.SetPosition(1, new Vector3(offset.x + width, offset.y + y, -0.1f));
        }
    }

    // Creates a width×height grid centered at (0,0)
    private void GenerateGrid()
    {
        // Allocate the array
        tiles = new Tile[width, height];
        var offset = new Vector2(-width / 2f + .5f, -height / 2f + .5f);

        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
            {
                var pos = new Vector2(x, y) + offset;
                var t = Instantiate(tilePrefab, pos, Quaternion.identity);
                t.name = $"Tile_{x}_{y}";
                tiles[x, y] = t;
            }
    }



   // Resets every tile back to unvisited-white, clears start/end, and cancels selection modes
    public void ResetGrid()
    {
        selectingStart = selectingEnd = false;
        startTile = endTile = null;
        foreach (var t in tiles) t.ResetState();
    }

    public void SoftReset()
    {
        selectingStart = selectingEnd = false;
        foreach (var t in tiles)
            t.ClearWave();
    }


    // Central click handler: toggles blocked, or assigns start/end.
    public void HandleTileClick(Tile t)
    {
        if(selectingStart)
        {
            // Clear Previous
            if (startTile != null) startTile.isStart = false;
            startTile = t;
            startTile.SetAsStart();
            selectingStart = false;
        }
        else if (selectingEnd)
        {
            if (endTile != null) endTile.isEnd = false;
            endTile = t;
            endTile.SetAsEnd();
            selectingEnd = false;
        }
        else
        {
            //normal toggle
            t.ToggleBlocked();
        }
    }


    // UI button hooks:
    public void SelectStartMode()
    {
        selectingStart = true;
        selectingEnd = false;
    }

    public void SelectEndMode()
    {
        selectingEnd = true;
        selectingStart = false;
    }



    // Called by the Run BFS UI button
    public void RunSearchVisualWrapper()
    {
        Debug.Log("▶ RunSearchVisualWrapper called. mode = " + currentMode);
        if (currentMode == SearchMode.BFS)
            StartCoroutine(RunBFS_Visual());
        else
            StartCoroutine(RunAStar_Visual());
    }

    private void DisableUI()
    {
        Btn_SelectStart.interactable =
        Btn_SelectEnd.interactable =
        Btn_ResetGrid.interactable =
        Btn_SoftReset.interactable =
        Btn_RunSearch.interactable =
        Btn_ToggleMode.interactable = false;
    }

    private void EnableUI()
    {
        Btn_SelectStart.interactable =
        Btn_SelectEnd.interactable =
        Btn_ResetGrid.interactable =
        Btn_SoftReset.interactable =
        Btn_RunSearch.interactable =
        Btn_ToggleMode.interactable = true;
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

    private float Heuristic(Vector2Int a, Vector2Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
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

    // Single Update method inside the class
    void Update()
    {
        // ...
    }
}

