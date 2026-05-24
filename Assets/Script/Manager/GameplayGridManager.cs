using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using TMPro;

public class GameplayGridManager : MonoBehaviour
{
    public static GameplayGridManager Instance;

    [Header("Tham chiếu")]
    public GameObject cellPrefab;
    public RectTransform gridContainer;
    public Canvas mainCanvas;
    
    [Header("Tham chiếu Thuyền & Phe phái")]
    public GameObject shipPrefab;
    public FactionData[] availableFactions;

    [Header("Cài đặt Tọa độ")]
    public GameObject coordinatePrefab; 
    public float hideThreshold1 = 25f;  
    public float hideThreshold2 = 15f;  

    [Header("Cài đặt hiển thị")]
    public float cellSize = 50f; 
    public float cellSpacing = 2f; 
    
    private float minZoom = 1f;
    private float maxZoom = 1f;
    private float currentZoom = 1f;

    private int mapWidth;
    private int mapHeight;
    private bool isCustomMap;
    private bool[] customMapData;

    private List<ShipController> activeShips = new List<ShipController>();
    public ShipController selectedShip;

    private List<GameObject> topLabels = new List<GameObject>();
    private List<GameObject> leftLabels = new List<GameObject>();
    
    private float defaultTopY;
    private float defaultLeftX;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        Invoke(nameof(GenerateGridFromNetwork), 0.5f);
    }

    private void GenerateGridFromNetwork()
    {
        if (LobbyNetworkManager.Instance == null) return;

        LobbySettings settings = LobbyNetworkManager.Instance.RoomSettings.Value;
        mapWidth = settings.mapSettings.mapWidth;
        mapHeight = settings.mapSettings.mapHeight;
        isCustomMap = settings.mapSettings.isCustomMap;
        customMapData = settings.mapSettings.customMapData;

        float totalWidth = mapWidth * (cellSize + cellSpacing);
        float totalHeight = mapHeight * (cellSize + cellSpacing);
        gridContainer.sizeDelta = new Vector2(totalWidth, totalHeight);
        Vector2 startOffset = new Vector2(-totalWidth / 2f + (cellSize / 2f), totalHeight / 2f - (cellSize / 2f));

        for (int y = 0; y < mapHeight; y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                bool isPlayable = true; 
                if (isCustomMap && customMapData != null)
                {
                    if (y * mapWidth + x < customMapData.Length)
                        isPlayable = customMapData[y * mapWidth + x];
                }

                if (!isPlayable) continue;

                GameObject obj = Instantiate(cellPrefab, gridContainer);
                RectTransform rect = obj.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(cellSize, cellSize);
                float posX = startOffset.x + x * (cellSize + cellSpacing);
                float posY = startOffset.y - y * (cellSize + cellSpacing); 
                rect.anchoredPosition = new Vector2(posX, posY);
                
                GameplayCell cellUI = obj.GetComponent<GameplayCell>();
                cellUI.Init(x, y, true);
            }
        }

        GenerateCoordinates(mapWidth, mapHeight, startOffset);
        CalculateZoomLimits(mapWidth, mapHeight, totalWidth, totalHeight);
        SpawnShipsInCenter(startOffset); 
        
        BringCoordinatesToFront();
    }

    private void GenerateCoordinates(int width, int height, Vector2 startOffset)
    {
        topLabels.Clear();
        leftLabels.Clear();

        defaultTopY = startOffset.y + (cellSize + cellSpacing);
        defaultLeftX = startOffset.x - (cellSize + cellSpacing);

        for (int x = 0; x < width; x++)
        {
            GameObject lbl = Instantiate(coordinatePrefab, gridContainer);
            RectTransform rect = lbl.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(cellSize, cellSize);
            
            float posX = startOffset.x + x * (cellSize + cellSpacing);
            rect.anchoredPosition = new Vector2(posX, defaultTopY);
            
            lbl.GetComponent<TextMeshProUGUI>().text = GetColumnName(x);
            topLabels.Add(lbl);
        }

        for (int y = 0; y < height; y++)
        {
            GameObject lbl = Instantiate(coordinatePrefab, gridContainer);
            RectTransform rect = lbl.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(cellSize, cellSize);
            
            float posY = startOffset.y - y * (cellSize + cellSpacing);
            rect.anchoredPosition = new Vector2(defaultLeftX, posY);
            
            lbl.GetComponent<TextMeshProUGUI>().text = (height - 1 - y).ToString();
            leftLabels.Add(lbl);
        }
    }

    private string GetColumnName(int index)
    {
        string columnName = "";
        int dividend = index + 1;
        while (dividend > 0)
        {
            int modulo = (dividend - 1) % 26;
            columnName = System.Convert.ToChar(65 + modulo).ToString() + columnName;
            dividend = (int)((dividend - modulo) / 26);
        }
        return columnName;
    }

    private void BringCoordinatesToFront()
    {
        foreach (var lbl in topLabels) lbl.transform.SetAsLastSibling();
        foreach (var lbl in leftLabels) lbl.transform.SetAsLastSibling();
    }

    private void UpdateCoordinateVisibility()
    {
        float actualSize = cellSize * currentZoom;
        
        int step = 1;
        if (actualSize < hideThreshold2) step = 4; 
        else if (actualSize < hideThreshold1) step = 2; 

        for (int i = 0; i < topLabels.Count; i++) topLabels[i].SetActive(i % step == 0);
        for (int i = 0; i < leftLabels.Count; i++) leftLabels[i].SetActive(i % step == 0);
    }

    private void CalculateZoomLimits(int width, int height, float totalW, float totalH)
    {
        RectTransform viewport = gridContainer.parent.GetComponent<RectTransform>();
        float viewWidth = viewport.rect.width;
        float viewHeight = viewport.rect.height;

        float fitZoom = Mathf.Min(viewWidth / totalW, viewHeight / totalH) * 0.95f; 
        minZoom = fitZoom;

        float tenByTenSizeW = 10f * (cellSize + cellSpacing);
        float tenByTenSizeH = 10f * (cellSize + cellSpacing);
        float zoom10x10 = Mathf.Min(viewWidth / tenByTenSizeW, viewHeight / tenByTenSizeH);
        
        maxZoom = Mathf.Max(fitZoom, zoom10x10 * 1.5f); 

        currentZoom = minZoom;
        
        gridContainer.localScale = new Vector3(currentZoom, currentZoom, 1f);
        gridContainer.anchoredPosition = Vector2.zero;

        UpdateCoordinateVisibility();
    }

    private void SpawnShipsInCenter(Vector2 startOffset)
    {
        activeShips.Clear();
        int myFactionIndex = 0;
        if (LobbyNetworkManager.Instance != null)
        {
            myFactionIndex = NetworkManager.Singleton.IsServer 
                ? LobbyNetworkManager.Instance.hostFactionIndex.Value 
                : LobbyNetworkManager.Instance.clientFactionIndex.Value;
        }

        FactionData myFaction = availableFactions[myFactionIndex];
        int[] shipSizes = { 5, 4, 3, 3, 2 };

        int centerGridX = mapWidth / 2;
        int centerGridY = mapHeight / 2 - 2; 

        for (int i = 0; i < shipSizes.Length; i++)
        {
            GameObject shipObj = Instantiate(shipPrefab, gridContainer);
            ShipController shipUI = shipObj.GetComponent<ShipController>();

            Sprite correctSprite = null;
            switch (shipSizes[i])
            {
                case 5: correctSprite = myFaction.shipSize5; break;
                case 4: correctSprite = myFaction.shipSize4; break;
                case 3: correctSprite = myFaction.shipSize3; break;
                case 2: correctSprite = myFaction.shipSize2; break;
            }

            shipUI.Initialize(shipSizes[i], correctSprite, cellSize, cellSpacing, startOffset, mainCanvas);
            
            shipUI.currentGridX = centerGridX;
            shipUI.currentGridY = centerGridY + i;
            shipUI.SnapToGrid();

            activeShips.Add(shipUI);
        }
    }

    private void Update()
    {
        if (Mouse.current != null)
        {
            float scroll = Mouse.current.scroll.ReadValue().y;
            
            if (Mathf.Abs(scroll) > 0.01f) 
            {
                // Lăn chuột zoom mượt với bước 10%
                ApplyZoom(Mathf.Sign(scroll), 0.1f);
            }
        }

        if (Keyboard.current != null && selectedShip != null)
        {
            if (Keyboard.current.upArrowKey.wasPressedThisFrame || Keyboard.current.wKey.wasPressedThisFrame) 
                OnButtonMoveUp();
            if (Keyboard.current.downArrowKey.wasPressedThisFrame || Keyboard.current.sKey.wasPressedThisFrame) 
                OnButtonMoveDown();
            if (Keyboard.current.leftArrowKey.wasPressedThisFrame || Keyboard.current.aKey.wasPressedThisFrame) 
                OnButtonMoveLeft();
            if (Keyboard.current.rightArrowKey.wasPressedThisFrame || Keyboard.current.dKey.wasPressedThisFrame) 
                OnButtonMoveRight();

            if (Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.rKey.wasPressedThisFrame) 
                OnButtonRotate();
        }
    }

    private void LateUpdate()
    {
        if (topLabels.Count == 0 || leftLabels.Count == 0) return;

        RectTransform viewportRect = gridContainer.parent.GetComponent<RectTransform>();

        Vector3 viewportTopEdge = viewportRect.TransformPoint(new Vector3(0, viewportRect.rect.yMax, 0));
        Vector3 localViewportTop = gridContainer.InverseTransformPoint(viewportTopEdge);

        Vector3 viewportLeftEdge = viewportRect.TransformPoint(new Vector3(viewportRect.rect.xMin, 0, 0));
        Vector3 localViewportLeft = gridContainer.InverseTransformPoint(viewportLeftEdge);

        float clampedY = Mathf.Min(defaultTopY, localViewportTop.y - cellSize / 2f);
        float clampedX = Mathf.Max(defaultLeftX, localViewportLeft.x + cellSize / 2f);

        Vector3 inverseScale = new Vector3(1f / currentZoom, 1f / currentZoom, 1f);

        foreach (var lbl in topLabels)
        {
            if (!lbl.activeSelf) continue;
            RectTransform rt = lbl.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, clampedY);
            rt.localScale = inverseScale;
        }

        foreach (var lbl in leftLabels)
        {
            if (!lbl.activeSelf) continue;
            RectTransform rt = lbl.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(clampedX, rt.anchoredPosition.y);
            rt.localScale = inverseScale;
        }
    }

    public bool IsPlacementValid(ShipController checkingShip)
    {
        List<Vector2Int> cells = checkingShip.GetOccupiedCells();

        foreach (Vector2Int cell in cells)
        {
            if (cell.x < 0 || cell.x >= mapWidth || cell.y < 0 || cell.y >= mapHeight) 
                return false;

            if (isCustomMap && customMapData != null)
            {
                if (!customMapData[cell.y * mapWidth + cell.x]) return false;
            }

            foreach (ShipController otherShip in activeShips)
            {
                if (otherShip == checkingShip) continue; 
                if (otherShip.GetOccupiedCells().Contains(cell)) return false;
            }
        }
        return true;
    }

    public void SelectShip(ShipController ship)
    {
        if (selectedShip != null && selectedShip != ship)
        {
            selectedShip.SetSelectedVisual(false);
        }

        selectedShip = ship;
        
        if (selectedShip != null)
        {
            selectedShip.SetSelectedVisual(true);
        }
    }

    // --- CÁC HÀM XỬ LÝ NÚT BẤM MỚI ---

    private void ApplyZoom(float direction, float speedMultiplier)
    {
        currentZoom += direction * currentZoom * speedMultiplier; 
        currentZoom = Mathf.Clamp(currentZoom, minZoom, maxZoom);
        gridContainer.localScale = new Vector3(currentZoom, currentZoom, 1f);
        UpdateCoordinateVisibility();
    }

    // Bấm nút Zoom In sẽ phóng to nhanh hơn lăn chuột một chút (20%)
    public void OnButtonZoomIn() { ApplyZoom(1f, 0.2f); }
    
    // Bấm nút Zoom Out sẽ thu nhỏ nhanh hơn lăn chuột một chút (20%)
    public void OnButtonZoomOut() { ApplyZoom(-1f, 0.2f); }

    public void OnButtonRandomize()
    {
        SelectShip(null);

        // Bước 1: "Nhấc" toàn bộ tàu ra khỏi bản đồ ảo (Tọa độ -100) 
        // để chúng không tự làm vật cản của nhau khi test vị trí mới
        foreach (ShipController ship in activeShips)
        {
            ship.currentGridX = -100; 
            ship.currentGridY = -100;
        }

        // Bước 2: Bắt đầu ném từng chiếc vào bản đồ
        foreach (ShipController ship in activeShips)
        {
            bool placed = false;
            int attempts = 0;
            int maxAttempts = 500; // Tăng số lần thử lên 500 để dễ tìm đường trong map hẹp

            while (!placed && attempts < maxAttempts)
            {
                attempts++;

                bool randVertical = Random.value > 0.5f;
                int randX = Random.Range(0, mapWidth);
                int randY = Random.Range(0, mapHeight);

                // Gọi hàm mới tạo để ép tọa độ và xoay hình ảnh vật lý
                ship.SetGridPosition(randX, randY, randVertical);

                // Kiểm tra xem vị trí vừa ép có đè lên vùng cấm hay tàu trước đó không
                if (IsPlacementValid(ship))
                {
                    placed = true;
                    ship.ValidatePlacement(); // Đổi sang màu Xanh hợp lệ
                }
            }

            // Nếu xui xẻo (map quá chật) thử 500 lần không được, đành chịu báo Đỏ
            if (!placed)
            {
                Debug.LogWarning($"Bản đồ quá hẹp, không thể tìm chỗ cho tàu {ship.shipLength} ô.");
                ship.ValidatePlacement(); 
            }
        }
        
        Debug.Log("Hoàn tất tự động xếp tàu ngẫu nhiên.");
    }
    public void OnButtonRotate() { if (selectedShip != null) selectedShip.RotateShip(); }
    public void OnButtonMoveUp() { if (selectedShip != null) selectedShip.MoveStep(0, -1); }
    public void OnButtonMoveDown() { if (selectedShip != null) selectedShip.MoveStep(0, 1); }
    public void OnButtonMoveLeft() { if (selectedShip != null) selectedShip.MoveStep(-1, 0); }
    public void OnButtonMoveRight() { if (selectedShip != null) selectedShip.MoveStep(1, 0); }
}