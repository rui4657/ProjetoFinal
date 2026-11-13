using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.AI;

/// <summary>Solid window sill for players; enemies break boards, then vault to a validated inner landing.</summary>
public sealed class BarricadeEntry : MonoBehaviour
{
    public BreachSite Site { get; private set; }
    public BarricadeNetwork Network { get; private set; }
    public int Planks => Network != null ? Network.Ledger.Count(Site.Id) : 0;
    public bool IsOpenSector => Network != null && (Network.World.UnlockedZones & Site.RequiredZone) == Site.RequiredZone;
    public Vector3 InnerPoint => transform.position + transform.forward * 3;
    public Vector3 OuterPoint => transform.position - transform.forward * 3;
    public bool HasCapacity { get { visitors.RemoveAll(z => z == null || z.IsDead); return visitors.Count < 3; } }
    private readonly List<ZombieAI> visitors = new List<ZombieAI>();
    private readonly Transform[] boards = new Transform[6];
    private readonly BoxCollider[] boardColliders = new BoxCollider[6];
    private readonly float[] fellAt = new float[6];
    private Material concrete, wood, iron;
    private TextMeshPro caption;
    private float nextRepairAt;
    private int visiblePlanks = -1;

    public static BarricadeEntry Create(BarricadeNetwork network, BreachSite site, Vector3 point)
    {
        var go = new GameObject("Barricade " + site.Id); go.transform.SetParent(network.transform, false); go.transform.position = point;
        var entry = go.AddComponent<BarricadeEntry>(); entry.Site = site; entry.Network = network;
        entry.Build(); go.transform.rotation = Quaternion.LookRotation(site.Inward);
        network.Entries.Add(entry); return entry;
    }
    private void Build()
    {
        concrete = WeaponModels.Material(new Color(.29f, .3f, .27f), .05f);
        wood = WeaponModels.Material(new Color(.29f, .19f, .1f), .02f);
        iron = WeaponModels.Material(new Color(.12f, .14f, .14f), .75f);
        Solid("Window sill", new Vector3(0, .85f, 0), new Vector3(4, 1.7f, .7f), concrete);
        Solid("Left pier", new Vector3(-1.78f, 2.85f, 0), new Vector3(.44f, 2.3f, .7f), concrete);
        Solid("Right pier", new Vector3(1.78f, 2.85f, 0), new Vector3(.44f, 2.3f, .7f), concrete);
        Solid("Lintel", new Vector3(0, 4.03f, 0), new Vector3(4, .26f, .7f), concrete);
        var obstacle = gameObject.AddComponent<NavMeshObstacle>(); obstacle.shape = NavMeshObstacleShape.Box;
        obstacle.center = Vector3.up * 2; obstacle.size = new Vector3(4, 4.2f, 1); obstacle.carving = true;
        var prefab = Resources.Load<GameObject>("World/BarricadePlank");
        for (int i = 0; i < boards.Length; i++)
        {
            var board = new GameObject("Repairable plank " + i).transform; board.SetParent(transform, false);
            boards[i] = board; ResetBoard(i);
            board.localRotation = Quaternion.identity;
            if (prefab != null) WorldArt.Fit(prefab, board, new Vector3(3.25f, .23f, .12f));
            else WeaponModels.Piece(board, "Timber", PrimitiveType.Cube, Vector3.zero, new Vector3(3.25f, .23f, .12f), wood);
            boardColliders[i] = board.gameObject.AddComponent<BoxCollider>(); boardColliders[i].size = new Vector3(3.25f, .23f, .12f);
            for (int nail = -1; nail <= 1; nail += 2)
                WeaponModels.Piece(board, "Iron nail", PrimitiveType.Sphere, new Vector3(nail * 1.4f, 0, .075f), Vector3.one * .035f, iron);
        }
        var plate = new GameObject("Window label", typeof(RectTransform), typeof(TextMeshPro)); plate.transform.SetParent(transform, false);
        plate.transform.localPosition = new Vector3(0, 1.35f, .38f); plate.transform.localRotation = Quaternion.Euler(0, 180, 0);
        caption = plate.GetComponent<TextMeshPro>(); caption.font = TMP_Settings.defaultFontAsset; caption.fontSize = 2;
        caption.color = new Color(.85f, .8f, .62f); caption.alignment = TextAlignmentOptions.Center;
        caption.rectTransform.sizeDelta = new Vector2(2.7f, .5f);
        var station = gameObject.AddComponent<WorldInteraction>(); station.Configure(Network.World,
            WorldInteraction.InteractionKind.Barricade, "BARRICADA", 3.6f, transform);
        RefreshBoards(true);
    }
    private void Solid(string title, Vector3 point, Vector3 size, Material material)
    {
        var part = WeaponModels.Piece(transform, title, PrimitiveType.Cube, point, size, material);
        part.gameObject.AddComponent<BoxCollider>();
    }
    private void ResetBoard(int i)
    { boards[i].localPosition = new Vector3(0, 1.94f + i * .35f, .08f); boards[i].localRotation = Quaternion.Euler(0, 0, i % 2 == 0 ? 4 : -4); }
    public void RefreshBoards(bool immediate)
    {
        int next = Planks;
        for (int i = 0; i < boards.Length; i++)
        {
            if (boards[i] == null) continue;
            if (i < next) { boards[i].gameObject.SetActive(true); ResetBoard(i); fellAt[i] = -1; }
            else if (!immediate && i < visiblePlanks) { fellAt[i] = Time.time; boards[i].gameObject.SetActive(true); }
            else if (immediate) { boards[i].gameObject.SetActive(false); fellAt[i] = -1; }
            if (boardColliders[i] != null) boardColliders[i].enabled = i < next;
        }
        visiblePlanks = next;
        if (caption != null) caption.text = "BARRICADA " + (Site.Id + 1) + "  ·  " + next + "/6\n<size=65%>MANTÉM [E] PARA REPARAR</size>";
    }
    public bool TryRepair()
    {
        if (Time.time < nextRepairAt || !IsOpenSector || Planks >= 6) return false;
        if (!Network.Repair(this)) return false;
        nextRepairAt = Time.time + .8f; return true;
    }
    public bool Register(ZombieAI zombie)
    { if (!HasCapacity || zombie == null || visitors.Contains(zombie)) return false; visitors.Add(zombie); return true; }
    public void Release(ZombieAI zombie) { visitors.Remove(zombie); }
    public bool Bash(ZombieAI zombie)
    {
        if (zombie == null || zombie.IsDead || !visitors.Contains(zombie) || Time.timeScale <= 0) return false;
        bool changed = Network.Ledger.Damage(Site.Id, zombie.archetype == ZombieArchetype.Brute ? 2 : 1);
        if (changed) { RefreshBoards(false); Network.Save(); } return changed;
    }
    private void Update()
    {
        if (Time.timeScale <= 0) return;
        for (int i = 0; i < boards.Length; i++)
        {
            if (i < Planks || fellAt[i] < 0 || boards[i] == null) continue;
            float age = Time.time - fellAt[i];
            if (age > .8f) { boards[i].gameObject.SetActive(false); fellAt[i] = -1; continue; }
            boards[i].localPosition += Vector3.down * (Time.deltaTime * (1 + age * 5));
            boards[i].localRotation *= Quaternion.Euler(Time.deltaTime * 85, 0, Time.deltaTime * 35);
        }
    }
    private void OnDestroy()
    {
        if (Network != null) Network.Entries.Remove(this);
        if (concrete != null) Destroy(concrete); if (wood != null) Destroy(wood); if (iron != null) Destroy(iron);
    }
}
