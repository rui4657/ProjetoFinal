using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

/// <summary>Runs against the real imported assets and physics in the user's Unity Editor.</summary>
public static class DeadlineValidation
{
    [MenuItem("Tools/DEADLINE/Verificar zonas e variantes")]
    public static void Validate()
    {
        var issues = new List<string>();
        var textures = new HashSet<Texture>();
        string[] names = { "Walker", "Runner", "Brute", "Spitter" };
        for (int i = 0; i < names.Length; i++)
        {
            var prefab = Resources.Load<GameObject>("Zombies/" + names[i]);
            if (prefab == null) { issues.Add("Falta o prefab " + names[i]); continue; }
            var zombie = prefab.GetComponent<ZombieAI>();
            if (zombie == null || (int)zombie.archetype != i) issues.Add("Papel incorreto no prefab " + names[i]);
            bool head = false, hips = false;
            foreach (var renderer in prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                foreach (var bone in renderer.bones)
                {
                    if (bone == null) continue;
                    head |= bone.name == "Zombie_Head"; hips |= bone.name == "Zombie_Hips";
                }
                foreach (var material in renderer.sharedMaterials)
                    if (material != null && material.HasProperty("_MainTex") && material.GetTexture("_MainTex") != null)
                        textures.Add(material.GetTexture("_MainTex"));
            }
            if (!head || !hips) issues.Add("Rig deformador incompleto em " + names[i]);
        }
        if (textures.Count < 3) issues.Add("As variantes não têm os três conjuntos de pele importados.");
        if (Application.isPlaying)
        {
            var world = WorldProgression.Instance;
            if (world == null || world.GetComponentInChildren<QuarantineZones>() == null) issues.Add("Os perímetros não foram instalados nesta cena.");
            else
            {
                Physics.SyncTransforms();
                foreach (var station in Object.FindObjectsByType<WorldInteraction>(FindObjectsSortMode.None))
                {
                    int flag = station.Kind == WorldInteraction.InteractionKind.ChurchGate ? 1
                        : station.Kind == WorldInteraction.InteractionKind.VillageGate ? 2
                        : station.Kind == WorldInteraction.InteractionKind.OutskirtsGate ? 4 : 0;
                    if (flag == 0) continue;
                    var collider = station.GetComponentInParent<BoxCollider>();
                    var obstacle = station.GetComponentInParent<NavMeshObstacle>();
                    bool closed = (world.UnlockedZones & flag) == 0;
                    if (collider == null || obstacle == null || collider.enabled != closed || obstacle.enabled != closed || !obstacle.carving)
                    { issues.Add("Colisão/navegação incoerente em " + station.Title); continue; }
                    if (!closed) continue;
                    Vector3 normal = collider.transform.forward;
                    var hits = Physics.RaycastAll(collider.bounds.center - normal * 1.2f, normal, 2.4f,
                        Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
                    bool solid = false; foreach (var hit in hits) if (hit.collider == collider) solid = true;
                    if (!solid) issues.Add("A física não detetou o portão fechado " + station.Title);
                }
                foreach (var zombie in Object.FindObjectsByType<ZombieAI>(FindObjectsSortMode.None))
                {
                    if (!zombie.IsDead) continue;
                    var ragdoll = zombie.GetComponent<ZombieRagdoll>();
                    if (ragdoll == null || !ragdoll.IsFalling) { issues.Add("Morte sem queda física: " + zombie.name); continue; }
                    if (ragdoll.HasSkeleton && ragdoll.Age > 2 && ragdoll.Age < 5)
                    {
                        var head = ragdoll.FindBone("Head"); var hips = ragdoll.FindBone("Hips");
                        if (head != null && hips != null && head.position.y - hips.position.y > 1.3f)
                            Debug.LogWarning("Confirmar visualmente este cadáver: ainda parece vertical. " + zombie.name, zombie);
                    }
                }
            }
        }
        if (issues.Count == 0)
            Debug.Log("DEADLINE: variantes/skins/rig válidos" + (Application.isPlaying ? "; portões, navegação e raios físicos coerentes nesta partida." : ". Entra em Play e repete para verificar os portões reais."));
        else Debug.LogError("DEADLINE — verificações a corrigir:\n" + string.Join("\n", issues));
    }
}
