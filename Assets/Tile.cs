using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tile : MonoBehaviour
{
    // Determines if the tile can be walked on or not
    public bool isWalkable = true;

    [SerializeField] private Color walkableColor = Color.white;
    [SerializeField] private Color blockedColor = Color.red;

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
        // Toggle the boolean
        isWalkable = !isWalkable;
        UpdateColor();
    }

    // Helper method to change color based on isWalkable
    public void UpdateColor()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = isWalkable ? walkableColor : blockedColor;
        }
    }

    // Update is called once per frame
    void Update()
    {

    }
  
        
  
}
