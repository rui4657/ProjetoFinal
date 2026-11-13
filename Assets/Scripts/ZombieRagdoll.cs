using System.Collections.Generic;
using UnityEngine;

/// <summary>Uses the deforming skeleton, not animation-controller subasset IDs, for reliable death falls.</summary>
public sealed class ZombieRagdoll : MonoBehaviour
{
    private sealed class Part
    {
        public Transform Bone;
        public Rigidbody Body;
        public Collider Collider;
        public Vector3 Position;
        public Quaternion Rotation;
    }
    private static readonly List<ZombieRagdoll> corpses = new List<ZombieRagdoll>();
    private readonly List<Part> parts = new List<Part>();
    private Animator animator;
    private bool dead, sinking, fallback;
    private float deathAt, sinkAt;
    private Quaternion fallFrom, fallTo;
    private Vector3 fallPosition;
    public bool HasSkeleton => parts.Count >= 5;
    public bool IsFalling => dead;
    public float Age => dead ? Mathf.Max(0, Time.time - deathAt) : 0;

    public void Prepare(Animator source)
    {
        animator = source;
        if (parts.Count > 0) return;
        string[] names = { "Hips", "Spine", "Head", "LeftArm", "LeftForeArm", "RightArm", "RightForeArm",
            "LeftUpLeg", "LeftLeg", "RightUpLeg", "RightLeg" };
        string[] ends = { "Spine", "Neck", null, "LeftForeArm", "LeftHand", "RightForeArm", "RightHand",
            "LeftLeg", "LeftFoot", "RightLeg", "RightFoot" };
        var player = FindFirstObjectByType<PlayerHealth>();
        for (int i = 0; i < names.Length; i++)
        {
            Transform bone = FindBone(names[i]); if (bone == null) continue;
            Transform end = ends[i] != null ? FindBone(ends[i]) : null;
            var body = bone.GetComponent<Rigidbody>(); if (body == null) body = bone.gameObject.AddComponent<Rigidbody>();
            body.isKinematic = true; body.useGravity = false; body.mass = i == 0 ? 8 : i == 1 ? 6 : i == 2 ? 2 : 2.5f;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            body.linearDamping = .15f; body.angularDamping = .8f; body.maxAngularVelocity = 10;
            var collider = bone.gameObject.AddComponent<CapsuleCollider>(); collider.enabled = false;
            Vector3 localEnd = end != null ? bone.InverseTransformPoint(end.position) : Vector3.up * .18f;
            if (localEnd.sqrMagnitude < .0001f) localEnd = Vector3.up * .18f;
            collider.center = localEnd * .5f;
            Vector3 absolute = new Vector3(Mathf.Abs(localEnd.x), Mathf.Abs(localEnd.y), Mathf.Abs(localEnd.z));
            collider.direction = absolute.x > absolute.y && absolute.x > absolute.z ? 0 : absolute.z > absolute.y ? 2 : 1;
            collider.radius = i <= 1 ? .13f : i == 2 ? .085f : .06f;
            collider.height = Mathf.Max(collider.radius * 2, localEnd.magnitude);
            if (player != null)
                foreach (var playerCollider in player.GetComponentsInChildren<Collider>()) Physics.IgnoreCollision(collider, playerCollider, true);
            foreach (var other in parts) Physics.IgnoreCollision(collider, other.Collider, true);
            parts.Add(new Part { Bone = bone, Body = body, Collider = collider });
        }
        foreach (var part in parts)
        {
            Part parentPart = null;
            for (Transform parent = part.Bone.parent; parent != null && parentPart == null; parent = parent.parent)
                foreach (var other in parts) if (other.Bone == parent) { parentPart = other; break; }
            if (parentPart == null) continue;
            var joint = part.Bone.gameObject.AddComponent<CharacterJoint>(); joint.connectedBody = parentPart.Body;
            joint.autoConfigureConnectedAnchor = false; joint.anchor = Vector3.zero;
            joint.connectedAnchor = parentPart.Bone.InverseTransformPoint(part.Bone.position);
            joint.axis = Vector3.right; joint.swingAxis = Vector3.up;
            joint.lowTwistLimit = new SoftJointLimit { limit = -35 };
            joint.highTwistLimit = new SoftJointLimit { limit = 35 };
            joint.swing1Limit = new SoftJointLimit { limit = 65 };
            joint.swing2Limit = new SoftJointLimit { limit = 45 };
            joint.enableCollision = false; joint.enablePreprocessing = false;
            joint.enableProjection = true; joint.projectionDistance = .12f; joint.projectionAngle = 15;
        }
    }

