using UnityEngine;

public class MouseLook : MonoBehaviour
{
    public float mouseSensitivity = 100f;
    public Transform playerBody;

    float xRotation = 0f;

    void Start()
    {
        // bloquear o rato no centro do ecrã 
        mouseSensitivity = PlayerPrefs.GetFloat("opt_sens", 100f);
        Cursor.lockState = CursorLockMode.Locked;
        mouseSensitivity = PlayerPrefs.GetFloat("opt_sens", 1f);

    }

    void Update()
    {
        if (Time.timeScale == 0f)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            return;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        //Movimento do rato
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        // Cima e baixo
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f); // Bloquear o movimento vertical

        // Vertical
        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        //Horizontal
        playerBody.Rotate(Vector3.up * mouseX);
    }
}
