using UnityEngine;

/// <summary>Subtle generated interface cues; no external sound files required.</summary>
public sealed class SurvivalAudio : MonoBehaviour
{
    AudioSource source;
    AudioClip hit, kill, round, purchase;

    void Awake()
    {
        source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        source.volume = 0.28f;
        hit = Tone("Hit confirmation", 0.045f, 1250, 850);
        kill = Tone("Elimination", 0.09f, 740, 1480);
        round = Tone("Round complete", 0.45f, 220, 440);
        purchase = Tone("Upgrade", 0.18f, 440, 880);
    }

    public void Hit(bool killed, bool headshot)
    {
        source.pitch = headshot ? 1.2f : 1f;
        source.PlayOneShot(killed ? kill : hit, killed ? 0.7f : 0.4f);
    }

    public void RoundComplete() { source.pitch = 1; source.PlayOneShot(round, 0.8f); }
    public void Purchase() { source.pitch = 1; source.PlayOneShot(purchase, 0.65f); }

    static AudioClip Tone(string name, float seconds, float from, float to)
    {
        const int rate = 22050;
        float[] data = new float[Mathf.CeilToInt(rate * seconds)];
        float phase = 0;
        for (int i = 0; i < data.Length; i++)
        {
            float progress = i / (float)data.Length;
            phase += 2 * Mathf.PI * Mathf.Lerp(from, to, progress) / rate;
            float envelope = Mathf.Sin(Mathf.PI * progress) * (1 - progress);
            data[i] = Mathf.Sin(phase) * envelope * 0.3f;
        }
        AudioClip clip = AudioClip.Create(name, data.Length, 1, rate, false);
        clip.SetData(data, 0);
        return clip;
    }

    void OnDestroy()
    {
        if (hit != null) Destroy(hit);
        if (kill != null) Destroy(kill);
        if (round != null) Destroy(round);
        if (purchase != null) Destroy(purchase);
    }
}