    public Transform FindBone(string suffix)
    {
        // The prefab contains both control and deform bones. Only the latter skin the mesh.
        foreach (var renderer in GetComponentsInChildren<SkinnedMeshRenderer>(true))
            foreach (var bone in renderer.bones)
                if (bone != null && bone.name == "Zombie_" + suffix) return bone;
        foreach (var bone in GetComponentsInChildren<Transform>(true))
            if (bone.name == "Zombie_" + suffix) return bone;
        return null;
    }

    public void Collapse(bool headshot, Vector3 direction)
    {
        if (dead) return;
        dead = true; deathAt = Time.time;
        foreach (var part in parts) { part.Position = part.Bone.position; part.Rotation = part.Bone.rotation; }
        if (animator != null) animator.enabled = false;
        foreach (var renderer in GetComponentsInChildren<SkinnedMeshRenderer>(true)) renderer.updateWhenOffscreen = true;
        foreach (var part in parts)
        {
            // Preserve the visible pose even if disabling Animator resets its defaults.
            part.Bone.position = part.Position; part.Bone.rotation = part.Rotation;
            part.Body.position = part.Position; part.Body.rotation = part.Rotation;
            part.Bone.gameObject.layer = 2;
            part.Collider.enabled = HasSkeleton; part.Body.isKinematic = !HasSkeleton; part.Body.useGravity = HasSkeleton;
        }
        if (HasSkeleton)
        {
            Vector3 impulse = direction.sqrMagnitude > .01f ? direction.normalized : -transform.forward;
            foreach (var part in parts)
            {
                if (part.Bone.name == "Zombie_Hips") part.Body.AddForce((impulse * 1.6f + Vector3.up * .25f) * part.Body.mass, ForceMode.Impulse);
                if (headshot && part.Bone.name == "Zombie_Head") part.Body.AddForce(impulse * part.Body.mass * 2.4f, ForceMode.Impulse);
            }
        }
        else
        {
            // A custom prefab with no matching skeleton still falls and stays down; never waits upright in a bind pose.
            fallback = true; fallFrom = transform.rotation; fallPosition = transform.position;
            fallTo = fallFrom * Quaternion.Euler(82, 0, headshot ? 12 : -8);
        }
        corpses.RemoveAll(corpse => corpse == null); corpses.Add(this);
        while (corpses.Count > 12) { var oldest = corpses[0]; corpses.RemoveAt(0); if (oldest != null) oldest.BeginSink(); }
    }

    private void BeginSink()
    {
        if (sinking) return;
        sinking = true; sinkAt = Time.time;
        foreach (var part in parts) { part.Body.isKinematic = true; part.Collider.enabled = false; }
    }

    private void Update()
    {
        if (!dead || Time.timeScale <= 0) return;
        if (fallback && !sinking)
        {
            float t = Mathf.Clamp01((Time.time - deathAt) / .65f);
            transform.rotation = Quaternion.Slerp(fallFrom, fallTo, Mathf.SmoothStep(0, 1, t));
            transform.position = fallPosition - Vector3.up * (t * .25f);
        }
        if (!sinking && Time.time - deathAt >= 6) BeginSink();
        if (sinking)
        {
            transform.position += Vector3.down * (Time.deltaTime * 2);
            if (Time.time - sinkAt >= 1.5f) Destroy(gameObject);
        }
    }

    private void OnDestroy() { corpses.Remove(this); }
}
