using System.Collections.Generic;
using UnityEngine;

/// <summary>Visible, dodgeable acid throw. Swept collision stops at the first wall or player.</summary>
public sealed class ZombieProjectile : MonoBehaviour
{
    private static readonly List<ZombieProjectile> active = new List<ZombieProjectile>();
    private ZombieAI owner;
    private PlayerHealth target;
    private Vector3 velocity;
    private float expiresAt;
    private int damage;
    private Material material;
    private readonly RaycastHit[] hits = new RaycastHit[24];
    public static int ActiveCount { get { active.RemoveAll(p => p == null); return active.Count; } }

    public static ZombieProjectile Launch(ZombieAI source, PlayerHealth player, Vector3 origin, Vector3 aim, int amount)
    {
        if (source == null || source.archetype != ZombieArchetype.Spitter || source.IsDead || player == null || player.IsDead
            || Time.timeScale <= 0 || ActiveCount >= 12 || (SurvivalSession.Instance != null && SurvivalSession.Instance.IsGameOver)) return null;
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere); go.name = "Acid projectile"; go.layer = 2;
        go.transform.position = origin; go.transform.localScale = Vector3.one * .3f;
        var collider = go.GetComponent<Collider>(); collider.enabled = false; Destroy(collider);
        var projectile = go.AddComponent<ZombieProjectile>(); projectile.owner = source; projectile.target = player;
        projectile.damage = Mathf.Max(1, amount); projectile.expiresAt = Time.time + 3f;
        float flight = Mathf.Clamp((aim - origin).magnitude / 13f, .45f, 1.4f);
        projectile.velocity = (aim - origin) / flight - Vector3.down * (3f * flight);
        var shader = Shader.Find("Standard");
        if (shader != null)
        {
            projectile.material = new Material(shader); projectile.material.color = new Color(.48f, .85f, .08f);
            projectile.material.EnableKeyword("_EMISSION"); projectile.material.SetColor("_EmissionColor", new Color(.7f, 1f, .04f) * 1.5f);
            go.GetComponent<Renderer>().sharedMaterial = projectile.material;
        }
        active.Add(projectile);
        return projectile;
    }

    private void Update()
    {
        var session = SurvivalSession.Instance;
        if (target == null || target.IsDead || (session != null && session.IsGameOver) || Time.time >= expiresAt)
        { Destroy(gameObject); return; }
        if (Time.timeScale <= 0f) return;
        float dt = Mathf.Min(Time.deltaTime, .05f);
        Vector3 delta = velocity * dt + Vector3.down * (3f * dt * dt);
        int count = Physics.SphereCastNonAlloc(new Ray(transform.position, delta.normalized), .15f, hits,
            delta.magnitude, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        float nearest = float.PositiveInfinity; Collider first = null;
        for (int i = 0; i < count; i++)
        {
            Collider hit = hits[i].collider;
            if (hit == null || (owner != null && hit.transform.IsChildOf(owner.transform))) continue;
            if (hits[i].distance < nearest) { nearest = hits[i].distance; first = hit; }
        }
        if (count == hits.Length) { Destroy(gameObject); return; }
        if (first != null)
        {
            if (first.GetComponentInParent<PlayerHealth>() == target) target.TakeDamage(damage);
            Destroy(gameObject); return;
        }
        transform.position += delta;
        velocity += Vector3.down * (6f * dt);
    }

    private void OnDestroy()
    {
        active.Remove(this);
        if (material != null) Destroy(material);
    }
}
