using System;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;

public class LocalizationManager : MonoBehaviour
{
    public static LocalizationManager Instance { get; private set; }

    // Sự kiện phát ra khi đổi ngôn ngữ, các UI Text sẽ đăng ký lắng nghe sự kiện này
    public event Action OnLanguageChanged;

    private Dictionary<string, string> localizedText;
    private string currentLanguage;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(this.gameObject);
        
        localizedText = new Dictionary<string, string>();
    }

    private void Start()
    {
        // Khi game vừa chạy, lấy ngôn ngữ từ DataManager để load
        LoadLanguage(DataManager.Instance.CurrentSettings.languageCode);
    }

    public void LoadLanguage(string langCode)
    {
        TextAsset textAsset = Resources.Load<TextAsset>("Localization/" + langCode);
        
        if (textAsset != null)
        {
            localizedText = JsonConvert.DeserializeObject<Dictionary<string, string>>(textAsset.text);
            currentLanguage = langCode;
            Debug.Log("Đã tải ngôn ngữ: " + langCode);

            // Cập nhật lại thông số lưu trữ và lưu file
            DataManager.Instance.CurrentSettings.languageCode = langCode;
            DataManager.Instance.SaveSettings();

            // Kích hoạt sự kiện để toàn bộ UI Text tự động đổi chữ
            OnLanguageChanged?.Invoke();
        }
        else
        {
            Debug.LogError("Không tìm thấy file ngôn ngữ: " + langCode);
        }
    }

    public string GetText(string key)
    {
        if (localizedText != null && localizedText.ContainsKey(key))
        {
            return localizedText[key];
        }
        return "[" + key + "]"; // Trả về chính key đó trong ngoặc vuông để dễ debug nếu thiếu
    }
}