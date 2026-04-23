using Unity.Netcode;
using UnityEngine;

public class MainMenuController : MonoBehaviour
{
    public void StartHost()
    {
        if (NetworkManager.Singleton.StartHost())
        {
            Debug.Log("Đã tạo phòng thành công! Đang chờ người chơi...");
            // TODO: Bật Panel chứa các nút chỉnh Custom Map lên
        }
        else
        {
            Debug.Log("Lỗi: Không thể tạo phòng.");
        }
    }

    public void StartClient()
    {
        if (NetworkManager.Singleton.StartClient())
        {
            Debug.Log("Đang kết nối vào phòng...");
            // TODO: Bật Panel chữ "Đang chờ Host bắt đầu..."
        }
        else
        {
            Debug.Log("Lỗi: Không thể kết nối.");
        }
    }
}