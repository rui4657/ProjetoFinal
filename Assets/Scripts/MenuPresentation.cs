using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Gives the existing menu a consistent theme without replacing its controls
/// or their serialized callbacks. All added decoration is scene-local.
/// </summary>
[DisallowMultipleComponent]
public sealed class MenuPresentation : MonoBehaviour
{
    private static readonly Color Paper = new Color32(242, 238, 225, 255);
    private static readonly Color Muted = new Color32(169, 176, 173, 255);
    private static readonly Color Charcoal = new Color32(22, 27, 29, 255);
    private static readonly Color Red = new Color32(159, 43, 40, 255);
    private static readonly Color Amber = new Color32(226, 174, 104, 255);

    private RectTransform canvasRect;
    private RectTransform titleRect;
    private RectTransform controlsCard;
    private TMP_FontAsset menuFont;
    private Texture2D backdropGradient;
    private Button[] menuButtons;
    private TMP_Text[] buttonLabels;
    private bool[] buttonStates;
    private bool initialized;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterSceneHook()
    {
        // This also avoids duplicate subscriptions with domain reload disabled.
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != "MainMenu") return;

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Canvas canvas in root.GetComponentsInChildren<Canvas>(true))
            {
                if (!canvas.isRootCanvas || canvas.renderMode == RenderMode.WorldSpace)
                    continue;

                // Only theme the canvas that contains this menu.
                if (canvas.transform.Find("buttonsPanel") == null) continue;
                if (canvas.GetComponent<MenuPresentation>() == null)
                    canvas.gameObject.AddComponent<MenuPresentation>();
            }
        }
    }

    private void Awake()
    {
        canvasRect = transform as RectTransform;
        if (canvasRect == null || transform.Find("DeadlinePresentation") != null) return;

        TMP_Text title = FindText("TitleText");
        menuFont = title != null && title.font != null ? title.font : TMP_Settings.defaultFontAsset;

        // Include inactive option, save and name controls, preserving their state.
        foreach (TMP_Text text in GetComponentsInChildren<TMP_Text>(true))
        {
            text.color = Paper;
            text.raycastTarget = false;
        }
        menuButtons = GetComponentsInChildren<Button>(true);
        buttonLabels = new TMP_Text[menuButtons.Length];
        buttonStates = new bool[menuButtons.Length];
        for (int i = 0; i < menuButtons.Length; i++)
        {
            ThemeButton(menuButtons[i]);
            buttonLabels[i] = menuButtons[i].GetComponentInChildren<TMP_Text>(true);
            buttonStates[i] = !menuButtons[i].IsInteractable();
        }
        foreach (Slider slider in GetComponentsInChildren<Slider>(true))
            ThemeSlider(slider);
        foreach (Toggle toggle in GetComponentsInChildren<Toggle>(true))
            ThemeToggle(toggle);
        foreach (TMP_InputField input in GetComponentsInChildren<TMP_InputField>(true))
            ThemeNameInput(input);

        CreateBackdrop();
        ThemeTitle(title);
        ThemeButtonPanel();
        ThemeOptions();
        initialized = true;
        RefreshButtonStates();
        ApplyResponsiveSpacing();
    }

    private void CreateBackdrop()
    {
        RectTransform decoration = MakeRect("DeadlinePresentation", canvasRect);
        Stretch(decoration);
        CanvasGroup group = decoration.gameObject.AddComponent<CanvasGroup>();
        group.interactable = false;
        group.blocksRaycasts = false;

        // Keep the original RawImage and its texture, with a soft veil above it.
        Transform background = transform.Find("RawImage");
        decoration.SetSiblingIndex(background != null ? background.GetSiblingIndex() + 1 : 0);
        backdropGradient = new Texture2D(64, 1, TextureFormat.RGBA32, false);
        backdropGradient.name = "Deadline menu veil";
        backdropGradient.wrapMode = TextureWrapMode.Clamp;
        backdropGradient.filterMode = FilterMode.Bilinear;
        for (int x = 0; x < backdropGradient.width; x++)
        {
            float t = x / (float)(backdropGradient.width - 1);
            backdropGradient.SetPixel(x, 0,
                Color.Lerp(new Color(0.025f, 0.035f, 0.04f, 0.93f),
                    new Color(0.025f, 0.035f, 0.04f, 0.34f), t));
        }
        backdropGradient.Apply();
        RawImage veil = decoration.gameObject.AddComponent<RawImage>();
        veil.texture = backdropGradient;
        veil.raycastTarget = false;

        MakeLine(decoration, "TopAccent", new Vector2(120, -92), new Vector2(48, 4), Red);
        MakeText(decoration, "Mode", "SOLO  /  SOBREVIVÊNCIA", 20, Amber,
            new Vector2(184, -77), new Vector2(590, 40), 2f);
        MakeText(decoration, "Edition", "ZOMBIE SURVIVAL", 22, Muted,
            new Vector2(124, -202), new Vector2(700, 44), 5f);
        MakeLine(decoration, "TitleRule", new Vector2(124, -446), new Vector2(88, 3), Red);
        MakeText(decoration, "Tagline", "Sobrevive. Melhora. Repete.", 29, Paper,
            new Vector2(124, -480), new Vector2(650, 54));
        MakeText(decoration, "Intro", "Cada ronda é uma nova hipótese.", 21, Muted,
            new Vector2(124, -536), new Vector2(650, 40));

        controlsCard = MakeRect("ControlsCard", decoration);
        Place(controlsCard, new Vector2(120, -632), new Vector2(600, 174));
        MakePanel(controlsCard, new Color(0.06f, 0.08f, 0.085f, 0.88f));
        MakeLine(controlsCard, "Accent", Vector2.zero, new Vector2(3, 174), Red);
        MakeText(controlsCard, "Heading", "PREPARA-TE PARA A PRÓXIMA RONDA", 17, Amber,
            new Vector2(25, -17), new Vector2(550, 32), 1.2f);
        MakeText(controlsCard, "Movement", "<b>W A S D</b>   MOVER        <b>RATO</b>   OLHAR", 18, Paper,
            new Vector2(25, -64), new Vector2(550, 34));
        MakeText(controlsCard, "Combat", "<b>CLIQUE</b>   DISPARAR     <b>R</b>   RECARREGAR     <b>ESC</b>   PAUSA", 16, Muted,
            new Vector2(25, -111), new Vector2(550, 34));

        TMP_Text actionHeading = MakeText(decoration, "ActionsHeading", "O TEU PRÓXIMO PASSO", 18, Amber,
            Vector2.zero, new Vector2(480, 40), 2f);
        AnchorTopRight(actionHeading.rectTransform, new Vector2(-190, -365));

        TMP_Text footer = MakeText(decoration, "Footer", "DEADLINE  /  NINGUÉM SOBREVIVE PARADO", 16, Muted,
            Vector2.zero, new Vector2(900, 34), 1.5f);
        footer.rectTransform.anchorMin = footer.rectTransform.anchorMax = Vector2.zero;
        footer.rectTransform.pivot = Vector2.zero;
        footer.rectTransform.anchoredPosition = new Vector2(124, 48);
    }

    private void ThemeTitle(TMP_Text title)
    {
        if (title == null) return;
        titleRect = title.rectTransform;
        title.text = "DEADLINE";
        title.color = Paper;
        title.fontStyle = FontStyles.Bold;
        title.characterSpacing = 4f;
        title.enableAutoSizing = true;
        title.fontSizeMin = 62f;
        title.fontSizeMax = 132f;
        title.fontSize = 132f;
        title.alignment = TextAlignmentOptions.TopLeft;
        title.enableWordWrapping = false;
        title.overflowMode = TextOverflowModes.Ellipsis;
        titleRect.sizeDelta = new Vector2(850f, 174f);
    }

    private void ThemeButtonPanel()
    {
        RectTransform panel = transform.Find("buttonsPanel") as RectTransform;
        if (panel == null) return;
        RectTransform frame = MakeRect("MenuFrame", panel);
        Stretch(frame);
        frame.offsetMin = new Vector2(-18f, -18f);
        frame.offsetMax = new Vector2(65f, 18f);
        MakePanel(frame, new Color(0.04f, 0.055f, 0.06f, 0.84f));
        frame.SetAsFirstSibling();
    }

    private void ThemeButton(Button button)
    {
        bool primary = button.name == "btnStart";
        bool deletion = button.name == "btnDeleteSave";
        Image image = button.targetGraphic as Image;
        if (image != null)
        {
            image.sprite = null;
            image.type = Image.Type.Simple;
            image.color = Color.white;
        }

        ColorBlock colors = button.colors;
        colors.normalColor = primary ? Red : Charcoal;
        colors.highlightedColor = primary ? new Color32(190, 57, 48, 255) : new Color32(53, 61, 61, 255);
        colors.selectedColor = colors.highlightedColor;
        colors.pressedColor = primary ? new Color32(114, 29, 29, 255) : new Color32(34, 40, 41, 255);
        colors.disabledColor = new Color32(26, 29, 30, 210);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.14f;
        button.transition = Selectable.Transition.ColorTint;
        button.colors = colors;

        // Only the main caption is translated; save names and other child text
        // keep their own content and remain under the original controllers.
        TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
        {
            label.fontStyle = FontStyles.Bold;
            label.fontSize = deletion ? 28f : 25f;
            label.characterSpacing = deletion ? 0f : 1.1f;
            label.enableAutoSizing = true;
            label.fontSizeMin = deletion ? 18f : 16f;
            label.fontSizeMax = deletion ? 28f : 25f;
            label.alignment = deletion ? TextAlignmentOptions.Center : TextAlignmentOptions.MidlineLeft;
            label.margin = deletion ? Vector4.zero : new Vector4(27f, 0f, 16f, 0f);
            label.enableWordWrapping = false;
            label.overflowMode = TextOverflowModes.Ellipsis;
            switch (button.name)
            {
                case "btnStart": label.text = "INICIAR JOGO"; break;
                case "btnLoad": label.text = "CONTINUAR"; break;
                case "btnOptions": label.text = "OPÇÕES"; break;
                case "btnExit": label.text = "SAIR"; break;
                case "btnBack": label.text = "VOLTAR"; break;
                case "btnDeleteSave": label.text = "×"; break;
            }
        }

        if (deletion) return;
        RectTransform accent = MakeRect("ThemeAccent", button.transform);
        accent.anchorMin = Vector2.zero;
        accent.anchorMax = new Vector2(0f, 1f);
        accent.pivot = new Vector2(0f, 0.5f);
        accent.sizeDelta = new Vector2(3f, 0f);
        accent.anchoredPosition = Vector2.zero;
        MakePanel(accent, primary ? Amber : Red);
    }

    private void ThemeOptions()
    {
        Transform panel = transform.Find("OptionPanel");
        if (panel == null) return;
        Transform background = panel.Find("Fundo");
        if (background != null)
        {
            Image image = background.GetComponent<Image>();
            if (image != null) image.color = new Color(0.04f, 0.055f, 0.06f, 0.985f);
        }

        foreach (TMP_Text text in panel.GetComponentsInChildren<TMP_Text>(true))
        {
            if (text.GetComponentInParent<Button>() != null) continue;
            text.fontSize = text.name == "OPTIONS" ? 46f : 28f;
            text.color = text.name.EndsWith("ValueText") ? Amber : Paper;
            if (text.name == "OPTIONS")
            {
                text.text = "OPÇÕES";
                text.fontStyle = FontStyles.Bold;
                text.rectTransform.sizeDelta = new Vector2(420f, 74f);
                text.rectTransform.anchoredPosition = new Vector2(0f, -32f);
            }
            else if (text.name == "Sensitivity") text.text = "Sensibilidade";
            else if (text.name == "Fullscreen") text.text = "Ecrã inteiro";
        }
    }

    private static void ThemeSlider(Slider slider)
    {
        foreach (Image image in slider.GetComponentsInChildren<Image>(true))
        {
            if (image.transform == slider.handleRect) image.color = Paper;
            else if (image.transform == slider.fillRect) image.color = Amber;
            else image.color = new Color32(61, 70, 70, 255);
        }
        ColorBlock colors = slider.colors;
        colors.normalColor = Paper;
        colors.highlightedColor = Amber;
        colors.selectedColor = Amber;
        colors.pressedColor = Red;
        slider.colors = colors;
    }

    private static void ThemeToggle(Toggle toggle)
    {
        if (toggle.targetGraphic != null) toggle.targetGraphic.color = Muted;
        if (toggle.graphic != null) toggle.graphic.color = Amber;
        foreach (Text label in toggle.GetComponentsInChildren<Text>(true)) label.color = Paper;
    }

    private static void ThemeNameInput(TMP_InputField input)
    {
        if (input.targetGraphic != null) input.targetGraphic.color = Charcoal;
        if (input.textComponent != null) input.textComponent.color = Paper;
        TMP_Text placeholder = input.placeholder as TMP_Text;
        if (placeholder != null) placeholder.color = Muted;
        input.caretColor = Amber;
        input.customCaretColor = true;
        input.selectionColor = new Color(0.63f, 0.23f, 0.2f, 0.55f);
    }

    private void OnRectTransformDimensionsChange()
    {
        if (initialized) ApplyResponsiveSpacing();
    }

    private void LateUpdate()
    {
        // Save controllers change interactable after their Start or after delete.
        // Only update text when that state actually changes.
        if (initialized) RefreshButtonStates();
    }

    private void RefreshButtonStates()
    {
        for (int i = 0; i < menuButtons.Length; i++)
        {
            if (menuButtons[i] == null) continue;
            bool available = menuButtons[i].IsInteractable();
            if (buttonStates[i] == available) continue;
            buttonStates[i] = available;
            if (buttonLabels[i] != null)
                buttonLabels[i].color = available ? Paper : new Color(0.52f, 0.56f, 0.55f, 1f);
            Transform accent = menuButtons[i].transform.Find("ThemeAccent");
            if (accent != null)
                accent.GetComponent<Image>().color = available
                    ? (menuButtons[i].name == "btnStart" ? Amber : Red)
                    : new Color(0.27f, 0.30f, 0.30f, 1f);
        }
    }

    private void ApplyResponsiveSpacing()
    {
        float width = canvasRect.rect.width > 0f ? canvasRect.rect.width : 1920f;
        if (titleRect != null)
            titleRect.sizeDelta = new Vector2(Mathf.Clamp(width - 920f, 300f, 850f), 174f);
        if (controlsCard != null) controlsCard.gameObject.SetActive(width >= 1350f);
    }

    private TMP_Text FindText(string objectName)
    {
        foreach (TMP_Text text in GetComponentsInChildren<TMP_Text>(true))
            if (text.name == objectName) return text;
        return null;
    }

    private TMP_Text MakeText(Transform parent, string objectName, string value, float size,
        Color color, Vector2 position, Vector2 dimensions, float tracking = 0f)
    {
        RectTransform rect = MakeRect(objectName, parent);
        Place(rect, position, dimensions);
        TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        if (menuFont != null) text.font = menuFont;
        text.text = value;
        text.fontSize = size;
        text.color = color;
        text.alignment = TextAlignmentOptions.MidlineLeft;
        text.characterSpacing = tracking;
        text.raycastTarget = false;
        text.enableWordWrapping = false;
        text.overflowMode = TextOverflowModes.Ellipsis;
        return text;
    }

    private static RectTransform MakeRect(string objectName, Transform parent)
    {
        GameObject child = new GameObject(objectName, typeof(RectTransform));
        child.layer = parent.gameObject.layer;
        RectTransform rect = child.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        return rect;
    }

    private static void Place(RectTransform rect, Vector2 position, Vector2 dimensions)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = position;
        rect.sizeDelta = dimensions;
    }

    private static void AnchorTopRight(RectTransform rect, Vector2 position)
    {
        rect.anchorMin = rect.anchorMax = Vector2.one;
        rect.pivot = Vector2.one;
        rect.anchoredPosition = position;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    private static void MakePanel(RectTransform rect, Color color)
    {
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
    }

    private static void MakeLine(Transform parent, string objectName, Vector2 position,
        Vector2 dimensions, Color color)
    {
        RectTransform rect = MakeRect(objectName, parent);
        Place(rect, position, dimensions);
        MakePanel(rect, color);
    }

    private void OnDestroy()
    {
        if (backdropGradient != null) Destroy(backdropGradient);
    }
}
