using UnityEngine;

public sealed class WeaponVisuals : MonoBehaviour
{
    private GunSystem gun;
    private Transform authored, authoredMuzzle, pump, slide;
    private readonly Transform[] models = new Transform[4];
    private readonly AudioClip[] shots = new AudioClip[4];
    private AudioClip authoredShot, authoredReload, mechanical;
    private Material steel, polymer, detail, flashMaterial;
    private Transform flash;
    private float flashUntil, firedAt = -100;
    private Vector3 pumpRest, slideRest;
    private bool pumpPlayed;
    private bool ready;

    public void Initialize(GunSystem owner)
    {
        gun = owner; if (ready || gun == null || gun.playerCam == null) return;
        authored = gun.ViewMotion != null ? gun.ViewMotion.viewModelRoot : null;
        authoredMuzzle = gun.muzzlePoint; authoredShot = gun.shotClip; authoredReload = gun.reloadClip;
        steel = WeaponModels.Material(new Color(.15f, .19f, .21f), .8f);
        polymer = WeaponModels.Material(new Color(.055f, .065f, .06f), .1f);
        detail = WeaponModels.Material(new Color(.31f, .29f, .22f), .7f);
        flashMaterial = WeaponModels.Material(new Color(1f, .62f, .12f), 0);
        if (flashMaterial != null) { flashMaterial.EnableKeyword("_EMISSION"); flashMaterial.SetColor("_EmissionColor", new Color(1f, .4f, .04f) * 3); }
        Transform parent = gun.ViewMotion != null && gun.ViewMotion.MotionPivot != null ? gun.ViewMotion.MotionPivot : gun.playerCam.transform;
        for (int i = 0; i < models.Length; i++)
        {
            if (i == (int)WeaponId.Rifle && authored != null) continue;
            models[i] = WeaponModels.Create((WeaponId)i, parent, steel, polymer, detail);
            models[i].localPosition = new Vector3(.2f, -.22f, .42f); models[i].gameObject.SetActive(false);
            foreach (var child in models[i].GetComponentsInChildren<Transform>(true)) child.gameObject.layer = 2;
            shots[i] = MakeShot((WeaponId)i);
        }
        mechanical = MakeMechanical();
        if (gun.sfxSource == null) gun.sfxSource = gameObject.AddComponent<AudioSource>();
        gun.sfxSource.playOnAwake = false; gun.sfxSource.spatialBlend = 0;
        ready = true; Show(gun.ActiveWeapon);
    }

    public void Show(WeaponId id)
    {
        if (!ready) return;
        if (flash != null) { Destroy(flash.gameObject); flash = null; }
        if (authored != null) authored.gameObject.SetActive(id == WeaponId.Rifle);
        for (int i = 0; i < models.Length; i++) if (models[i] != null) models[i].gameObject.SetActive(i == (int)id);
        Transform model = id == WeaponId.Rifle && authored != null ? authored : models[(int)id];
        gun.muzzlePoint = id == WeaponId.Rifle && authoredMuzzle != null ? authoredMuzzle : model.Find("Muzzle");
        gun.shotClip = id == WeaponId.Rifle && authoredShot != null ? authoredShot : shots[(int)id];
        gun.reloadClip = id == WeaponId.Rifle && authoredReload != null ? authoredReload : mechanical;
        if (gun.ViewMotion != null) gun.ViewMotion.BindModel(model);
        pump = model.Find("Pump"); if (pump != null) pumpRest = pump.localPosition;
        slide = model.Find("Slide"); if (slide != null) slideRest = slide.localPosition;
        if (id != WeaponId.Rifle && gun.muzzlePoint != null)
        {
            flash = WeaponModels.Piece(gun.muzzlePoint, "Muzzle flash", PrimitiveType.Sphere,
                new Vector3(0, 0, .025f), new Vector3(.045f, .045f, .09f), flashMaterial);
            flash.gameObject.SetActive(false);
        }
    }
    public void KickShot()
    {
        firedAt = Time.time; flashUntil = Time.time + .045f; pumpPlayed = false;
        if (flash != null) flash.gameObject.SetActive(true);
    }
    private void Update()
    {
        if (flash != null && Time.time >= flashUntil) flash.gameObject.SetActive(false);
        if (slide != null && Time.timeScale > 0)
        {
            float t = (Time.time - firedAt) / .13f;
            slide.localPosition = slideRest - Vector3.forward * (t >= 0 && t < 1 ? Mathf.Sin(t * Mathf.PI) * .025f : 0);
        }
        if (pump != null && Time.timeScale > 0)
        {
            float t = (Time.time - firedAt - .12f) / .5f;
            pump.localPosition = pumpRest - Vector3.forward * (t >= 0 && t < 1 ? Mathf.Sin(t * Mathf.PI) * .09f : 0);
            if (t >= 0 && t < 1 && !pumpPlayed && gun != null && gun.sfxSource != null)
            { gun.sfxSource.PlayOneShot(mechanical, .45f); pumpPlayed = true; }
        }
    }
    private static AudioClip MakeShot(WeaponId id)
    {
        const int rate = 22050; float duration = id == WeaponId.Shotgun ? .28f : id == WeaponId.SMG ? .095f : .15f;
        var data = new float[Mathf.CeilToInt(duration * rate)]; var random = new System.Random(813 + (int)id);
        float low = 0; float bass = id == WeaponId.Shotgun ? 65 : id == WeaponId.SMG ? 145 : 105;
        for (int i = 0; i < data.Length; i++)
        {
            float t = i / (float)rate; float noise = (float)random.NextDouble() * 2 - 1;
            low += (noise - low) * .18f;
            float attack = Mathf.Min(1, t * 3000);
            float crack = noise * Mathf.Exp(-t * 90) * .4f;
            float body = (low * .65f + Mathf.Sin(2 * Mathf.PI * bass * t) * .18f) * Mathf.Exp(-t * (id == WeaponId.Shotgun ? 18 : 35));
            data[i] = (crack + body) * attack;
        }
        var clip = AudioClip.Create(WeaponCatalog.Get(id).Name + " shot", data.Length, 1, rate, false); clip.SetData(data, 0); return clip;
    }
    private static AudioClip MakeMechanical()
    {
        const int rate = 22050; var data = new float[rate / 3]; var random = new System.Random(714);
        for (int i = 0; i < data.Length; i++)
        {
            float t = i / (float)rate; float tick = Mathf.Exp(-t * 150) + (t > .18f ? Mathf.Exp(-(t - .18f) * 100) * .7f : 0);
            data[i] = ((float)random.NextDouble() * 2 - 1) * tick * .22f;
        }
        var clip = AudioClip.Create("Weapon mechanism", data.Length, 1, rate, false); clip.SetData(data, 0); return clip;
    }
    private void OnDestroy()
    {
        foreach (var model in models) if (model != null) Destroy(model.gameObject);
        foreach (var clip in shots) if (clip != null) Destroy(clip);
        if (mechanical != null) Destroy(mechanical);
        if (steel != null) Destroy(steel); if (polymer != null) Destroy(polymer); if (detail != null) Destroy(detail);
        if (flashMaterial != null) Destroy(flashMaterial);
    }
}
