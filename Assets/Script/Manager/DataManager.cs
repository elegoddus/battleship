using UnityEngine;
using System.IO;
using Newtonsoft.Json;

public class DataManager : MonoBehaviour
{
    // Singleton pattern
    public static DataManager Instance { get; private set; }

    public GameSettings CurrentSettings { get; private set; }
    
    private string settingsFilePath;

    private void Awake()
    {
        // Đảm bảo chỉ có 1 DataManager tồn tại khi chuyển Scene
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(this.gameObject);

        // Application.persistentDataPath là đường dẫn an toàn để lưu file trên cả PC, Android và iOS
        settingsFilePath = Path.Combine(Application.persistentDataPath, "PlayerSettings.json");
        
        LoadSettings();
    }

    public void SaveSettings()
    {
        try
        {
            string json = JsonConvert.SerializeObject(CurrentSettings, Formatting.Indented);
            File.WriteAllText(settingsFilePath, json);
            Debug.Log("Đã lưu cài đặt thành công tại: " + settingsFilePath);
        }
        catch (System.Exception e)
        {
            Debug.LogError("Lỗi khi lưu file JSON: " + e.Message);
        }
    }

    public void LoadSettings()
    {
        if (File.Exists(settingsFilePath))
        {
            try
            {
                string json = File.ReadAllText(settingsFilePath);
                CurrentSettings = JsonConvert.DeserializeObject<GameSettings>(json);
                Debug.Log("Đã tải cài đặt người chơi.");
            }
            catch (System.Exception e)
            {
                Debug.LogError("Lỗi khi đọc file JSON, tạo profile mới. Lỗi: " + e.Message);
                CreateDefaultSettings();
            }
        }
        else
        {
            Debug.Log("Không tìm thấy file save, tạo profile cài đặt mặc định.");
            CreateDefaultSettings();
        }
    }

    private void CreateDefaultSettings()
    {
        CurrentSettings = new GameSettings();
        SaveSettings();
    }
}