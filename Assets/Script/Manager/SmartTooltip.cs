using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

// THÊM: IPointerMoveHandler để tooltip di chuyển theo chuột
public class SmartTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, IPointerMoveHandler
{
    [Header("Cài đặt nội dung")]
    [TextArea(2, 5)] public string tooltipContent;
    public GameObject tooltipPopup;
    public TextMeshProUGUI tooltipText;

    private RectTransform popupRect;
    private Vector2 currentPointerPos; // Biến lưu tọa độ mới

    private void Start()
    {
        if (tooltipPopup != null)
        {
            popupRect = tooltipPopup.GetComponent<RectTransform>();
            tooltipPopup.SetActive(false);
        }
    }

    public void OnPointerEnter(PointerEventData eventData) { currentPointerPos = eventData.position; ShowTooltip(); }
    public void OnPointerMove(PointerEventData eventData)  { currentPointerPos = eventData.position; if (tooltipPopup.activeSelf) UpdatePosition(); }
    public void OnPointerDown(PointerEventData eventData)  { currentPointerPos = eventData.position; ShowTooltip(); }
    
    public void OnPointerExit(PointerEventData eventData)  { HideTooltip(); }
    public void OnPointerUp(PointerEventData eventData)    { HideTooltip(); }

    private void ShowTooltip()
    {
        if (tooltipPopup == null) return;
        tooltipText.text = tooltipContent;
        tooltipPopup.SetActive(true);
        UpdatePosition();
    }

    private void HideTooltip()
    {
        if (tooltipPopup != null) tooltipPopup.SetActive(false);
    }

    private void UpdatePosition()
    {
        float offsetY = Application.isMobilePlatform ? 100f : -15f;
        float offsetX = 15f;

        float pivotX = (currentPointerPos.x / Screen.width) > 0.6f ? 1f : 0f;
        float pivotY = (currentPointerPos.y / Screen.height) > 0.6f ? 1f : 0f;

        popupRect.pivot = new Vector2(pivotX, pivotY);
        popupRect.position = currentPointerPos + new Vector2(offsetX, offsetY);
    }
}