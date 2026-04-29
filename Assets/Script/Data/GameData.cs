using System.Collections.Generic;
using Unity.Netcode;

// Đánh dấu Serializable để có thể lưu ra JSON
[System.Serializable]
public class GameSettings
{
    public string languageCode = "vi"; // Mặc định là tiếng Việt
    public float masterVolume = 1f;
    public float musicVolume = 1f;
    public float sfxVolume = 1f;
    public bool enableQuickFire = false; // Tùy chọn Bắn Nhanh (Quick Fire)

    public bool enableScreenShake = true;
    public bool enableVibrate = true;

    public string playerName = "Chỉ huy vô danh";
}

[System.Serializable]
// 1. Cấu hình Luật chơi (Custom Match)
public struct MatchRules : INetworkSerializable
{
    public bool isShipMode; // true: Tàu, false: Máy bay
    public bool hasTurnTimer;
    public int turnTimeSeconds; // Mặc định 30s
    public bool isCustomMatch; // Bật/tắt Custom Match
    
    // Mảng lưu số lượng tàu: [0]: Tàu 5, [1]: Tàu 4, [2]: Tàu 3, [3]: Tàu 2
    public int[] fleetComposition; 
    public int maxPlanes;
    public int maxFlightPath;
    public bool isFreeFlightPath; 

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref isShipMode);
        serializer.SerializeValue(ref hasTurnTimer);
        serializer.SerializeValue(ref turnTimeSeconds);
        serializer.SerializeValue(ref isCustomMatch);
        
        // Đồng bộ mảng trong Netcode
        int length = 0;
        if (!serializer.IsReader) length = fleetComposition?.Length ?? 0;
        serializer.SerializeValue(ref length);
        if (serializer.IsReader) fleetComposition = new int[length];
        for (int i = 0; i < length; i++) serializer.SerializeValue(ref fleetComposition[i]);

        serializer.SerializeValue(ref maxPlanes);
        serializer.SerializeValue(ref maxFlightPath);
        serializer.SerializeValue(ref isFreeFlightPath);
    }
}

// 2. Cấu hình Bản đồ (Custom Map)
[System.Serializable]
public struct MapSettings : INetworkSerializable
{
    public bool isCustomMap;
    public int mapWidth;
    public int mapHeight;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref isCustomMap);
        serializer.SerializeValue(ref mapWidth);
        serializer.SerializeValue(ref mapHeight);
    }
}

// 3. GÓI CẤU HÌNH TỔNG (Thay thế LobbySettings cũ của bạn bằng cái này)
[System.Serializable]
public struct LobbySettings : INetworkSerializable
{
    public MatchRules matchRules;
    public MapSettings mapSettings;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        matchRules.NetworkSerialize(serializer);
        mapSettings.NetworkSerialize(serializer);
    }
}
