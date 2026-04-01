using UnityEngine;

public class WinPanelManager : MonoBehaviour
{
    public static WinPanelManager Instance;

    [SerializeField] private GameObject winPanel;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (winPanel != null)
        {
            winPanel.SetActive(false);
        }
    }

    public void ShowWinPanel()
    {
        if (winPanel == null) return;

        winPanel.SetActive(true);
        Time.timeScale = 0f;
        AudioListener.pause = true;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void HideWinPanel()
    {
        if (winPanel != null)
        {
            winPanel.SetActive(false);
        }

        Time.timeScale = 1f;
        AudioListener.pause = false;
    }
}
