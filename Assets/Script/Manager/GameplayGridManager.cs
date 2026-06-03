using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;

public class GameplayGridManager : MonoBehaviour
{
    public static GameplayGridManager Instance;

    [Header("Luồng Game (UI Flow)")]
    public GameObject panelPrepare;
    public GameObject panelGame;

    [Header("Vị trí Lưới (Containers)")]
    public RectTransform prepareViewport;       // Viewport của Panel_Prepare
    public RectTransform frameMainMapViewport;  // Viewport của Panel_Game
    public RectTransform frameMiniMap;          // Khung chứa Mini Map

    [Header("Hai Lưới Bản Đồ")]
    public RectTransform myGridContainer;       // Lưới nhà mình
    public RectTransform enemyGridContainer;    // Lưới nhà địch

    [Header("Tham chiếu")]
    public GameObject cellPrefab;
    public Canvas mainCanvas;
    public GameObject shipPrefab;
    public FactionData[] availableFactions;
    public GameObject coordinatePrefab; 

    [Header("Cài đặt hiển thị")]
    public float cellSize = 50f; 
    public float cellSpacing = 2f; 
    public float hideThreshold1 = 25f;  
    public float hideThreshold2 = 15f; 

    // Biến trạng thái
    public bool isCombatPhase = false;
    private int mapWidth, mapHeight;
    private bool isCustomMap;
    private bool[] customMapData;

    public bool isViewingEnemy = true;

    // Quản lý Zoom
    private float minZoom = 1f, maxZoom = 1f, currentZoom = 1f;
    private RectTransform activeGrid; 
    private RectTransform activeViewport;

