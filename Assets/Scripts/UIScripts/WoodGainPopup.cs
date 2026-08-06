/*
    "Wood also display how much they're getting (ex: +10 display under Wood
    resource when an enemy DMG by player)" - floating "+N" text that pops under
    the Wood counter every time WoodManager's balance goes UP (wave-complete
    bonus, Wood Farm Tower payout, tower sell refund, etc). Only fires on
    increases - spending Wood (buying/upgrading towers) stays silent, since the
    ask was specifically about seeing gains.

    WoodManager only exposes GetCurrWood(), not a change event, so this polls
    once per frame and diffs against the last-seen value rather than adding a
    WoodManager-side hook - keeps WoodManager.cs untouched, the same "read the
    existing manager from outside" approach PathHighlighter/EnemyHoverInfo
    already use for MapGenerator/Counter.

    Locates the on-screen "Wood: N" Text by scanning every Text for one whose
    content starts with "Wood:" (exactly what WoodManager.AddWood()/RemoveWood()
    write) rather than needing a direct reference into WoodManager's private
    playerWoodTxt field. Re-checked every frame until found, in case the HUD
    isn't built yet on the first frame.

    Self-installing, no scene/prefab wiring needed. Reuses DamageNumbers.cs's
    pooled-floating-text approach, kept as its own small pool here since this
    one anchors to a UI element's screen position instead of a world position.
*/
using UnityEngine;
using UnityEngine.UI;

public class WoodGainPopup : MonoBehaviour {
    private const int PoolSize = 8;
    private const float Lifetime = 1f;
    private const float RiseSpeed = 60f;

    private struct Slot {
        public RectTransform rect;
        public Text text;
        public float timeLeft;
        public bool active;
    }

    private RectTransform canvasRect;
    private Slot[] pool;
    private Text woodText;
    private int lastWood = int.MinValue;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install() {
        if (FindObjectOfType<WoodGainPopup>() != null) return;
        GameObject go = new GameObject("~WoodGainPopup");
        DontDestroyOnLoad(go);
        go.AddComponent<WoodGainPopup>();
    }

    private void Awake() {
        BuildPool();
    }

    private void LateUpdate() {
        if (woodText == null) woodText = FindWoodText();

        if (WoodManager.main != null) {
            int current = WoodManager.main.GetCurrWood();

            if (lastWood == int.MinValue) {
                lastWood = current; // baseline on first read - don't pop for the starting balance
            } else if (current > lastWood && woodText != null) {
                Spawn(current - lastWood);
                lastWood = current;
            } else {
                lastWood = current;
            }
        }

        Tick();
    }

    private void Tick() {
        for (int i = 0; i < PoolSize; i++) {
            if (!pool[i].active) continue;

            pool[i].timeLeft -= Time.unscaledDeltaTime;
            if (pool[i].timeLeft <= 0f) {
                pool[i].active = false;
                pool[i].rect.gameObject.SetActive(false);
                continue;
            }

            pool[i].rect.anchoredPosition += new Vector2(0f, RiseSpeed) * Time.unscaledDeltaTime;

            Color c = pool[i].text.color;
            c.a = Mathf.Clamp01(pool[i].timeLeft / (Lifetime * 0.4f));
            pool[i].text.color = c;
        }
    }

    private void Spawn(int amount) {
        int slot = -1;
        for (int i = 0; i < PoolSize; i++) {
            if (!pool[i].active) { slot = i; break; }
        }
        if (slot < 0) return; // pool exhausted - skip rather than allocate

        // Screen Space Overlay canvas: a UI element's RectTransform.position IS
        // already a screen-space pixel position, no camera conversion needed.
        Vector2 screenPos = woodText.rectTransform.position;
        screenPos.y -= 26f; // "under Wood resource"

        Vector2 local;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPos, null, out local);

        pool[slot].rect.anchoredPosition = local;
        pool[slot].rect.gameObject.SetActive(true);
        pool[slot].text.text = "+" + amount;

        Color c = pool[slot].text.color;
        c.a = 1f;
        pool[slot].text.color = c;

        pool[slot].timeLeft = Lifetime;
        pool[slot].active = true;
    }

    private static Text FindWoodText() {
        Text[] all = FindObjectsOfType<Text>();
        foreach (Text t in all) {
            if (t != null && t.text != null && t.text.StartsWith("Wood:")) return t;
        }
        return null;
    }

    private void BuildPool() {
        GameObject canvasObj = new GameObject("WoodGainPopupCanvas");
        canvasObj.transform.SetParent(transform, false);
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 6600; // above DamageNumbers (6500), below the tower-menu UI (6001... clarifying: below 7000-range hover/menu panels)
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        // No GraphicRaycaster - purely visual, must never eat clicks.
        canvasRect = canvasObj.GetComponent<RectTransform>();

        pool = new Slot[PoolSize];
        Font font = RuntimeUIFont.Get();

        for (int i = 0; i < PoolSize; i++) {
            GameObject go = new GameObject("WOODGAIN_" + i);
            go.transform.SetParent(canvasObj.transform, false);

            RectTransform rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(100f, 30f);
            rt.pivot = new Vector2(0.5f, 1f);

            Text t = go.AddComponent<Text>();
            t.font = font;
            t.fontSize = 20;
            t.fontStyle = FontStyle.Bold;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = new Color(0.4f, 1f, 0.4f, 1f);
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.supportRichText = false;

            Shadow sh = go.AddComponent<Shadow>();
            sh.effectColor = new Color(0f, 0f, 0f, 0.85f);
            sh.effectDistance = new Vector2(1f, -1f);

            go.SetActive(false);

            pool[i] = new Slot { rect = rt, text = t, active = false };
        }
    }
}
