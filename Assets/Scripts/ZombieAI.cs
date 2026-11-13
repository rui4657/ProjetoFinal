using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class ZombieAI : MonoBehaviour
{
    public ZombieArchetype archetype = ZombieArchetype.Walker;
    public string DisplayName => ZombieProfiles.Get(archetype).Name;
    private ZombieRagdoll ragdoll;
    private Vector3 lastHitDirection;
    private float nextRangedAttack;
    private Transform headBone;
    private BarricadeEntry breach;
    private Vector3 breachLanding, vaultFrom;
    private float vaultAt = -1, nextBashAt, pendingBashAt;
    public bool IsEntering => breach != null;
    public bool HasHeadBone => headBone != null;
    public bool IsHeadHit(Vector3 point) => headBone != null &&
        Mathf.Abs(point.y - (headBone.position.y + .05f * transform.lossyScale.y)) <= Mathf.Max(.22f, .16f * transform.lossyScale.y);
    private NavMeshAgent agent;
    private Transform target;
    private PlayerHealth playerHealth;
    private Animator anim;
    private RoundManager roundOwner;
    private bool canAttack = true;
    private bool attacking;
    private bool deathReported;
    private int currentHealth;
    private int baseHealth;
    private float baseSpeed;
    private float nextRepath;
    private float nextAttackCheck;
    private readonly RaycastHit[] obstructionHits = new RaycastHit[16];
    private bool hasSpeedParameter;
    private bool hasAttackParameter;
    private bool hasHurtParameter;
    private bool hasAnimationRateParameter;
    private bool hasCycleOffsetParameter;
    private bool attackInterrupted;
    private bool powerUpRolled;
    private float staggerUntil;
    private float nextStaggerTime;
    private float hitFlashUntil;
    private float lastFlashStrength = -1f;
    private float cadenceVariation;
    private Collider bodyCollider;
    private Collider targetCollider;
    private VisualSurface[] visualSurfaces;
    private static readonly int ColorProperty = Shader.PropertyToID("_Color");
    private static readonly int BaseColorProperty = Shader.PropertyToID("_BaseColor");

    private struct VisualSurface
    {
        public Renderer renderer;
        public MaterialPropertyBlock properties;
        public int colorProperty;
        public Color baseColor;
    }

    [Header("Combat")]
    public float attackRange = 2f;
    public int damage = 25;
    public float attackCooldown = 1.5f;
    [Min(0f)] public float attackWindup = 0.35f;
    [Min(0.05f)] public float repathInterval = 0.2f;
    public LayerMask attackObstructionMask = ~0;

    [Header("Health and round scaling")]
    public int maxHealth = 50;
    [Min(1f)] public float healthGrowthPerRound = 1.12f;
    [Min(0f)] public float speedIncreasePerRound = 0.16f;
    [Min(0.1f)] public float maximumMoveSpeed = 5.3f;

    [Header("Animator parameters")]
    public string speedParam = "Speed";
    public string attackTrigger = "Attack";
    public string deadBool = "Dead";
    public string deathStateName = "death1";
    public string headshotDeathStateName = "death2";
    public string hurtTrigger = "Hurt";
    [Min(0.1f)] public float walkAnimationSpeed = 3f;
    [Min(0.1f)] public float runAnimationSpeed = 4.4f;

    [Header("Hit reaction")]
    [Range(0f, 0.5f)] public float staggerDuration = 0.3f;
    [Min(0.5f)] public float staggerCooldown = 1.15f;
    [Range(0f, 0.1f)] public float appearanceVariation = 0.07f;

    [Header("3D audio")]
    public AudioSource sfx;
    public AudioClip attackClip;
    public AudioClip hurtClip;
    public AudioClip deathClip;
    [Range(0f, 1f)] public float attackVol = 1f;
    [Range(0f, 1f)] public float hurtVol = 0.8f;
    [Range(0f, 1f)] public float deathVol = 1f;

    [Header("Hit audio cooldown")]
    public float hurtSfxCooldown = 0.15f;
    private float nextHurtTime;

    [Header("Death")]
    public AnimationClip deathAnim;
    public float destroyDelay = 2f;

    public bool IsDead { get; private set; }
    public int CurrentHealth => currentHealth;

    private bool AgentReady => agent != null && agent.enabled && agent.isOnNavMesh;
    private bool RunEnded => (playerHealth != null && playerHealth.IsDead) ||
        (SurvivalSession.Instance != null && SurvivalSession.Instance.IsGameOver);

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponentInChildren<Animator>();
        baseHealth = Mathf.Max(1, maxHealth);
        currentHealth = baseHealth;
        baseSpeed = agent != null ? agent.speed : 3.5f;
        var profile = ZombieProfiles.Get(archetype);
        baseHealth = profile.Health; currentHealth = baseHealth; baseSpeed = profile.Speed;
        damage = profile.Damage; attackCooldown = profile.Cooldown; attackWindup = profile.Windup;
        attackRange = archetype == ZombieArchetype.Brute ? 2.8f : 2f;
        if (archetype == ZombieArchetype.Brute) staggerDuration = 0;
        else if (archetype == ZombieArchetype.Runner) { staggerDuration = .1f; staggerCooldown = 1.8f; }
        playerHealth = FindFirstObjectByType<PlayerHealth>();
        bodyCollider = GetComponent<Collider>();
        if (playerHealth != null)
        {
            target = playerHealth.transform;
            targetCollider = playerHealth.GetComponent<Collider>();
        }
        CacheAnimatorParameters();
        cadenceVariation = Random.Range(0.96f, 1.04f);
        if (anim != null)
        {
            // The agent owns translation. Root motion and global Animator speed would
            // otherwise fight navigation or alter the timing of attacks and deaths.
            anim.applyRootMotion = false;
            anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            if (hasCycleOffsetParameter) anim.SetFloat("CycleOffset", Random.value);
        }
        CacheVisualVariation();
        ragdoll = GetComponent<ZombieRagdoll>();
        if (ragdoll == null) ragdoll = gameObject.AddComponent<ZombieRagdoll>();
        gameObject.AddComponent<ZombieAppearance>().Build(archetype);
        Transform head = ragdoll.FindBone("Head");
        headBone = head;
        if (head != null)
        {
            var hitbox = head.gameObject.AddComponent<SphereCollider>();
            hitbox.center = new Vector3(0, .04f, 0); hitbox.radius = .12f;
        }

        if (sfx == null) sfx = GetComponent<AudioSource>();
        if (sfx == null) sfx = gameObject.AddComponent<AudioSource>();
        sfx.playOnAwake = false;
        sfx.loop = false;
        sfx.spatialBlend = 1f;
        sfx.rolloffMode = AudioRolloffMode.Logarithmic;
        sfx.minDistance = 1.5f;
        sfx.maxDistance = 20f;
        sfx.dopplerLevel = 0f;
    }

    public void InitializeForRound(int round, RoundManager owner)
    {
        var profile = ZombieProfiles.Get(archetype);
        baseHealth = profile.Health; baseSpeed = profile.Speed;
        damage = profile.Damage; attackCooldown = profile.Cooldown; attackWindup = profile.Windup;
        attackRange = archetype == ZombieArchetype.Brute ? 2.8f : 2f;
        roundOwner = owner;
        deathReported = false;
        int progression = round <= 1 ? 0 : Mathf.Min(60, round - 1);
        maxHealth = ZombieProfiles.HealthForRound(archetype, round);
        currentHealth = maxHealth;
        if (agent != null)
        {
            agent.speed = Mathf.Min(archetype == ZombieArchetype.Runner ? 7.4f : 5.2f, baseSpeed + progression * .1f);
            agent.acceleration = archetype == ZombieArchetype.Runner ? 24 : 12;
            agent.angularSpeed = archetype == ZombieArchetype.Runner ? 600 : 240;
            agent.stoppingDistance = Mathf.Max(0.1f, attackRange - 0.2f);
        }
    }

    private void Start()
    {
        if (agent != null) agent.stoppingDistance = Mathf.Max(0.1f, attackRange - 0.2f);
    }

    private void Update()
    {
        UpdateVisualFeedback();
        if (IsDead || Time.timeScale <= 0f) return;
        if (RunEnded || target == null)
        {
            SetStopped(true);
            SetAnimationSpeed(0f);
            return;
        }
        if (breach != null) { UpdateBreach(); return; }
        if (!AgentReady) return;
        if (Time.time < staggerUntil)
        {
            SetStopped(true);
            SetAnimationSpeed(0f);
            return;
        }
        if (archetype == ZombieArchetype.Spitter) { UpdateSpitter(); return; }

        if (!attacking && Time.time >= nextRepath)
        {
            SetStopped(false);
            agent.SetDestination(target.position);
            nextRepath = Time.time + Mathf.Max(0.05f, repathInterval);
        }
        SetAnimationSpeed(attacking ? 0f : agent.velocity.magnitude);

        if (canAttack && Time.time >= nextAttackCheck && CanReachPlayer())
        {
            nextAttackCheck = Time.time + 0.1f;
            if (HasClearAttack()) StartCoroutine(Attack());
        }

        if (attacking)
        {
            Vector3 facing = target.position - transform.position;
            facing.y = 0f;
            if (facing.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(facing), Time.deltaTime * 8f);
        }
    }

    public bool BeginBreach(BarricadeEntry entry, Vector3 landing)
    {
        if (IsDead || breach != null || entry == null || !entry.Register(this)) return false;
        breach = entry; breachLanding = landing; vaultAt = -1; pendingBashAt = 0;
        if (agent != null) agent.enabled = false;
        return true;
    }
    private void UpdateBreach()
    {
        if (Time.time < staggerUntil) { pendingBashAt = 0; SetAnimationSpeed(0); return; }
        Quaternion facing = Quaternion.LookRotation(breach.transform.forward);
        if (vaultAt >= 0)
        {
            float t = Mathf.Clamp01((Time.time - vaultAt) / 1.1f);
            float arc = Mathf.Sin(t * Mathf.PI);
            transform.position = Vector3.Lerp(vaultFrom, breachLanding, t) + Vector3.up * (arc * 1.9f);
            transform.rotation = facing * Quaternion.Euler(55 * arc, 0, 0);
            if (t >= 1)
            {
                if (agent != null)
                {
                    var filter = new NavMeshQueryFilter { agentTypeID = agent.agentTypeID, areaMask = NavMesh.AllAreas };
                    if (!NavMesh.SamplePosition(breachLanding, out NavMeshHit hit, 1.5f, filter)) return;
                    transform.position = hit.position;
                    agent.enabled = true;
                    // Warp can recover an agent that was off-mesh while crossing the sill.
                    if (!agent.Warp(hit.position)) { agent.enabled = false; return; }
                    transform.position = hit.position;
                }
                breach.Release(this); breach = null;
                nextRepath = 0;
            }
            return;
        }
        Vector3 attackSpot = breach.transform.position - breach.transform.forward * 1.4f;
        transform.rotation = Quaternion.Slerp(transform.rotation, facing, Time.deltaTime * 8);
        if ((transform.position - attackSpot).sqrMagnitude > .04f)
        {
            transform.position = Vector3.MoveTowards(transform.position, attackSpot, Time.deltaTime * Mathf.Min(baseSpeed, 3));
            SetAnimationSpeed(baseSpeed); return;
        }
        SetAnimationSpeed(0);
        if (breach.Planks == 0)
        {
            vaultAt = Time.time; vaultFrom = transform.position;
            if (anim != null && hasAttackParameter) anim.SetTrigger(attackTrigger);
            return;
        }
        if (pendingBashAt > 0 && Time.time >= pendingBashAt)
        { breach.Bash(this); pendingBashAt = 0; }
        if (pendingBashAt <= 0 && Time.time >= nextBashAt)
        {
            pendingBashAt = Time.time + Mathf.Max(.25f, attackWindup);
            nextBashAt = Time.time + Mathf.Max(.9f, attackCooldown);
            if (anim != null && hasAttackParameter) anim.SetTrigger(attackTrigger);
            PlayOne(attackClip, attackVol * .5f);
        }
    }

    private void UpdateSpitter()
    {
        Vector3 facing = target.position - transform.position; facing.y = 0;
        if (facing.sqrMagnitude > .001f)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(facing), Time.deltaTime * 6);
        if (attacking) { SetAnimationSpeed(0); return; }
        Vector3 toPlayer = target.position - transform.position;
        float distance = toPlayer.magnitude;
        if (Time.time >= nextRepath)
        {
            nextRepath = Time.time + .3f;
            if (distance < 6)
            {
                Vector3 retreat = transform.position - toPlayer.normalized * 4;
                if (NavMesh.SamplePosition(retreat, out NavMeshHit hit, 3, NavMesh.AllAreas))
                { SetStopped(false); agent.SetDestination(hit.position); }
            }
            else if (distance > 13 || !HasClearAttack()) { SetStopped(false); agent.SetDestination(target.position); }
            else SetStopped(true);
        }
        SetAnimationSpeed(agent.velocity.magnitude);
        if (distance <= 19 && Time.time >= nextRangedAttack && HasClearAttack()) StartCoroutine(Spit());
    }

    private IEnumerator Spit()
    {
        attacking = true; attackInterrupted = false; SetStopped(true); nextRangedAttack = Time.time + attackCooldown;
        if (anim != null && hasAttackParameter) anim.SetTrigger(attackTrigger);
        PlayOne(attackClip, attackVol);
        // The tell comes before the throw; the projectile aims at the player's position at release.
        yield return new WaitForSeconds(attackWindup);
        if (!IsDead && !RunEnded && !attackInterrupted && Time.time >= staggerUntil && playerHealth != null && HasClearAttack())
        {
            Vector3 origin = (headBone != null ? headBone.position : AttackOrigin + Vector3.up * .5f) + transform.forward * .45f;
            ZombieProjectile.Launch(this, playerHealth, origin, TargetCenter, damage);
        }
        yield return new WaitForSeconds(.25f);
        attacking = false;
        if (!IsDead && !RunEnded) SetStopped(false);
    }

    private IEnumerator Attack()
    {
        canAttack = false;
        attacking = true;
        attackInterrupted = false;
        SetStopped(true);
        if (anim != null && hasAttackParameter) anim.SetTrigger(attackTrigger);
        PlayOne(attackClip, attackVol);

        // The player has time to dodge. Starting an animation is not an automatic hit.
        yield return new WaitForSeconds(Mathf.Max(0f, attackWindup));
        if (!IsDead && !RunEnded && !attackInterrupted && Time.time >= staggerUntil &&
            target != null && playerHealth != null && CanReachPlayer() && HasClearAttack())
        {
            playerHealth.TakeDamage(Mathf.Max(0, damage));
        }

        yield return new WaitForSeconds(Mathf.Max(0.05f, attackCooldown - attackWindup));
        attacking = false;
        canAttack = !IsDead && !RunEnded;
        if (canAttack && Time.time >= staggerUntil) SetStopped(false);
    }

    private Vector3 AttackOrigin => bodyCollider != null ? bodyCollider.bounds.center : transform.TransformPoint(Vector3.up);
    private Vector3 TargetCenter => targetCollider != null ? targetCollider.bounds.center : target.position + Vector3.up;

    private bool CanReachPlayer()
    {
        if (target == null) return false;
        // Using feet as a second check prevents a tall collider reaching through a
        // ceiling; using actual centers also respects the prefab's nonuniform scale.
        if (bodyCollider != null && targetCollider != null &&
            Mathf.Abs(bodyCollider.bounds.min.y - targetCollider.bounds.min.y) > 1.25f) return false;
        return (TargetCenter - AttackOrigin).sqrMagnitude <= attackRange * attackRange;
    }

    private bool HasClearAttack()
    {
        if (target == null || playerHealth == null) return false;
        Vector3 start = AttackOrigin;
        Vector3 finish = TargetCenter;
        Vector3 direction = finish - start;
        float distance = direction.magnitude;
        if (distance <= 0.001f) return true;
        int count = Physics.RaycastNonAlloc(start, direction / distance, obstructionHits, distance, attackObstructionMask, QueryTriggerInteraction.Ignore);
        // A full buffer cannot establish a clear path safely: defer this attack.
        if (count == obstructionHits.Length) return false;
        float closest = float.PositiveInfinity;
        Collider obstacle = null;
        for (int i = 0; i < count; i++)
        {
            Transform hitTransform = obstructionHits[i].collider.transform;
            if (hitTransform == transform || hitTransform.IsChildOf(transform)) continue;
            if (obstructionHits[i].distance < closest)
            {
                closest = obstructionHits[i].distance;
                obstacle = obstructionHits[i].collider;
            }
        }
        return obstacle == null || obstacle.GetComponentInParent<PlayerHealth>() == playerHealth;
    }

    public void TakeDamage(int amount)
    {
        TakeDamage(amount, false);
    }

    public void TakeDamage(int amount, bool headshot)
    {
        if (IsDead || amount <= 0 || RunEnded) return;
        currentHealth = Mathf.Max(0, currentHealth - amount);
        if (hurtClip != null && Time.time >= nextHurtTime)
        {
            PlayOne(hurtClip, hurtVol);
            nextHurtTime = Time.time + Mathf.Max(0f, hurtSfxCooldown);
        }
        hitFlashUntil = Time.time + 0.13f;
        if (currentHealth == 0) Die(headshot);
        else TryStagger(headshot);
    }

    public void TakeDamage(int amount, bool headshot, Vector3 direction)
    {
        lastHitDirection = direction;
        TakeDamage(amount, headshot);
    }

    private void TryStagger(bool headshot)
    {
        if (breach != null && vaultAt >= 0) return;
        // Even automatic fire cannot keep a zombie permanently stunned.
        if (Time.time < nextStaggerTime || staggerDuration <= 0f) return;
        staggerUntil = Time.time + Mathf.Min(0.45f, staggerDuration * (headshot ? 1.25f : 1f));
        nextStaggerTime = Time.time + Mathf.Max(staggerDuration + 0.5f, staggerCooldown);
        nextRepath = staggerUntil;
        attackInterrupted = attacking;
        SetStopped(true);
        if (anim != null && hasHurtParameter)
        {
            if (hasAttackParameter) anim.ResetTrigger(attackTrigger);
            anim.SetTrigger(hurtTrigger);
        }
    }

    private void Die(bool headshot)
    {
        if (IsDead) return;
        IsDead = true;
        Vector3 dropPosition = breach != null ? breachLanding : transform.position;
        if (breach != null) { breach.Release(this); breach = null; }
        StopAllCoroutines();
        canAttack = false;
        attacking = false;
        SetStopped(true);
        if (agent != null) agent.enabled = false;
        if (ragdoll == null) ragdoll = gameObject.AddComponent<ZombieRagdoll>();
        ragdoll.Prepare(anim);
        foreach (Collider collider in GetComponentsInChildren<Collider>()) collider.enabled = false;
        ragdoll.Collapse(headshot, lastHitDirection);
        PlayOne(deathClip, deathVol);
        if (!powerUpRolled)
        {
            powerUpRolled = true;
            if (SurvivalSession.Instance != null) SurvivalSession.Instance.TryDropPowerUp(dropPosition);
        }
        NotifyRoundOnce();
    }

    private void NotifyRoundOnce()
    {
        if (deathReported || roundOwner == null) return;
        deathReported = true;
        roundOwner.ZombieDied();
    }

    private void OnDestroy()
    {
        if (breach != null) breach.Release(this);
        // External cleanup of a living spawned zombie must not strand its wave.
        NotifyRoundOnce();
    }

    private void SetStopped(bool value)
    {
        if (AgentReady) agent.isStopped = value;
    }

    private void CacheAnimatorParameters()
    {
        if (anim == null) return;
        foreach (AnimatorControllerParameter parameter in anim.parameters)
        {
            if (parameter.name == speedParam && parameter.type == AnimatorControllerParameterType.Float) hasSpeedParameter = true;
            if (parameter.name == attackTrigger && parameter.type == AnimatorControllerParameterType.Trigger) hasAttackParameter = true;
            if (parameter.name == hurtTrigger && parameter.type == AnimatorControllerParameterType.Trigger) hasHurtParameter = true;
            if (parameter.name == "AnimationRate" && parameter.type == AnimatorControllerParameterType.Float) hasAnimationRateParameter = true;
            if (parameter.name == "CycleOffset" && parameter.type == AnimatorControllerParameterType.Float) hasCycleOffsetParameter = true;
        }
    }

    private void SetAnimationSpeed(float value)
    {
        if (anim == null) return;
        float movement = archetype == ZombieArchetype.Runner ? value : Mathf.Min(2.5f, value);
        if (hasSpeedParameter) anim.SetFloat(speedParam, movement, 0.08f, Time.deltaTime);
        if (hasAnimationRateParameter)
        {
            float referenceSpeed = archetype == ZombieArchetype.Runner ? runAnimationSpeed : walkAnimationSpeed;
            float rate = value > 0.1f ? Mathf.Clamp(value / Mathf.Max(0.1f, referenceSpeed) * cadenceVariation, 0.6f, 1.45f) : 1f;
            anim.SetFloat("AnimationRate", rate, 0.1f, Time.deltaTime);
        }
    }

    private void CacheVisualVariation()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        visualSurfaces = new VisualSurface[renderers.Length];
        float variation = Mathf.Clamp(appearanceVariation, 0f, 0.1f);
        Color tint = Color.Lerp(new Color(0.92f, 0.98f, 0.9f, 1f), new Color(1f, 0.93f, 0.89f, 1f), Random.value);
        tint = Color.Lerp(Color.white, tint, variation / 0.1f);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            Material material = renderer.sharedMaterial;
            if (material == null) continue;
            int property = material.HasProperty(BaseColorProperty) ? BaseColorProperty : ColorProperty;
            if (!material.HasProperty(property)) continue;
            MaterialPropertyBlock properties = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(properties);
            Color baseColor = material.GetColor(property) * tint;
            baseColor.a = material.GetColor(property).a;
            visualSurfaces[i] = new VisualSurface { renderer = renderer, properties = properties, colorProperty = property, baseColor = baseColor };
            properties.SetColor(property, baseColor);
            renderer.SetPropertyBlock(properties);
        }
    }

    private void UpdateVisualFeedback()
    {
        float strength = Mathf.Clamp01((hitFlashUntil - Time.time) / 0.13f);
        if (visualSurfaces == null || Mathf.Abs(strength - lastFlashStrength) < 0.01f) return;
        lastFlashStrength = strength;
        for (int i = 0; i < visualSurfaces.Length; i++)
        {
            VisualSurface surface = visualSurfaces[i];
            if (surface.renderer == null || surface.properties == null) continue;
            Color bruised = surface.baseColor * new Color(1.08f, 0.65f, 0.6f, 1f);
            Color color = Color.Lerp(surface.baseColor, bruised, strength * 0.35f);
            color.a = surface.baseColor.a;
            surface.properties.SetColor(surface.colorProperty, color);
            surface.renderer.SetPropertyBlock(surface.properties);
        }
    }

    private void PlayOne(AudioClip clip, float volume)
    {
        if (sfx == null || clip == null) return;
        sfx.pitch = Random.Range(0.95f, 1.05f);
        sfx.PlayOneShot(clip, volume);
    }
}
