/*
    "Ectos: Ectoplasma, Năng lượng cho các kĩ năng" (toDo2.txt) has no on-screen
    counter anywhere in the scene - EctoManager tracks the value and Boons already
    spend it (see ShopManager), but there's nowhere for the player to see it.

    Self-installing, same pattern as DamageNumbers/TowerCooldownDisplay/DevConsole -
    no scene/prefab wiring needed. Positioned to sit just under the existing
    "Wood: X" box (see PlayerMoneyBG.prefab's anchoredPosition) rather than
    hand-editing GameCanvas.prefab's nested UI, which risks silently corrupting
    layout that can't be verified without opening the Editor.
*/
using UnityEngine;
using UnityEngine.UI;

public class EctoDisplay : MonoBehaviour {
    private static readonly Color EctoColor = new Color(0.55f, 0.85f, 1f, 1f);

    private Text ectoText;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install() {
        if (FindObjectOfType<EctoDisplay>() != null) return;
        GameObject go = new GameObject("~EctoDisplay");
        DontDestroyOnLoad(go);
        go.AddComponent<EctoDisplay>();
    }

    private void Awake() {
        BuildUi();
    }

    private void Update() {
        if (ectoText == null) return;
        ectoText.text = EctoManager.main != null ? "Ectos: " + EctoManager.main.GetCurrEctos() : "Ectos: -";
    }

    private void BuildUi() {
        GameObject canvasObj = new GameObject("EctoDisplayCanvas");
        canvasObj.transform.SetParent(transform, false);
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 6000;
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        // No GraphicRaycaster - purely informational, must never eat clicks.

        GameObject bgObj = new GameObject("EctoBG");
        bgObj.transform.SetParent(canvasObj.transform, false);
        RectTransform bgRect = bgObj.AddComponent<RectTransform>();
        bgRect.anchorMin = new Vector2(0.5f, 0.5f);
        bgRect.anchorMax = new Vector2(0.5f, 0.5f);
        // Directly under PlayerMoneyBG's own anchoredPosition (-290, 119.49621).
        bgRect.anchoredPosition = new Vector2(-290f, 119.49621f - 45f);
        bgRect.sizeDelta = new Vector2(200f, 40f);
        Image bg = bgObj.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.5f);

        GameObject textObj = new GameObject("EctoText");
        textObj.transform.SetParent(bgObj.transform, false);
        RectTransform tr = textObj.AddComponent<RectTransform>();
        tr.anchorMin = Vector2.zero;
        tr.anchorMax = Vector2.one;
        tr.offsetMin = Vector2.zero;
        tr.offsetMax = Vector2.zero;

        ectoText = textObj.AddComponent<Text>();
        ectoText.font = RuntimeUIFont.Get();
        ectoText.fontSize = 18;
        ectoText.fontStyle = FontStyle.Bold;
        ectoText.alignment = TextAnchor.MiddleCenter;
        ectoText.color = EctoColor;
        ectoText.text = "Ectos: 0";

        Shadow sh = textObj.AddComponent<Shadow>();
        sh.effectColor = new Color(0f, 0f, 0f, 0.85f);
        sh.effectDistance = new Vector2(1f, -1f);
    }
}
