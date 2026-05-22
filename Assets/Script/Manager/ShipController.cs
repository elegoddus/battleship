using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class ShipController : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public int shipLength;
    public bool isVertical = false;
    public bool isSelected = false; 
    
    public int currentGridX;
    public int currentGridY;

    public RectTransform rectTransform { get; private set; }
    private Image shipImage;

    private float cellSize;
    private float cellSpacing;
    private Vector2 startOffset;
    private Canvas mainCanvas;

    // --- HỆ THỐNG MÀU SẮC MỚI ---
    private Color normalColor = Color.white; // Màu gốc của ảnh
    private Color selectedValidColor = new Color(0.6f, 1f, 0.6f, 1f); // Xanh lá sáng khi được chọn
    private Color invalidColor = new Color(1f, 0.2f, 0.2f, 0.8f); // Đỏ khi lỗi

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        shipImage = GetComponent<Image>();
    }

    public void Initialize(int length, Sprite factionSprite, float cellSize, float cellSpacing, Vector2 startOffset, Canvas canvas)
    {
        this.shipLength = length;
        this.cellSize = cellSize;
        this.cellSpacing = cellSpacing;
        this.startOffset = startOffset;
        this.mainCanvas = canvas;

        if (factionSprite != null) shipImage.sprite = factionSprite;

        float totalLength = (length * cellSize) + ((length - 1) * cellSpacing);
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.sizeDelta = new Vector2(totalLength, cellSize);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        
        UpdateVisualRotation();
        SnapToGrid();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        rectTransform.SetAsLastSibling();

        if (GameplayGridManager.Instance != null)
        {
            GameplayGridManager.Instance.SelectShip(this);
        }

        // --- CÁCH XỬ LÝ CỦA HASBRO / GAME CHUẨN ---
        // 1. Nếu là PC: Người chơi có thể click CHUỘT PHẢI để xoay ngay lập tức
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            RotateShip();
            return;
        }

        // 2. Nếu là Mobile hoặc PC (Chuột trái): Chỉ xoay khi Double Tap / Double Click
        if (eventData.clickCount == 2)
        {
            RotateShip();
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (GameplayGridManager.Instance != null)
        {
            GameplayGridManager.Instance.SelectShip(this);
        }
        rectTransform.SetAsLastSibling();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (mainCanvas != null)
        {
            float currentZoom = rectTransform.parent.localScale.x;
            rectTransform.anchoredPosition += eventData.delta / (mainCanvas.scaleFactor * currentZoom);
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        SnapToGrid();
    }

    public void RotateShip()
    {
        isVertical = !isVertical;
        UpdateVisualRotation();
        SnapToGrid();
    }

    private void UpdateVisualRotation()
    {
        rectTransform.localEulerAngles = new Vector3(0, 0, isVertical ? -90 : 0);
    }

    public void MoveStep(int xStep, int yStep)
    {
        currentGridX += xStep;
        currentGridY += yStep;
        ApplyGridPosition();
    }

    public void SnapToGrid()
    {
        Vector2 currentPos = rectTransform.anchoredPosition;
        float gridStep = cellSize + cellSpacing;

        float offsetCorrectionX = (shipLength % 2 == 0 && !isVertical) ? gridStep / 2f : 0f;
        float offsetCorrectionY = (shipLength % 2 == 0 && isVertical) ? gridStep / 2f : 0f;

        currentGridX = Mathf.RoundToInt((currentPos.x - offsetCorrectionX - startOffset.x) / gridStep);
        currentGridY = Mathf.RoundToInt(-(currentPos.y + offsetCorrectionY - startOffset.y) / gridStep);

        ApplyGridPosition();
    }

    private void ApplyGridPosition()
    {
        float gridStep = cellSize + cellSpacing;
        float offsetCorrectionX = (shipLength % 2 == 0 && !isVertical) ? gridStep / 2f : 0f;
        float offsetCorrectionY = (shipLength % 2 == 0 && isVertical) ? gridStep / 2f : 0f;

        float snapX = startOffset.x + currentGridX * gridStep + offsetCorrectionX;
        float snapY = startOffset.y - currentGridY * gridStep - offsetCorrectionY;

        rectTransform.anchoredPosition = new Vector2(snapX, snapY);
        ValidatePlacement();
    }

    public List<Vector2Int> GetOccupiedCells()
    {
        List<Vector2Int> cells = new List<Vector2Int>();
        int startX = isVertical ? currentGridX : currentGridX - (shipLength - 1) / 2;
        int startY = isVertical ? currentGridY - (shipLength - 1) / 2 : currentGridY;

        for (int i = 0; i < shipLength; i++)
        {
            if (isVertical) cells.Add(new Vector2Int(startX, startY + i));
            else cells.Add(new Vector2Int(startX + i, startY));
        }
        return cells;
    }

    public void ValidatePlacement()
    {
        if (GameplayGridManager.Instance == null) return;

        bool isValid = GameplayGridManager.Instance.IsPlacementValid(this);
        
        // Logic ưu tiên hiển thị:
        // 1. Lỗi -> Đỏ
        // 2. Không lỗi + Đang chọn -> Xanh sáng
        // 3. Không lỗi + Không chọn -> Trở về màu gốc của Sprite
        if (!isValid)
        {
            shipImage.color = invalidColor;
        }
        else
        {
            shipImage.color = isSelected ? selectedValidColor : normalColor;
        }
    }

    public void SetSelectedVisual(bool show)
    {
        this.isSelected = show;
        
        // Phóng to lên 15% (1.15) để phân biệt rõ ràng hơn so với lúc trước
        rectTransform.localScale = isSelected ? new Vector3(1.15f, 1.15f, 1f) : Vector3.one;
        ValidatePlacement(); 
    }
}