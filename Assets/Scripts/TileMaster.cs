using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.EventSystems; // Required for UI checking

public class TilemapClicker : MonoBehaviour
{
    [Header("Assign in Inspector")]
    public Tilemap myTilemap;
    public TileBase tileToPlace; // Optional: if you want to paint tiles

    private Camera mainCam;

    void Start()
    {
        // Cache the main camera for performance
        mainCam = Camera.main;
    }

    void Update()
    {
        // 1. Detect Left Mouse Click
        if (Input.GetMouseButtonDown(0))
        {
            // Safeguard: Check if the mouse is currently over a UI element (like a button)
            // If it is, we ignore the click so we don't accidentally dig dirt behind a UI panel.
            if (EventSystem.current.IsPointerOverGameObject())
                return;

            // 2. Get the mouse screen position
            Vector3 mouseScreenPos = Input.mousePosition;

            // 3. Convert Screen Space to World Space
            Vector3 mouseWorldPos = mainCam.ScreenToWorldPoint(mouseScreenPos);
            mouseWorldPos.z = 0; // Force Z to 0 so it aligns perfectly with 2D tilemaps

            // 4. Convert World Space to Tilemap Cell Space (Vector3Int)
            Vector3Int cellPosition = myTilemap.WorldToCell(mouseWorldPos);

            // 5. Do something with that coordinate!
            DoSomethingWithTile(cellPosition);
        }
    }

    private void DoSomethingWithTile(Vector3Int gridCoords)
    {
        // --- EXAMPLE A: Read the tile ---
        TileBase clickedTile = myTilemap.GetTile(gridCoords);
        if (clickedTile != null)
        {
            Debug.Log($"You clicked on: {clickedTile.name} at coordinates {gridCoords}");
        }
        else
        {
            Debug.Log($"You clicked on an empty space at {gridCoords}");
        }

        // --- EXAMPLE B: Paint a new tile ---
        myTilemap.SetTile(gridCoords, tileToPlace);

        // --- EXAMPLE C: Delete a tile ---
        // myTilemap.SetTile(gridCoords, null);

        // --- EXAMPLE D: Change the color of the clicked tile ---
        // myTilemap.SetTileFlags(gridCoords, TileFlags.None); // Must unlock flags to change color
        // myTilemap.SetColor(gridCoords, Color.red);
    }
}
