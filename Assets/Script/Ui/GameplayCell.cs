using UnityEngine;
using UnityEngine.UI;

public class GameplayCell : MonoBehaviour
{
    public int gridX;
    public int gridY;
    public bool isPlayable; // Vùng chiến sự (Đúng) hay Vùng trống (Sai)

    private Image cellImage;

    private void Awake()
    {
        cellImage = GetComponent<Image>();
    }

    public void Init(int x, int y, bool playable)
    {
        gridX = x;
        gridY = y;
        isPlayable = playable;

        // Vùng chiến sự có màu xanh dương nhạt (biển), Vùng trống thì trong suốt
        cellImage.color = isPlayable ? new Color(0.2f, 0.5f, 0.8f, 0.8f) : new Color(0, 0, 0, 0);
    }
}