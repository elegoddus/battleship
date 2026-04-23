using UnityEngine;
using TMPro; // Bắt buộc dùng TextMeshPro

[RequireComponent(typeof(TextMeshProUGUI))]
public class LocalizedText : MonoBehaviour
{
    [Tooltip("Nhập Key từ file JSON vào đây (VD: menu_play)")]
    public string localizationKey;

    private TextMeshProUGUI textComponent;

    private void Awake()
    {
        textComponent = GetComponent<TextMeshProUGUI>();
    }

    private void Start()
    {
        // Đăng ký lắng nghe sự kiện đổi ngôn ngữ
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.OnLanguageChanged += UpdateText;
            UpdateText(); // Cập nhật text ngay khi vừa spawn
        }
    }

    private void OnDestroy()
    {
        // Phải hủy đăng ký khi object bị hủy để tránh lỗi memory leak
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.OnLanguageChanged -= UpdateText;
        }
    }

    private void UpdateText()
    {
        if (string.IsNullOrEmpty(localizationKey)) return;

        textComponent.text = LocalizationManager.Instance.GetText(localizationKey);
    }
}