using System;
using UnityEngine;

/// <summary>Additive first-person motion. The authored model pose and animation paths remain intact.</summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(50)]
public class WeaponViewMotion : MonoBehaviour
{
    [Header("Optional model override")]
    public Transform viewModelRoot;
    [Header("Motion")]
    [Range(0f, 0.03f)] public float breathingAmount = 0.0025f;
    [Range(0f, 0.04f)] public float walkingBob = 0.012f;
    [Range(0f, 0.06f)] public float swayAmount = 0.025f;
    [Min(1f)] public float poseSpeed = 14f;
    [Min(1f)] public float recoilRecovery = 18f;

    public Transform MotionPivot { get; private set; }
    public bool IsInitialized => MotionPivot != null;
    public float AimBlend { get; private set; }
    public Vector3 AimOffset { get; private set; }

    private GunSystem gun;
    private PlayerController movement;
    private Animator modelAnimator;
    private Transform originalParent;
    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private Vector3 originalScale;
    private Transform magazine;
    private Vector3 magazinePosition;
    private Quaternion magazineRotation;
    private Vector3 posePosition;
    private Vector3 poseEuler;
    private Vector3 recoilPosition;
    private Vector3 recoilEuler;
    private float motionPhase;
    private float movementBlend;
    private float sprintBlend;
    private float animatorReturnAt;
    private float meleeStartedAt = float.NegativeInfinity;
    private float comfortPollAt;
    private float comfortScale = 1f;
    private bool wasReloading;
    private bool boltPulled;
    private bool hasFireState;
    private bool hasBoltState;
    private bool hasIdleState;
    private bool animatorStatesReady;
    private static readonly int FireState = Animator.StringToHash("Base Layer.AK74_Fire");
    private static readonly int BoltState = Animator.StringToHash("Base Layer.AK74_BoltBack");
    private static readonly int IdleState = Animator.StringToHash("Base Layer.New State");

    public void Initialize(GunSystem owner)
    {
        if (IsInitialized || !owner || !owner.playerCam) return;
        gun = owner;
        if (gun.player) movement = gun.player.GetComponent<PlayerController>();
        Transform camera = gun.playerCam.transform;
        if (!viewModelRoot) viewModelRoot = FindViewModel(owner, camera);
        // A malformed reference must never animate the camera or the player hierarchy.
        if (!viewModelRoot || viewModelRoot == camera || !viewModelRoot.IsChildOf(camera)) return;
        originalParent = viewModelRoot.parent;
        originalPosition = viewModelRoot.localPosition;
        originalRotation = viewModelRoot.localRotation;
        originalScale = viewModelRoot.localScale;

        GameObject pivot = new GameObject("WeaponMotionPivot");
        MotionPivot = pivot.transform;
        MotionPivot.SetParent(originalParent, false);
        MotionPivot.localPosition = Vector3.zero;
        MotionPivot.localRotation = Quaternion.identity;
        MotionPivot.localScale = Vector3.one;
        // The new parent is identity, so all original local position/rotation/scale values remain valid.
        viewModelRoot.SetParent(MotionPivot, false);
        modelAnimator = viewModelRoot.GetComponentInChildren<Animator>();
        CacheAnimatorStates();
        Transform sight = null;
        foreach (Transform child in viewModelRoot.GetComponentsInChildren<Transform>(true))
        {
            if (string.Equals(child.name, "mag", StringComparison.OrdinalIgnoreCase)
                || string.Equals(child.name, "magazine", StringComparison.OrdinalIgnoreCase)) magazine = child;
            if (string.Equals(child.name, "rearSight", StringComparison.OrdinalIgnoreCase)) sight = child;
        }
        if (magazine)
        {
            magazinePosition = magazine.localPosition;
            magazineRotation = magazine.localRotation;
        }
        AimOffset = new Vector3(-0.24f, 0.15f, -0.06f);
        if (sight)
        {
            Vector3 cameraSight = camera.InverseTransformPoint(sight.position);
            float desiredDepth = Mathf.Max(gun.playerCam.nearClipPlane + 0.08f, cameraSight.z - 0.06f);
            Vector3 cameraOffset = new Vector3(-cameraSight.x, -cameraSight.y, Mathf.Clamp(desiredDepth - cameraSight.z, -0.12f, 0.08f));
            AimOffset = originalParent.InverseTransformVector(camera.TransformVector(cameraOffset));
            AimOffset = Vector3.ClampMagnitude(AimOffset, 0.6f);
        }
    }

