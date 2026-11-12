using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseMenu : MonoBehaviour
{
    [Header("Panels")]
    public GameObject pausePanel;     // PausePanel (Resume/Options/Exit)
    public GameObject optionsPanel;   // PauseOptionsPanel (sliders/toggle/back)

    [Header("Options UI (arrasta do teu options panel)")]
    public Slider volumeSlider;          // 0..1
    public Slider sensitivitySlider;     // ex: 50..300 (tu defines no Inspector)
    public Toggle fullscreenToggle;

    [Header("Scene")]
    public string menuSceneName = "MainMenu"; // mete aqui o nome exato da cena do menu

    private bool isPaused;

    const string VOL_KEY = "opt_volume";
    const string SENS_KEY = "opt_sens";
    const string FULL_KEY = "opt_full";

    void Awake()
    {
        // garantir que começa fechado
        if (pausePanel) pausePanel.SetActive(false);
        if (optionsPanel) optionsPanel.SetActive(false);

        Time.timeScale = 1f;
        isPaused = false;
    }

    void Start()
    {
        // Carregar prefs
        float vol = PlayerPrefs.GetFloat(VOL_KEY, 1f);
        float sens = PlayerPrefs.GetFloat(SENS_KEY, 100f);
        bool full = PlayerPrefs.GetInt(FULL_KEY, Screen.fullScreen ? 1 : 0) == 1;

        // Aplicar já
        AudioListener.volume = Mathf.Clamp01(vol);
        Screen.fullScreen = full;

        // Meter valores na UI SEM disparar listeners (para evitar bugs)
        if (volumeSlider)
        {
            volumeSlider.SetValueWithoutNotify(vol);
            volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
        }

        if (sensitivitySlider)
        {
            sensitivitySlider.SetValueWithoutNotify(sens);
            sensitivitySlider.onValueChanged.AddListener(OnSensitivityChanged);
        }

        if (fullscreenToggle)
        {
            fullscreenToggle.SetIsOnWithoutNotify(full);
            fullscreenToggle.onValueChanged.AddListener(OnFullscreenChanged);
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // Se estiver nas options, ESC volta ao pause panel
            if (optionsPanel && optionsPanel.activeSelf)
            {
                BackFromOptions();
                return;
            }

            if (isPaused) Resume();
            else Pause();
        }
    }

    public void Pause()
    {
        isPaused = true;
        Time.timeScale = 0f;

        if (pausePanel) pausePanel.SetActive(true);
        if (optionsPanel) optionsPanel.SetActive(false);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void Resume()
    {
        isPaused = false;
        Time.timeScale = 1f;

        if (pausePanel) pausePanel.SetActive(false);
        if (optionsPanel) optionsPanel.SetActive(false);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void OpenOptions()
    {
        if (pausePanel) pausePanel.SetActive(false);
        if (optionsPanel) optionsPanel.SetActive(true);
    }

    public void BackFromOptions()
    {
        if (optionsPanel) optionsPanel.SetActive(false);
        if (pausePanel) pausePanel.SetActive(true);
    }

    public void ExitToMenu()
    {
        Time.timeScale = 1f;
        isPaused = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        SceneManager.LoadScene(menuSceneName);
    }

    // -------- Options Handlers --------
    public void OnVolumeChanged(float v)
    {
        v = Mathf.Clamp01(v);
        PlayerPrefs.SetFloat(VOL_KEY, v);
        AudioListener.volume = v;
    }

    public void OnSensitivityChanged(float s)
    {
        PlayerPrefs.SetFloat(SENS_KEY, s);

        // opcional: atualizar já no MouseLook (se existir)
        var ml = FindFirstObjectByType<MouseLook>();
        if (ml != null) ml.mouseSensitivity = s;
    }

    public void OnFullscreenChanged(bool isFull)
    {
        PlayerPrefs.SetInt(FULL_KEY, isFull ? 1 : 0);
        Screen.fullScreen = isFull;
    }
}
