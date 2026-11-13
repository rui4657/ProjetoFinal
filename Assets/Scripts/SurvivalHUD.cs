using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>Responsive, asset-free HUD and between-round arsenal, built once per gameplay scene.</summary>
public sealed class SurvivalHUD : MonoBehaviour
{
    readonly Color ink = new Color(0.035f, 0.045f, 0.055f, 0.92f);
    readonly Color paper = new Color(0.92f, 0.93f, 0.9f);
    readonly Color muted = new Color(0.57f, 0.62f, 0.64f);
    readonly Color red = new Color(0.9f, 0.19f, 0.19f);
    readonly Color gold = new Color(1f, 0.73f, 0.32f);
    SurvivalSession session;
    Canvas canvas;
    RectTransform safeRoot;
    GameObject playRoot, shopRoot, endRoot;
    TMP_Text round, remaining, health, ammo, weapon, points, bonus, notice, interaction, countdown;
    TMP_Text shopBalance, shopNotice, endStats;
    Image healthFill, staminaFill, reloadFill;
    Image[] crosshair, damageEdges;
    TMP_Text hitMarker;
    Button ammoButton, weaponButton, healthButton, reloadButton;
    TMP_Text ammoOffer, weaponOffer, healthOffer, reloadOffer;
    float nextRefresh;

    void Start()
    {
        session = GetComponent<SurvivalSession>();
        Build();
    }

