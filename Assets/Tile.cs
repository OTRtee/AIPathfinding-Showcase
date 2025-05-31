using UnityEngine;
using UnityEngine.UI;
using TMPro;


public class Tile : MonoBehaviour
{

    // Configurable colors for each state
    [SerializeField] private Color walkableColor = Color.white;
    [SerializeField] private Color blockedColor = Color.red;
    [SerializeField] private Color startColor = Color.green;
    [SerializeField] private Color endColor = Color.magenta;
    [SerializeField] private TMP_Text _label; 

    private SpriteRenderer _sr;

    // State flags for each tile
    public bool isWalkable = true;
    public bool isStart = false; 
    public bool isEnd = false;

    void Awake()
    {
        // Grab the SpriteRenderer so we can change the tile's color
        _sr = GetComponent<SpriteRenderer>();
        UpdateColor();

        if (_label == null)
            _label = GetComponentInChildren<TMP_Text>();


        // 5% inward padding so grid lines show through
        transform.localScale = Vector3.one * 0.95f;

        // Only clear label if actually assigned 
        if (_label != null)
            ClearLabel();
    }

    // Show a number on top of the tile
    public void SetLabel(string text)
    {
        if (_label != null)
            _label.text = text;
    }

    // Clear any text lavel
    public void ClearLabel()
    {
        _label.text = "";
    }

    void OnMouseDown()
    {
        // Delegate all mode logic to the GridManager
        GridManager.Instance.HandleTileClick(this); 
    }


   // Completely resets this tile back to default (white, walkable).
    public void ResetState()
    {
        isWalkable = true;
        isStart = false;
        isEnd = false;
        UpdateColor();
    }


    // Clears any BFS‐wave colour but preserves start/end/block state
    public void ClearWave()
    {
        UpdateColor();
        if (_label != null) ClearLabel();
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