    static Transform FindViewModel(GunSystem owner, Transform camera)
    {
        Transform candidate = null;
        if (owner.muzzlePoint)
        {
            Animator animator = owner.muzzlePoint.GetComponentInParent<Animator>();
            candidate = animator ? animator.transform : owner.muzzlePoint.parent;
        }
        if (!candidate) candidate = camera.Find("WeaponHolder");
        if (!candidate || !candidate.IsChildOf(camera)) return null;
        // GunSystem is a separate sibling in this scene. Follow the actual model branch instead.
        while (candidate.parent && candidate.parent != camera) candidate = candidate.parent;
        return candidate;
    }

    void LateUpdate()
    {
        if (!gun || !IsInitialized) return;
        if (!gun.isActiveAndEnabled) { ResetPose(); return; }
        if (Time.timeScale <= 0f || (gun.player && gun.player.IsDead)) return;
        if (!animatorStatesReady) CacheAnimatorStates();
        float dt = Mathf.Min(Time.deltaTime, 0.05f);
        if (Time.unscaledTime >= comfortPollAt)
        {
            comfortScale = PlayerPrefs.GetInt("opt_motion", 1) != 0 ? 1f : 0.2f;
            comfortPollAt = Time.unscaledTime + 0.5f;
        }
        float blend = 1f - Mathf.Exp(-poseSpeed * dt);
        bool reloading = gun.IsReloading;
        AimBlend = Mathf.MoveTowards(AimBlend, gun.IsAiming && !reloading ? 1f : 0f, dt * 9f);
        float speed = movement && movement.controller ? Vector3.ProjectOnPlane(movement.controller.velocity, Vector3.up).magnitude : 0f;
        float walk = movement ? Mathf.Clamp01(speed / Mathf.Max(1f, movement.speed)) : 0f;
        movementBlend = Mathf.Lerp(movementBlend, walk, blend);
        sprintBlend = Mathf.Lerp(sprintBlend, movement && movement.IsSprinting && !reloading && AimBlend < 0.2f ? 1f : 0f, blend);
        motionPhase += dt * Mathf.Lerp(8f, 13f, sprintBlend) * movementBlend;
        float subtle = Mathf.Lerp(1f, 0.15f, AimBlend) * comfortScale;
        Vector3 position = new Vector3(Mathf.Sin(Time.time * 1.3f) * breathingAmount,
            Mathf.Sin(Time.time * 1.7f) * breathingAmount, 0f) * subtle;
        position += new Vector3(Mathf.Sin(motionPhase), Mathf.Cos(motionPhase * 2f) * 0.6f, 0f)
            * walkingBob * movementBlend * subtle;
        Vector3 euler = new Vector3(0f, 0f, Mathf.Sin(motionPhase) * movementBlend * 0.8f * subtle);
        float mouseX = Mathf.Clamp(Input.GetAxisRaw("Mouse X") / Mathf.Max(0.001f, dt) * 0.01f, -1f, 1f);
        float mouseY = Mathf.Clamp(Input.GetAxisRaw("Mouse Y") / Mathf.Max(0.001f, dt) * 0.01f, -1f, 1f);
        if (Cursor.lockState != CursorLockMode.Locked) { mouseX = 0f; mouseY = 0f; }
        position += new Vector3(-mouseX, -mouseY, 0f) * swayAmount * subtle;
        euler += new Vector3(mouseY * 1.5f, -mouseX * 2f, -mouseX * 1.4f) * subtle;
        position += AimOffset * AimBlend + new Vector3(0.06f, -0.13f, -0.04f) * sprintBlend;
        euler += new Vector3(12f, -8f, -10f * comfortScale) * sprintBlend;
        float reloadProgress = reloading ? gun.ReloadProgress : 0f;
        float reloadPose = ReloadPoseWeight(reloadProgress);
        position += new Vector3(0.04f, -0.08f, -0.02f) * reloadPose;
        euler += new Vector3(-10f, 6f, -23f) * reloadPose;
        float meleePhase = (Time.time - meleeStartedAt) / 0.28f;
        float melee = meleePhase >= 0f && meleePhase < 1f ? Mathf.Sin(meleePhase * Mathf.PI) : 0f;
        position += new Vector3(-0.09f, -0.04f, 0.17f) * melee;
        euler += new Vector3(9f, -8f, 12f) * melee;
        posePosition = Vector3.Lerp(posePosition, position, blend);
        poseEuler = Vector3.Lerp(poseEuler, euler, blend);
        float recovery = 1f - Mathf.Exp(-recoilRecovery * dt);
        recoilPosition = Vector3.Lerp(recoilPosition, Vector3.zero, recovery);
        recoilEuler = Vector3.Lerp(recoilEuler, Vector3.zero, recovery);
        MotionPivot.localPosition = posePosition + recoilPosition;
        MotionPivot.localRotation = Quaternion.Euler(poseEuler + recoilEuler);
        UpdateReloadModel(reloading, reloadProgress);
        if (animatorReturnAt > 0f && Time.time >= animatorReturnAt && !boltPulled) ReturnAnimatorToIdle();
    }