    void Build()
    {
        var canvasGO = new GameObject("Survival Interface", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGO.transform.SetParent(transform, false);
        canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        safeRoot = Rect("Safe Area", canvasGO.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        playRoot = Rect("HUD", safeRoot, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).gameObject;

        var brand = Panel("Brand", playRoot.transform, new Vector2(0, 1), new Vector2(48, -48), new Vector2(240, 54));
        Label(brand, "DEADLINE", 25, paper, new Vector2(16, -10), new Vector2(210, 30));
        Label(brand, "SOBREVIVÊNCIA  /  SOLO", 12, muted, new Vector2(16, -38), new Vector2(210, 18));
        var rounds = Panel("Round", playRoot.transform, Vector2.one, new Vector2(-48, -48), new Vector2(270, 124));
        Label(rounds, "RONDA", 14, muted, new Vector2(20, -12), new Vector2(220, 22));
        round = Label(rounds, "01", 56, red, new Vector2(16, -30), new Vector2(225, 66));
        remaining = Label(rounds, "PREPARAÇÃO", 15, paper, new Vector2(20, -98), new Vector2(235, 23));

        var vital = Panel("Vitals", playRoot.transform, Vector2.zero, new Vector2(48, 48), new Vector2(290, 118));
        Label(vital, "VIDA", 13, muted, new Vector2(18, -13), new Vector2(90, 22));
        health = Label(vital, "100", 38, paper, new Vector2(18, -32), new Vector2(240, 48));
        healthFill = Bar(vital, new Vector2(18, -86), new Vector2(254, 6), red);
        staminaFill = Bar(vital, new Vector2(18, -101), new Vector2(254, 3), paper);

        var gun = Panel("Weapon", playRoot.transform, new Vector2(1, 0), new Vector2(-48, 48), new Vector2(300, 118));
        weapon = Label(gun, "AK", 16, gold, new Vector2(20, -14), new Vector2(260, 26));
        ammo = Label(gun, "30 / 120", 42, paper, new Vector2(18, -41), new Vector2(268, 56));
        reloadFill = Bar(gun, new Vector2(20, -103), new Vector2(260, 3), gold);

        points = CenterLabel(playRoot.transform, "500  PONTOS", 23, gold, new Vector2(0, 1), new Vector2(0, -52), new Vector2(420, 36));
        bonus = CenterLabel(playRoot.transform, "", 20, gold, new Vector2(0, 1), new Vector2(0, -87), new Vector2(240, 30));
        notice = CenterLabel(playRoot.transform, "", 26, paper, new Vector2(0.5f, 1), new Vector2(0, -185), new Vector2(1000, 45));
        countdown = CenterLabel(playRoot.transform, "", 17, muted, new Vector2(0.5f, 0), new Vector2(0, 66), new Vector2(1050, 60));
        interaction = CenterLabel(playRoot.transform, "", 21, gold, new Vector2(0.5f, 0), new Vector2(0, 175), new Vector2(1000, 50));
        crosshair = new Image[4];
        for (int i = 0; i < 4; i++)
        {
            var r = Rect("Crosshair", playRoot.transform, Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.zero,
                i < 2 ? new Vector2(8, 2) : new Vector2(2, 8));
            crosshair[i] = r.gameObject.AddComponent<Image>(); crosshair[i].color = paper; crosshair[i].raycastTarget = false;
        }
        hitMarker = CenterLabel(playRoot.transform, "×", 46, paper, Vector2.one * 0.5f, Vector2.zero, new Vector2(60, 60));

        damageEdges = new Image[4];
        for (int i = 0; i < 4; i++)
        {
            var r = Rect("Damage Edge", playRoot.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            if (i < 2) { r.anchorMin = new Vector2(i, 0); r.anchorMax = new Vector2(i, 1); r.sizeDelta = new Vector2(22, 0); }
            else { r.anchorMin = new Vector2(0, i - 2); r.anchorMax = new Vector2(1, i - 2); r.sizeDelta = new Vector2(0, 22); }
            damageEdges[i] = r.gameObject.AddComponent<Image>(); damageEdges[i].raycastTarget = false;
        }
        BuildShop(); BuildEnd();
        shopRoot.SetActive(false); endRoot.SetActive(false);
    }

    void BuildShop()
    {
        shopRoot = Overlay("Arsenal");
        var card = Panel("Arsenal Card", shopRoot.transform, Vector2.one * 0.5f, Vector2.zero, new Vector2(960, 790));
        Label(card, "ARSENAL", 48, paper, new Vector2(48, -40), new Vector2(850, 65));
        Label(card, "MELHORA O TEU EQUIPAMENTO ENTRE RONDAS", 16, muted, new Vector2(50, -108), new Vector2(850, 30));
        shopBalance = Label(card, "", 24, gold, new Vector2(50, -153), new Vector2(850, 40));
        ammoButton = Offer(card, 218, "MUNIÇÕES", "Carregador e reserva completos", session.BuyAmmo, out ammoOffer);
        weaponButton = Offer(card, 326, "ARSENAL REFORÇADO", "+50% de dano por nível · máximo 3 níveis", session.BuyWeaponUpgrade, out weaponOffer);
        healthButton = Offer(card, 434, "FORTITUDE", "+50 vida máxima · máximo 2 melhorias", session.BuyHealthUpgrade, out healthOffer);
        reloadButton = Offer(card, 542, "RECARGA RÁPIDA", "Reduz o tempo de recarga em 35%", session.BuyQuickReload, out reloadOffer);
        shopNotice = Label(card, "", 16, gold, new Vector2(50, -650), new Vector2(860, 28));
        MakeButton(card, "VOLTAR À PARTIDA   [B / ESC]", new Vector2(50, -687), new Vector2(860, 58), session.CloseShop);
    }

    void BuildEnd()
    {
        endRoot = Overlay("Game Over");
        var card = Panel("Results", endRoot.transform, Vector2.one * 0.5f, Vector2.zero, new Vector2(820, 720));
        Label(card, "FIM DA LINHA", 52, red, new Vector2(50, -48), new Vector2(720, 76));
        Label(card, "CADA RONDA CONTA. VOLTA MAIS FORTE.", 17, muted, new Vector2(54, -130), new Vector2(720, 30));
        endStats = Label(card, "", 28, paper, new Vector2(54, -208), new Vector2(710, 270));
        MakeButton(card, "TENTAR NOVAMENTE", new Vector2(50, -535), new Vector2(720, 62), session.Retry);
        MakeButton(card, "MENU PRINCIPAL", new Vector2(50, -615), new Vector2(720, 55), session.ReturnToMenu);
    }

    void Update()
    {
        if (canvas == null || session == null) return;
        Rect area = Screen.safeArea;
        safeRoot.anchorMin = new Vector2(area.xMin / Mathf.Max(1, Screen.width), area.yMin / Mathf.Max(1, Screen.height));
        safeRoot.anchorMax = new Vector2(area.xMax / Mathf.Max(1, Screen.width), area.yMax / Mathf.Max(1, Screen.height));
        bool normalPause = Time.timeScale <= 0f && !session.IsShopOpen && !session.IsGameOver;
        canvas.enabled = !normalPause;
        playRoot.SetActive(!session.IsGameOver && !session.IsShopOpen);
        shopRoot.SetActive(session.IsShopOpen);
        endRoot.SetActive(session.IsGameOver);
        if (Time.unscaledTime >= nextRefresh) { RefreshText(); nextRefresh = Time.unscaledTime + 0.1f; }
        var p = session.Player; var g = session.Gun;
        float hp = p != null ? p.CurrentHP / (float)Mathf.Max(1, p.MaxHP) : 1;
        SetBar(healthFill, hp);
        SetBar(staminaFill, session.Movement != null ? session.Movement.StaminaNormalized : 1);
        SetBar(reloadFill, g != null && g.IsReloading ? g.ReloadProgress : 0);
        float gap = g != null && g.IsAiming ? 6 : 12;
        if (session.Movement != null && session.Movement.IsSprinting) gap += 8;
        for (int i = 0; i < crosshair.Length; i++)
            crosshair[i].rectTransform.anchoredPosition = i < 2 ? new Vector2((i == 0 ? -1 : 1) * gap, 0) : new Vector2(0, (i == 2 ? -1 : 1) * gap);
        hitMarker.enabled = Time.unscaledTime < session.HitMarkerUntil;
        hitMarker.color = session.LastHitWasHeadshot ? gold : session.LastHitWasKill ? red : paper;
        float damage = p != null ? Mathf.Clamp01(1 - (Time.time - p.LastDamageTime) / 0.6f) * 0.65f : 0;
        float danger = hp < 0.3f ? (0.15f + Mathf.Sin(Time.unscaledTime * 5) * 0.08f) : 0;
        foreach (var edge in damageEdges) edge.color = new Color(red.r, red.g, red.b, Mathf.Max(damage, danger));
    }

    void RefreshText()
    {
        var p = session.Player; var g = session.Gun; var r = session.Rounds;
        health.text = p != null ? p.CurrentHP + "<size=45%><color=#929FA3> / " + p.MaxHP + "</color></size>" : "—";
        health.color = p != null && p.CurrentHP <= p.MaxHP * 0.3f ? red : paper;
        ammo.text = g != null ? g.currentAmmo + "<size=48%><color=#929FA3> / " + g.reserveAmmo + "</color></size>" : "—";
        ammo.color = g != null && g.currentAmmo <= 5 ? gold : paper;
        weapon.text = g != null ? g.gunName.ToUpperInvariant() + (session.WeaponTier > 0 ? "  ·  NÍVEL " + session.WeaponTier : "") : "";
        points.text = session.Points.ToString("N0") + "  PONTOS";
        bonus.text = Time.unscaledTime < session.PointsPopupUntil ? "+" + session.LastPointsAwarded : "";
        notice.text = Time.unscaledTime < session.MessageUntil ? session.Message : "";
        round.text = r != null ? r.currentRound.ToString("00") : "01";
        remaining.text = r == null ? "" : r.inIntermission ? "PREPARAÇÃO" : r.ZombiesRemaining + " INIMIGOS RESTANTES";
        countdown.text = r != null && r.inIntermission ? "PRÓXIMA RONDA EM " + Mathf.CeilToInt(r.intermissionTimer) + "s\n[B] ARSENAL   ·   [ENTER] COMEÇAR AGORA" : "[SHIFT] CORRER   ·   [R] RECARREGAR   ·   [ESC] PAUSA";
        if (r != null && r.HasSpawnError) countdown.text = "NÃO FOI POSSÍVEL INICIAR A RONDA. [ESC] PARA REGRESSAR AO MENU.";
        interaction.text = Time.unscaledTime < session.InteractionUntil ? session.InteractionPrompt : g != null && g.IsReloading ? "A RECARREGAR…" : g != null && g.currentAmmo == 0 && g.reserveAmmo == 0 ? "SEM MUNIÇÕES · PROCURA O POSTO DE REABASTECIMENTO" : "";
        if (session.IsShopOpen)
        {
            shopBalance.text = "SALDO   " + session.Points.ToString("N0") + " PONTOS";
            shopNotice.text = Time.unscaledTime < session.MessageUntil ? session.Message : "";
            bool full = g != null && g.currentAmmo >= g.magSize && g.reserveAmmo >= g.maxReserveAmmo;
            OfferState(ammoButton, ammoOffer, full ? "COMPLETO" : "500 PTS", !full && g != null && session.Points >= 500);
            OfferState(weaponButton, weaponOffer, session.WeaponTier >= 3 ? "NÍVEL MÁXIMO" : session.UpgradeCost + " PTS", session.WeaponTier < 3 && session.Points >= session.UpgradeCost);
            OfferState(healthButton, healthOffer, session.HealthBonus >= 100 ? "MÁXIMO" : "2000 PTS", session.HealthBonus < 100 && session.Points >= 2000);
            OfferState(reloadButton, reloadOffer, session.HasQuickReload ? "ATIVO" : "1500 PTS", !session.HasQuickReload && session.Points >= 1500);
        }
        if (session.IsGameOver)
        {
            int secs = Mathf.FloorToInt(session.ElapsedSeconds);
            endStats.text = "RONDA ALCANÇADA    <color=#E63030>" + (r != null ? r.currentRound : 1) + "</color>\n\nELIMINAÇÕES    " + session.Kills + "    <size=65%>HEADSHOTS  " + session.Headshots + "</size>\n\nTEMPO DESTA SESSÃO    " + (secs / 60).ToString("00") + ":" + (secs % 60).ToString("00") + "    <size=65%>RECORDE  " + PlayerPrefs.GetInt("best_round", 1) + " RONDAS</size>";
        }
    }

    void OfferState(Button button, TMP_Text label, string text, bool available)
    { button.interactable = available; label.text = text; label.color = available ? gold : muted; }

    Button Offer(Transform parent, float y, string title, string detail, UnityEngine.Events.UnityAction action, out TMP_Text price)
    {
        Button b = MakeButton(parent, "", new Vector2(50, -y), new Vector2(860, 90), action);
        Label(b.transform, title, 21, paper, new Vector2(20, -12), new Vector2(600, 32));
        Label(b.transform, detail, 15, muted, new Vector2(20, -50), new Vector2(610, 28));
        price = Label(b.transform, "", 19, gold, new Vector2(645, -30), new Vector2(190, 32));
        price.alignment = TextAlignmentOptions.Right;
        return b;
    }

    GameObject Overlay(string name)
    {
        var r = Rect(name, safeRoot, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        var bg = r.gameObject.AddComponent<Image>(); bg.color = new Color(0.015f, 0.02f, 0.025f, 0.96f);
        return r.gameObject;
    }

    RectTransform Panel(string name, Transform parent, Vector2 anchor, Vector2 position, Vector2 size)
    {
        var r = Rect(name, parent, anchor, anchor, position, size);
        r.pivot = anchor;
        var image = r.gameObject.AddComponent<Image>(); image.color = ink; image.raycastTarget = false;
        return r;
    }

    static RectTransform Rect(string name, Transform parent, Vector2 min, Vector2 max, Vector2 position, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform)); go.layer = 5; go.transform.SetParent(parent, false);
        var r = (RectTransform)go.transform; r.anchorMin = min; r.anchorMax = max; r.anchoredPosition = position; r.sizeDelta = size;
        return r;
    }

    TMP_Text Label(Transform parent, string value, float size, Color color, Vector2 position, Vector2 dimensions)
    {
        var r = Rect(value.Length > 0 ? value : "Label", parent, new Vector2(0, 1), new Vector2(0, 1), position, dimensions);
        r.pivot = new Vector2(0, 1);
        var t = r.gameObject.AddComponent<TextMeshProUGUI>(); t.text = value; t.font = TMP_Settings.defaultFontAsset;
        t.fontSize = size; t.color = color; t.raycastTarget = false; t.richText = true; t.textWrappingMode = TextWrappingModes.NoWrap;
        t.overflowMode = TextOverflowModes.Ellipsis;
        return t;
    }

    TMP_Text CenterLabel(Transform parent, string value, float size, Color color, Vector2 anchor, Vector2 position, Vector2 dimensions)
    {
        var t = Label(parent, value, size, color, position, dimensions); var r = t.rectTransform;
        // Horizontal center, with the requested top/bottom/center edge.
        r.anchorMin = r.anchorMax = new Vector2(0.5f, anchor.y); r.pivot = Vector2.one * 0.5f;
        t.alignment = TextAlignmentOptions.Center;
        return t;
    }

    Image Bar(Transform parent, Vector2 position, Vector2 size, Color color)
    {
        var r = Rect("Bar Track", parent, new Vector2(0, 1), new Vector2(0, 1), position, size); r.pivot = new Vector2(0, 1);
        var track = r.gameObject.AddComponent<Image>(); track.color = new Color(1, 1, 1, 0.1f); track.raycastTarget = false;
        var fillRect = Rect("Fill", r, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        var fill = fillRect.gameObject.AddComponent<Image>(); fill.color = color; fill.raycastTarget = false;
        return fill;
    }

    static void SetBar(Image image, float amount)
    {
        image.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(amount), 1);
    }

    Button MakeButton(Transform parent, string title, Vector2 position, Vector2 size, UnityEngine.Events.UnityAction action)
    {
        var r = Rect(title.Length > 0 ? title : "Offer", parent, new Vector2(0, 1), new Vector2(0, 1), position, size); r.pivot = new Vector2(0, 1);
        var image = r.gameObject.AddComponent<Image>(); image.color = new Color(0.12f, 0.14f, 0.16f);
        var button = r.gameObject.AddComponent<Button>(); button.targetGraphic = image;
        var c = button.colors; c.normalColor = Color.white; c.highlightedColor = new Color(1.35f, 1.2f, 1.05f); c.pressedColor = gold;
        c.selectedColor = new Color(1.25f, 1.15f, 1); c.disabledColor = new Color(0.55f, 0.55f, 0.55f); c.fadeDuration = 0.12f; button.colors = c;
        button.onClick.AddListener(action);
        var text = Label(r, title, 19, paper, Vector2.zero, size); text.rectTransform.anchorMin = Vector2.zero; text.rectTransform.anchorMax = Vector2.one;
        text.rectTransform.offsetMin = text.rectTransform.offsetMax = Vector2.zero; text.alignment = TextAlignmentOptions.Center;
        return button;
    }
}
