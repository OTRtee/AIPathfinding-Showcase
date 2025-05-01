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

        // offset so the grid is centered around (0,0)
        Vector2 offset = new Vector2(-width / 2f + 0.5f, -height / 2f + 0.5f);

        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
            {
                Vector2 spawnPos = new Vector2(x, y) + offset;
                Tile t = Instantiate(tilePrefab, spawnPos, Quaternion.identity);
                t.name = $"Tile_{x}_{y}";
                tiles[x, y] = t;
            }
    }



    // Resets all tiles to walkable & white
    public void ResetGrid()
    {
        if (tiles == null) return;

        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
            {
                Tile t = tiles[x, y];
                t.isWalkable = true;
                t.isStart = false;
                t.isEnd = false;
                t.UpdateColor();
            }
    }

    // Called by the Run BFS UI button
    public void RunBFSVisualWrapper()
    {
        StartCoroutine(RunBFS_Visual());
    }


    // BFS coroutine: waves in yellow, path in blue, then moves Agent.
    private IEnumerator RunBFS_Visual()
    {
        // Find user-selected start & end
        Vector2Int start = FindStartCoords();
        Vector2Int end = FindEndCoords();

        if (start.x < 0 || end.x < 0)
        {
            Debug.LogWarning("Start or End tile not selected!");
            yield break;
        }


        // Check walkability
        if (!tiles[start.x, start.y].isWalkable || !tiles[end.x, end.y].isWalkable)
        {
            Debug.LogWarning("Start or End is blocked!");
            yield break;
        }

        // Spawn Agent to the start tile
        if (agentInstance == null)
        {
            agentInstance = Instantiate(agentPrefab);
        }
        Vector3 startWorld = tiles[start.x, start.y].transform.position;
        agentInstance.position = new Vector3(startWorld.x, startWorld.y, -1f);

        // Prepare BFS data structures
        var queue = new Queue<Vector2Int>();
        var visited = new bool[width, height];
        var parents = new Vector2Int[width, height];
        queue.Enqueue(start);
        visited[start.x, start.y] = true;
        parents[start.x, start.y] = new Vector2Int(-1, -1);


        // BFS Lop
        while (queue.Count > 0)
        {
            Vector2Int cur = queue.Dequeue();
            Tile curTile = tiles[cur.x, cur.y];

            // wave visualization (yellow)
            if (!curTile.isStart && !curTile.isEnd)
                curTile.GetComponent<SpriteRenderer>().color = Color.yellow;

            // if we reached the end, build & animate path
            if (cur == end)
            {
                Debug.Log("Path found!");
                ReconstructPath(parents, start, end);
                yield break;
            }

            // enqueue valid neighbors
            foreach (var nb in GetNeighbors(cur))
            {
                if (!visited[nb.x, nb.y])
                {
                    visited[nb.x, nb.y] = true;
                    parents[nb.x, nb.y] = cur;
                    queue.Enqueue(nb);
                }
            }

            // pause so the user sees the expansion
            yield return new WaitForSeconds(0.05f);

        }
        Debug.Log("No path found!");
    }

    // Walks back from end→start via parents[], colors path blue, then kicks off Agent movement.
    private void ReconstructPath(Vector2Int[,] parents, Vector2Int start, Vector2Int end)
    {

        var path = new List<Vector2Int>();
        var cur = end;

        // backtrack until we hit (-1,-1)
        while (cur.x != -1)
        {
            path.Add(cur);
            cur = parents[cur.x, cur.y];
        }
        path.Reverse();  // now start→end

        // color the path blue
        foreach (var p in path)
        {
            Tile t = tiles[p.x, p.y];
            if (!t.isStart && !t.isEnd)
                t.GetComponent<SpriteRenderer>().color = Color.blue;
        }

        // Kick off the move coroutine
        StartCoroutine(MoveAgentAlong(path));

    }

    // Moves agentInstance tile-by-tile along the final path.
    private IEnumerator MoveAgentAlong(List<Vector2Int> path)
    {
        foreach (var coords in path)
        {
            // target position on that tile (keep z = -1)
            Vector3 target = tiles[coords.x, coords.y].transform.position;
            target.z = agentInstance.position.z;

            // smooth move (0.2s per tile)
            float elapsed = 0f, duration = 0.2f;
            Vector3 from = agentInstance.position;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                agentInstance.position = Vector3.Lerp(from, target, elapsed / duration);
                yield return null;
            }
            agentInstance.position = target;
        }
    }


    // find the tile marked isStart
    private Vector2Int FindStartCoords()
    {
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                if (tiles[x, y].isStart)
                    return new Vector2Int(x, y);

        return new Vector2Int(-1, -1);
    }

    // find the tile marked isEnd
    private Vector2Int FindEndCoords()
    {
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                if (tiles[x, y].isEnd)
                    return new Vector2Int(x, y);

        return new Vector2Int(-1, -1);
    }

    /// Returns walkable neighbors (up/down/left/right).
    private List<Vector2Int> GetNeighbors(Vector2Int cur)
    {
        var list = new List<Vector2Int>();
        int x = cur.x, y = cur.y;

        if (y + 1 < height && tiles[x, y + 1].isWalkable) list.Add(new Vector2Int(x, y + 1));
        if (y - 1 >= 0 && tiles[x, y - 1].isWalkable) list.Add(new Vector2Int(x, y - 1));
        if (x - 1 >= 0 && tiles[x - 1, y].isWalkable) list.Add(new Vector2Int(x - 1, y));
        if (x + 1 < width && tiles[x + 1, y].isWalkable) list.Add(new Vector2Int(x + 1, y));

        return list;
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



    // Single Update method inside the class
    void Update()
    {
        // ...
    }
}