    public void KickShot()
    {
        if (!IsInitialized || !gun || Time.timeScale <= 0f) return;
        float strength = gun.IsAiming ? 0.5f : 1f;
        recoilPosition.z = Mathf.Max(-0.09f, recoilPosition.z - 0.035f * strength);
        recoilEuler.x = Mathf.Max(-8f, recoilEuler.x - 2.2f * strength);
        recoilEuler.y = Mathf.Clamp(recoilEuler.y + UnityEngine.Random.Range(-0.6f, 0.6f) * strength, -2.5f, 2.5f);
        recoilEuler.z = Mathf.Clamp(recoilEuler.z + 0.4f * strength, -2f, 2f);
        if (hasFireState && modelAnimator && modelAnimator.isActiveAndEnabled)
        {
            modelAnimator.Play(FireState, 0, 0f);
            animatorReturnAt = Time.time + 0.1f;
        }
    }

    void CacheAnimatorStates()
    {
        // Native Animator initialisation can finish after GunSystem.Awake.
        if (!modelAnimator || !modelAnimator.runtimeAnimatorController || !modelAnimator.isInitialized) return;
        hasIdleState = modelAnimator.HasState(0, IdleState);
        hasFireState = hasIdleState && modelAnimator.HasState(0, FireState);
        hasBoltState = hasIdleState && modelAnimator.HasState(0, BoltState);
        animatorStatesReady = true;
    }

    public void KickMelee()
    {
        if (Time.timeScale > 0f) meleeStartedAt = Time.time;
    }

    void UpdateReloadModel(bool reloading, float progress)
    {
        if (magazine)
        {
            float withdrawal = reloading ? MagazineWithdrawal(progress) : 0f;
            magazine.localPosition = magazinePosition + new Vector3(0.02f, -0.22f, -0.04f) * withdrawal;
            magazine.localRotation = magazineRotation * Quaternion.Euler(8f * withdrawal, 0f, -8f * withdrawal);
        }
        bool pullBolt = reloading && progress >= 0.78f && progress < 0.93f;
        if (pullBolt && !boltPulled && hasBoltState && modelAnimator && modelAnimator.isActiveAndEnabled)
            modelAnimator.Play(BoltState, 0, 0f);
        if ((boltPulled && !pullBolt) || (wasReloading && !reloading)) ReturnAnimatorToIdle();
        boltPulled = pullBolt;
        wasReloading = reloading;
    }

    public static float MagazineWithdrawal(float progress)
    {
        float takeOut = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.15f, 0.4f, progress));
        float insert = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.56f, 0.82f, progress));
        return takeOut * (1f - insert);
    }

    public static float ReloadPoseWeight(float progress)
    {
        float lower = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0f, 0.18f, progress));
        float recover = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.82f, 1f, progress));
        return lower * (1f - recover);
    }

    void ReturnAnimatorToIdle()
    {
        if (hasIdleState && modelAnimator && modelAnimator.isActiveAndEnabled) modelAnimator.Play(IdleState, 0, 0f);
        animatorReturnAt = 0f;
    }

    public void ResetPose()
    {
        if (MotionPivot)
        {
            MotionPivot.localPosition = Vector3.zero;
            MotionPivot.localRotation = Quaternion.identity;
        }
        posePosition = poseEuler = recoilPosition = recoilEuler = Vector3.zero;
        movementBlend = sprintBlend = AimBlend = 0f;
        wasReloading = boltPulled = false;
        meleeStartedAt = float.NegativeInfinity;
        if (magazine)
        {
            magazine.localPosition = magazinePosition;
            magazine.localRotation = magazineRotation;
        }
        ReturnAnimatorToIdle();
    }

    void OnDisable() => ResetPose();

    void OnDestroy()
    {
        ResetPose();
        if (viewModelRoot && originalParent)
        {
            viewModelRoot.SetParent(originalParent, false);
            viewModelRoot.localPosition = originalPosition;
            viewModelRoot.localRotation = originalRotation;
            viewModelRoot.localScale = originalScale;
        }
        if (MotionPivot) Destroy(MotionPivot.gameObject);
    }
}
