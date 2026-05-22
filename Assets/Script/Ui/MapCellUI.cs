using UnityEngine;
using UnityEngine.UI;

public class MapCellUI : MonoBehaviour
{
    public int x;
    public int y;
    public bool isPlayableZone = false; 

    private Image cellImage;

    private void Awake()
    {
        cellImage = GetComponent<Image>();
    }

    public void Init(int posX, int posY)
    {
        x = posX;
        y = posY;
        SetState(false);
    }

    public void SetState(bool isPlayable)
    {
        isPlayableZone = isPlayable;
        
        // Màu sắc: Vùng chiến sự (màu sáng/rõ), Vùng trống (trong suốt/mờ)
        // Bạn có thể tùy chỉnh mã màu ở đây cho hợp với theme game
        cellImage.color = isPlayableZone ? new Color(0.2f, 0.6f, 0.2f, 1f) : new Color(1f, 1f, 1f, 0.2f);
    }
}