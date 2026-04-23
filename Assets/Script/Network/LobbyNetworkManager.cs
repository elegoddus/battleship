using Unity.Netcode;
using UnityEngine;
using System;

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

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(this.gameObject);
        else Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        // Khi vừa kết nối vào phòng, lấy dữ liệu mới nhất update lên UI
        OnRoomSettingsChanged?.Invoke(RoomSettings.Value);

        // Lắng nghe nếu Host thay đổi thông số thì update lại
        RoomSettings.OnValueChanged += (oldValue, newValue) =>
        {
            OnRoomSettingsChanged?.Invoke(newValue);
            Debug.Log($"[Đồng bộ mạng] Cấu hình mới: Map {newValue.mapWidth}x{newValue.mapHeight}, Máy bay: {newValue.maxPlanes}");
        };
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