using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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


    private Tile[,] tiles; // spawned grid
    private Tile startTile;// store the current start
    private Tile endTile; // store the current start


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



    // Resets all tiles to walkable & white
    public void ResetGrid()
    {
        startTile = endTile = null;
        selectingStart = selectingEnd = false;

        foreach (var t in tiles)
        {
            t.isWalkable = true;
            t.isStart = t.isEnd = false;
            t.UpdateColor();
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
        if (startTile == null || endTile == null)
        {
            Debug.LogWarning("Select both start and end!");
            yield break;
        }

        if (!startTile.isWalkable || !endTile.isWalkable)
        {
            Debug.LogWarning("Start or End is blocked!");
            yield break;
        }

        if (agentInstance == null)
            agentInstance = Instantiate(agentPrefab);

        agentInstance.position = startTile.transform.position + Vector3.back;

        var queue = new Queue<Vector2Int>();
        var visited = new bool[width, height];
        var parents = new Vector2Int[width, height];

        Vector2Int s = FindCoords(startTile);
        Vector2Int e = FindCoords(endTile);

        queue.Enqueue(s);
        visited[s.x, s.y] = true;
        parents[s.x, s.y] = new Vector2Int(-1, -1);

        while (queue.Count > 0)
        {
            var cur = queue.Dequeue();
            var tile = tiles[cur.x, cur.y];

            if (!tile.isStart && !tile.isEnd)
                tile.PaintWave(Color.yellow);

            if (cur == e)
            {
                ReconstructPath(parents, s, e);
                yield break;
            }

            foreach (var nb in GetNeighbors(cur))
            {
                if (!visited[nb.x, nb.y])
                {
                    visited[nb.x, nb.y] = true;
                    parents[nb.x, nb.y] = cur;
                    queue.Enqueue(nb);
                }
            }

            yield return new WaitForSeconds(0.05f);
        }

        Debug.Log("No path found!");
    }

    // Walks back from end→start via parents[], colors path blue, then kicks off Agent movement.
    private void ReconstructPath(Vector2Int[,] parents, Vector2Int s, Vector2Int e)
    {
        var path = new List<Vector2Int>();
        var cur = e;
        while (cur.x != -1)
        {
            path.Add(cur);
            cur = parents[cur.x, cur.y];
        }
        path.Reverse();

        foreach (var p in path)
        {
            var t = tiles[p.x, p.y];
            if (!t.isStart && !t.isEnd)
                t.PaintWave(Color.blue);
        }

        StartCoroutine(MoveAgentAlong(path));
    }

    // Moves agentInstance tile-by-tile along the final path.
    private IEnumerator MoveAgentAlong(List<Vector2Int> path)
    {
        foreach (var p in path)
        {
            var target = tiles[p.x, p.y].transform.position + Vector3.back;
            float elapsed = 0f, duration = 0.2f;
            var from = agentInstance.position;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                agentInstance.position = Vector3.Lerp(from, target, elapsed / duration);
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

