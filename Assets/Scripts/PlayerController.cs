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

    Vector3 velocity;
    bool isGrounded;

    void Awake()
    {
        int loadFlag = PlayerPrefs.GetInt("load", 0);
        if (loadFlag == 1)
        {
            GameState gs = SaveSystem.instance.Load();
            if (gs != null)
            {
                Vector3 savedPos = new Vector3(gs.pos_x, gs.pos_y, gs.pos_z);

                if (savedPos != Vector3.zero)
                {
                    // desliga o controller para não dar tilt ao teletransportar
                    if (controller != null) controller.enabled = false;
                    transform.position = savedPos;
                    if (controller != null) controller.enabled = true;

                    Debug.Log("PlayerController: posição aplicada do save = " + savedPos);
                }
            }
        }
    }

    void Update()
    {
        // Verificar chão
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);

        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        Vector3 move = transform.right * x + transform.forward * z;

        controller.Move(move * speed * Time.deltaTime);

        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        velocity.y += gravity * Time.deltaTime;

        controller.Move(new Vector3(0, velocity.y, 0) * Time.deltaTime);
    }
}
