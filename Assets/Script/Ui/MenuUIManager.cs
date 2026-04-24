using UnityEngine;
using Unity.Netcode;

public class MenuUIManager : MonoBehaviour
{
    public static MenuUIManager Instance;

    [Header("UI Panels")]
    public GameObject panelMainMenu;
    public GameObject panelLobby;
    public GameObject panelRoom;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    private void Start()
    {
        ShowMainMenu(); // Mặc định hiển thị Main Menu khi vào game
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback += HandleClientDisconnect;
        }
    }

    private void OnDestroy()
    {
        // Hủy lắng nghe để tránh lỗi rò rỉ bộ nhớ
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback -= HandleClientDisconnect;
        }
    }

    // Hàm tự động chạy khi Client bị mất kết nối với Host
    private void HandleClientDisconnect(ulong clientId)
    {
        // Kiểm tra xem ID người vừa thoát có phải là CHÍNH MÌNH không
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            // Chính mình bị văng (hoặc mình tự bấm thoát, hoặc Host sập)
            Debug.Log("Mình đã ngắt kết nối. Đang quay về Menu...");
            ShowMainMenu();
        }
        else
        {
            // Một NGƯỜI CHƠI KHÁC vừa thoát (Trường hợp mình là Host và Client bỏ đi)
            Debug.Log($"Người chơi {clientId} đã rời phòng.");
            // Không làm gì cả, Host vẫn ở lại Panel Room chờ người khác vào.
        }
    }

    private void HideAllPanels()
    {
        panelMainMenu.SetActive(false);
        panelLobby.SetActive(false);
        panelRoom.SetActive(false);
    }

    public void ShowMainMenu()
    {
        HideAllPanels();
        panelMainMenu.SetActive(true);
    }

    public void ShowLobby()
    {
        HideAllPanels();
        panelLobby.SetActive(true);
    }

    public void ShowRoom()
    {
        HideAllPanels();
        panelRoom.SetActive(true);
    }

    
}