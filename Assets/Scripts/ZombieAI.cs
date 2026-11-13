using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class ZombieAI : MonoBehaviour
{
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
    private bool hasDeadParameter;

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
        playerHealth = FindFirstObjectByType<PlayerHealth>();
        if (playerHealth != null) target = playerHealth.transform;
        CacheAnimatorParameters();

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
        roundOwner = owner;
        deathReported = false;
        int progression = Mathf.Clamp(round - 1, 0, 60);
        maxHealth = Mathf.Clamp(Mathf.RoundToInt(baseHealth * Mathf.Pow(Mathf.Max(1f, healthGrowthPerRound), progression)), 1, 100000);
        currentHealth = maxHealth;
        if (agent != null)
        {
            agent.speed = Mathf.Min(Mathf.Max(0.1f, maximumMoveSpeed), baseSpeed + progression * Mathf.Max(0f, speedIncreasePerRound));
            agent.stoppingDistance = Mathf.Max(0.1f, attackRange - 0.2f);
        }
    }

    private void Start()
    {
        if (agent != null) agent.stoppingDistance = Mathf.Max(0.1f, attackRange - 0.2f);
    }

    private void Update()
    {
        if (IsDead || Time.timeScale <= 0f) return;
        if (RunEnded || target == null)
        {
            SetStopped(true);
            SetAnimationSpeed(0f);
            return;
        }
        if (!AgentReady) return;

        if (!attacking && Time.time >= nextRepath)
        {
            SetStopped(false);
            agent.SetDestination(target.position);
            nextRepath = Time.time + Mathf.Max(0.05f, repathInterval);
        }
        SetAnimationSpeed(attacking ? 0f : agent.velocity.magnitude);

        float distanceSquared = (target.position - transform.position).sqrMagnitude;
        if (canAttack && Time.time >= nextAttackCheck && distanceSquared <= attackRange * attackRange)
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

    private IEnumerator Attack()
    {
        canAttack = false;
        attacking = true;
        SetStopped(true);
        if (anim != null && hasAttackParameter) anim.SetTrigger(attackTrigger);
        PlayOne(attackClip, attackVol);

        // The player has time to dodge. Starting an animation is not an automatic hit.
        yield return new WaitForSeconds(Mathf.Max(0f, attackWindup));
        if (!IsDead && !RunEnded && target != null && playerHealth != null &&
            (target.position - transform.position).sqrMagnitude <= attackRange * attackRange && HasClearAttack())
        {
            playerHealth.TakeDamage(Mathf.Max(0, damage));
        }

        yield return new WaitForSeconds(Mathf.Max(0.05f, attackCooldown - attackWindup));
        attacking = false;
        canAttack = !IsDead && !RunEnded;
        if (canAttack) SetStopped(false);
    }

    private bool HasClearAttack()
    {
        if (target == null || playerHealth == null) return false;
        Vector3 start = transform.position + Vector3.up;
        Vector3 finish = target.position + Vector3.up;
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
        if (currentHealth == 0) Die();
    }

    private void Die()
    {
        if (IsDead) return;
        IsDead = true;
        StopAllCoroutines();
        canAttack = false;
        attacking = false;
        SetStopped(true);
        if (agent != null) agent.enabled = false;
        foreach (Collider collider in GetComponentsInChildren<Collider>()) collider.enabled = false;

        SetAnimationSpeed(0f);
        if (anim != null)
        {
            if (hasDeadParameter) anim.SetBool(deadBool, true);
            int state = Animator.StringToHash(deathStateName);
            if (!string.IsNullOrEmpty(deathStateName) && anim.HasState(0, state)) anim.CrossFade(state, 0.05f);
        }
        PlayOne(deathClip, deathVol);
        NotifyRoundOnce();
        float delay = deathAnim != null ? deathAnim.length : destroyDelay;
        Destroy(gameObject, Mathf.Max(0.1f, delay));
    }

    private void NotifyRoundOnce()
    {
        if (deathReported || roundOwner == null) return;
        deathReported = true;
        roundOwner.ZombieDied();
    }

    private void OnDestroy()
    {
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
            if (parameter.name == deadBool && parameter.type == AnimatorControllerParameterType.Bool) hasDeadParameter = true;
        }
    }

    private void SetAnimationSpeed(float value)
    {
        if (anim != null && hasSpeedParameter) anim.SetFloat(speedParam, value);
    }

    private void PlayOne(AudioClip clip, float volume)
    {
        if (sfx == null || clip == null) return;
        sfx.pitch = Random.Range(0.95f, 1.05f);
        sfx.PlayOneShot(clip, volume);
    }
}
