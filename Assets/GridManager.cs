using System.Collections;
using System.Collections.Generic;
//using System.Diagnostics;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    [SerializeField] private int width = 10;   // # of tiles horizontally
    [SerializeField] private int height = 10;  // # of tiles vertically
    [SerializeField] private Tile tilePrefab;  // Reference to Tile prefab

    // Store references to all spawned tiles here
    private List<Tile> allTiles = new List<Tile>();

    void Start()
    {
        GenerateGrid();
    }

    private void GenerateGrid()
    {
        Vector2 offset = new Vector2(-width / 2f + 0.5f, -height / 2f + 0.5f);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                // Each tile is at an integer coordinate plus the offset
                Vector2 spawnPos = new Vector2(x, y) + offset;
                Tile spawnedTile = Instantiate(tilePrefab, spawnPos, Quaternion.identity);
                spawnedTile.name = $"Tile_{x}_{y}";

                // Add each newly spawned tile to allTiles 
                allTiles.Add(spawnedTile);
            }
        }
    }

    // Method that resets all tiles to walkable 
    public void ResetGrid()
    {
        foreach (Tile tile in allTiles)
        {
            tile.isWalkable = true;
            tile.UpdateColor();
        }
    }

    // Placeholder for BFS or A*
    public void RunPathfinding()
    {
        Debug.Log("Run BFS or A* Here!");
    }



    // Update is called once per frame
    void Update()
    {

    }
}     

