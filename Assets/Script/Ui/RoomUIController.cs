using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;
using System.Collections.Generic;
using Unity.Collections;

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

    [Header("UI - Labels (Dành cho Song ngữ)")]
    public TextMeshProUGUI txtLabelGameMode;
    public TextMeshProUGUI txtLabelMapSize;
    public TextMeshProUGUI txtLabelCustomMap;
    public TextMeshProUGUI txtLabelCustomMatch;
    public TextMeshProUGUI txtBtnDrawMap;

    [Header("UI - Center Settings (Tương tác)")]
    public TMP_Dropdown dropdownGameMode; 
    public TMP_InputField inputMapWidth;
    public TMP_InputField inputMapHeight;
    public Toggle toggleCustomMap;
    public GameObject btnDrawMap; 
    public Toggle toggleCustomMatch;
    public GameObject panelCustomGameOptions;

    [Header("UI - Bottom Buttons")]
    public Button btnLeave;
    public Button btnAction;
    public TextMeshProUGUI txtBtnAction;

    [Header("Dữ liệu Phe phái")]
    public FactionData[] availableFactions;

    private void Start()
    {
        // Đăng ký sự kiện Nút bấm (Chỉ làm 1 lần ở Start)
        btnLeave.onClick.AddListener(LeaveRoom);
        btnAction.onClick.AddListener(OnActionButtonClicked);

        // Logic Hiện/Ẩn menu con
        toggleCustomMap.onValueChanged.AddListener((isOn) => { if(btnDrawMap) btnDrawMap.SetActive(isOn); });
        toggleCustomMatch.onValueChanged.AddListener((isOn) => { if(panelCustomGameOptions) panelCustomGameOptions.SetActive(isOn); });

        // Lắng nghe Host thay đổi Cài đặt
        dropdownGameMode.onValueChanged.AddListener(delegate { OnHostChangedSettings(); });
        toggleCustomMap.onValueChanged.AddListener(delegate { OnHostChangedSettings(); });
        toggleCustomMatch.onValueChanged.AddListener(delegate { OnHostChangedSettings(); });
        inputMapWidth.onEndEdit.AddListener(delegate { OnHostChangedSettings(); });
        inputMapHeight.onEndEdit.AddListener(delegate { OnHostChangedSettings(); });

        localFactionDropdown.onValueChanged.AddListener(OnLocalFactionChanged);
    }

    private void OnEnable()
    {
        bool isServer = NetworkManager.Singleton.IsServer;
        if (centerSettingsGroup != null) centerSettingsGroup.interactable = isServer;

        if (isServer)
        {
            txtBtnAction.text = "BẮT ĐẦU";
            btnAction.interactable = false; 
        }
        else
        {
            txtBtnAction.text = "SẴN SÀNG";
            btnAction.interactable = true;
            txtRemoteStatus.text = "CHỦ PHÒNG";
        }

        // Setup Ngôn ngữ & Dropdown
        ApplyLanguageToSettingsUI();
        SetupFactionDropdown();

        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.OnLanguageChanged += ApplyLanguageToSettingsUI;
            LocalizationManager.Instance.OnLanguageChanged += SetupFactionDropdown;
            LocalizationManager.Instance.OnLanguageChanged += UpdateFactionUI;
        }

        // Lắng nghe Mạng
        if (LobbyNetworkManager.Instance != null)
        {
            LobbyNetworkManager.Instance.isClientReady.OnValueChanged += UpdateReadyUI;
            LobbyNetworkManager.Instance.hostName.OnValueChanged += OnNameChanged;
            LobbyNetworkManager.Instance.clientName.OnValueChanged += OnNameChanged;
            LobbyNetworkManager.Instance.hostFactionIndex.OnValueChanged += OnFactionChanged;
            LobbyNetworkManager.Instance.clientFactionIndex.OnValueChanged += OnFactionChanged;
            LobbyNetworkManager.Instance.RoomSettings.OnValueChanged += SyncSettingsUI;

            // Gọi đồng bộ lần đầu tiên
            UpdateNamesUI();
            UpdateFactionUI();
            UpdateReadyUI(false, LobbyNetworkManager.Instance.isClientReady.Value);
            SyncSettingsUI(new LobbySettings(), LobbyNetworkManager.Instance.RoomSettings.Value);
        }
    }

    private void OnDisable()
    {
        // Gỡ lắng nghe để tránh lỗi rò rỉ bộ nhớ
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.OnLanguageChanged -= ApplyLanguageToSettingsUI;
            LocalizationManager.Instance.OnLanguageChanged -= SetupFactionDropdown;
            LocalizationManager.Instance.OnLanguageChanged -= UpdateFactionUI;
        }

        if (LobbyNetworkManager.Instance != null)
        {
            LobbyNetworkManager.Instance.isClientReady.OnValueChanged -= UpdateReadyUI;
            LobbyNetworkManager.Instance.hostName.OnValueChanged -= OnNameChanged;
            LobbyNetworkManager.Instance.clientName.OnValueChanged -= OnNameChanged;
            LobbyNetworkManager.Instance.hostFactionIndex.OnValueChanged -= OnFactionChanged;
            LobbyNetworkManager.Instance.clientFactionIndex.OnValueChanged -= OnFactionChanged;
            LobbyNetworkManager.Instance.RoomSettings.OnValueChanged -= SyncSettingsUI;
        }
    }

    // --- CÁC HÀM XỬ LÝ (WRAPPERS) ---
    private void OnNameChanged(FixedString32Bytes oldVal, FixedString32Bytes newVal) { UpdateNamesUI(); }
    private void OnFactionChanged(int oldVal, int newVal) { UpdateFactionUI(); }

    private void ApplyLanguageToSettingsUI()
    {
        if (txtLabelGameMode != null) txtLabelGameMode.text = LocalizationManager.Instance.GetText("setting_game_mode");
        if (txtLabelMapSize != null) txtLabelMapSize.text = LocalizationManager.Instance.GetText("setting_map_size");
        if (txtLabelCustomMap != null) txtLabelCustomMap.text = LocalizationManager.Instance.GetText("setting_custom_map");
        if (txtLabelCustomMatch != null) txtLabelCustomMatch.text = LocalizationManager.Instance.GetText("setting_custom_match");
        if (txtBtnDrawMap != null) txtBtnDrawMap.text = LocalizationManager.Instance.GetText("btn_draw_map");

        if (dropdownGameMode != null)
        {
            int currentValue = dropdownGameMode.value; 
            dropdownGameMode.ClearOptions();
            dropdownGameMode.AddOptions(new List<string> {
                LocalizationManager.Instance.GetText("mode_ships"),  
                LocalizationManager.Instance.GetText("mode_planes")  
            });
            dropdownGameMode.SetValueWithoutNotify(currentValue);
        }
    }

    private void SetupFactionDropdown()
    {
        localFactionDropdown.ClearOptions();
        List<string> options = new List<string>();
        foreach (var faction in availableFactions)
        {
            options.Add(LocalizationManager.Instance.GetText(faction.factionKey));
        }
        localFactionDropdown.AddOptions(options);
        
        if (NetworkManager.Singleton.IsServer)
            localFactionDropdown.SetValueWithoutNotify(LobbyNetworkManager.Instance.hostFactionIndex.Value);
        else
            localFactionDropdown.SetValueWithoutNotify(LobbyNetworkManager.Instance.clientFactionIndex.Value);
    }

    private void OnLocalFactionChanged(int selectedIndex)
    {
        if (NetworkManager.Singleton.IsServer)
            LobbyNetworkManager.Instance.hostFactionIndex.Value = selectedIndex; 
        else
            LobbyNetworkManager.Instance.ChangeClientFactionServerRpc(selectedIndex); 
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

    private void OnHostChangedSettings()
    {
        if (!NetworkManager.Singleton.IsServer) return;

        LobbySettings current = LobbyNetworkManager.Instance.RoomSettings.Value;

        current.matchRules.isShipMode = (dropdownGameMode.value == 0); 
        current.mapSettings.isCustomMap = toggleCustomMap.isOn;
        current.matchRules.isCustomMatch = toggleCustomMatch.isOn;

        if (int.TryParse(inputMapWidth.text, out int w)) current.mapSettings.mapWidth = w;
        if (int.TryParse(inputMapHeight.text, out int h)) current.mapSettings.mapHeight = h;

        LobbyNetworkManager.Instance.HostUpdateSettings(current);
    }

    private void SyncSettingsUI(LobbySettings oldSettings, LobbySettings newSettings)
    {
        dropdownGameMode.SetValueWithoutNotify(newSettings.matchRules.isShipMode ? 0 : 1);
        toggleCustomMap.SetIsOnWithoutNotify(newSettings.mapSettings.isCustomMap);
        toggleCustomMatch.SetIsOnWithoutNotify(newSettings.matchRules.isCustomMatch);
        inputMapWidth.SetTextWithoutNotify(newSettings.mapSettings.mapWidth.ToString());
        inputMapHeight.SetTextWithoutNotify(newSettings.mapSettings.mapHeight.ToString());

        if(btnDrawMap) btnDrawMap.SetActive(newSettings.mapSettings.isCustomMap);
        if(panelCustomGameOptions) panelCustomGameOptions.SetActive(newSettings.matchRules.isCustomMatch);
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

    public void OnActionButtonClicked() 
    {
        if (NetworkManager.Singleton.IsServer)
        {
            Debug.Log("Chủ phòng đã bấm Bắt đầu trận đấu!");
            // NetworkManager.Singleton.SceneManager.LoadScene("Gameplay", UnityEngine.SceneManagement.LoadSceneMode.Single);
        }
        else
        {
            LobbyNetworkManager.Instance.ToggleReadyServerRpc();
        }
    }

    public void LeaveRoom()
    {
        if (NetworkManager.Singleton != null) NetworkManager.Singleton.Shutdown();
        if (MenuUIManager.Instance != null) MenuUIManager.Instance.ShowMainMenu();
    }
}