using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // For Button

public class GridManager : MonoBehaviour
{
    public static GridManager Instance; // Tiles can check selectingStart/selectingEnd


    [Header("Grid Settings")]
    [SerializeField] private int width = 10;    // Grid columns
    [SerializeField] private int height = 10;   // Grid rows
    [SerializeField] private Tile tilePrefab;   // Tile prefab


    [Header("Agent Settings")]
    [SerializeField] private Transform agentPrefab;  // Agent prefab
    private Transform agentInstance;   // spawned Agent


    [Header("Selection Mode")]
    public bool selectingStart = false;         // true when waiting for user to pick Start
    public bool selectingEnd = false;         // true when waiting for user to pick End


    [Header("BFS Timing")]
    [SerializeField]  public float waveStepDelay = 0.05f;
    [SerializeField] public float pathStepDelay = 0.10f;


    [Header("UI Buttons")]
    public Button Btn_SelectStart;
    public Button Btn_SelectEnd;
    public Button Btn_ResetGrid;
    public Button Btn_RunBFS;


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

        foreach (var t in tiles)
        {
            t.ResetState();
        }
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
    public void RunBFSVisualWrapper()
    {
        StartCoroutine(RunBFS_Visual());
    }



    // BFS coroutine: waves in yellow, path in blue, then moves Agent.
    private IEnumerator RunBFS_Visual()
    {
        // disable all controls
        Btn_SelectStart.interactable =
        Btn_SelectEnd.interactable =
        Btn_ResetGrid.interactable =
        Btn_RunBFS.interactable = false;


        if (startTile == null || endTile == null)
        {
            Debug.LogWarning("You must select both Start AND End before running BFS.");
            yield break;
        }

        if (!startTile.isWalkable || !endTile.isWalkable)
        {
            Debug.LogWarning("Start or End is blocked.");
            yield break;
        }
        // spawn/move agent
        if (agentInstance == null)
            agentInstance = Instantiate(agentPrefab);
        agentInstance.position = startTile.transform.position + Vector3.back;

        // Prepare BFS structures
        var visited = new bool[width, height];
        var parent = new Vector2Int[width, height];
        var q = new Queue<Vector2Int>();

        Vector2Int s = FindCoords(startTile);
        Vector2Int e = FindCoords(endTile);

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

        // rebuild path (green)
        var path = new List<Vector2Int>();
        var pcur = e;
        while (pcur.x != -1)
        {
            path.Add(pcur);
            pcur = parent[pcur.x, pcur.y];
        }
        path.Reverse();

        foreach (var p in path)
        {
            var pt = tiles[p.x, p.y];
            if (!pt.isStart && !pt.isEnd)
                pt.PaintWave(Color.green);
            yield return new WaitForSeconds(pathStepDelay);
        }

        // move agent
        yield return StartCoroutine(MoveAgentAlong(path));

    FINISH:
        // re-enable all controls
        Btn_SelectStart.interactable =
        Btn_SelectEnd.interactable =
        Btn_ResetGrid.interactable =
        Btn_RunBFS.interactable = true;
    }


    //// Walks back from end→start via parents[], colors path blue, then kicks off Agent movement.
    //private void ReconstructPath(Vector2Int[,] parents, Vector2Int s, Vector2Int e)
    //{
    //    var path = new List<Vector2Int>();
    //    var cur = e;
    //    while (cur.x != -1)
    //    {
    //        path.Add(cur);
    //        cur = parents[cur.x, cur.y];
    //    }
    //    path.Reverse();

    //    foreach (var p in path)
    //    {
    //        var t = tiles[p.x, p.y];
    //        if (!t.isStart && !t.isEnd)
    //            t.PaintWave(Color.blue);
    //    }

    //    StartCoroutine(MoveAgentAlong(path));
    //}

    // Moves agentInstance tile-by-tile along the final path.
    private IEnumerator MoveAgentAlong(List<Vector2Int> path)
    {
        foreach (var p in path)
        {
            var target = tiles[p.x, p.y].transform.position + Vector3.back;
            var start = agentInstance.position;
            float t = 0, dur = 0.2f;

            while (t < dur)
            {
                t += Time.deltaTime;
                agentInstance.position = Vector3.Lerp(start, target, t / dur);
                yield return null;
            }
            agentInstance.position = target;
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

