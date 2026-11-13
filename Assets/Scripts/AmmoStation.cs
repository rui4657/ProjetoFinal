using System.Collections.Generic;
using UnityEngine;

public class AmmoStation : MonoBehaviour
{
    [Header("Ammunition")]
    public int giveReserveAmmo = 90;
    public int maxReserveAmmo = 300;
    public bool refillMagazine = true;
    [Header("Interaction")]
    public KeyCode useKey = KeyCode.E;
    public float cooldown = 0.5f;
    public int pointCost = 250;

    readonly HashSet<Collider> contacts = new HashSet<Collider>();
    GunSystem gun;
    PlayerHealth player;
    float nextUseTime;

    void Update()
    {
        if (contacts.Count == 0 || gun == null || player == null || player.IsDead || Time.timeScale <= 0f) return;
        var session = SurvivalSession.Instance;
        if (session != null && (session.IsGameOver || session.IsShopOpen)) return;
        int limit = Mathf.Min(Mathf.Max(0, maxReserveAmmo), gun.maxReserveAmmo);
        bool full = gun.reserveAmmo >= limit && (!refillMagazine || gun.currentAmmo >= gun.magSize);
        if (session != null)
            session.ShowInteraction(full ? "MUNIÇÕES COMPLETAS" : "[" + useKey + "] REABASTECER · " + pointCost + " PONTOS");
        if (full || Time.time < nextUseTime || !Input.GetKeyDown(useKey)) return;
        nextUseTime = Time.time + Mathf.Max(0.1f, cooldown);
        if (session != null && !session.TrySpend(Mathf.Max(0, pointCost))) return;
        int amount = Mathf.Min(Mathf.Max(0, giveReserveAmmo), Mathf.Max(0, limit - gun.reserveAmmo));
        gun.AddReserveAmmo(amount, refillMagazine);
        if (session != null) session.ShowMessage("Munições reabastecidas.");
        if (SaveSystem.instance != null) SaveSystem.instance.RequestSave();
    }

    void OnTriggerEnter(Collider other)
    {
        var health = other.GetComponentInParent<PlayerHealth>();
        if (health == null) return;
        contacts.Add(other);
        player = health;
        gun = health.GetComponentInChildren<GunSystem>();
        if (gun == null) gun = FindFirstObjectByType<GunSystem>();
    }

    void OnTriggerExit(Collider other)
    {
        contacts.Remove(other);
        if (contacts.Count == 0) { gun = null; player = null; }
    }

    void OnDisable()
    {
        contacts.Clear(); gun = null; player = null;
    }
}
