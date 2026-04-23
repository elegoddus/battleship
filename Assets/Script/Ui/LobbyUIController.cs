using UnityEngine;
using TMPro; // Bắt buộc dùng TMP cho chữ
using Unity.Netcode; // Dùng để gọi lệnh Host/Client

public class LobbyUIController : MonoBehaviour
{
    [Header("Lobby Main UI")]
    public TMP_InputField inputPlayerName;

    [Header("Create Room Popup")]
    public GameObject popupCreateRoom;
    public TMP_InputField inputRoomName;
    public TMP_InputField inputPassword;

    private void OnEnable()
    {
        // Khi Panel_Lobby được bật lên, tự động điền tên cũ đã lưu vào ô nhập
        if (DataManager.Instance != null && !string.IsNullOrEmpty(DataManager.Instance.CurrentSettings.playerName))
        {
            inputPlayerName.text = DataManager.Instance.CurrentSettings.playerName;
        }
        
        // Đảm bảo Popup tạo phòng luôn tắt khi mới vào Lobby
        popupCreateRoom.SetActive(false);
    }

    // Gọi hàm này khi người chơi nhập xong tên (gắn vào sự kiện OnEndEdit)
    public void SavePlayerName()
    {
        if (DataManager.Instance != null && !string.IsNullOrEmpty(inputPlayerName.text))
        {
            DataManager.Instance.CurrentSettings.playerName = inputPlayerName.text;
            DataManager.Instance.SaveSettings();
            Debug.Log("Đã lưu tên: " + inputPlayerName.text);
        }
    }

    // --- CÁC NÚT Ở LOBBY CHÍNH ---

    public void OpenCreateRoomPopup()
    {
        popupCreateRoom.SetActive(true);
    }

    public void OnClickFindRoom()
    {
        SavePlayerName(); // Lưu tên trước khi vào trận
        Debug.Log("Đang tìm phòng LAN để kết nối...");
        
        // Gọi lệnh Client của Netcode
        if (NetworkManager.Singleton.StartClient())
        {
            MenuUIManager.Instance.ShowRoom(); // Chuyển sang Panel_Room
        }
        else
        {
            Debug.LogError("Lỗi: Không thể kết nối vào phòng!");
        }
    }

    // --- CÁC NÚT TRONG POPUP TẠO PHÒNG ---

    public void CloseCreateRoomPopup()
    {
        popupCreateRoom.SetActive(false);
    }

    public void ConfirmCreateRoom()
    {
        SavePlayerName(); // Lưu tên
        
        // MVP: Tạm thời lấy text ra log để check xem ô nhập liệu có hoạt động không
        string rName = inputRoomName.text;
        string rPass = inputPassword.text;
        Debug.Log($"Đang khởi tạo phòng: {rName} | Mật khẩu: {rPass}");

        // Gọi lệnh Host của Netcode
        if (NetworkManager.Singleton.StartHost())
        {
            popupCreateRoom.SetActive(false);
            MenuUIManager.Instance.ShowRoom(); // Chuyển sang Panel_Room
        }
        else
        {
            Debug.LogError("Lỗi: Không thể khởi tạo Máy chủ!");
        }
    }
}