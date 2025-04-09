using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoBehaviour
{

    [SerializeField] private int width = 10;       // # of tiles horizontally
    [SerializeField] private int height = 10;      // # of tiles vertically
    [SerializeField] private Tile tilePrefab;      // Reference to Tile prefab


    // Start is called before the first frame update
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
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
