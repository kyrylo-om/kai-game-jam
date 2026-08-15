using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.EventSystems;

public class GridBuildController : MonoBehaviour
{
    [Header("Tilemaps")]
    public Tilemap mainTilemap;
    public Tilemap hoverTilemap;
    public Tilemap levelTilemap;

    [Header("Tiles & VFX")]
    public TileBase blockTile;
    public TileBase previewTile;
    public ParticleSystem deleteParticlePrefab;

    [Header("Settings")]
    public float deleteHoldTime = 2f; // Shortened for better feel
    public float placementAnimDuration = 0.15f;
    public AnimationCurve deleteShakeCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

    [Header("Border Maker Pro Max")]
    public bool enforceAreaLimits = true;

    [Tooltip("Assign a PolygonCollider2D here and use 'Edit Collider' to draw a custom shape.")]
    public Collider2D playableAreaCollider;
    public Tilemap allowedZoneTilemap;

    [Header("Inventory Settings")]
    // Starting amounts for I, J, L, O, S, T, Z
    public int[] startingInventory = new int[7] { 5, 5, 5, 5, 5, 5, 5 };

    // The current live inventory
    public int[] currentInventory = new int[7];

    // Events for the UI to listen to
    public Action OnInventoryChanged;
    public Action<TetrominoType> OnPieceSelected;
    public Action OnPieceDeselected; // Fired when Esc is pressed or we run out

    [Header("State (Read-Only)")]
    public bool isEditMode = false;
    public TetrominoType currentPieceType = TetrominoType.I;
    public int currentRotation = 0;

    private Camera mainCam;
    private Dictionary<Vector3Int, PlacedPiece> gridMap = new Dictionary<Vector3Int, PlacedPiece>();

    private float previewHideTimer = 0f;
    private PlacedPiece pieceBeingDeleted = null;
    private float currentDeleteTimer = 0f;

    void Awake()
    {
        // AWAKE runs before START. This fixes the UI reading zeros!
        Array.Copy(startingInventory, currentInventory, 7);
    }

    void Start()
    {
        mainCam = Camera.main;
    }

    void Update()
    {
        // ESCAPE to cancel placement mode
        if (Input.GetKeyDown(KeyCode.Escape) && isEditMode)
        {
            CancelPlacement();
        }

        Vector3 mouseWorldPos = mainCam.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0;
        Vector3Int mouseCell = mainTilemap.WorldToCell(mouseWorldPos);

        bool isHoveringUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

        if (previewHideTimer > 0)
            previewHideTimer -= Time.deltaTime;

        if (isEditMode)
        {
            HandleEditMode(mouseCell, isHoveringUI);
        }
        else
        {
            HandlePlayMode(mouseCell, isHoveringUI); // Our "Hold to Delete" mode
        }
    }

    // Called by the UI Buttons
    public void SelectPiece(TetrominoType newType)
    {
        if (currentInventory[(int)newType] <= 0) return; // Can't select empty

        isEditMode = true;
        currentPieceType = newType;
        currentRotation = 0;

        ResetDeleteState(); // Cancel any accidental delete holds
        OnPieceSelected?.Invoke(newType);
    }

    public void CancelPlacement()
    {
        isEditMode = false;
        hoverTilemap.ClearAllTiles();
        OnPieceDeselected?.Invoke();
    }

    // ==========================================
    //               EDIT MODE
    // ==========================================
    private void HandleEditMode(Vector3Int mouseCell, bool isHoveringUI)
    {
        // 1. Rotation Logic
        // Rotate forward with 'R' or Right-Click
        if (Input.GetKeyDown(KeyCode.R) || Input.GetMouseButtonDown(1))
        {
            currentRotation = (currentRotation + 1) % 4;
        }

        // Rotate with Mouse Wheel
        float scroll = Input.mouseScrollDelta.y;
        if (scroll > 0.01f)
        {
            currentRotation = (currentRotation + 1) % 4; // Scroll Up: Forward
        }
        else if (scroll < -0.01f)
        {
            currentRotation = (currentRotation - 1 + 4) % 4; // Scroll Down: Backward (+4 prevents negative modulo bugs)
        }

        // 2. Preview & Placement
        if (!isHoveringUI && previewHideTimer <= 0)
        {
            DrawHoverPreview(mouseCell);

            if (Input.GetMouseButtonDown(0))
            {
                TryPlacePiece(mouseCell);
            }
        }
        else
        {
            hoverTilemap.ClearAllTiles();
        }
    }

