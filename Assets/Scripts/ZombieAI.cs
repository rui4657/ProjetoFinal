using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class ZombieAI : MonoBehaviour
{
    NavMeshAgent agent;
    Transform target;
    PlayerHealth playerHealth;
    Animator anim;

    [Header("Combate")]
    public float attackRange = 2.0f;
    public int damage = 10;
    public float attackCooldown = 1.5f;

    bool canAttack = true;
    bool isDead = false;

    [Header("Vida")]
    public int maxHealth = 50;
    int currentHealth;

    [Header("Animator (nomes têm de bater com o Animator Controller)")]
    public string speedParam = "Speed";     // Float
    public string attackTrigger = "Attack"; // Trigger
    public string deadBool = "Dead";        // Bool
    public string deathStateName = "death1"; // nome do STATE no Animator

    [Header("Audio (3D)")]
    public AudioSource sfx;
    public AudioClip attackClip;
    public AudioClip hurtClip;
    public AudioClip deathClip;
    [Range(0f, 1f)] public float attackVol = 1f;
    [Range(0f, 1f)] public float hurtVol = 0.8f;
    [Range(0f, 1f)] public float deathVol = 1f;

    [Header("Anti-spam Hurt")]
    public float hurtSfxCooldown = 0.15f;
    float nextHurtTime = 0f;

    [Header("Morte")]
    public AnimationClip deathAnim;  // arrasta aqui o clip death1 (recomendado)
    public float destroyDelay = 2f;  // usado se deathAnim não estiver ligado

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponentInChildren<Animator>();

        if (sfx == null) sfx = GetComponent<AudioSource>();
        if (sfx == null) sfx = gameObject.AddComponent<AudioSource>();

        // defaults 3D seguros
        sfx.playOnAwake = false;
        sfx.loop = false;
        sfx.spatialBlend = 1f;
        sfx.rolloffMode = AudioRolloffMode.Logarithmic;
        sfx.minDistance = 1.5f;
        sfx.maxDistance = 20f;
        sfx.dopplerLevel = 0f;
    }

    void Start()
    {
        currentHealth = maxHealth;

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
        {
            target = p.transform;
            playerHealth = p.GetComponent<PlayerHealth>();
        }

        if (agent) agent.stoppingDistance = Mathf.Max(0f, attackRange - 0.1f);
    }

    void Update()
    {
        if (isDead || target == null || agent == null) return;

        agent.SetDestination(target.position);

        // anima conforme o movimento do NavMesh
        if (anim) anim.SetFloat(speedParam, agent.desiredVelocity.magnitude);

        float dist = Vector3.Distance(transform.position, target.position);
        if (dist <= attackRange && canAttack && playerHealth != null)
            StartCoroutine(Attack());
    }

    IEnumerator Attack()
    {
        canAttack = false;

        if (agent) agent.isStopped = true;

        if (anim) anim.SetTrigger(attackTrigger);
        PlayOne(attackClip, attackVol);

        playerHealth.TakeDamage(damage);

        yield return new WaitForSeconds(attackCooldown);

        if (!isDead && agent) agent.isStopped = false;
        canAttack = true;
    }

    public void TakeDamage(int amount)
    {
        if (isDead) return;

        currentHealth -= amount;

        if (hurtClip && Time.time >= nextHurtTime)
        {
            PlayOne(hurtClip, hurtVol);
            nextHurtTime = Time.time + hurtSfxCooldown;
        }

        if (currentHealth <= 0)
            Die();
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;

        StopAllCoroutines();
        canAttack = false;

        if (agent)
        {
            agent.isStopped = true;
            agent.enabled = false;
        }

        foreach (var c in GetComponentsInChildren<Collider>())
            c.enabled = false;

        if (anim)
        {
            anim.SetFloat(speedParam, 0f);
            anim.SetBool(deadBool, true);
            anim.CrossFade(deathStateName, 0.05f);
        }

        PlayOne(deathClip, deathVol);

        RoundManager rm = FindObjectOfType<RoundManager>();
        if (rm != null) rm.ZombieDied();

        float delay = (deathAnim != null) ? deathAnim.length : destroyDelay;
        Destroy(gameObject, delay);
    }

    void PlayOne(AudioClip clip, float vol)
    {
        if (!sfx || !clip) return;
        sfx.pitch = Random.Range(0.95f, 1.05f);
        sfx.PlayOneShot(clip, vol);
    }
}
