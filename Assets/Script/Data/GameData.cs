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
public struct LobbySettings : INetworkSerializable
{
    public int mapWidth;
    public int mapHeight;
    public bool useCustomMap;
    public int maxPlanes;
    public int totalFlightPathCoverage;

    // Hàm bắt buộc của NGO để đóng gói dữ liệu gửi đi
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref mapWidth);
        serializer.SerializeValue(ref mapHeight);
        serializer.SerializeValue(ref useCustomMap);
        serializer.SerializeValue(ref maxPlanes);
        serializer.SerializeValue(ref totalFlightPathCoverage);
    }
}