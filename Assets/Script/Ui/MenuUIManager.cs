using UnityEngine;

public class MenuUIManager : MonoBehaviour
{
    public static MenuUIManager Instance;

    [Header("UI Panels")]
    public GameObject panelMainMenu;
    public GameObject panelLobby;
    public GameObject panelRoom;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    private void Start()
    {
        ShowMainMenu(); // Mặc định hiển thị Main Menu khi vào game
    }

    private void HideAllPanels()
    {
        panelMainMenu.SetActive(false);
        panelLobby.SetActive(false);
        panelRoom.SetActive(false);
    }

    public void ShowMainMenu()
    {
        HideAllPanels();
        panelMainMenu.SetActive(true);
    }

    public void ShowLobby()
    {
        HideAllPanels();
        panelLobby.SetActive(true);
    }

    public void ShowRoom()
    {
        HideAllPanels();
        panelRoom.SetActive(true);
    }
}