using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MenuOptions : MonoBehaviour
{
    [Header("UI")]
    public Slider volumeSlider;          
    public Slider sensitivitySlider;     
    public Toggle fullscreenToggle;

    [Header("Textos (opcional)")]
    public TMP_Text volumeValueText;     
    public TMP_Text sensitivityValueText; 

    void Start()
    {
        // Carregar prefs
        float vol = PlayerPrefs.GetFloat("opt_volume", 1f);
        float sens = PlayerPrefs.GetFloat("opt_sens", 150f);
        bool full = PlayerPrefs.GetInt("opt_full", Screen.fullScreen ? 1 : 0) == 1;

        // Aplicar
        AudioListener.volume = Mathf.Clamp01(vol);
        Screen.fullScreen = full;

        // Atualizar UI sem disparar eventos
        if (volumeSlider) volumeSlider.SetValueWithoutNotify(vol);
        if (sensitivitySlider) sensitivitySlider.SetValueWithoutNotify(sens);
        if (fullscreenToggle) fullscreenToggle.SetIsOnWithoutNotify(full);

        UpdateTexts(vol, sens);
    }

    public void OnVolumeChanged(float v)
    {
        v = Mathf.Clamp01(v);
        PlayerPrefs.SetFloat("opt_volume", v);
        AudioListener.volume = v;
        UpdateTexts(v, sensitivitySlider ? sensitivitySlider.value : PlayerPrefs.GetFloat("opt_sens", 150f));
    }

    public void OnSensitivityChanged(float s)
    {
        PlayerPrefs.SetFloat("opt_sens", s);
        UpdateTexts(volumeSlider ? volumeSlider.value : PlayerPrefs.GetFloat("opt_volume", 1f), s);
    }

    public void OnFullscreenChanged(bool isFull)
    {
        PlayerPrefs.SetInt("opt_full", isFull ? 1 : 0);
        Screen.fullScreen = isFull;
    }

    void UpdateTexts(float vol, float sens)
    {
        if (volumeValueText) volumeValueText.text = Mathf.RoundToInt(vol * 100f) + "%";
        if (sensitivityValueText) sensitivityValueText.text = Mathf.RoundToInt(sens).ToString();
    }
}
