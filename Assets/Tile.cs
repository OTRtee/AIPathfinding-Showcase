using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tile : MonoBehaviour
{
    // State flags for each tile
    public bool isWalkable = true; // can our agent walk here?
    public bool isStart = false; // is this the start tile?
    public bool isEnd = false; // is this the end tile?

    // Configurable colors for each state
    [SerializeField] private Color walkableColor = Color.white;
    [SerializeField] private Color blockedColor = Color.red;
    [SerializeField] private Color startColor = Color.green;
    [SerializeField] private Color endColor = Color.magenta;

    private SpriteRenderer spriteRenderer;

    void Awake()
    {
        // Grab the SpriteRenderer so we can change the tile's color
        spriteRenderer = GetComponent<SpriteRenderer>();
        UpdateColor();
    }

    // Called when the user clicks on this tile
    void OnMouseDown()
    {
        // If GridManager told us to pick start, do this 
        if (GridManager.Instance.selectingStart)
        {
            SetAsStart();
            // Turn off start-selcetion mode
            GridManager.Instance.selectingStart = false;
        }
        // ELSE - if picking END, mark this tile as END 
        else if (GridManager.Instance.selectingEnd)
        {
            SetAsEnd();
            GridManager.Instance.selectingEnd = false;
        }

        // Otherwise this click just toggles blocked/unblocked 
        else
        {
            // Toggle the boolean
            isWalkable = !isWalkable;

            // Clearing start/end flags to avoid conflicts 
            isStart = false;
            isEnd = false;
            UpdateColor();
        }
          
    }


    // Applies the correct color based on current flags.
    public void UpdateColor()
    {
        if (isStart)
            spriteRenderer.color = startColor;
        else if (isEnd)
            spriteRenderer.color = endColor;
        else
            spriteRenderer.color = isWalkable ? walkableColor : blockedColor;
    }

    /// <summary>
    /// Mark this tile as the BFS start node.
    /// Ensures it's walkable and un‑marks any previous end.
    /// </summary>
    public void SetAsStart()
    {
        isStart = true;
        isEnd = false;
        isWalkable = true;
        UpdateColor();
    }

    /// <summary>
    /// Mark this tile as the BFS end node.
    /// Ensures it's walkable and un‑marks any previous start.
    /// </summary>
    public void SetAsEnd()
    {
        isEnd = true;
        isStart = false;
        isWalkable = true;
        UpdateColor();
    }


    // Update is called once per frame
    void Update()
    {

    }
  
        
  
}
