using UnityEngine;

/// <summary>Separate skins live in the four prefab assets; these attachments distinguish silhouettes too.</summary>
public sealed class ZombieAppearance : MonoBehaviour
{
    private Material attachmentMaterial;
    public void Build(ZombieArchetype kind)
    {
        if (kind == ZombieArchetype.Walker || kind == ZombieArchetype.Runner) return;
        var shader = Shader.Find("Standard"); if (shader == null) return;
        attachmentMaterial = new Material(shader);
        attachmentMaterial.color = kind == ZombieArchetype.Brute ? new Color(.15f, .18f, .16f) : new Color(.48f, .62f, .12f);
        if (kind == ZombieArchetype.Brute)
        { attachmentMaterial.SetFloat("_Metallic", .8f); attachmentMaterial.SetFloat("_Glossiness", .22f); }
        else
        { attachmentMaterial.EnableKeyword("_EMISSION"); attachmentMaterial.SetColor("_EmissionColor", new Color(.15f, .25f, .025f)); }
        Transform chest = Bone("Zombie_Spine1") ?? Bone("Zombie_Spine");
        Transform head = Bone("Zombie_Head");
        if (kind == ZombieArchetype.Brute)
        {
            Attach(chest, "Reinforced chest plate", PrimitiveType.Cube, new Vector3(0, .08f, .1f), new Vector3(.4f, .35f, .12f));
            Attach(Bone("Zombie_LeftArm"), "Left shoulder armour", PrimitiveType.Sphere, Vector3.zero, new Vector3(.22f, .28f, .25f));
            Attach(Bone("Zombie_RightArm"), "Right shoulder armour", PrimitiveType.Sphere, Vector3.zero, new Vector3(.22f, .28f, .25f));
            Attach(head, "Steel helmet", PrimitiveType.Sphere, new Vector3(0, .1f, -.025f), new Vector3(.23f, .22f, .23f));
        }
        else
        {
            for (int i = 0; i < 5; i++)
                Attach(chest, "Acid gland", PrimitiveType.Sphere, new Vector3((i % 2 == 0 ? -1 : 1) * .11f, .05f + i * .035f, -.12f),
                    Vector3.one * (.1f + i * .009f));
            Attach(Bone("Zombie_Neck"), "Acid throat", PrimitiveType.Sphere, new Vector3(0, .02f, .04f), new Vector3(.17f, .14f, .17f));
        }
    }

    private Transform Bone(string name)
    {
        foreach (var child in GetComponentsInChildren<Transform>(true)) if (child.name == name) return child;
        return null;
    }

    private void Attach(Transform parent, string title, PrimitiveType shape, Vector3 position, Vector3 scale)
    {
        if (parent == null) return;
        var part = GameObject.CreatePrimitive(shape); part.name = title; part.layer = 6;
        part.transform.SetParent(parent, false); part.transform.localPosition = position; part.transform.localScale = scale;
        part.GetComponent<Renderer>().sharedMaterial = attachmentMaterial;
        var collider = part.GetComponent<Collider>(); collider.enabled = false; Destroy(collider);
    }

    private void OnDestroy() { if (attachmentMaterial != null) Destroy(attachmentMaterial); }
}
