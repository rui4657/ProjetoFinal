using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Pause and arsenal share one Escape handler, so a single press closes only one panel.
[DefaultExecutionOrder(-10)]
public class PauseMenu : MonoBehaviour
{
    [Header("Panels")]
    public GameObject pausePanel;
    public GameObject optionsPanel;
    [Header("Options UI")]
    public Slider volumeSlider;
    public Slider sensitivitySlider;
    public Toggle fullscreenToggle;
    [Header("Scene")]
    public string menuSceneName = "MainMenu";
    public bool IsPaused => isPaused;
    private bool isPaused;
    private bool listenersRegistered;
    private PlayerHealth player;
    private MouseLook mouseLook;
    private const string VOL_KEY = "opt_volume";
    private const string SENS_KEY = "opt_sens";
    private const string FULL_KEY = "opt_full";

    private void Awake()
    {
        if (pausePanel != null) pausePanel.SetActive(false);
        if (optionsPanel != null) optionsPanel.SetActive(false);
        Time.timeScale = 1f;
    }

    private void Start()
    {
        player = FindFirstObjectByType<PlayerHealth>();
        mouseLook = FindFirstObjectByType<MouseLook>();
        float volume = Mathf.Clamp01(PlayerPrefs.GetFloat(VOL_KEY, 1f));
        float sensitivity = Mathf.Clamp(PlayerPrefs.GetFloat(SENS_KEY, 100f), 1f, 500f);
        bool fullscreen = PlayerPrefs.GetInt(FULL_KEY, Screen.fullScreen ? 1 : 0) == 1;
        AudioListener.volume = volume;
        Screen.fullScreen = fullscreen;
        if (volumeSlider != null)
        {
            volumeSlider.SetValueWithoutNotify(volume);
            if (!HasPersistentListener(volumeSlider.onValueChanged, this, nameof(OnVolumeChanged)))
                volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
        }
        if (sensitivitySlider != null)
        {
            sensitivitySlider.SetValueWithoutNotify(sensitivity);
            if (!HasPersistentListener(sensitivitySlider.onValueChanged, this, nameof(OnSensitivityChanged)))
                sensitivitySlider.onValueChanged.AddListener(OnSensitivityChanged);
        }
        if (fullscreenToggle != null)
        {
            fullscreenToggle.SetIsOnWithoutNotify(fullscreen);
            if (!HasPersistentListener(fullscreenToggle.onValueChanged, this, nameof(OnFullscreenChanged)))
                fullscreenToggle.onValueChanged.AddListener(OnFullscreenChanged);
        }
        listenersRegistered = true;
    }

    private void Update()
    {
        if (GameOver()) return;
        var session = SurvivalSession.Instance;
        if (session != null && session.IsShopOpen)
        {
            if (Input.GetKeyDown(KeyCode.Escape)) session.CloseShop();
            return;
        }
        if (!Input.GetKeyDown(KeyCode.Escape)) return;
        if (isPaused && optionsPanel != null && optionsPanel.activeSelf) BackFromOptions();
        else if (isPaused) Resume();
        else Pause();
    }

    private bool GameOver()
    {
        return (player != null && player.IsDead) ||
            (SurvivalSession.Instance != null && SurvivalSession.Instance.IsGameOver);
    }

    public void Pause()
    {
        if (GameOver() || (SurvivalSession.Instance != null && SurvivalSession.Instance.IsShopOpen)) return;
        if (SaveSystem.instance != null) SaveSystem.instance.FlushSave();
        isPaused = true;
        Time.timeScale = 0f;
        if (pausePanel != null) pausePanel.SetActive(true);
        if (optionsPanel != null) optionsPanel.SetActive(false);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void Resume()
    {
        if (GameOver()) return;
        isPaused = false;
        Time.timeScale = 1f;
        if (pausePanel != null) pausePanel.SetActive(false);
        if (optionsPanel != null) optionsPanel.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        PlayerPrefs.Save();
    }

    public void OpenOptions()
    {
        if (!isPaused || GameOver()) return;
        if (pausePanel != null) pausePanel.SetActive(false);
        if (optionsPanel != null) optionsPanel.SetActive(true);
    }

    public void BackFromOptions()
    {
        if (GameOver()) return;
        if (optionsPanel != null) optionsPanel.SetActive(false);
        if (pausePanel != null) pausePanel.SetActive(true);
        PlayerPrefs.Save();
    }

    public void ExitToMenu()
    {
        if (SaveSystem.instance != null) SaveSystem.instance.FlushSave();
        var rounds = FindFirstObjectByType<RoundManager>();
        if (rounds != null) rounds.StopWaves();
        PlayerPrefs.Save();
        Time.timeScale = 1f;
        isPaused = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        SceneManager.LoadScene(menuSceneName);
    }

    public void OnVolumeChanged(float value)
    {
        value = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(VOL_KEY, value);
        AudioListener.volume = value;
    }

    public void OnSensitivityChanged(float value)
    {
        value = Mathf.Clamp(value, 1f, 500f);
        PlayerPrefs.SetFloat(SENS_KEY, value);
        if (mouseLook != null) mouseLook.mouseSensitivity = value;
    }

    public void OnFullscreenChanged(bool fullscreen)
    {
        PlayerPrefs.SetInt(FULL_KEY, fullscreen ? 1 : 0);
        Screen.fullScreen = fullscreen;
    }

    private void OnApplicationFocus(bool focused)
    {
        if (!focused && player != null && !isPaused && Time.timeScale > 0f) Pause();
    }

    private static bool HasPersistentListener(UnityEventBase events, Object target, string method)
    {
        for (int i = 0; i < events.GetPersistentEventCount(); i++)
            if (events.GetPersistentTarget(i) == target && events.GetPersistentMethodName(i) == method) return true;
        return false;
    }

    private void OnDestroy()
    {
        if (!listenersRegistered) return;
        if (volumeSlider != null) volumeSlider.onValueChanged.RemoveListener(OnVolumeChanged);
        if (sensitivitySlider != null) sensitivitySlider.onValueChanged.RemoveListener(OnSensitivityChanged);
        if (fullscreenToggle != null) fullscreenToggle.onValueChanged.RemoveListener(OnFullscreenChanged);
    }
}
