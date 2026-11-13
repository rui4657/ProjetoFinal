using UnityEngine;

public class MouseLook : MonoBehaviour
{
    public float mouseSensitivity = 100f;
    public Transform playerBody;
    [Range(0.1f, 1f)] public float aimSensitivityMultiplier = 0.6f;
    [Min(1f)] public float recoilRecovery = 12f;

    private float xRotation;
    private Vector2 recoilOffset;
    private PlayerHealth health;
    private GunSystem gun;
    private bool hasFocus = true;

    void Start()
    {
        mouseSensitivity = Mathf.Clamp(PlayerPrefs.GetFloat("opt_sens", 100f), 1f, 500f);
        if (!playerBody) playerBody = GetComponentInParent<PlayerController>()?.transform;
        if (playerBody)
        {
            health = playerBody.GetComponent<PlayerHealth>();
            gun = playerBody.GetComponentInChildren<GunSystem>();
        }
        xRotation = transform.localEulerAngles.x;
        if (xRotation > 180f) xRotation -= 360f;
        if (Time.timeScale > 0f) SetCursor(true);
    }

    void Update()
    {
        bool blocked = Time.timeScale <= 0f || !hasFocus || !playerBody || (health && health.IsDead)
            || (SurvivalSession.Instance != null && SurvivalSession.Instance.IsGameOver);
        if (blocked)
        {
            SetCursor(false);
            return;
        }
        // A click after changing focus deliberately resumes mouse capture.
        if (Cursor.lockState != CursorLockMode.Locked)
        {
            if (!Input.GetMouseButtonDown(0)) return;
            SetCursor(true);
        }
        float sensitivity = Mathf.Clamp(mouseSensitivity, 1f, 500f) * 0.02f;
        if (gun && gun.IsAiming) sensitivity *= aimSensitivityMultiplier;
        // Legacy Mouse X/Y contain frame delta already; deltaTime made aiming FPS dependent.
        float mouseX = Input.GetAxis("Mouse X") * sensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * sensitivity;
        xRotation = Mathf.Clamp(xRotation - mouseY, -85f, 85f);
        recoilOffset = Vector2.Lerp(recoilOffset, Vector2.zero, 1f - Mathf.Exp(-recoilRecovery * Time.deltaTime));
        transform.localRotation = Quaternion.Euler(Mathf.Clamp(xRotation - recoilOffset.x, -89f, 89f), recoilOffset.y, 0f);
        playerBody.Rotate(Vector3.up * mouseX);
    }

    public void AddRecoil(float vertical, float horizontal)
    {
        recoilOffset.x = Mathf.Clamp(recoilOffset.x + vertical, -10f, 12f);
        recoilOffset.y = Mathf.Clamp(recoilOffset.y + horizontal, -6f, 6f);
    }

    void OnApplicationFocus(bool focused)
    {
        hasFocus = focused;
        if (!focused) SetCursor(false);
    }

    static void SetCursor(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }
}
