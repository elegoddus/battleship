using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class MapEditorInputHandler : MonoBehaviour
{
    public MapEditorController controller;
    public RectTransform viewport; // Khung nhìn (mặt nạ)
    public RectTransform gridContainer;

    [Header("Cấu hình di chuyển")]
    public float panSpeed = 1.0f;
    public float zoomSpeed = 0.05f;
    public float autoZoomOutStep = 0.002f; // Tốc độ zoom out tự động khi vẽ sát biên

    private Vector2 lastMousePos;

    void Update()
    {
        if (Application.isMobilePlatform) HandleMobile();
        else HandlePC();
    }

    private void HandlePC()
{
    var mouse = Mouse.current;
    var kb = Keyboard.current;
    Vector2 mousePos = mouse.position.ReadValue();

    // --- LOGIC CHUYỂN CHẾ ĐỘ TỰ ĐỘNG ---
    
    // 1. Ưu tiên cao nhất: Chuột phải = Kéo (Pan)
    if (mouse.rightButton.isPressed)
    {
        SetTool(false, false, true); // Tắt Vẽ, Tắt Xóa, Bật Kéo
    }
    // 2. Ưu tiên 2: Giữ Shift = Xóa
    else if (kb.shiftKey.isPressed)
    {
        SetTool(false, true, false); // Bật Xóa
    }
    // 3. Mặc định: Vẽ
    else
    {
        // Chỉ tự động về Vẽ nếu đang không nhấn Chuột phải hoặc Shift
        SetTool(true, false, false);
    }

    // --- THỰC THI HÀNH ĐỘNG ---

    // Zoom (Con lăn)
    float scroll = mouse.scroll.ReadValue().y;
    if (scroll != 0) ApplyZoom(scroll * zoomSpeed);

    // Di chuyển (Nếu đang ở chế độ Pan)
    if (controller.toolPan.isOn && mouse.rightButton.isPressed)
    {
        Vector2 delta = mousePos - lastMousePos;
        gridContainer.anchoredPosition += delta * panSpeed;
    }

    // Vẽ hoặc Xóa (Nếu nhấn Chuột trái)
    if (mouse.leftButton.isPressed && !controller.toolPan.isOn)
    {
        ExecuteActionAtPos(mousePos);
        CheckAutoZoomOut(mousePos); // Sẽ tăng tốc độ ở hàm dưới
    }

    lastMousePos = mousePos;
}


    private void SetTool(bool pen, bool eraser, bool pan)
{
    if(controller.toolPen.isOn != pen) controller.toolPen.isOn = pen;
    if(controller.toolEraser.isOn != eraser) controller.toolEraser.isOn = eraser;
    if(controller.toolPan.isOn != pan) controller.toolPan.isOn = pan;
}

    private void HandleMobile()
    {
        if (Input.touchCount == 1)
        {
            Touch touch = Input.GetTouch(0);
            if (controller.toolPan.isOn)
            {
                gridContainer.anchoredPosition += touch.deltaPosition * panSpeed;
            }
            else
            {
                ExecuteActionAtPos(touch.position);
                CheckAutoZoomOut(touch.position);
            }
        }
        else if (Input.touchCount == 2)
        {
            // Xử lý Pinch Zoom như cũ
        }
    }

    private void ExecuteActionAtPos(Vector2 pos)
    {
        PointerEventData eventData = new PointerEventData(EventSystem.current) { position = pos };
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        foreach (var res in results)
        {
            var cell = res.gameObject.GetComponent<MapCellUI>();
            if (cell)
            {
                controller.ExecuteDrawAction(cell);
                break;
            }
        }
    }

    private void CheckAutoZoomOut(Vector2 pos)
    {
        // Lấy tọa độ chuột trong không gian của Viewport
        RectTransformUtility.ScreenPointToLocalPointInRectangle(viewport, pos, null, out Vector2 localPos);
        
        Rect r = viewport.rect;
        float margin = 80f; // Khoảng cách tới biên để kích hoạt zoom out

        // Nếu chuột gần sát 4 cạnh của Viewport khi đang vẽ
        if (localPos.x < r.xMin + margin || localPos.x > r.xMax - margin ||
            localPos.y < r.yMin + margin || localPos.y > r.yMax - margin)
        {
            ApplyZoom(-0.01f); // Tự động thu nhỏ lại để thấy vùng rộng hơn
        }
    }

    private void ApplyZoom(float delta)
    {
        float newScale = Mathf.Clamp(gridContainer.localScale.x + delta, 0.3f, 2.0f);
        gridContainer.localScale = new Vector3(newScale, newScale, 1f);
    }
}