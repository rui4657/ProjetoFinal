using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public CharacterController controller;
    public float speed = 6f;
    public float gravity = -30f;
    public float jumpHeight = 1.2f;
    public Transform groundCheck;
    public float groundDistance = 0.2f;
    public LayerMask groundMask;

    [Header("Movimento")]
    [Min(1f)] public float sprintMultiplier = 1.5f;
    [Min(1f)] public float acceleration = 30f;
    [Min(1f)] public float maxStamina = 100f;
    [Min(1f)] public float sprintDrain = 22f;
    [Min(1f)] public float staminaRecovery = 18f;
    [Min(0f)] public float recoveryDelay = 0.8f;

    public bool IsSprinting { get; private set; }
    public float StaminaNormalized => Mathf.Clamp01(stamina / Mathf.Max(1f, maxStamina));

    private PlayerHealth health;
    private GunSystem gun;
    private Vector3 planarVelocity;
    private float verticalVelocity;
    private float stamina;
    private float lastSprintTime;
    private bool exhausted;

    void Awake()
    {
        if (!controller) controller = GetComponent<CharacterController>();
        health = GetComponent<PlayerHealth>();
        gun = GetComponentInChildren<GunSystem>();
        stamina = Mathf.Max(1f, maxStamina);
    }

    void Update()
    {
        if (!controller || !controller.enabled || Time.timeScale <= 0f || (health && health.IsDead)
            || (SurvivalSession.Instance != null && SurvivalSession.Instance.IsGameOver)
            || Cursor.lockState != CursorLockMode.Locked)
        {
            IsSprinting = false;
            planarVelocity = Vector3.zero;
            return;
        }
        bool grounded = controller.isGrounded;
        if (groundCheck && groundMask.value != 0)
            grounded |= Physics.CheckSphere(groundCheck.position, Mathf.Max(0.01f, groundDistance), groundMask, QueryTriggerInteraction.Ignore);
        if (grounded && verticalVelocity < 0f) verticalVelocity = -2f;
        Vector2 input = Vector2.ClampMagnitude(new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")), 1f);
        if (stamina >= maxStamina * 0.25f) exhausted = false;
        IsSprinting = !exhausted && stamina > 0f && input.y > 0.1f
            && Input.GetKey(KeyCode.LeftShift) && !(gun && gun.IsAiming);
        if (IsSprinting)
        {
            stamina = Mathf.Max(0f, stamina - sprintDrain * Time.deltaTime);
            lastSprintTime = Time.time;
            if (stamina <= 0f) { exhausted = true; IsSprinting = false; }
        }
        else if (Time.time - lastSprintTime >= recoveryDelay)
            stamina = Mathf.Min(maxStamina, stamina + staminaRecovery * Time.deltaTime);
        float moveSpeed = Mathf.Max(0f, speed) * (IsSprinting ? sprintMultiplier : 1f);
        if (gun && gun.IsAiming) moveSpeed *= 0.65f;
        Vector3 targetVelocity = (transform.right * input.x + transform.forward * input.y) * moveSpeed;
        planarVelocity = Vector3.MoveTowards(planarVelocity, targetVelocity, acceleration * Time.deltaTime);
        float effectiveGravity = Mathf.Min(-0.01f, gravity);
        if (Input.GetButtonDown("Jump") && grounded)
            verticalVelocity = Mathf.Sqrt(Mathf.Max(0f, jumpHeight) * -2f * effectiveGravity);
        verticalVelocity = Mathf.Max(-60f, verticalVelocity + effectiveGravity * Time.deltaTime);
        CollisionFlags collision = controller.Move((planarVelocity + Vector3.up * verticalVelocity) * Time.deltaTime);
        if ((collision & CollisionFlags.Above) != 0 && verticalVelocity > 0f) verticalVelocity = 0f;
    }

    public void Teleport(Vector3 position)
    {
        if (float.IsNaN(position.x) || float.IsNaN(position.y) || float.IsNaN(position.z)
            || float.IsInfinity(position.x) || float.IsInfinity(position.y) || float.IsInfinity(position.z)) return;
        bool wasEnabled = controller && controller.enabled;
        if (wasEnabled) controller.enabled = false;
        transform.position = position;
        if (wasEnabled) controller.enabled = true;
        planarVelocity = Vector3.zero;
        verticalVelocity = 0f;
    }

    public void ApplyLoadedPosition(Vector3 position) => Teleport(position);
}
