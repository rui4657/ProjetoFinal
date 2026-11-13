using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GunSystem : MonoBehaviour
{
    [Header("Arma")]
    public string gunName = "AK";
    public Camera playerCam;
    public Transform muzzlePoint;
    public PlayerHealth player;

    [Header("Tiro")]
    [Min(0.1f)] public float fireRate = 8f;
    [Min(1f)] public float maxDistance = 100f;
    [Min(1)] public int damage = 20;
    public LayerMask hitMask = Physics.DefaultRaycastLayers;
    [Min(5f)] public float headshotMultiplier = 5f;
    [Range(0f, 5f)] public float hipSpread = 0.85f;
    [Range(0f, 5f)] public float aimedSpread = 0.08f;
    [Range(0f, 5f)] public float recoil = 0.9f;

    [Header("Corpo a corpo")]
    [Min(1)] public int meleeDamage = 75;
    [Min(0.5f)] public float meleeRange = 2.1f;
    [Range(0.05f, 0.5f)] public float meleeRadius = 0.2f;
    [Min(0.2f)] public float meleeCooldown = 0.8f;

    [Header("Munição")]
    [Min(1)] public int magSize = 30;
    public int reserveAmmo = 120;
    public int currentAmmo;
    [Min(1)] public int maxReserveAmmo = 300;
    [Min(0.1f)] public float reloadTime = 2f;

    [Header("Pontaria")]
    [Range(20f, 80f)] public float aimedFieldOfView = 44f;
    [Min(1f)] public float aimTransitionSpeed = 12f;

    [Header("Efeitos")]
    public ParticleSystem muzzleFlash;
    public LineRenderer bulletTrail;
    public GameObject impactEffect;
    public float trailTime = 0.05f;

    [Header("Audio")]
    public AudioSource sfxSource;
    public AudioClip shotClip;
    public AudioClip reloadClip;
    [Range(0f, 1f)] public float shotVolume = 1f;
    [Range(0f, 1f)] public float reloadVolume = 1f;

    public bool IsReloading { get; private set; }
    public bool IsAiming { get; private set; }
    public bool IsMeleeing => Time.time < meleeRecoveryUntil;
    public WeaponViewMotion ViewMotion { get; private set; }
    public WeaponLoadout Loadout { get; private set; } = new WeaponLoadout();
    public WeaponId ActiveWeapon => Loadout.Current.Id;
    public bool IsSwitching => Time.time < switchingUntil;
    public float ReloadProgress => IsReloading ? Mathf.Clamp01((Time.time - reloadStartedAt) / reloadDuration) : 0f;

    private readonly RaycastHit[] raycastBuffer = new RaycastHit[64];
    private MouseLook mouseLook;
    private PlayerController movement;
    private Coroutine reloadRoutine;
    private float nextTimeToFire;
    private float reloadStartedAt;
    private float reloadDuration = 1f;
    private float trailExpiresAt;
    private float baseFieldOfView = 60f;
    private float switchingUntil;
    private WeaponVisuals visuals;
    private readonly Dictionary<ZombieAI, ShotImpact> shotImpacts = new Dictionary<ZombieAI, ShotImpact>();
    private struct ShotImpact { public int Damage; public bool Headshot; public Vector3 Direction; }
    private float nextMeleeAt;
    private float meleeRecoveryUntil;

    void Awake()
    {
        headshotMultiplier = Mathf.Max(5f, headshotMultiplier);
        if (!playerCam) playerCam = Camera.main;
        if (!player) player = GetComponentInParent<PlayerHealth>();
        if (!player) player = FindFirstObjectByType<PlayerHealth>();
        if (!sfxSource) sfxSource = GetComponent<AudioSource>();
        if (playerCam)
        {
            baseFieldOfView = playerCam.fieldOfView;
            mouseLook = playerCam.GetComponent<MouseLook>();
        }
        if (player) movement = player.GetComponent<PlayerController>();
        InitNewGame();
        if (bulletTrail)
        {
            bulletTrail.positionCount = 2;
            bulletTrail.useWorldSpace = true;
            bulletTrail.enabled = false;
        }
        ViewMotion = GetComponent<WeaponViewMotion>();
        if (!ViewMotion) ViewMotion = gameObject.AddComponent<WeaponViewMotion>();
        ViewMotion.Initialize(this);
        visuals = gameObject.AddComponent<WeaponVisuals>();
        visuals.Initialize(this);
    }

    void Update()
    {
        if (bulletTrail && Time.time >= trailExpiresAt) bulletTrail.enabled = false;
        bool blocked = Time.timeScale <= 0f || (player && player.IsDead)
            || (SurvivalSession.Instance != null && SurvivalSession.Instance.IsGameOver)
            || Cursor.lockState != CursorLockMode.Locked;
        if (blocked)
        {
            IsAiming = false;
            if (player && player.IsDead) CancelReload();
            return;
        }
        if (Input.GetKeyDown(KeyCode.Alpha1)) TrySwitchWeapon(0);
        else if (Input.GetKeyDown(KeyCode.Alpha2)) TrySwitchWeapon(1);
        else if (Input.GetKeyDown(KeyCode.Q) || Mathf.Abs(Input.GetAxisRaw("Mouse ScrollWheel")) > .01f)
            TrySwitchWeapon(1 - Loadout.Active);
        if (IsSwitching) { IsAiming = false; return; }
        if (Input.GetKeyDown(KeyCode.V)) TryMelee();
        if (IsReloading && WeaponCatalog.Get(ActiveWeapon).ShellReload && currentAmmo > 0 && Input.GetMouseButtonDown(0)) CancelReload();
        IsAiming = !IsReloading && !IsMeleeing && Input.GetMouseButton(1);
        if (IsReloading || IsMeleeing) return;
        if (Input.GetKeyDown(KeyCode.R) || (currentAmmo == 0 && reserveAmmo > 0 && !IsMeleeing))
        {
            BeginReload();
            return;
        }
        if (currentAmmo <= 0)
        {
            if (Input.GetMouseButtonDown(0) && SurvivalSession.Instance != null)
                SurvivalSession.Instance.ShowMessage("SEM MUNIÇÕES · procura a estação de abastecimento");
            return;
        }
        bool trigger = WeaponCatalog.Get(ActiveWeapon).Automatic ? Input.GetMouseButton(0) : Input.GetMouseButtonDown(0);
        if (trigger && Time.time >= nextTimeToFire && playerCam) ShootIfPossible();
    }

    void LateUpdate()
    {
        if (!playerCam || Time.timeScale <= 0f) return;
        float target = IsAiming ? Mathf.Min(aimedFieldOfView, baseFieldOfView) : baseFieldOfView;
        if (!IsAiming && movement && movement.IsSprinting) target += 5f;
        playerCam.fieldOfView = Mathf.Lerp(playerCam.fieldOfView, target, 1f - Mathf.Exp(-aimTransitionSpeed * Time.deltaTime));
    }

    void ShootIfPossible()
    {
        currentAmmo--;
        nextTimeToFire = Time.time + 1f / Mathf.Max(0.1f, fireRate);
        if (sfxSource && shotClip) sfxSource.PlayOneShot(shotClip, shotVolume);
        if (visuals) visuals.KickShot();
        if (SurvivalSession.Instance != null) SurvivalSession.Instance.RegisterShot();
        Shoot();
        if (ViewMotion) ViewMotion.KickShot();
        if (mouseLook) mouseLook.AddRecoil(recoil * (IsAiming ? 0.6f : 1f), UnityEngine.Random.Range(-recoil, recoil) * 0.3f);
        RequestSave();
    }

    void BeginReload()
    {
        if (IsReloading || IsMeleeing || reserveAmmo <= 0 || currentAmmo >= magSize) return;
        IsAiming = false;
        IsReloading = true;
        reloadStartedAt = Time.time;
        float multiplier = SurvivalSession.Instance != null ? SurvivalSession.Instance.ReloadMultiplier : 1f;
        reloadDuration = Mathf.Max(0.1f, reloadTime * multiplier);
        reloadRoutine = StartCoroutine(Reload());
    }

    IEnumerator Reload()
    {
        if (WeaponCatalog.Get(ActiveWeapon).ShellReload)
        {
            while (currentAmmo < magSize && reserveAmmo > 0)
            {
                reloadStartedAt = Time.time;
                if (sfxSource && reloadClip) sfxSource.PlayOneShot(reloadClip, reloadVolume * .5f);
                yield return new WaitForSeconds(reloadDuration);
                if (player && player.IsDead) { CancelReload(); yield break; }
                currentAmmo++; reserveAmmo--; RequestSave();
            }
            IsReloading = false; reloadRoutine = null; yield break;
        }
        if (sfxSource && reloadClip) sfxSource.PlayOneShot(reloadClip, reloadVolume);
        yield return new WaitForSeconds(reloadDuration);
        if (player && player.IsDead) { CancelReload(); yield break; }
        int toLoad = Mathf.Min(Mathf.Max(0, magSize - currentAmmo), reserveAmmo);
        currentAmmo += toLoad;
        reserveAmmo -= toLoad;
        IsReloading = false;
        reloadRoutine = null;
        RequestSave();
    }

    void CancelReload()
    {
        if (reloadRoutine != null) StopCoroutine(reloadRoutine);
        reloadRoutine = null;
        IsReloading = false;
    }

    void Shoot()
    {
        shotImpacts.Clear();
        var spec = WeaponCatalog.Get(ActiveWeapon);
        Vector3 hitPoint = playerCam.transform.position + playerCam.transform.forward * maxDistance;
        for (int pellet = 0; pellet < spec.Pellets; pellet++)
            ShootPellet(spec, ref hitPoint);
        bool accuracyCredited = false;
        foreach (var pair in shotImpacts)
        {
            var zombie = pair.Key; var impact = pair.Value;
            zombie.TakeDamage(impact.Damage, impact.Headshot, impact.Direction);
            if (SurvivalSession.Instance != null)
                SurvivalSession.Instance.RegisterHit(zombie.IsDead, impact.Headshot, false, !accuracyCredited);
            accuracyCredited = true;
        }
        if (muzzleFlash && ActiveWeapon == WeaponId.Rifle) muzzleFlash.Play();
        if (bulletTrail && muzzlePoint)
        {
            bulletTrail.SetPosition(0, muzzlePoint.position); bulletTrail.SetPosition(1, hitPoint);
            bulletTrail.enabled = true; trailExpiresAt = Time.time + Mathf.Max(.01f, trailTime);
        }
    }

    void ShootPellet(WeaponSpec spec, ref Vector3 hitPoint)
    {
        Vector2 spread = UnityEngine.Random.insideUnitCircle * (IsAiming ? aimedSpread : hipSpread);
        Vector3 direction = playerCam.transform.TransformDirection(Quaternion.Euler(spread.y, spread.x, 0f) * Vector3.forward);
        Ray ray = new Ray(playerCam.transform.position, direction);
        hitPoint = ray.GetPoint(maxDistance);
        if (FindClosestHit(ray, out RaycastHit hit))
        {
            hitPoint = hit.point;
            ZombieAI zombie = hit.collider.GetComponentInParent<ZombieAI>();
            if (zombie && !zombie.IsDead)
            {
                bool headshot = IsHeadshot(hit, zombie);
                float multiplier = SurvivalSession.Instance != null ? SurvivalSession.Instance.DamageMultiplier : 1f;
                int hitDamage = SurvivalSession.Instance != null && SurvivalSession.Instance.InstaKillActive
                    ? Mathf.Max(1, zombie.CurrentHealth)
                    : Mathf.Max(1, Mathf.RoundToInt(damage * multiplier * WeaponCatalog.DamageAt(spec.Id, hit.distance) * (headshot ? headshotMultiplier : 1f)));
                shotImpacts.TryGetValue(zombie, out ShotImpact impact);
                impact.Damage += hitDamage; impact.Headshot |= headshot; impact.Direction = direction;
                shotImpacts[zombie] = impact;
            }
            else
            {
                HealthDummy dummy = hit.collider.GetComponentInParent<HealthDummy>();
                if (dummy) dummy.TakeDamage(damage);
            }
            if (impactEffect)
            {
                GameObject impact = Instantiate(impactEffect, hit.point + hit.normal * 0.01f, Quaternion.LookRotation(hit.normal));
                Destroy(impact, 2f);
            }
        }
    }

    bool FindClosestHit(Ray ray, out RaycastHit closest)
        => FindClosestHit(ray, maxDistance, 0f, out closest);

    bool FindClosestHit(Ray ray, float distanceLimit, float radius, out RaycastHit closest)
    {
        int mask = hitMask.value == 0 ? Physics.DefaultRaycastLayers : hitMask.value;
        int count = radius > 0f
            ? Physics.SphereCastNonAlloc(ray, radius, raycastBuffer, distanceLimit, mask, QueryTriggerInteraction.Ignore)
            : Physics.RaycastNonAlloc(ray, raycastBuffer, distanceLimit, mask, QueryTriggerInteraction.Ignore);
        RaycastHit[] hits = raycastBuffer;
        // Saturation must not let an omitted nearby wall become a shot through scenery.
        if (count == raycastBuffer.Length)
        {
            hits = radius > 0f
                ? Physics.SphereCastAll(ray, radius, distanceLimit, mask, QueryTriggerInteraction.Ignore)
                : Physics.RaycastAll(ray, distanceLimit, mask, QueryTriggerInteraction.Ignore);
            count = hits.Length;
        }
        closest = default;
        float distance = float.PositiveInfinity;
        bool found = false;
        for (int i = 0; i < count; i++)
        {
            Collider collider = hits[i].collider;
            if (!collider || (player && collider.transform.IsChildOf(player.transform))) continue;
            if (hits[i].distance >= distance) continue;
            closest = hits[i];
            distance = hits[i].distance;
            found = true;
        }
        return found;
    }

    public bool TryMelee()
    {
        if (!isActiveAndEnabled || !playerCam || Time.timeScale <= 0f || Time.time < nextMeleeAt || IsSwitching
            || (player && player.IsDead) || Cursor.lockState != CursorLockMode.Locked
            || (SurvivalSession.Instance != null && SurvivalSession.Instance.IsGameOver)) return false;
        nextMeleeAt = Time.time + Mathf.Max(0.2f, meleeCooldown);
        meleeRecoveryUntil = Time.time + 0.28f;
        CancelReload();
        IsAiming = false;
        if (ViewMotion) ViewMotion.KickMelee();
        Ray ray = new Ray(playerCam.transform.position, playerCam.transform.forward);
        if (!FindClosestHit(ray, meleeRange, meleeRadius, out RaycastHit hit)) return true;
        ZombieAI zombie = hit.collider.GetComponentInParent<ZombieAI>();
        if (!zombie || zombie.IsDead) return true;
        // The generous sweep must not reach a zombie around an intervening wall.
        Vector3 toHit = hit.point - ray.origin;
        if (toHit.sqrMagnitude > 0.0001f && FindClosestHit(new Ray(ray.origin, toHit.normalized), toHit.magnitude + 0.03f, 0f, out RaycastHit obstruction)
            && obstruction.collider.GetComponentInParent<ZombieAI>() != zombie) return true;
        float multiplier = SurvivalSession.Instance != null ? SurvivalSession.Instance.DamageMultiplier : 1f;
        int hitDamage = SurvivalSession.Instance != null && SurvivalSession.Instance.InstaKillActive
            ? Mathf.Max(1, zombie.CurrentHealth) : Mathf.Max(1, Mathf.RoundToInt(meleeDamage * multiplier));
        zombie.TakeDamage(hitDamage, false, ray.direction);
        if (SurvivalSession.Instance != null) SurvivalSession.Instance.RegisterHit(zombie.IsDead, false, true);
        RequestSave();
        return true;
    }

    static bool IsHeadshot(RaycastHit hit, ZombieAI zombie)
    {
        for (Transform bone = hit.collider.transform; bone && bone != zombie.transform; bone = bone.parent)
            if (bone.name.IndexOf("head", StringComparison.OrdinalIgnoreCase) >= 0) return true;
        if (zombie.HasHeadBone) return zombie.IsHeadHit(hit.point);
        // Existing zombies use a single capsule; its top 22% represents the head.
        Bounds bounds = hit.collider.bounds;
        return bounds.size.y > 0.3f && hit.point.y >= bounds.min.y + bounds.size.y * 0.78f;
    }

    public void AddReserveAmmo(int amount, bool refillMagazine = false)
    {
        if (amount < 0) return;
        if (refillMagazine)
        {
            CancelReload();
            currentAmmo = Mathf.Max(1, magSize);
        }
        reserveAmmo = Mathf.Clamp(reserveAmmo + amount, 0, Mathf.Max(1, maxReserveAmmo));
        RequestSave();
    }

    public void InitNewGame()
    {
        CancelReload();
        Loadout = new WeaponLoadout(); ApplyActiveWeapon(false);
        nextTimeToFire = 0f;
        nextMeleeAt = meleeRecoveryUntil = 0f;
    }

    public void ApplyLoadedGun(string armaNome, int ammoMag, int ammoReserva)
    {
        CancelReload();
        Loadout = WeaponLoadout.FromLegacy(armaNome, ammoMag, ammoReserva); ApplyActiveWeapon(false);
    }

    public string CaptureLoadout() { SyncAmmo(); return Loadout.Encode(); }
    public void RestoreLoadout(string encoded, string legacyName, int magazine, int reserve)
    {
        CancelReload();
        Loadout = WeaponLoadout.TryDecode(encoded, out WeaponLoadout parsed) ? parsed : WeaponLoadout.FromLegacy(legacyName, magazine, reserve);
        ApplyActiveWeapon(false);
    }
    private void SyncAmmo()
    {
        Loadout.Current.Magazine = Mathf.Clamp(currentAmmo, 0, magSize);
        Loadout.Current.Reserve = Mathf.Clamp(reserveAmmo, 0, maxReserveAmmo);
    }
    public bool TrySwitchWeapon(int slot)
    {
        if (Time.timeScale <= 0 || IsSwitching || IsMeleeing || (player && player.IsDead)
            || (SurvivalSession.Instance != null && SurvivalSession.Instance.IsGameOver)) return false;
        SyncAmmo();
        if (!Loadout.Switch(slot)) return false;
        CancelReload(); ApplyActiveWeapon(true); RequestSave(); return true;
    }
    public bool PurchaseWeapon(WeaponId id, bool mystery = false)
    {
        var session = SurvivalSession.Instance;
        if (!WeaponCatalog.IsValid(id) || session == null || session.IsGameOver || Time.timeScale <= 0 || (player && player.IsDead)) return false;
        SyncAmmo(); int owned = Loadout.IndexOf(id); var spec = WeaponCatalog.Get(id);
        if (owned >= 0 && Loadout.Get(owned).IsFull) { session.ShowMessage("Esta arma já tem munições completas."); return false; }
        int cost = mystery ? 950 : owned >= 0 ? spec.AmmoPrice : spec.Price;
        if (!session.TrySpend(cost)) return false;
        if (owned >= 0)
        {
            Loadout.Get(owned).Fill();
            if (owned == Loadout.Active) { CancelReload(); currentAmmo = magSize; reserveAmmo = maxReserveAmmo; }
            session.ShowMessage(spec.Name + " · MUNIÇÕES COMPLETAS");
        }
        else
        {
            CancelReload(); Loadout.Acquire(id); ApplyActiveWeapon(true);
            session.ShowMessage("EQUIPADA · " + spec.Name + " · [1]/[2] TROCAR");
        }
        RequestSave(); return true;
    }
    public void RefillAllWeapons()
    {
        CancelReload(); SyncAmmo(); Loadout.FillAll(); currentAmmo = Loadout.Current.Magazine; reserveAmmo = Loadout.Current.Reserve; RequestSave();
    }
    private void ApplyActiveWeapon(bool switching)
    {
        var spec = WeaponCatalog.Get(ActiveWeapon);
        gunName = spec.Name; damage = spec.Damage; fireRate = spec.Rate; maxDistance = spec.Range;
        magSize = spec.Magazine; maxReserveAmmo = spec.ReserveLimit; reloadTime = spec.ReloadSeconds;
        hipSpread = spec.HipSpread; aimedSpread = spec.AimSpread; recoil = spec.Recoil;
        currentAmmo = Loadout.Current.Magazine; reserveAmmo = Loadout.Current.Reserve;
        IsAiming = false; switchingUntil = switching ? Time.time + .4f : 0;
        nextTimeToFire = Mathf.Max(nextTimeToFire, switchingUntil);
        if (ViewMotion) ViewMotion.ResetPose();
        if (visuals) visuals.Show(ActiveWeapon);
    }

    void RequestSave()
    {
        if (SaveSystem.instance != null && (!player || !player.IsDead)) SaveSystem.instance.RequestSave();
    }

    void OnDisable()
    {
        CancelReload();
        IsAiming = false;
        meleeRecoveryUntil = 0f;
        if (bulletTrail) bulletTrail.enabled = false;
        if (playerCam) playerCam.fieldOfView = baseFieldOfView;
        if (ViewMotion) ViewMotion.ResetPose();
    }
}
