using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.AI;

public sealed class WeaponRack : MonoBehaviour
{
    public bool IsReady => Time.time >= readyAt;
    private WorldInteraction station;
    private Material steel, timber, brass;
    private TextMeshPro caption;
    private Transform display, lid;
    private float readyAt, nextCaption;
    private bool mystery;
    private WeaponId lastOffer;

    public void Build(WorldInteraction interaction, bool isMystery)
    {
        station = interaction; mystery = isMystery;
        steel = WeaponModels.Material(new Color(.13f, .17f, .18f), .7f);
        timber = WeaponModels.Material(new Color(.27f, .17f, .085f), .02f);
        brass = WeaponModels.Material(new Color(.58f, .47f, .23f), .65f);
        var body = gameObject.AddComponent<BoxCollider>();
        body.center = Vector3.up * (mystery ? .6f : .9f); body.size = new Vector3(1.75f, mystery ? 1.2f : 1.8f, mystery ? .8f : .45f);
        var obstacle = gameObject.AddComponent<NavMeshObstacle>(); obstacle.shape = NavMeshObstacleShape.Box;
        obstacle.center = body.center; obstacle.size = body.size; obstacle.carving = true;
        if (mystery)
        {
            WeaponModels.Piece(transform, "Supply chest", PrimitiveType.Cube, new Vector3(0, .66f, 0), new Vector3(1.6f, .68f, .7f), timber);
            for (int side = -1; side <= 1; side += 2)
            {
                WeaponModels.Piece(transform, "Chest band", PrimitiveType.Cube, new Vector3(side * .58f, .66f, .36f), new Vector3(.07f, .7f, .025f), steel);
                WeaponModels.Piece(transform, "Chest foot", PrimitiveType.Cube, new Vector3(side * .6f, .18f, 0), new Vector3(.18f, .36f, .55f), steel);
            }
            lid = new GameObject("Chest lid hinge").transform; lid.SetParent(transform, false); lid.localPosition = new Vector3(0, 1.02f, -.35f);
            WeaponModels.Piece(lid, "Chest lid", PrimitiveType.Cube, new Vector3(0, 0, .35f), new Vector3(1.66f, .09f, .76f), timber);
            WeaponModels.Piece(transform, "Question emblem", PrimitiveType.Cube, new Vector3(0, .72f, .36f), new Vector3(.36f, .36f, .02f), brass);
        }
        else
        {
            WeaponModels.Piece(transform, "Weapon board", PrimitiveType.Cube, new Vector3(0, 1.28f, 0), new Vector3(1.65f, .87f, .09f), timber);
            for (int side = -1; side <= 1; side += 2)
                WeaponModels.Piece(transform, "Rack support", PrimitiveType.Cube, new Vector3(side * .7f, .75f, -.08f), new Vector3(.07f, 1.5f, .1f), steel);
            ShowWeapon(station.OfferedWeapon);
        }
        var text = new GameObject("Weapon offer", typeof(RectTransform), typeof(TextMeshPro)); text.transform.SetParent(transform, false);
        text.transform.localPosition = new Vector3(0, mystery ? .65f : 1.92f, mystery ? .39f : .07f); text.transform.localRotation = Quaternion.Euler(0, 180, 0);
        caption = text.GetComponent<TextMeshPro>(); caption.font = TMP_Settings.defaultFontAsset; caption.fontSize = 2.1f;
        caption.color = new Color(.94f, .88f, .68f); caption.alignment = TextAlignmentOptions.Center;
        caption.rectTransform.sizeDelta = new Vector2(1.7f, .6f); UpdateCaption();
    }
    private void ShowWeapon(WeaponId id)
    {
        if (display != null) Destroy(display.gameObject);
        lastOffer = id; display = WeaponModels.Create(id, transform, steel, timber, brass);
        display.localPosition = new Vector3(0, mystery ? 1.45f : 1.28f, mystery ? 0 : .14f);
        display.localRotation = Quaternion.Euler(0, 90, 0);
        display.localScale = Vector3.one * .95f;
    }
    public bool TryMysteryPurchase()
    {
        var session = SurvivalSession.Instance;
        if (!mystery || !IsReady || session == null || session.Gun == null) return false;
        var offers = new List<WeaponId>();
        for (int i = 1; i <= 3; i++) if (session.Gun.Loadout.IndexOf((WeaponId)i) < 0) offers.Add((WeaponId)i);
        if (offers.Count == 0) return false;
        WeaponId selected = offers[Random.Range(0, offers.Count)];
        if (!session.Gun.PurchaseWeapon(selected, true)) return false;
        ShowWeapon(selected); readyAt = Time.time + 2.5f; UpdateCaption(); return true;
    }
    private void UpdateCaption()
    {
        if (caption == null) return;
        if (mystery) caption.text = IsReady ? "CAIXA MISTERIOSA\n<size=65%>950 PONTOS · [E]</size>" : WeaponCatalog.Get(lastOffer).Name + "\n<size=65%>ARMA ENTREGUE</size>";
        else
        {
            var spec = WeaponCatalog.Get(station.OfferedWeapon);
            bool owned = SurvivalSession.Instance != null && SurvivalSession.Instance.Gun != null && SurvivalSession.Instance.Gun.Loadout.IndexOf(spec.Id) >= 0;
            caption.text = spec.Name + "\n<size=65%>" + (owned ? spec.AmmoPrice + " PTS · MUNIÇÕES" : spec.Price + " PTS · [E] COMPRAR") + "</size>";
        }
    }
    private void Update()
    {
        if (Time.unscaledTime >= nextCaption) { nextCaption = Time.unscaledTime + .3f; UpdateCaption(); }
        if (lid != null && Time.timeScale > 0)
            lid.localRotation = Quaternion.Slerp(lid.localRotation, Quaternion.Euler(IsReady ? 0 : -45, 0, 0), Time.deltaTime * 5);
    }
    private void OnDestroy() { if (steel != null) Destroy(steel); if (timber != null) Destroy(timber); if (brass != null) Destroy(brass); }
}
