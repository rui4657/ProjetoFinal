using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

/// <summary>
/// Scene-local lighting, distance haze and quiet environmental detail for the
/// existing village. SurvivalSession installs this explicitly for gameplay.
/// No scene assets, surface materials or render-pipeline settings are changed.
/// </summary>
[DisallowMultipleComponent]
public sealed class LevelAtmosphere : MonoBehaviour
{
    private Camera playerCamera;
    private Transform player;
    private Material skyMaterial;
    private Material originalSkybox;
    private AudioSource windSource;
    private AudioClip windClip;
    private float nextSettingsPoll;
    private float ambienceVolume;

    /// <summary>Returns the existing scene-local atmosphere, or installs one.</summary>
    public static LevelAtmosphere Ensure()
    {
        PlayerHealth health = FindFirstObjectByType<PlayerHealth>();
        if (health == null) return null;

        Scene gameplay = health.gameObject.scene;
        foreach (LevelAtmosphere atmosphere in FindObjectsByType<LevelAtmosphere>(FindObjectsSortMode.None))
            if (atmosphere.gameObject.scene == gameplay) return atmosphere;

        GameObject owner = new GameObject("Village Atmosphere");
        SceneManager.MoveGameObjectToScene(owner, gameplay);
        return owner.AddComponent<LevelAtmosphere>();
    }

    private void Start()
    {
        PlayerHealth health = FindFirstObjectByType<PlayerHealth>();
        if (health == null || health.gameObject.scene != gameObject.scene)
        {
            Destroy(gameObject);
            return;
        }

        player = health.transform;
        playerCamera = health.GetComponentInChildren<Camera>(true);
        if (playerCamera == null) return;

        ReadSettings();
        ApplyLighting();
        ApplyCamera();
        CreateWind();
    }

    private void ApplyLighting()
    {
        // The authored scene used linear fog from 0 to 20 metres. Open the
        // combat space while keeping haze beyond the village's useful sightline.
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = new Color(0.32f, 0.35f, 0.37f);
        RenderSettings.fogStartDistance = 55f;
        RenderSettings.fogEndDistance = 175f;

        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.37f, 0.43f, 0.48f);
        RenderSettings.ambientEquatorColor = new Color(0.23f, 0.27f, 0.29f);
        RenderSettings.ambientGroundColor = new Color(0.12f, 0.13f, 0.14f);
        RenderSettings.ambientIntensity = 1f;
        RenderSettings.reflectionIntensity = 0.65f;

        Light sun = RenderSettings.sun;
        if (sun == null || sun.gameObject.scene != gameObject.scene)
        {
            foreach (Light candidate in FindObjectsByType<Light>(FindObjectsSortMode.None))
            {
                if (candidate.type != LightType.Directional || candidate.gameObject.scene != gameObject.scene)
                    continue;
                sun = candidate;
                break;
            }
        }
        if (sun != null)
        {
            // Preserve the authored sun direction; a restrained neutral profile keeps enemies legible.
            sun.color = new Color(0.84f, 0.89f, 0.95f);
            sun.intensity = 0.62f;
            sun.shadows = LightShadows.Hard;
            sun.shadowStrength = 0.72f;
            sun.shadowBias = 0.04f;
            sun.shadowNormalBias = 0.35f;
            RenderSettings.sun = sun;
        }

        originalSkybox = RenderSettings.skybox;
        if (originalSkybox != null)
        {
            skyMaterial = new Material(originalSkybox);
            skyMaterial.name = "Village Sky (runtime)";
            // Only tune properties supported by the existing sky shader.
            if (skyMaterial.HasProperty("_Exposure")) skyMaterial.SetFloat("_Exposure", 0.85f);
            if (skyMaterial.HasProperty("_SkyTint"))
                skyMaterial.SetColor("_SkyTint", new Color(0.42f, 0.49f, 0.56f));
            if (skyMaterial.HasProperty("_GroundColor"))
                skyMaterial.SetColor("_GroundColor", new Color(0.18f, 0.20f, 0.21f));
            if (skyMaterial.HasProperty("_AtmosphereThickness"))
                skyMaterial.SetFloat("_AtmosphereThickness", 1.1f);
            RenderSettings.skybox = skyMaterial;
        }
        DynamicGI.UpdateEnvironment();
    }

    private void ApplyCamera()
    {
        // The weapon/camera is close to walls in this map. A shorter near plane
        // avoids slicing the held weapon; preserve its authored FOV and motion.
        playerCamera.nearClipPlane = Mathf.Min(playerCamera.nearClipPlane, 0.08f);
        playerCamera.farClipPlane = Mathf.Clamp(playerCamera.farClipPlane, 180f, 300f);
        playerCamera.backgroundColor = RenderSettings.fogColor;
    }

    private void CreateWind()
    {
        // Filtered noise instead of combat-masking music. A deterministic local
        // RNG leaves zombie/spawn randomness untouched. Crossfade the loop seam.
        const int rate = 11025;
        const int count = rate * 12;
        const int crossfade = rate / 2;
        float[] raw = new float[count + crossfade];
        System.Random random = new System.Random(2718);
        float low = 0f;
        float air = 0f;
        for (int i = 0; i < raw.Length; i++)
        {
            float noise = (float)random.NextDouble() * 2f - 1f;
            low += (noise - low) * 0.012f;
            air += (noise - air) * 0.075f;
            float gust = 0.65f + 0.2f * Mathf.Sin(2f * Mathf.PI * i / (rate * 6f));
            raw[i] = (low * 0.55f + air * 0.16f) * gust;
        }
        float[] samples = new float[count];
        System.Array.Copy(raw, samples, count);
        for (int i = 0; i < crossfade; i++)
            samples[i] = Mathf.Lerp(raw[count + i], raw[i], i / (float)crossfade);

        windClip = AudioClip.Create("Village distant wind", count, 1, rate, false);
        windClip.SetData(samples, 0);
        windSource = gameObject.AddComponent<AudioSource>();
        windSource.playOnAwake = false;
        windSource.clip = windClip;
        windSource.loop = true;
        windSource.spatialBlend = 0f;
        windSource.volume = 0f;
        windSource.priority = 224;
        windSource.dopplerLevel = 0f;
        windSource.ignoreListenerVolume = false;
        windSource.Play();
    }

    private void ReadSettings()
    {
        ambienceVolume = 0.08f; // AudioListener already applies the master-volume preference.

    }

    private void Update()
    {
        if (player == null || playerCamera == null) return;
        if (Time.unscaledTime >= nextSettingsPoll)
        {
            nextSettingsPoll = Time.unscaledTime + 0.5f;
            ReadSettings();

        }
        if (windSource != null)
        {
            float target = Time.timeScale > 0f ? ambienceVolume : 0f;
            windSource.volume = Mathf.MoveTowards(windSource.volume, target, Time.unscaledDeltaTime * 0.04f);
        }
    }

    private void OnDestroy()
    {
        // Unity loads each scene's RenderSettings during scene changes. Only
        // release this component's transient assets, never modify asset files.
        if (skyMaterial != null)
        {
            if (RenderSettings.skybox == skyMaterial) RenderSettings.skybox = originalSkybox;
            Destroy(skyMaterial);
        }
        if (windClip != null) Destroy(windClip);
    }
}
