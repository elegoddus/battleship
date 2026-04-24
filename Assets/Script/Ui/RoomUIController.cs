using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;
using System.Collections.Generic;

public class RoomUIController : MonoBehaviour
{
    [Header("UI - Left (Local Player)")]
    public TextMeshProUGUI txtLocalName;
    public TMP_Dropdown localFactionDropdown;

    [Header("UI - Right (Remote Player)")]
    public TextMeshProUGUI txtRemoteName;
    public TextMeshProUGUI txtRemoteStatus;
    public TextMeshProUGUI txtRemoteFaction;

    [Header("UI - Center (Settings)")]
    public CanvasGroup centerSettingsGroup;

    [Header("UI - Bottom Buttons")]
    public Button btnLeave;
    public Button btnAction;
    public TextMeshProUGUI txtBtnAction;

    [Header("Dữ liệu Phe phái")]
    public FactionData[] availableFactions; // Kéo thả 3 file FactionData vào đây


    private void OnEnable()
    {
        // Khi Panel này bật lên (sau khi đã StartHost hoặc StartClient)
        bool isServer = NetworkManager.Singleton.IsServer;

        // 1. Khóa/Mở cài đặt ở giữa
        if (centerSettingsGroup != null) centerSettingsGroup.interactable = isServer;

        // 2. Thiết lập nút Bắt đầu / Sẵn sàng
        if (isServer)
        {
            txtBtnAction.text = "BẮT ĐẦU";
            btnAction.interactable = false; // Đợi Client ready mới mở
        }
        else
        {
            txtBtnAction.text = "SẴN SÀNG";
            btnAction.interactable = true;
            txtRemoteStatus.text = "CHỦ PHÒNG";
        }

        // 3. Đăng ký sự kiện thay đổi nút Sẵn sàng
        if (LobbyNetworkManager.Instance != null)
        {
            LobbyNetworkManager.Instance.isClientReady.OnValueChanged += UpdateReadyUI;
            
            // Lắng nghe khi có người đổi tên (Client vào phòng)
            LobbyNetworkManager.Instance.hostName.OnValueChanged += (oldVal, newVal) => UpdateNamesUI();
            LobbyNetworkManager.Instance.clientName.OnValueChanged += (oldVal, newVal) => UpdateNamesUI();
            
            UpdateNamesUI(); // Gọi lần đầu
        }

        localFactionDropdown.onValueChanged.AddListener(OnLocalFactionChanged);

        if (LobbyNetworkManager.Instance != null)
        {
            LobbyNetworkManager.Instance.isClientReady.OnValueChanged += UpdateReadyUI;
            LobbyNetworkManager.Instance.hostName.OnValueChanged += (oldVal, newVal) => UpdateNamesUI();
            LobbyNetworkManager.Instance.clientName.OnValueChanged += (oldVal, newVal) => UpdateNamesUI();
            
            // 2. Lắng nghe mạng khi có người đổi phe
            LobbyNetworkManager.Instance.hostFactionIndex.OnValueChanged += (oldVal, newVal) => UpdateFactionUI();
            LobbyNetworkManager.Instance.clientFactionIndex.OnValueChanged += (oldVal, newVal) => UpdateFactionUI();
            
            UpdateNamesUI(); 
            UpdateFactionUI(); // Gọi lần đầu để load UI
        }

        SetupFactionDropdown();
        
        // Đăng ký sự kiện đổi ngôn ngữ để dịch lại Dropdown nếu người chơi đổi ngôn ngữ giữa chừng
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.OnLanguageChanged += SetupFactionDropdown;
            LocalizationManager.Instance.OnLanguageChanged += UpdateFactionUI;
        }
    }

    private void UpdateNamesUI()
    {
        string hName = LobbyNetworkManager.Instance.hostName.Value.ToString();
        string cName = LobbyNetworkManager.Instance.clientName.Value.ToString();

        if (NetworkManager.Singleton.IsServer)
        {
            txtLocalName.text = string.IsNullOrEmpty(hName) ? "Chủ phòng" : hName;
            txtRemoteName.text = string.IsNullOrEmpty(cName) ? "Đang chờ đối thủ..." : cName;
        }
        else
        {
            txtLocalName.text = string.IsNullOrEmpty(cName) ? "Người chơi" : cName;
            txtRemoteName.text = string.IsNullOrEmpty(hName) ? "Chủ phòng" : hName;
        }
    }

    private void OnDisable()
    {
        // Phải hủy đăng ký khi tắt Panel để tránh lỗi bộ nhớ
        localFactionDropdown.onValueChanged.RemoveListener(OnLocalFactionChanged);
        if (LobbyNetworkManager.Instance != null)
        {
            LobbyNetworkManager.Instance.isClientReady.OnValueChanged -= UpdateReadyUI;
        }

        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.OnLanguageChanged -= SetupFactionDropdown;
            LocalizationManager.Instance.OnLanguageChanged -= UpdateFactionUI;
        }
    }

    // Hàm tự động nhét chữ vào Dropdown
    private void SetupFactionDropdown()
    {
        localFactionDropdown.ClearOptions();
        List<string> options = new List<string>();

        foreach (var faction in availableFactions)
        {
            // Dịch từ Key sang chữ hiển thị (VD: faction_russia -> Liên bang Nga)
            options.Add(LocalizationManager.Instance.GetText(faction.factionKey));
        }

        localFactionDropdown.AddOptions(options);
        
        // Giữ nguyên lựa chọn hiện tại
        if (NetworkManager.Singleton.IsServer)
            localFactionDropdown.SetValueWithoutNotify(LobbyNetworkManager.Instance.hostFactionIndex.Value);
        else
            localFactionDropdown.SetValueWithoutNotify(LobbyNetworkManager.Instance.clientFactionIndex.Value);
    }

    private void OnLocalFactionChanged(int selectedIndex)
    {
        if (NetworkManager.Singleton.IsServer)
        {
            LobbyNetworkManager.Instance.hostFactionIndex.Value = selectedIndex; // Host tự ghi
        }
        else
        {
            LobbyNetworkManager.Instance.ChangeClientFactionServerRpc(selectedIndex); // Client phải xin Server ghi
        }
    }

    private void UpdateFactionUI()
    {
        int hFaction = LobbyNetworkManager.Instance.hostFactionIndex.Value;
        int cFaction = LobbyNetworkManager.Instance.clientFactionIndex.Value;
        if (availableFactions == null || availableFactions.Length == 0) return;

        if (NetworkManager.Singleton.IsServer)
        {
            if (cFaction >= 0 && cFaction < availableFactions.Length)
                txtRemoteFaction.text = LocalizationManager.Instance.GetText("room_faction") + ": " + LocalizationManager.Instance.GetText(availableFactions[cFaction].factionKey);
        }
        else
        {
            if (hFaction >= 0 && hFaction < availableFactions.Length)
                txtRemoteFaction.text = LocalizationManager.Instance.GetText("room_faction") + ": " + LocalizationManager.Instance.GetText(availableFactions[hFaction].factionKey);
        }
    }

    private void UpdateReadyUI(bool previousValue, bool isReady)
    {
        if (NetworkManager.Singleton.IsServer)
        {
            btnAction.interactable = isReady;
            txtRemoteStatus.text = isReady ? "<color=green>ĐÃ SẴN SÀNG</color>" : "<color=red>CHƯA SẴN SÀNG</color>";
        }
        else
        {
            txtBtnAction.text = isReady ? "HỦY SẴN SÀNG" : "SẴN SÀNG";
        }
    }

    public void OnActionButtonClicked() // Gắn hàm này vào nút btn_Action trên Inspector
    {
        if (NetworkManager.Singleton.IsServer)
        {
            Debug.Log("Chủ phòng đã bấm Bắt đầu trận đấu!");
            // NetworkManager.Singleton.SceneManager.LoadScene("Gameplay", UnityEngine.SceneManagement.LoadSceneMode.Single);
        }
        else
        {
            // Client bấm Sẵn sàng -> Gọi hàm bên NetworkManager
            LobbyNetworkManager.Instance.ToggleReadyServerRpc();
        }
    }

    public void LeaveRoom()
    {
        // 1. Ngắt kết nối mạng (Tắt Host hoặc ngắt Client)
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.Shutdown();
        }
        
        // 2. Gọi MenuUIManager để ẩn Panel Room và hiện Panel MainMenu
        if (MenuUIManager.Instance != null)
        {
            MenuUIManager.Instance.ShowMainMenu();
        }
    }
}