using UnityEngine;

public class AmmoStation : MonoBehaviour
{
    [Header("Ammo que dá")]
    public int giveReserveAmmo = 90;   // quanto adiciona à reserva
    public int maxReserveAmmo = 300;   // limite da reserva
    public bool refillMagazine = true; // enche o carregador também

    [Header("Uso")]
    public KeyCode useKey = KeyCode.E;
    public float cooldown = 0.5f;

    private GunSystem gun;
    private bool canUse;
    private float nextUseTime;

    void Update()
    {
        if (!canUse) return;
        if (Time.time < nextUseTime) return;

        if (Input.GetKeyDown(useKey))
        {
            if (!gun) gun = FindFirstObjectByType<GunSystem>();
            if (!gun) return;

            if (refillMagazine)
                gun.currentAmmo = gun.magSize;

            gun.reserveAmmo = Mathf.Clamp(gun.reserveAmmo + giveReserveAmmo, 0, maxReserveAmmo);

            nextUseTime = Time.time + cooldown;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        canUse = true;

        // tenta buscar a arma no player (se estiver como child)
        gun = other.GetComponentInChildren<GunSystem>();
        if (!gun) gun = FindFirstObjectByType<GunSystem>();
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        canUse = false;
    }
}
