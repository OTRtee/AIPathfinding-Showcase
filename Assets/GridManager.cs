using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    [SerializeField] private int width = 10;   // # of tiles horizontally
    [SerializeField] private int height = 10;  // # of tiles vertically
    [SerializeField] private Tile tilePrefab;  // Reference to Tile prefab

    // 2D array storing the spawned tiles (makes BFS easier)
    private Tile[,] tiles;

    void Start()
    {
        GenerateGrid();
    }

    private void GenerateGrid()
    {
        // Allocate the array
        tiles = new Tile[width, height];

        // offset so the grid is centered around (0,0)
        Vector2 offset = new Vector2(-width / 2f + 0.5f, -height / 2f + 0.5f);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector2 spawnPos = new Vector2(x, y) + offset;
                Tile spawnedTile = Instantiate(tilePrefab, spawnPos, Quaternion.identity);
                spawnedTile.name = $"Tile_{x}_{y}";

                // Store the spawned tile in the 2D array
                tiles[x, y] = spawnedTile;
            }
        }
    }

    // Method that resets all tiles to walkable (white)
    public void ResetGrid()
    {
        if (tiles == null) return;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Tile tile = tiles[x, y];
                tile.isWalkable = true;
                tile.UpdateColor();
            }
        }
    }

    // BFS Method
    public void RunBFS()
    {
        // Hardcode start at (0,0) and end at (width-1, height-1)
        Vector2Int startCoords = new Vector2Int(0, 0);
        Vector2Int endCoords = new Vector2Int(width - 1, height - 1);

        // Ensure tiles exist and we haven't blocked the start/end
        if (tiles == null ||
            !tiles[startCoords.x, startCoords.y].isWalkable ||
            !tiles[endCoords.x, endCoords.y].isWalkable)
        {
            Debug.Log("Start or End is blocked or grid is null!");
            return;
        }

        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        bool[,] visited = new bool[width, height];
        Vector2Int[,] parents = new Vector2Int[width, height];  // store path

        // Enqueue the start
        queue.Enqueue(startCoords);
        visited[startCoords.x, startCoords.y] = true;
        parents[startCoords.x, startCoords.y] = new Vector2Int(-1, -1);

        // BFS loop
        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();

            if (current == endCoords)
            {
                Debug.Log("Path Found!");
                ReconstructPath(parents, startCoords, endCoords);
                return;
            }

            // explore neighbors
            foreach (var neighbor in GetNeighbors(current))
            {
                if (!visited[neighbor.x, neighbor.y])
                {
                    visited[neighbor.x, neighbor.y] = true;
                    parents[neighbor.x, neighbor.y] = current;
                    queue.Enqueue(neighbor);
                }
            }
        }

        Debug.Log("No path found!");
    }

    // returns a list of valid adjacent tiles (up/down/left/right) that are walkable
    private List<Vector2Int> GetNeighbors(Vector2Int current)
    {
        List<Vector2Int> neighbors = new List<Vector2Int>();
        int x = current.x;
        int y = current.y;

        // Up
        if (y + 1 < height && tiles[x, y + 1].isWalkable)
            neighbors.Add(new Vector2Int(x, y + 1));

        // Down
        if (y - 1 >= 0 && tiles[x, y - 1].isWalkable)
            neighbors.Add(new Vector2Int(x, y - 1));

        // Left
        if (x - 1 >= 0 && tiles[x - 1, y].isWalkable)
            neighbors.Add(new Vector2Int(x - 1, y));

        // Right
        if (x + 1 < width && tiles[x + 1, y].isWalkable)
            neighbors.Add(new Vector2Int(x + 1, y));

        return neighbors;
    }

    // Reconstructs the path from end -> start using the parents array, then highlights it
    private void ReconstructPath(Vector2Int[,] parents, Vector2Int start, Vector2Int end)
    {
        List<Vector2Int> path = new List<Vector2Int>();
        Vector2Int current = end;

        while (current.x != -1 && current.y != -1)
        {
            path.Add(current);
            current = parents[current.x, current.y];
        }

        path.Reverse(); // now path is start -> end

        // highlight path in blue
        foreach (Vector2Int coords in path)
        {
            Tile tile = tiles[coords.x, coords.y];
            tile.GetComponent<SpriteRenderer>().color = Color.blue;
        }
    }

    // Single Update method inside the class
    void Update()
    {
        // ...
    }
}

