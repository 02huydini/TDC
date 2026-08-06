/*
    "Make a way to return camera to main hall, incase of bugged camera like camera
    flying off to corner that can't find map anymore."

    CameraController has no boundary clamp on camera position at all, so it's a real
    way to get stuck. This is an always-on-screen button (top-right corner) calling
    CameraController.main.ReturnToMainHall() - Home key does the same thing, but a
    lost player might not know that, and a visible button doesn't require finding a
    keyboard shortcut while already disoriented. Self-installing, same pattern as
    EctoDisplay/UpgradeInfoDisplay - no scene/prefab wiring needed.

    Moved down from the very top-right corner: GameCanvas already has the Skip
    Timer/FastForward icon button anchored top-right at roughly (-82.6,-32.129),
    sized ~38x36 - this button's old 160x40 box at (-20,-20) overlapped it, and
    since this sits on a much higher sortingOrder (6002) it was silently eating
    clicks meant for Skip Timer. Dropped to y=-70 so it occupies its own row below
    that icon instead of on top of it.
*/
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class CameraResetButton : MonoBehaviour {
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install() {
        if (FindObjectOfType<CameraResetButton>() != null) return;
        GameObject go = new GameObject("~CameraResetButton");
        DontDestroyOnLoad(go);
        go.AddComponent<CameraResetButton>();
    }

    private void Awake() {
        BuildUi();
    }

    private void BuildUi() {
        GameObject canvasObj = new GameObject("CameraResetCanvas");
        canvasObj.transform.SetParent(transform, false);
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 6002;
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        // This one needs to actually receive clicks, unlike the info-only overlays.
        canvasObj.AddComponent<GraphicRaycaster>();

        // Requires an EventSystem in the scene to receive clicks at all - this
        // project already has one wired up (used by every other UI button), so not
        // creating one here on purpose: a second EventSystem causes duplicate input
        // handling, which is worse than this button silently not working in the
        // unlikely case one's ever missing.
        if (EventSystem.current == null) {
            Debug.LogWarning("CameraResetButton: no EventSystem in scene - this button won't receive clicks. Home key still works.");
        }

        GameObject btnObj = new GameObject("CameraResetButton");
        btnObj.transform.SetParent(canvasObj.transform, false);
        RectTransform rect = btnObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.anchoredPosition = new Vector2(-20f, -70f);
        rect.sizeDelta = new Vector2(160f, 40f);

        Image bg = btnObj.AddComponent<Image>();
        bg.color = new Color(0.15f, 0.15f, 0.15f, 0.85f);

        Button button = btnObj.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color(0.3f, 0.3f, 0.3f, 0.95f);
        colors.pressedColor = new Color(0.05f, 0.05f, 0.05f, 0.95f);
        button.colors = colors;
        button.onClick.AddListener(OnClick);

        GameObject textObj = new GameObject("Label");
        textObj.transform.SetParent(btnObj.transform, false);
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        Text label = textObj.AddComponent<Text>();
        label.font = RuntimeUIFont.Get();
        label.fontSize = 16;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = Color.white;
        label.text = "Return to Hall";
    }

    private void OnClick() {
        if (CameraController.main != null) CameraController.main.ReturnToMainHall();
    }
}