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

    private SpriteRenderer _sr;

    void Awake()
    {
        // Grab the SpriteRenderer so we can change the tile's color
        _sr = GetComponent<SpriteRenderer>();
        UpdateColor();
    }

    void OnMouseDown()
    {
        // Delegate all mode logic to the GridManager
        GridManager.Instance.HandleTileClick(this); 
    }



    // Repaint based on state flags
    public void UpdateColor()
    {
        if (isStart) _sr.color = startColor;
        else if (isEnd) _sr.color = endColor;
        else _sr.color = isWalkable ? walkableColor : blockedColor;
    }


    // Temporarily paint as BFS wave
    public void PaintWave(Color waveColor)
    {
        _sr.color = waveColor;
    }

    // Helper to toggle walkable/block state
    public void ToggleBlocked()
    {
        isWalkable = !isWalkable;
        isStart = isEnd = false;
        UpdateColor();
    }

    // Mark this tile as the BFS start node.
    public void SetAsStart()
    {
        isStart = true;
        isEnd = false;
        isWalkable = true;
        UpdateColor();
    }

   
    // Mark this tile as the BFS end node.
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
