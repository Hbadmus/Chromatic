using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;


public class PauseMenuManager : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject pauseMenuRoot;

    [Header("Panels")]
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject soundPanel;
    [SerializeField] private GameObject controlPanel;

    [Header("Volume Sliders")]
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;

    [Header("Control Description")]
    [SerializeField] private TMP_Text controlText;
    [SerializeField] private Text legacyControlText;
    [SerializeField] private string[] controlLines =
    {
        "W / A / S / D: Move",
        "Space: Jump",
        "Left Mouse: Shoot",
        "Right Mouse: Drain",
        "F: Set current respawn point (when near active point)",
        "Esc: Pause / Continue"
    };

    public bool IsPaused { get; private set; }

    private void Awake()
    {
        // Hide all panels first so nothing is visible in edit mode
        if (mainPanel != null) mainPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (soundPanel != null) soundPanel.SetActive(false);
        if (controlPanel != null) controlPanel.SetActive(false);

        SetPaused(false, force: true);
        ShowMainPanel();
        RefreshControlText();
    }

    private void Update()
    {
        if (Keyboard.current == null) return;

        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (IsPaused) Resume();
            else Pause();
        }
    }

    private void OnDestroy()
    {
        if (IsPaused)
            Time.timeScale = 1f;
    }

    public void Pause()
    {
        SetPaused(true);
        ShowMainPanel();
    }

    public void Resume()
    {
        SetPaused(false);
    }

    public void OnContinuePressed()
    {
        Resume();
    }

    public void OnRestartPressed()
    {
        Time.timeScale = 1f;

        if (PlayerHealth.Instance != null)
        {
            Destroy(PlayerHealth.Instance.gameObject);
        }

        Scene current = SceneManager.GetActiveScene();
        SceneManager.LoadScene(current.buildIndex);
    }

    public void OnSettingsPressed()
    {
        if (mainPanel != null) mainPanel.SetActive(false);
        if (soundPanel != null) soundPanel.SetActive(false);
        if (controlPanel != null) controlPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(true);
    }

    public void OnSoundPressed()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (soundPanel != null) soundPanel.SetActive(true);
        InitVolumeSliders();
    }

    public void OnControlPressed()
    {
        if (mainPanel != null) mainPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (soundPanel != null) soundPanel.SetActive(false);
        if (controlPanel != null) controlPanel.SetActive(true);
    }

    public void OnBackPressed()
    {
        if (soundPanel != null && soundPanel.activeSelf)
        {
            soundPanel.SetActive(false);
            if (settingsPanel != null) settingsPanel.SetActive(true);
            return;
        }
        if (controlPanel != null && controlPanel.activeSelf)
        {
            controlPanel.SetActive(false);
            if (settingsPanel != null) settingsPanel.SetActive(true);
            return;
        }
        ShowMainPanel();
    }

    public void OnExitPressed()
    {
        Time.timeScale = 1f;

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void InitVolumeSliders()
    {
        if (SoundManager.Instance == null) return;
        InitSlider(masterSlider, "MasterVolume", OnMasterChanged);
        InitSlider(musicSlider,  "MusicVolume",  OnMusicChanged);
        InitSlider(sfxSlider,    "SFXVolume",    OnSFXChanged);
    }

    private void InitSlider(Slider slider, string param, UnityEngine.Events.UnityAction<float> callback)
    {
        if (slider == null) return;
        slider.onValueChanged.RemoveListener(callback);
        slider.value = SoundManager.Instance.GetVolume(param);
        slider.onValueChanged.AddListener(callback);
    }

    private void OnMasterChanged(float v) { if (SoundManager.Instance != null) SoundManager.Instance.SetVolume("MasterVolume", v); }
    private void OnMusicChanged(float v)  { if (SoundManager.Instance != null) SoundManager.Instance.SetVolume("MusicVolume",  v); }
    private void OnSFXChanged(float v)    { if (SoundManager.Instance != null) SoundManager.Instance.SetVolume("SFXVolume",    v); }

    public void RefreshControlText()
    {
        string content = string.Join("\n\n", controlLines);

        if (controlText != null)
            controlText.text = content;

        if (legacyControlText != null)
            legacyControlText.text = content;
    }

    private void ShowMainPanel()
    {
        if (mainPanel != null) mainPanel.SetActive(true);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (soundPanel != null) soundPanel.SetActive(false);
        if (controlPanel != null) controlPanel.SetActive(false);
    }

    private void SetPaused(bool paused, bool force = false)
    {
        if (!force && IsPaused == paused) return;

        IsPaused = paused;
        Time.timeScale = paused ? 0f : 1f;

        if (pauseMenuRoot != null)
            pauseMenuRoot.SetActive(paused);

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }
}
