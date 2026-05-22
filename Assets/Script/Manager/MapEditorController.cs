using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class MapEditorController : MonoBehaviour
{
    [Header("Tham chiếu")]
    public Transform gridContainer;
    public GameObject cellPrefab;
    public MapEditorInputHandler inputHandler;

    [Header("Công cụ & UI")]
    public Toggle toolPen;
    public Toggle toolEraser;
    public Toggle toolPan;
    public Button btnClearAll;
    public Button btnSave;

    private const int GRID_MAX_SIZE = 100;
    private MapCellUI[,] cells = new MapCellUI[GRID_MAX_SIZE, GRID_MAX_SIZE];

    private void Awake()
    {
        GenerateVirtualGrid();
        
        // Gán sự kiện nút
        if (btnClearAll) btnClearAll.onClick.AddListener(ResetToDefault);
        if (btnSave) btnSave.onClick.AddListener(SaveMap);
    }

    private void GenerateVirtualGrid()
    {
        foreach (Transform child in gridContainer) Destroy(child.gameObject);

        for (int y = 0; y < GRID_MAX_SIZE; y++)
        {
            for (int x = 0; x < GRID_MAX_SIZE; x++)
            {
                GameObject obj = Instantiate(cellPrefab, gridContainer);
                MapCellUI cellUI = obj.GetComponent<MapCellUI>();
                cellUI.Init(x, y);
                cells[x, y] = cellUI;
            }
        }
        
        // Gọi hàm Reset để căn giữa ngay khi vừa tạo xong lưới
        ResetToDefault();
    }

    private void SetInitialZone()
    {
        // Reset toàn bộ về Empty trước
        foreach (var c in cells) c.SetState(false);

        // Thiết lập 10x10 mặc định ở giữa
        int start = (GRID_MAX_SIZE - 10) / 2;
        for (int y = start; y < start + 10; y++)
        {
            for (int x = start; x < start + 10; x++)
            {
                cells[x, y].SetState(true);
            }
        }
    }

    public void ResetToDefault()
    {
        // 1. Lấy kích thước chuẩn từ mạng
        int targetSize = 10;
        if (LobbyNetworkManager.Instance != null)
        {
            targetSize = LobbyNetworkManager.Instance.RoomSettings.Value.mapSettings.mapWidth;
            targetSize = Mathf.Clamp(targetSize, 5, GRID_MAX_SIZE);
        }
        
        // 2. Xóa toàn bộ
        foreach (var c in cells) c.SetState(false);

        // 3. Tính toán tâm và vẽ lại khối vuông chuẩn
        int startX = (GRID_MAX_SIZE - targetSize) / 2;
        int startY = (GRID_MAX_SIZE - targetSize) / 2;

        for (int y = startY; y < startY + targetSize; y++)
        {
            for (int x = startX; x < startX + targetSize; x++)
            {
                if (x >= 0 && x < GRID_MAX_SIZE && y >= 0 && y < GRID_MAX_SIZE)
                {
                    cells[x, y].SetState(true);
                }
            }
        }

        // 4. Đưa camera về lại trung tâm
        gridContainer.localScale = Vector3.one;
        gridContainer.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
        
        Debug.Log($"Đã reset bản đồ về {targetSize}x{targetSize} ở tâm lưới.");
    }

    

    public void ExecuteDrawAction(MapCellUI cell)
    {
        if (toolPen.isOn) cell.SetState(true);
        else if (toolEraser.isOn)
        {
            cell.SetState(false);
            if (!IsValidSize()) cell.SetState(true);
        }
    }

    public void SaveMap()
    {
        int minX = GRID_MAX_SIZE, maxX = 0, minY = GRID_MAX_SIZE, maxY = 0;
        bool hasAny = false;

        // 1. Tìm phạm vi bao quanh vùng đã vẽ
        foreach (var cell in cells)
        {
            if (cell.isPlayableZone)
            {
                hasAny = true;
                if (cell.x < minX) minX = cell.x;
                if (cell.x > maxX) maxX = cell.x;
                if (cell.y < minY) minY = cell.y;
                if (cell.y > maxY) maxY = cell.y;
            }
        }

        if (!hasAny) return;

        int finalW = (maxX - minX) + 1;
        int finalH = (maxY - minY) + 1;

        // 2. Trích xuất dữ liệu hình dáng (ép phẳng vùng vẽ thành mảng 1D)
        bool[] mapShape = new bool[finalW * finalH];
        for (int y = 0; y < finalH; y++)
        {
            for (int x = 0; x < finalW; x++)
            {
                // Tọa độ thực tế trong mảng 100x100 là (minX + x, minY + y)
                mapShape[y * finalW + x] = cells[minX + x, minY + y].isPlayableZone;
            }
        }

        // 3. Gửi toàn bộ gói dữ liệu lên mạng
        LobbySettings current = LobbyNetworkManager.Instance.RoomSettings.Value;
        current.mapSettings.mapWidth = finalW;
        current.mapSettings.mapHeight = finalH;
        current.mapSettings.customMapData = mapShape; // Lưu hình dáng

        current.mapSettings.isCustomMap = true;

        LobbyNetworkManager.Instance.HostUpdateSettings(current);

        Debug.Log($"Lưu bản đồ {finalW}x{finalH}. Tổng cộng {mapShape.Length} ô dữ liệu.");
        this.gameObject.SetActive(false);
    }

    private bool IsValidSize()
    {
        // Giữ logic kiểm tra 5x5 như cũ
        int minX = GRID_MAX_SIZE, maxX = 0, minY = GRID_MAX_SIZE, maxY = 0;
        int count = 0;
        foreach (var cell in cells)
        {
            if (cell.isPlayableZone)
            {
                count++;
                if (cell.x < minX) minX = cell.x;
                if (cell.x > maxX) maxX = cell.x;
                if (cell.y < minY) minY = cell.y;
                if (cell.y > maxY) maxY = cell.y;
            }
        }
        if (count == 0) return false;
        return ((maxX - minX + 1) >= 5 && (maxY - minY + 1) >= 5);
    }
}