using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

public class MenuOptions : MonoBehaviour
{
    [Header("UI")]
    public Slider volumeSlider;
    public Slider sensitivitySlider;
    public Toggle fullscreenToggle;
    [Header("Value labels")]
    public TMP_Text volumeValueText;
    public TMP_Text sensitivityValueText;
    private bool listenersRegistered;

    private void Start()
    {
        float volume = Mathf.Clamp01(PlayerPrefs.GetFloat("opt_volume", 1f));
        float sensitivity = Mathf.Clamp(PlayerPrefs.GetFloat("opt_sens", 100f), 1f, 500f);
        bool fullscreen = PlayerPrefs.GetInt("opt_full", Screen.fullScreen ? 1 : 0) == 1;
        AudioListener.volume = volume;
        Screen.fullScreen = fullscreen;
        if (volumeSlider != null)
        {
            volumeSlider.SetValueWithoutNotify(volume);
            if (!HasPersistentListener(volumeSlider.onValueChanged, nameof(OnVolumeChanged)))
                volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
        }
        if (sensitivitySlider != null)
        {
            sensitivitySlider.SetValueWithoutNotify(sensitivity);
            if (!HasPersistentListener(sensitivitySlider.onValueChanged, nameof(OnSensitivityChanged)))
                sensitivitySlider.onValueChanged.AddListener(OnSensitivityChanged);
        }
        if (fullscreenToggle != null)
        {
            fullscreenToggle.SetIsOnWithoutNotify(fullscreen);
            if (!HasPersistentListener(fullscreenToggle.onValueChanged, nameof(OnFullscreenChanged)))
                fullscreenToggle.onValueChanged.AddListener(OnFullscreenChanged);
        }
        listenersRegistered = true;
        UpdateTexts(volume, sensitivity);
    }

    public void OnVolumeChanged(float value)
    {
        value = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat("opt_volume", value);
        AudioListener.volume = value;
        UpdateTexts(value, PlayerPrefs.GetFloat("opt_sens", 100f));
    }

    public void OnSensitivityChanged(float value)
    {
        value = Mathf.Clamp(value, 1f, 500f);
        PlayerPrefs.SetFloat("opt_sens", value);
        var look = FindFirstObjectByType<MouseLook>();
        if (look != null) look.mouseSensitivity = value;
        UpdateTexts(PlayerPrefs.GetFloat("opt_volume", 1f), value);
    }

    public void OnFullscreenChanged(bool fullscreen)
    {
        PlayerPrefs.SetInt("opt_full", fullscreen ? 1 : 0);
        Screen.fullScreen = fullscreen;
    }

    private void UpdateTexts(float volume, float sensitivity)
    {
        if (volumeValueText != null) volumeValueText.text = Mathf.RoundToInt(volume * 100f) + "%";
        if (sensitivityValueText != null) sensitivityValueText.text = Mathf.RoundToInt(sensitivity).ToString();
    }

    private bool HasPersistentListener(UnityEventBase events, string method)
    {
        for (int i = 0; i < events.GetPersistentEventCount(); i++)
            if (events.GetPersistentTarget(i) == this && events.GetPersistentMethodName(i) == method) return true;
        return false;
    }

    private void OnDisable() { PlayerPrefs.Save(); }
    private void OnDestroy()
    {
        if (!listenersRegistered) return;
        if (volumeSlider != null) volumeSlider.onValueChanged.RemoveListener(OnVolumeChanged);
        if (sensitivitySlider != null) sensitivitySlider.onValueChanged.RemoveListener(OnSensitivityChanged);
        if (fullscreenToggle != null) fullscreenToggle.onValueChanged.RemoveListener(OnFullscreenChanged);
    }
}
