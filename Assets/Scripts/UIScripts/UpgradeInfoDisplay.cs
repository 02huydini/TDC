/*
    "I want Editor wiring on upgrading."

    True Editor wiring - dragging real Text objects into UpgradeManager's
    upgradeCostText/sellValueText/upgradePreviewText slots on the actual tower-menu
    prefab - isn't something I can safely do by hand-editing YAML here: the
    Upgrade/Sell buttons live 3+ nested PrefabInstance layers deep inside
    GameCanvas.prefab, and a mistake at that depth isn't something I can verify
    without opening the Editor.

    This is the equivalent done safely: build 3 Text elements at runtime and assign
    them directly into UpgradeManager.main's own public fields, once, as soon as
    it's available. From that point on UpgradeManager's own RefreshButtons() -
    already written to update those fields every tick a tower is selected - is what
    drives the text, exactly as if they'd been dragged in via the Inspector. This
    script only builds the Text objects and toggles their panel's visibility; it no
    longer computes or writes the text itself.
*/
using UnityEngine;
using UnityEngine.UI;

public class UpgradeInfoDisplay : MonoBehaviour {
    private RectTransform panelRect;
    private bool wired = false;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install() {
        if (FindObjectOfType<UpgradeInfoDisplay>() != null) return;
        GameObject go = new GameObject("~UpgradeInfoDisplay");
        DontDestroyOnLoad(go);
        go.AddComponent<UpgradeInfoDisplay>();
    }

    private void Awake() {
        BuildUi();
    }

    private void Update() {
        // Wire once, as soon as UpgradeManager.main exists (its own Start() sets
        // that reference - see UpgradeManager.cs).
        if (!wired) {
            if (UpgradeManager.main == null) return;
            WireIntoUpgradeManager();
            wired = true;
        }

        bool open = UpgradeManager.dummyUi != null && UpgradeManager.dummyUi.activeSelf
            && UpgradeManager.GetCurrentTower() != null;

        if (panelRect.gameObject.activeSelf != open) panelRect.gameObject.SetActive(open);
    }

    private void WireIntoUpgradeManager() {
        // Only take fields that are still unassigned - if someone DOES later wire
        // these up for real in the Editor, this backs off instead of overwriting it.
        if (UpgradeManager.main.upgradeCostText == null) UpgradeManager.main.upgradeCostText = MakeLine("Cost");
        if (UpgradeManager.main.sellValueText == null) UpgradeManager.main.sellValueText = MakeLine("Sell");
        if (UpgradeManager.main.upgradePreviewText == null) UpgradeManager.main.upgradePreviewText = MakeLine("Preview");

        // "Change target button in info UI into info text." This used to be a
        // clickable button (see git history / MakeButton below) - now it's a plain
        // read-only line, matching the other three (Cost/Sell/Preview). Cycling the
        // target mode still works via UpgradeManager.Update()'s [R] keybind; this
        // just stops offering a second, redundant way to do the same thing that
        // looked like a normal button but only had the one purpose.
        if (UpgradeManager.main.cycleTargetButton == null) {
            Text label = MakeLine("Target: Nearest");
            UpgradeManager.main.cycleTargetButton = label.gameObject;
            UpgradeManager.main.cycleTargetLabel = label;
        }
    }

    private void BuildUi() {
        GameObject canvasObj = new GameObject("UpgradeInfoCanvas");
        canvasObj.transform.SetParent(transform, false);
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 6001; // above EctoDisplay/other HUD overlays
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        // Needs to receive clicks now (the cycle-target button below) - previous
        // versions of this panel were informational-only, but the panel sits
        // bottom-center, clear of the real tower-menu buttons, so a raycaster here
        // doesn't risk stealing clicks meant for them.
        canvasObj.AddComponent<GraphicRaycaster>();

        GameObject panelObj = new GameObject("UpgradeInfoPanel");
        panelObj.transform.SetParent(canvasObj.transform, false);
        panelRect = panelObj.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0f);
        panelRect.anchorMax = new Vector2(0.5f, 0f);
        panelRect.pivot = new Vector2(0.5f, 0f);
        // Bottom-center, clear of wherever the tower-menu panel itself sits.
        panelRect.anchoredPosition = new Vector2(0f, 40f);
        panelRect.sizeDelta = new Vector2(440f, 140f);
        Image bg = panelObj.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.6f);

        VerticalLayoutGroup layout = panelObj.AddComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        layout.spacing = 2f;
        layout.padding = new RectOffset(10, 10, 6, 6);

        panelRect.gameObject.SetActive(false);
        panelObj.transform.SetParent(canvasObj.transform, false); // re-apply after layout add
    }

    private Text MakeLine(string placeholder) {
        GameObject textObj = new GameObject(placeholder + "Text");
        textObj.transform.SetParent(panelRect.transform, false);
        RectTransform tr = textObj.AddComponent<RectTransform>();
        tr.sizeDelta = new Vector2(0f, 26f);

        Text t = textObj.AddComponent<Text>();
        t.font = RuntimeUIFont.Get();
        t.fontSize = 16;
        t.alignment = TextAnchor.MiddleCenter;
        t.color = Color.white;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.text = "";
        return t;
    }

    private GameObject MakeButton(string initialLabel, UnityEngine.Events.UnityAction onClick, out Text label) {
        GameObject btnObj = new GameObject("CycleTargetButton");
        btnObj.transform.SetParent(panelRect.transform, false);
        RectTransform tr = btnObj.AddComponent<RectTransform>();
        tr.sizeDelta = new Vector2(0f, 30f);

        Image bg = btnObj.AddComponent<Image>();
        bg.color = new Color(0.2f, 0.35f, 0.5f, 0.9f);

        Button button = btnObj.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color(0.3f, 0.45f, 0.6f, 0.95f);
        colors.pressedColor = new Color(0.1f, 0.2f, 0.3f, 0.95f);
        button.colors = colors;
        button.onClick.AddListener(onClick);

        GameObject textObj = new GameObject("Label");
        textObj.transform.SetParent(btnObj.transform, false);
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        label = textObj.AddComponent<Text>();
        label.font = RuntimeUIFont.Get();
        label.fontSize = 16;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = Color.white;
        label.text = initialLabel;

        return btnObj;
    }
}
