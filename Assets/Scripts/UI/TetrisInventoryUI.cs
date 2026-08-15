using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TetrisInventoryUI : MonoBehaviour
{
    [Header("References")]
    public GridBuildController builder;
    public Transform hotbarContainer;
    public GameObject slotPrefab;

    [Header("UI Block Settings")]
    public float uiBlockSize = 15f;
    public Color activeColor = Color.white;
    public Color inactiveColor = new Color(0.5f, 0.5f, 0.5f, 1f);

    private GameObject[] slotObjects = new GameObject[7];
    private TMP_Text[] countTexts = new TMP_Text[7];
    private Image[] buttonBackgrounds = new Image[7];

    void Start()
    {
        GenerateHotbar();

        // Subscribe to the builder's events
        builder.OnInventoryChanged += UpdateInventoryUI;
        builder.OnPieceSelected += HighlightSelectedSlot;
        builder.OnPieceDeselected += ClearHighlights; // When Esc is pressed

        // Force initial visual update
        UpdateInventoryUI();
        ClearHighlights();
    }

    private void GenerateHotbar()
    {
        int totalPieces = Enum.GetValues(typeof(TetrominoType)).Length;

        for (int i = 0; i < totalPieces; i++)
        {
            TetrominoType type = (TetrominoType)i;

            GameObject slotObj = Instantiate(slotPrefab, hotbarContainer);
            slotObjects[i] = slotObj;

            buttonBackgrounds[i] = slotObj.GetComponent<Image>();
            countTexts[i] = slotObj.GetComponentInChildren<TMP_Text>();

            Button btn = slotObj.GetComponent<Button>();
            btn.onClick.AddListener(() => builder.SelectPiece(type));

            Transform shapeAnchor = slotObj.transform.Find("ShapeAnchor");
            DrawProceduralShape(shapeAnchor, type);
        }
    }

    private void DrawProceduralShape(Transform anchor, TetrominoType type)
    {
        Vector2Int[] offsets = TetrisData.Shapes[type][0];

        float sumX = 0, sumY = 0;
        foreach (var off in offsets)
        {
            sumX += off.x;
            sumY += off.y;
        }
        float centerX = sumX / 4f;
        float centerY = sumY / 4f;

        foreach (var offset in offsets)
        {
            GameObject blockObj = new GameObject("UIBlock");
            blockObj.transform.SetParent(anchor, false);

            Image img = blockObj.AddComponent<Image>();
            img.color = GetColorForPiece(type);

            RectTransform rect = img.rectTransform;
            rect.sizeDelta = new Vector2(uiBlockSize, uiBlockSize);

            float posX = (offset.x - centerX) * uiBlockSize;
            float posY = (offset.y - centerY) * uiBlockSize;

            rect.anchoredPosition = new Vector2(posX, posY);
        }
    }

    private void UpdateInventoryUI()
    {
        for (int i = 0; i < 7; i++)
        {
            int count = builder.currentInventory[i];

            // HIDE the card completely if we have 0 of this block!
            slotObjects[i].SetActive(count > 0);

            // Update the text in case we get it back (e.g., player deleted a piece)
            countTexts[i].text = $"x{count}";
        }
    }

    private void HighlightSelectedSlot(TetrominoType selectedType)
    {
        for (int i = 0; i < 7; i++)
        {
            if (i == (int)selectedType)
            {
                buttonBackgrounds[i].color = activeColor;
            }
            else
            {
                buttonBackgrounds[i].color = inactiveColor;
            }
        }
    }

    private void ClearHighlights()
    {
        // When ESC is pressed, return all buttons to their dark/inactive state
        for (int i = 0; i < 7; i++)
        {
            if (buttonBackgrounds[i] != null)
                buttonBackgrounds[i].color = inactiveColor;
        }
    }

    private Color GetColorForPiece(TetrominoType type)
    {
        switch (type)
        {
            case TetrominoType.I: return Color.cyan;
            case TetrominoType.J: return Color.blue;
            case TetrominoType.L: return new Color(1f, 0.5f, 0f);
            case TetrominoType.O: return Color.yellow;
            case TetrominoType.S: return Color.green;
            case TetrominoType.T: return new Color(0.5f, 0f, 0.5f);
            case TetrominoType.Z: return Color.red;
            default: return Color.white;
        }
    }

    void OnDestroy()
    {
        if (builder != null)
        {
            builder.OnInventoryChanged -= UpdateInventoryUI;
            builder.OnPieceSelected -= HighlightSelectedSlot;
            builder.OnPieceDeselected -= ClearHighlights;
        }
    }
}