using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;
using System.Collections.Generic;

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
    private ShipController selectedShip;

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

        CalculateZoomLimits(mapWidth, mapHeight, totalWidth, totalHeight);
        SpawnShipsInCenter(startOffset); 
    }

    private void CalculateZoomLimits(int width, int height, float totalW, float totalH)
    {
        RectTransform viewport = gridContainer.parent.GetComponent<RectTransform>();
        float viewWidth = viewport.rect.width;
        float viewHeight = viewport.rect.height;

        float fitZoom = Mathf.Min(viewWidth / totalW, viewHeight / totalH) * 0.9f;

        minZoom = Mathf.Min(fitZoom, 10f / Mathf.Max(width, height)); 
        minZoom = Mathf.Clamp(minZoom, 0.1f, 5f);
        maxZoom = fitZoom * 2.0f; 

        currentZoom = fitZoom;
        gridContainer.localScale = new Vector3(currentZoom, currentZoom, 1f);
        gridContainer.anchoredPosition = Vector2.zero;
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
                float scrollDir = Mathf.Sign(scroll);
                currentZoom += scrollDir * currentZoom * 0.1f; 
                
                currentZoom = Mathf.Clamp(currentZoom, minZoom, maxZoom);
                gridContainer.localScale = new Vector3(currentZoom, currentZoom, 1f);
            }
        }

        if (Keyboard.current != null && selectedShip != null)
        {
            // Di chuyển bằng phím Mũi tên hoặc WASD
            if (Keyboard.current.upArrowKey.wasPressedThisFrame || Keyboard.current.wKey.wasPressedThisFrame) 
                OnButtonMoveUp();
            if (Keyboard.current.downArrowKey.wasPressedThisFrame || Keyboard.current.sKey.wasPressedThisFrame) 
                OnButtonMoveDown();
            if (Keyboard.current.leftArrowKey.wasPressedThisFrame || Keyboard.current.aKey.wasPressedThisFrame) 
                OnButtonMoveLeft();
            if (Keyboard.current.rightArrowKey.wasPressedThisFrame || Keyboard.current.dKey.wasPressedThisFrame) 
                OnButtonMoveRight();

            // Xoay bằng phím Space hoặc R
            if (Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.rKey.wasPressedThisFrame) 
                OnButtonRotate();
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

    public void OnButtonRotate() { if (selectedShip != null) selectedShip.RotateShip(); }
    public void OnButtonMoveUp() { if (selectedShip != null) selectedShip.MoveStep(0, -1); }
    public void OnButtonMoveDown() { if (selectedShip != null) selectedShip.MoveStep(0, 1); }
    public void OnButtonMoveLeft() { if (selectedShip != null) selectedShip.MoveStep(-1, 0); }
    public void OnButtonMoveRight() { if (selectedShip != null) selectedShip.MoveStep(1, 0); }
}