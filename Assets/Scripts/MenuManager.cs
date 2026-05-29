using UnityEngine;
using UnityEngine.UI;

public class MenuManager : MonoBehaviour
{
    public GameObject mainPanel;
    public GameObject controlsPanel;
    public Button playButton;
    public Button controlsButton;
    public Button backButton;

    public TankController playerTank;

    void Start()
    {
        if (playerTank != null) playerTank.enabled = false;
        
        playButton.onClick.AddListener(PlayGame);
        controlsButton.onClick.AddListener(ShowControls);
        backButton.onClick.AddListener(ShowMain);
        
        ShowMain();
        
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    void PlayGame()
    {
        mainPanel.SetActive(false);
        if (playerTank != null) playerTank.enabled = true;
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    void ShowControls()
    {
        mainPanel.SetActive(false);
        controlsPanel.SetActive(true);
    }

    void ShowMain()
    {
        mainPanel.SetActive(true);
        controlsPanel.SetActive(false);
    }
}