    // ==========================================
    //               PLAY/DELETE MODE
    // ==========================================
    private void HandlePlayMode(Vector3Int mouseCell, bool isHoveringUI)
    {
        if (isHoveringUI)
        {
            ResetDeleteState();
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            if (gridMap.TryGetValue(mouseCell, out PlacedPiece clickedPiece))
            {
                pieceBeingDeleted = clickedPiece;
                currentDeleteTimer = 0f;
            }
        }

        if (Input.GetMouseButton(0) && pieceBeingDeleted != null)
        {
            if (gridMap.TryGetValue(mouseCell, out PlacedPiece hoverPiece) && hoverPiece == pieceBeingDeleted)
            {
                currentDeleteTimer += Time.deltaTime;
                float progress = currentDeleteTimer / deleteHoldTime;

                ApplyIntensifyingShake(pieceBeingDeleted, progress);

                if (currentDeleteTimer >= deleteHoldTime)
                {
                    SpawnDeleteParticles(pieceBeingDeleted);
                    DeletePieceFully(pieceBeingDeleted);
                    ResetDeleteState();
                }
            }
            else
            {
                ResetDeleteState();
            }
        }

        if (Input.GetMouseButtonUp(0) && pieceBeingDeleted != null)
        {
            ResetDeleteState();
        }
    }

    // ... (Shake, Particle, and Reset logic remains the same) ...

    private void ApplyIntensifyingShake(PlacedPiece piece, float progress)
    {
        float clampedProgress = Mathf.Clamp01(progress);
        float curve = deleteShakeCurve != null ? deleteShakeCurve.Evaluate(clampedProgress) : clampedProgress;
        float shakeAmount = Mathf.Lerp(0f, 0.15f, curve);

        Color damageColor = Color.Lerp(Color.white, Color.red, clampedProgress);

        foreach (Vector3Int cell in piece.OccupiedCells)
        {
            Vector3 randomOffset = new Vector3(
                UnityEngine.Random.Range(-shakeAmount, shakeAmount),
                UnityEngine.Random.Range(-shakeAmount, shakeAmount),
                0f
            );

            Matrix4x4 matrix = Matrix4x4.TRS(randomOffset, Quaternion.identity, Vector3.one);
            mainTilemap.SetTransformMatrix(cell, matrix);
            mainTilemap.SetColor(cell, damageColor);
        }
    }
    // Call this from pickups or level rewards!
    public void AddToInventory(TetrominoType type, int amount = 1)
    {
        currentInventory[(int)type] += amount;

        // This instantly tells our Flexbox UI to redraw the cards and un-hide them!
        OnInventoryChanged?.Invoke();
    }
    private void ResetDeleteState()
    {
        if (pieceBeingDeleted != null)
        {
            foreach (Vector3Int cell in pieceBeingDeleted.OccupiedCells)
            {
                mainTilemap.SetTransformMatrix(cell, Matrix4x4.identity);
                mainTilemap.SetColor(cell, Color.white);
            }
            pieceBeingDeleted = null;
        }
        currentDeleteTimer = 0f;
    }

    private void SpawnDeleteParticles(PlacedPiece piece)
    {
        if (deleteParticlePrefab == null) return;
        Vector3 worldCenter = mainTilemap.CellToWorld(piece.PivotPosition) + new Vector3(0.5f, 0.5f, 0f);
        Instantiate(deleteParticlePrefab, worldCenter, Quaternion.identity);
    }

    private void DeletePieceFully(PlacedPiece piece)
    {
        foreach (Vector3Int occupiedCell in piece.OccupiedCells)
        {
            mainTilemap.SetTile(occupiedCell, null);
            gridMap.Remove(occupiedCell);
        }

        // Refund the inventory!
        currentInventory[(int)piece.Type]++;
        OnInventoryChanged?.Invoke();
    }

    // ==========================================
    //          PLACEMENT LOGIC
    // ==========================================