    // Quản lý dữ liệu
    private List<ShipController> activeShips = new List<ShipController>();
    public ShipController selectedShip;
    private List<GameObject> topLabels = new List<GameObject>();
    private List<GameObject> leftLabels = new List<GameObject>();
    private float defaultTopY, defaultLeftX;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        InitPreparePhase();
    }

    // --- GIAI ĐOẠN 1: XẾP QUÂN ---
    private void InitPreparePhase()
    {
        isCombatPhase = false;
        panelPrepare.SetActive(true);
        panelGame.SetActive(false);

        int myFactionIndex = 0;

        // Cơ chế Test Nhanh (Bỏ qua mạng)
        if (LobbyNetworkManager.Instance == null)
        {
            mapWidth = 10;
            mapHeight = 10;
            isCustomMap = false;
            customMapData = new bool[100];
            for (int i = 0; i < 100; i++) customMapData[i] = true;
        }
        else
        {
            LobbySettings settings = LobbyNetworkManager.Instance.RoomSettings.Value;
            mapWidth = settings.mapSettings.mapWidth;
            mapHeight = settings.mapSettings.mapHeight;
            isCustomMap = settings.mapSettings.isCustomMap;
            customMapData = settings.mapSettings.customMapData;
            myFactionIndex = NetworkManager.Singleton.IsServer 
                ? LobbyNetworkManager.Instance.hostFactionIndex.Value 
                : LobbyNetworkManager.Instance.clientFactionIndex.Value;
        }

        // Gắn lưới của mình vào màn hình Prepare
        myGridContainer.SetParent(prepareViewport, false);
        activeGrid = myGridContainer;
        activeViewport = prepareViewport;

        ScrollRect prepareScroll = prepareViewport.parent.GetComponent<ScrollRect>();
        if (prepareScroll != null) prepareScroll.content = myGridContainer;

        Vector2 startOffset = CalculateStartOffset();
        SetGridSize(myGridContainer);
        GenerateCells(myGridContainer, startOffset);
        GenerateCoordinates(startOffset);
        CalculateZoomLimits();
        SpawnShipsInCenter(startOffset, myFactionIndex); 
        BringCoordinatesToFront();
    }

    // Nút SẴN SÀNG gọi hàm này
    public void OnButtonReady()
    {
        foreach(var ship in activeShips)
        {
            if (!IsPlacementValid(ship))
            {
                Debug.LogWarning("Có tàu lỗi vị trí. Không thể sẵn sàng!");
                return; 
            }
        }
        
        TransitionToCombatPhase();
    }

    // --- GIAI ĐOẠN 2: VÀO TRẬN ---
    private void TransitionToCombatPhase()
    {
        isCombatPhase = true;
        isViewingEnemy = true;
        SelectShip(null); 

        panelPrepare.SetActive(false);
        panelGame.SetActive(true);

        // Chuẩn bị kích thước và ô lưới cho bản đồ địch trước khi show
        Vector2 startOffset = CalculateStartOffset();
        SetGridSize(enemyGridContainer);
        GenerateCells(enemyGridContainer, startOffset);

        // Chạy hàm hoán đổi Map lần đầu tiên
        UpdateMapViews();
        Debug.Log("Đã chuyển sang Combat Phase.");
    }

    // Hàm gọi khi bấm nút Đổi Map
    public void OnButtonSwapMap()
    {
        if (!isCombatPhase) return;
        isViewingEnemy = !isViewingEnemy;
        UpdateMapViews();
    }

    private void UpdateMapViews()
    {
        if (isViewingEnemy)
        {
            SetupMiniMap(myGridContainer);
            SetupMainMap(enemyGridContainer);
        }
        else
        {
            SetupMiniMap(enemyGridContainer);
            SetupMainMap(myGridContainer);
        }
    }

    private void SetupMiniMap(RectTransform gridToMini)
    {
        gridToMini.SetParent(frameMiniMap, false);
        
        // Ép Anchor và Pivot về chính giữa để fix lỗi lệch khung
        gridToMini.anchorMin = new Vector2(0.5f, 0.5f);
        gridToMini.anchorMax = new Vector2(0.5f, 0.5f);
        gridToMini.pivot = new Vector2(0.5f, 0.5f);
        gridToMini.anchoredPosition = Vector2.zero;

        // Tính tỷ lệ thu nhỏ để bản đồ nằm trọn vẹn trong Mini Map
        float scaleX = frameMiniMap.rect.width / gridToMini.sizeDelta.x;
        float scaleY = frameMiniMap.rect.height / gridToMini.sizeDelta.y;
        float miniScale = Mathf.Min(scaleX, scaleY) * 0.95f; 
        gridToMini.localScale = new Vector3(miniScale, miniScale, 1f);
    }

    private void SetupMainMap(RectTransform gridToMain)
    {
        gridToMain.SetParent(frameMainMapViewport, false);
        
        // Đặt lại Anchor cho Main Map
        gridToMain.anchorMin = new Vector2(0.5f, 0.5f);
        gridToMain.anchorMax = new Vector2(0.5f, 0.5f);
        gridToMain.pivot = new Vector2(0.5f, 0.5f);

        activeGrid = gridToMain;
        activeViewport = frameMainMapViewport;
        
        // Gắn Content cho ScrollRect để có thể kéo thả bản đồ được
        ScrollRect mainScroll = frameMainMapViewport.parent.GetComponent<ScrollRect>();
        if (mainScroll != null) mainScroll.content = activeGrid;

        // Xóa tọa độ cũ và sinh lại tọa độ mới cho chuẩn với Grid hiện tại
        foreach (var lbl in topLabels) Destroy(lbl);
        foreach (var lbl in leftLabels) Destroy(lbl);
        topLabels.Clear();
        leftLabels.Clear();

        GenerateCoordinates(CalculateStartOffset());
        CalculateZoomLimits();
        BringCoordinatesToFront();
    }

    // --- CÁC HÀM TIỆN ÍCH DÙNG CHUNG ---
    private Vector2 CalculateStartOffset()
    {
        float totalWidth = mapWidth * (cellSize + cellSpacing);
        float totalHeight = mapHeight * (cellSize + cellSpacing);
        return new Vector2(-totalWidth / 2f + (cellSize / 2f), totalHeight / 2f - (cellSize / 2f));
    }

    private void SetGridSize(RectTransform grid)
    {
        float totalWidth = mapWidth * (cellSize + cellSpacing);
        float totalHeight = mapHeight * (cellSize + cellSpacing);
        grid.sizeDelta = new Vector2(totalWidth, totalHeight);
    }

    private void GenerateCells(RectTransform targetGrid, Vector2 startOffset)
    {
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

                GameObject obj = Instantiate(cellPrefab, targetGrid);
                RectTransform rect = obj.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(cellSize, cellSize);
                float posX = startOffset.x + x * (cellSize + cellSpacing);
                float posY = startOffset.y - y * (cellSize + cellSpacing); 
                rect.anchoredPosition = new Vector2(posX, posY);
                
                obj.GetComponent<GameplayCell>().Init(x, y, true);
            }
        }
    }

    private void GenerateCoordinates(Vector2 startOffset)
    {
        topLabels.Clear();
        leftLabels.Clear();

        defaultTopY = startOffset.y + (cellSize + cellSpacing);
        defaultLeftX = startOffset.x - (cellSize + cellSpacing);

        for (int x = 0; x < mapWidth; x++)
        {
            GameObject lbl = Instantiate(coordinatePrefab, activeGrid);
            RectTransform rect = lbl.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(cellSize, cellSize);
            rect.anchoredPosition = new Vector2(startOffset.x + x * (cellSize + cellSpacing), defaultTopY);
            
            lbl.GetComponent<TextMeshProUGUI>().text = GetColumnName(x);
            topLabels.Add(lbl);
        }

        for (int y = 0; y < mapHeight; y++)
        {
            GameObject lbl = Instantiate(coordinatePrefab, activeGrid);
            RectTransform rect = lbl.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(cellSize, cellSize);
            rect.anchoredPosition = new Vector2(defaultLeftX, startOffset.y - y * (cellSize + cellSpacing));
            
            lbl.GetComponent<TextMeshProUGUI>().text = (mapHeight - 1 - y).ToString();
            leftLabels.Add(lbl);
        }
    }

    private void SpawnShipsInCenter(Vector2 startOffset, int factionIndex)
    {
        activeShips.Clear();
        FactionData myFaction = availableFactions[factionIndex];
        int[] shipSizes = { 5, 4, 3, 3, 2 };

        int centerGridX = mapWidth / 2;
        int centerGridY = mapHeight / 2 - 2; 

        for (int i = 0; i < shipSizes.Length; i++)
        {
            GameObject shipObj = Instantiate(shipPrefab, activeGrid);
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

    // --- CÁC HÀM UPDATE & ZOOM DỰA TRÊN ACTIVE GRID ---
    private void CalculateZoomLimits()
    {
        float viewWidth = activeViewport.rect.width;
        float viewHeight = activeViewport.rect.height;

        float fitZoom = Mathf.Min(viewWidth / activeGrid.sizeDelta.x, viewHeight / activeGrid.sizeDelta.y) * 0.95f; 
        minZoom = fitZoom;

        float tenByTenSize = 10f * (cellSize + cellSpacing);
        float zoom10x10 = Mathf.Min(viewWidth / tenByTenSize, viewHeight / tenByTenSize);
        maxZoom = Mathf.Max(fitZoom, zoom10x10 * 1.5f); 

        currentZoom = minZoom;
        activeGrid.localScale = new Vector3(currentZoom, currentZoom, 1f);
        activeGrid.anchoredPosition = Vector2.zero;

        UpdateCoordinateVisibility();
    }

    private void ApplyZoom(float direction, float speedMultiplier)
    {
        if (activeGrid == null) return;
        currentZoom += direction * currentZoom * speedMultiplier; 
        currentZoom = Mathf.Clamp(currentZoom, minZoom, maxZoom);
        activeGrid.localScale = new Vector3(currentZoom, currentZoom, 1f);
        UpdateCoordinateVisibility();
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

    private void Update()
    {
        if (Mouse.current != null)
        {
            float scroll = Mouse.current.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) > 0.01f) ApplyZoom(Mathf.Sign(scroll), 0.1f);
        }

        if (!isCombatPhase && Keyboard.current != null && selectedShip != null)
        {
            if (Keyboard.current.upArrowKey.wasPressedThisFrame || Keyboard.current.wKey.wasPressedThisFrame) OnButtonMoveUp();
            if (Keyboard.current.downArrowKey.wasPressedThisFrame || Keyboard.current.sKey.wasPressedThisFrame) OnButtonMoveDown();
            if (Keyboard.current.leftArrowKey.wasPressedThisFrame || Keyboard.current.aKey.wasPressedThisFrame) OnButtonMoveLeft();
            if (Keyboard.current.rightArrowKey.wasPressedThisFrame || Keyboard.current.dKey.wasPressedThisFrame) OnButtonMoveRight();
            if (Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.rKey.wasPressedThisFrame) OnButtonRotate();
        }
    }

    private void LateUpdate()
    {
        if (topLabels.Count == 0 || leftLabels.Count == 0 || activeGrid == null) return;

        Vector3 viewportTopEdge = activeViewport.TransformPoint(new Vector3(0, activeViewport.rect.yMax, 0));
        Vector3 localViewportTop = activeGrid.InverseTransformPoint(viewportTopEdge);

        Vector3 viewportLeftEdge = activeViewport.TransformPoint(new Vector3(activeViewport.rect.xMin, 0, 0));
        Vector3 localViewportLeft = activeGrid.InverseTransformPoint(viewportLeftEdge);

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

    // --- CÁC HÀM LIÊN QUAN ĐẾN THUYỀN (GIỮ NGUYÊN TỪ TRƯỚC) ---
    public bool IsPlacementValid(ShipController checkingShip)
    {
        List<Vector2Int> cells = checkingShip.GetOccupiedCells();
        foreach (Vector2Int cell in cells)
        {
            if (cell.x < 0 || cell.x >= mapWidth || cell.y < 0 || cell.y >= mapHeight) return false;
            if (isCustomMap && customMapData != null && !customMapData[cell.y * mapWidth + cell.x]) return false;
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
        if (isCombatPhase) return; // Không cho chọn tàu lúc đánh nhau
        if (selectedShip != null && selectedShip != ship) selectedShip.SetSelectedVisual(false);
        selectedShip = ship;
        if (selectedShip != null) selectedShip.SetSelectedVisual(true);
    }

    private string GetColumnName(int index) { string n = ""; int d = index + 1; while (d > 0) { int m = (d - 1) % 26; n = System.Convert.ToChar(65 + m).ToString() + n; d = (int)((d - m) / 26); } return n; }
    private void BringCoordinatesToFront() { foreach (var lbl in topLabels) lbl.transform.SetAsLastSibling(); foreach (var lbl in leftLabels) lbl.transform.SetAsLastSibling(); }
    public void OnButtonZoomIn() { ApplyZoom(1f, 0.2f); }
    public void OnButtonZoomOut() { ApplyZoom(-1f, 0.2f); }
    public void OnButtonRotate() { if (selectedShip != null) selectedShip.RotateShip(); }
    public void OnButtonMoveUp() { if (selectedShip != null) selectedShip.MoveStep(0, -1); }
    public void OnButtonMoveDown() { if (selectedShip != null) selectedShip.MoveStep(0, 1); }
    public void OnButtonMoveLeft() { if (selectedShip != null) selectedShip.MoveStep(-1, 0); }
    public void OnButtonMoveRight() { if (selectedShip != null) selectedShip.MoveStep(1, 0); }
    
    public void OnButtonRandomize()
    {
        if (isCombatPhase) return;
        SelectShip(null);
        foreach (ShipController ship in activeShips) { ship.currentGridX = -100; ship.currentGridY = -100; }
        foreach (ShipController ship in activeShips)
        {
            bool placed = false; int attempts = 0;
            while (!placed && attempts < 500)
            {
                attempts++;
                ship.SetGridPosition(Random.Range(0, mapWidth), Random.Range(0, mapHeight), Random.value > 0.5f);
                if (IsPlacementValid(ship)) { placed = true; ship.ValidatePlacement(); }
            }
            if (!placed) ship.ValidatePlacement(); 
        }
    }
}