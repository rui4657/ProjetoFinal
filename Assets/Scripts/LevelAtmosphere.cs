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
    private Light[] practicalLights;
    private ParticleSystem dust;
    private Material dustMaterial;
    private Texture2D dustTexture;
    private Material skyMaterial;
    private Material originalSkybox;
    private AudioSource windSource;
    private AudioClip windClip;
    private float nextSettingsPoll;
    private float ambienceVolume;
    private bool motionEnabled;

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
        AddPracticalLights();
        CreateDust();
        CreateWind();
    }

    private void ApplyLighting()
    {
        // The authored scene used linear fog from 0 to 20 metres. Open the
        // combat space while keeping haze beyond the village's useful sightline.
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = new Color(0.29f, 0.34f, 0.36f);
        RenderSettings.fogStartDistance = 38f;
        RenderSettings.fogEndDistance = 145f;

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
            // Cool daylight keeps silhouettes visible; warm practicals provide
            // local contrast. The previous direct light was ~0.03 after tint.
            sun.color = new Color(0.84f, 0.89f, 0.95f);
            sun.intensity = 0.88f;
            sun.transform.rotation = Quaternion.Euler(28f, -32f, 0f);
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

    private void AddPracticalLights()
    {
        practicalLights = new Light[2];
        Transform church = FindLandmark("Pref_Church1_B");
        if (church != null)
        {
            // Existing church doorway, verified against MainScene coordinates:
            // root (455.61,17.55,579.55), entrance ~455.97 / 563.28.
            Vector3 position = church.TransformPoint(new Vector3(0.36f, 2.45f, -16.27f));
            practicalLights[0] = MakePractical("Church doorway glow", position,
                new Color(1f, 0.69f, 0.40f), 2.3f, 13f);
        }

        Transform guardHouse = FindLandmark("GuardHouse");
        if (guardHouse != null)
        {
            // Follow the existing façade facing the initial player position,
            // rather than assuming the building's pivot is at its doorway.
            Vector3 position = guardHouse.position + Vector3.up * 2.7f;
            Renderer[] renderers = guardHouse.GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                Bounds bounds = renderers[0].bounds;
                foreach (Renderer renderer in renderers) bounds.Encapsulate(renderer.bounds);
                Vector3 towardsPlayer = player.position - bounds.center;
                towardsPlayer.y = 0f;
                if (towardsPlayer.sqrMagnitude < 0.01f) towardsPlayer = Vector3.forward;
                towardsPlayer.Normalize();
                Vector3 closest = bounds.ClosestPoint(new Vector3(player.position.x,
                    bounds.center.y, player.position.z));
                position = new Vector3(closest.x, bounds.min.y + 2.7f, closest.z)
                    + towardsPlayer * 0.55f;
            }
            practicalLights[1] = MakePractical("Guardhouse doorway glow", position,
                new Color(1f, 0.78f, 0.51f), 1.8f, 11f);
        }
    }

    private Transform FindLandmark(string objectName)
    {
        foreach (GameObject root in gameObject.scene.GetRootGameObjects())
            foreach (Transform candidate in root.GetComponentsInChildren<Transform>(true))
                if (candidate.name == objectName && candidate.gameObject.activeInHierarchy)
                    return candidate;
        return null;
    }

    private Light MakePractical(string objectName, Vector3 position, Color color, float intensity, float range)
    {
        GameObject lamp = new GameObject(objectName);
        lamp.transform.SetParent(transform, false);
        lamp.transform.position = position;
        Light light = lamp.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = color;
        light.intensity = intensity;
        light.range = range;
        light.shadows = LightShadows.None;
        light.renderMode = LightRenderMode.Auto;
        light.bounceIntensity = 0f;
        return light;
    }

    private void CreateDust()
    {
        Shader shader = Shader.Find("Particles/Standard Unlit");
        if (shader == null) shader = Shader.Find("Legacy Shaders/Particles/Alpha Blended");
        if (shader == null) return; // Optional effect; lighting works without it.

        dustTexture = new Texture2D(32, 32, TextureFormat.RGBA32, false);
        dustTexture.name = "Soft dust (runtime)";
        dustTexture.wrapMode = TextureWrapMode.Clamp;
        for (int y = 0; y < dustTexture.height; y++)
        {
            for (int x = 0; x < dustTexture.width; x++)
            {
                float radius = new Vector2((x - 15.5f) / 15.5f, (y - 15.5f) / 15.5f).magnitude;
                float alpha = Mathf.Pow(Mathf.Clamp01(1f - radius), 2f);
                dustTexture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        dustTexture.Apply();

        dustMaterial = new Material(shader);
        dustMaterial.name = "Village Dust (runtime)";
        dustMaterial.mainTexture = dustTexture;
        if (dustMaterial.HasProperty("_Mode")) dustMaterial.SetFloat("_Mode", 2f);
        if (dustMaterial.HasProperty("_SrcBlend")) dustMaterial.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
        if (dustMaterial.HasProperty("_DstBlend")) dustMaterial.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
        if (dustMaterial.HasProperty("_ZWrite")) dustMaterial.SetInt("_ZWrite", 0);
        dustMaterial.EnableKeyword("_ALPHABLEND_ON");
        dustMaterial.renderQueue = (int)RenderQueue.Transparent;

        GameObject dustObject = new GameObject("Village airborne dust");
        dustObject.transform.SetParent(transform, false);
        dustObject.transform.position = player.position + Vector3.up * 3f;
        dust = dustObject.AddComponent<ParticleSystem>();
        dust.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ParticleSystem.MainModule main = dust.main;
        main.loop = true;
        main.playOnAwake = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(9f, 14f);
        main.startSpeed = 0.025f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.035f, 0.075f);
        main.startColor = new Color(0.82f, 0.87f, 0.85f, 0.16f);
        main.maxParticles = 36;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.cullingMode = ParticleSystemCullingMode.Pause;

        ParticleSystem.EmissionModule emission = dust.emission;
        emission.rateOverTime = 2.2f;
        ParticleSystem.ShapeModule shape = dust.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(22f, 7f, 22f);
        ParticleSystem.VelocityOverLifetimeModule velocity = dust.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.World;
        velocity.x = 0.055f;
        velocity.y = 0.018f;
        velocity.z = 0.025f;

        Gradient fade = new Gradient();
        fade.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f),
                new GradientAlphaKey(1f, 0.75f), new GradientAlphaKey(0f, 1f) });
        ParticleSystem.ColorOverLifetimeModule color = dust.colorOverLifetime;
        color.enabled = true;
        color.color = new ParticleSystem.MinMaxGradient(fade);

        ParticleSystemRenderer renderer = dust.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = dustMaterial;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.maxParticleSize = 0.015f;
        if (motionEnabled) dust.Play();
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
        ambienceVolume = 0.12f * Mathf.Clamp01(PlayerPrefs.GetFloat("opt_volume", 1f));
        motionEnabled = PlayerPrefs.GetInt("opt_motion", 1) != 0;
        if (dust != null)
        {
            if (motionEnabled && !dust.isPlaying) dust.Play();
            else if (!motionEnabled && dust.isPlaying)
                dust.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    private void Update()
    {
        if (player == null || playerCamera == null) return;
        if (Time.unscaledTime >= nextSettingsPoll)
        {
            nextSettingsPoll = Time.unscaledTime + 0.5f;
            ReadSettings();
            if (dust != null)
                dust.transform.position = player.position + Vector3.up * 3f;
            if (practicalLights != null)
                foreach (Light lamp in practicalLights)
                    if (lamp != null)
                        lamp.enabled = (lamp.transform.position - playerCamera.transform.position).sqrMagnitude < 65f * 65f;
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
        if (dustMaterial != null) Destroy(dustMaterial);
        if (dustTexture != null) Destroy(dustTexture);
        if (windClip != null) Destroy(windClip);
    }
}
