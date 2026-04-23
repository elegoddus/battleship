using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SettingsUIController : MonoBehaviour
{
    [Header("UI Elements")]
    public Slider masterSlider;
    public Slider musicSlider;
    public Slider sfxSlider;
    public Toggle screenShakeToggle;
    public Toggle vibrateToggle;

    private void OnEnable()
    {
        // Mỗi khi bật Panel này lên, tải dữ liệu từ DataManager gán vào UI
        LoadUIFromData();
    }

    private void LoadUIFromData()
    {
        if (DataManager.Instance == null) return;

        GameSettings settings = DataManager.Instance.CurrentSettings;

        masterSlider.value = settings.masterVolume;
        musicSlider.value = settings.musicVolume;
        sfxSlider.value = settings.sfxVolume;
        
        screenShakeToggle.isOn = settings.enableScreenShake;
        vibrateToggle.isOn = settings.enableVibrate;
    }

    // --- Các hàm này sẽ gắn vào sự kiện OnValueChanged của Slider / Toggle ---

    public void OnVolumeChanged()
    {
        DataManager.Instance.CurrentSettings.masterVolume = masterSlider.value;
        DataManager.Instance.CurrentSettings.musicVolume = musicSlider.value;
        DataManager.Instance.CurrentSettings.sfxVolume = sfxSlider.value;
        
        // TODO: Chèn logic gọi AudioMixer ở đây để chỉnh âm thanh thực tế
    }

    public void OnTogglesChanged()
    {
        DataManager.Instance.CurrentSettings.enableScreenShake = screenShakeToggle.isOn;
        DataManager.Instance.CurrentSettings.enableVibrate = vibrateToggle.isOn;
    }

    // --- Các hàm này gắn vào các Button tương ứng ---

    public void SetLanguageVI()
    {
        LocalizationManager.Instance.LoadLanguage("vi");
    }

    public void SetLanguageEN()
    {
        LocalizationManager.Instance.LoadLanguage("en");
    }

    public void SaveAndClose()
    {
        // Ghi xuống file JSON
        DataManager.Instance.SaveSettings();
        
        // Đóng Panel
        gameObject.SetActive(false);
    }
}