    private void TryPlacePiece(Vector3Int mouseCell)
    {
        if (currentInventory[(int)currentPieceType] <= 0) return;

        Vector3Int centeredPivot = GetCenteredPivot(currentPieceType, currentRotation, mouseCell);
        Vector2Int[] offsets = TetrisData.Shapes[currentPieceType][currentRotation];

        foreach (Vector2Int offset in offsets)
        {
            Vector3Int checkPos = centeredPivot + new Vector3Int(offset.x, offset.y, 0);
            if (IsCellBlocked(checkPos)) return;
        }

        PlacedPiece newPiece = new PlacedPiece
        {
            Type = currentPieceType,
            RotationIndex = currentRotation,
            PivotPosition = centeredPivot
        };

        foreach (Vector2Int offset in offsets)
        {
            Vector3Int tilePos = centeredPivot + new Vector3Int(offset.x, offset.y, 0);
            newPiece.OccupiedCells.Add(tilePos);
            gridMap.Add(tilePos, newPiece);

            mainTilemap.SetTile(tilePos, blockTile);
            mainTilemap.SetTileFlags(tilePos, TileFlags.None);
        }

        // Consume inventory
        currentInventory[(int)currentPieceType]--;
        OnInventoryChanged?.Invoke();

        previewHideTimer = placementAnimDuration;
        hoverTilemap.ClearAllTiles();
        StartCoroutine(AnimatePiecePlacement(newPiece));

        // If we just placed the last block of this type, auto-cancel placement mode!
        if (currentInventory[(int)currentPieceType] <= 0)
        {
            CancelPlacement();
        }
    }

    private IEnumerator AnimatePiecePlacement(PlacedPiece piece)
    {
        float elapsed = 0f;
        while (elapsed < placementAnimDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / placementAnimDuration;
            float curve = 1f - Mathf.Pow(1f - t, 3f);
            float currentScale = Mathf.Lerp(1.4f, 1f, curve);

            Matrix4x4 matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(currentScale, currentScale, 1f));

            foreach (Vector3Int cell in piece.OccupiedCells)
            {
                mainTilemap.SetTransformMatrix(cell, matrix);
            }
            yield return null;
        }

        foreach (Vector3Int cell in piece.OccupiedCells)
        {
            mainTilemap.SetTransformMatrix(cell, Matrix4x4.identity);
        }
    }

    // ==========================================
    //          HELPER MATH
    // ==========================================

    private Vector3Int GetCenteredPivot(TetrominoType type, int rotation, Vector3Int mouseCell)
    {
        Vector2Int[] offsets = TetrisData.Shapes[type][rotation];
        float sumX = 0, sumY = 0;

        foreach (Vector2Int off in offsets)
        {
            sumX += off.x;
            sumY += off.y;
        }

        int avgX = Mathf.FloorToInt((sumX / 4f) + 0.5f);
        int avgY = Mathf.FloorToInt((sumY / 4f) + 0.5f);

        return mouseCell - new Vector3Int(avgX, avgY, 0);
    }

    private bool IsCellBlocked(Vector3Int cellPos)
    {
        if (enforceAreaLimits)
        {
            if (playableAreaCollider != null)
            {
                Vector3 cellWorldCenter = mainTilemap.CellToWorld(cellPos) + new Vector3(mainTilemap.cellSize.x / 2f, mainTilemap.cellSize.y / 2f, 0);
                if (!playableAreaCollider.OverlapPoint(cellWorldCenter)) return true;
            }
            if (allowedZoneTilemap != null)
            {
                if (!allowedZoneTilemap.HasTile(cellPos)) return true;
            }
        }
        if (gridMap.ContainsKey(cellPos)) return true;
        if (levelTilemap != null && levelTilemap.HasTile(cellPos)) return true;
        return false;
    }

    private void DrawHoverPreview(Vector3Int mouseCell)
    {
        hoverTilemap.ClearAllTiles();

        Vector3Int centeredPivot = GetCenteredPivot(currentPieceType, currentRotation, mouseCell);
        Vector2Int[] offsets = TetrisData.Shapes[currentPieceType][currentRotation];
        bool canPlace = true;

        foreach (Vector2Int offset in offsets)
        {
            Vector3Int tilePos = centeredPivot + new Vector3Int(offset.x, offset.y, 0);

            if (IsCellBlocked(tilePos)) canPlace = false;

            hoverTilemap.SetTile(tilePos, previewTile);

            if (!canPlace) hoverTilemap.SetColor(tilePos, new Color(1, 0, 0, 0.5f));
            else hoverTilemap.SetColor(tilePos, new Color(1, 1, 1, 0.7f));
        }
    }
}
