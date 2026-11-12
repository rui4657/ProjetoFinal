using UnityEngine;

public class UIAudio : MonoBehaviour
{
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioClip clickClip;
    [SerializeField, Range(0f, 1f)] private float clickVolume = 1f;

    void Awake()
    {
        // Se não ligares no Inspector, tenta apanhar o 2º AudioSource
        if (!sfxSource)
        {
            var sources = GetComponents<AudioSource>();
            if (sources.Length >= 2) sfxSource = sources[1];
            else if (sources.Length == 1) sfxSource = sources[0];
        }
    }

    public void PlayClick()
    {
        if (sfxSource && clickClip)
            sfxSource.PlayOneShot(clickClip, clickVolume);
    }
}
