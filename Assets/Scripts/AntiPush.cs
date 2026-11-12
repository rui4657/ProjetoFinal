using UnityEngine;

public class AntiPush : MonoBehaviour
{
    public CharacterController controller;

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        // Se bater numa parede, dá um mini “pushback”
        if (hit.collider.CompareTag("Wall"))
        {
            Vector3 push = hit.normal * 0.3f;
            controller.Move(push);
        }
    }
}
