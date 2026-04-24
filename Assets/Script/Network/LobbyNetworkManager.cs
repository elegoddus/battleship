using Unity.Netcode;
using UnityEngine;
using System;
using Unity.Collections;

public class LobbyNetworkManager : NetworkBehaviour
{
    public static LobbyNetworkManager Instance { get; private set; }

    // Biến này lưu trạng thái phòng. Chỉ Host được ghi (WritePermission.Server), ai cũng được đọc.
    public NetworkVariable<LobbySettings> RoomSettings = new NetworkVariable<LobbySettings>(
        new LobbySettings { mapWidth = 10, mapHeight = 10, useCustomMap = false, maxPlanes = 5, totalFlightPathCoverage = 30 },
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    // Sự kiện để báo cho UI biết khi có người đổi cài đặt
    public event Action<LobbySettings> OnRoomSettingsChanged;

    public NetworkVariable<bool> isClientReady = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<FixedString32Bytes> hostName = new NetworkVariable<FixedString32Bytes>("", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<FixedString32Bytes> clientName = new NetworkVariable<FixedString32Bytes>("", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<int> hostFactionIndex = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int> clientFactionIndex = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(this.gameObject);
        else Instance = this;
    }

    [ServerRpc(RequireOwnership = false)]
    public void ToggleReadyServerRpc()
    {
        isClientReady.Value = !isClientReady.Value; 
    }

    // public override void OnNetworkSpawn()
    // {
    //     // Khi vừa kết nối vào phòng, lấy dữ liệu mới nhất update lên UI
    //     OnRoomSettingsChanged?.Invoke(RoomSettings.Value);

    //     // Lắng nghe nếu Host thay đổi thông số thì update lại
    //     RoomSettings.OnValueChanged += (oldValue, newValue) =>
    //     {
    //         OnRoomSettingsChanged?.Invoke(newValue);
    //         Debug.Log($"[Đồng bộ mạng] Cấu hình mới: Map {newValue.mapWidth}x{newValue.mapHeight}, Máy bay: {newValue.maxPlanes}");
    //     };
    // }
    public override void OnNetworkSpawn()
    {
        // Khi Host vừa tạo phòng xong, gán luôn tên Host vào mạng
        if (IsServer)
        {
            hostName.Value = DataManager.Instance.CurrentSettings.playerName;
            NetworkManager.Singleton.OnClientDisconnectCallback += CleanUpWhenClientLeaves;

        }
        // Khi Client vừa vào phòng, bắn tín hiệu (ServerRpc) báo tên cho Host biết
        else if (IsClient)
        {
            SetClientNameServerRpc(DataManager.Instance.CurrentSettings.playerName);
        }
    }

    public override void OnNetworkDespawn()
    {
        // Tránh rò rỉ bộ nhớ khi hủy object
        if (IsServer)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback -= CleanUpWhenClientLeaves;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void ChangeClientFactionServerRpc(int newIndex)
    {
        clientFactionIndex.Value = newIndex;
    }

    private void CleanUpWhenClientLeaves(ulong clientId)
    {
        // Kiểm tra chắc chắn người rời đi KHÔNG PHẢI là Host
        if (clientId != NetworkManager.ServerClientId)
        {
            Debug.Log("Dọn dẹp dữ liệu của Client vừa rời đi...");
            clientName.Value = ""; // Xóa tên Client
            isClientReady.Value = false; // Xóa trạng thái Sẵn sàng
            clientFactionIndex.Value = 0;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetClientNameServerRpc(FixedString32Bytes name)
    {
        clientName.Value = name; // Server nhận tên và cập nhật cho tất cả mọi người
    }

    // Hàm này gắn vào các nút bấm / Slider trên giao diện của Host
    public void HostUpdateSettings(LobbySettings newSettings)
    {
        if (IsServer) // Chỉ thực thi nếu người bấm là Chủ phòng
        {
            RoomSettings.Value = newSettings;
        }
    }
}