/*
    Floating damage numbers. Called from Enemy.takeDamage() so every damage source
    (bullets, AOE, ElementalShot, status ticks, player click-damage) automatically
    spawns a number without each call site needing to know about the UI.

    Self-installing (AfterSceneLoad), no prefab or Inspector wiring needed.

    Number style:
      amount <= 5   small gray    (status tick, chip damage)
      amount <= 20  white         (normal hit)
      amount <= 50  yellow        (solid hit)
      amount >  50  orange-red    (heavy hit)

    Numbers drift upward ~80px/s in screen space over 1 second then fade and
    are returned to a pool of 30. A small random X offset prevents stacking when
    multiple enemies are hit simultaneously at the same position.
*/
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DamageNumbers : MonoBehaviour {
    private const int PoolSize = 30;
    private const float Lifetime = 1f;
    private const float RiseSpeed = 80f;

    private struct Slot {
        public RectTransform rect;
        public Text text;
        public float timeLeft;
        public Vector2 velocity;
        public bool active;
    }

    private static DamageNumbers instance;
    private Camera cam;
    private RectTransform canvasRect;
    private Slot[] pool;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install() {
        if (FindObjectOfType<DamageNumbers>() != null) return;
        GameObject go = new GameObject("~DamageNumbers");
        DontDestroyOnLoad(go);
        go.AddComponent<DamageNumbers>();
    }

    private void Awake() {
        instance = this;
        BuildPool();
    }

    private void LateUpdate() {
        cam = Camera.main;
        for (int i = 0; i < PoolSize; i++) {
            if (!pool[i].active) continue;
            pool[i].timeLeft -= Time.unscaledDeltaTime;
            if (pool[i].timeLeft <= 0f) {
                pool[i].active = false;
                pool[i].rect.gameObject.SetActive(false);
                continue;
            }
            // Drift upward
            pool[i].rect.anchoredPosition += pool[i].velocity * Time.unscaledDeltaTime;
            // Fade out in last 40% of life
            float fade = Mathf.Clamp01(pool[i].timeLeft / (Lifetime * 0.4f));
            Color c = pool[i].text.color;
            c.a = fade;
            pool[i].text.color = c;
        }
    }

    // Called from Enemy.takeDamage(). worldPos is the enemy's transform.position.
    public static void Show(Vector3 worldPos, float amount) {
        if (instance == null) return;
        instance.SpawnNumber(worldPos, amount);
    }

    private void SpawnNumber(Vector3 worldPos, float amount) {
        int slot = GetFreeSlot();
        if (slot < 0) return; // pool exhausted - skip rather than allocate

        cam = Camera.main;
        if (cam == null) return;

        // Convert world -> screen -> canvas local
        Vector2 screenPos = cam.WorldToScreenPoint(worldPos);
        screenPos.x += Random.Range(-18f, 18f); // scatter so stacked hits are readable
        screenPos.y += 20f;                      // start slightly above the sprite pivot

        Vector2 localPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPos, null, out localPos);

        pool[slot].rect.anchoredPosition = localPos;
        pool[slot].rect.gameObject.SetActive(true);

        // Content + color
        pool[slot].text.text = Mathf.CeilToInt(amount).ToString();
        pool[slot].text.color = DamageColor(amount);
        pool[slot].text.fontSize = DamageFontSize(amount);

        // Reset animation state
        pool[slot].timeLeft = Lifetime;
        pool[slot].velocity = new Vector2(Random.Range(-8f, 8f), RiseSpeed);
        pool[slot].active = true;
    }

    private static Color DamageColor(float amount) {
        if (amount <= 5f)  return new Color(0.65f, 0.65f, 0.65f, 1f); // gray
        if (amount <= 20f) return new Color(1f, 1f, 1f, 1f);           // white
        if (amount <= 50f) return new Color(1f, 0.92f, 0.2f, 1f);      // yellow
        return new Color(1f, 0.45f, 0.1f, 1f);                          // orange-red
    }

    private static int DamageFontSize(float amount) {
        if (amount <= 5f)  return 18;
        if (amount <= 20f) return 24;
        return 30;
    }

    private int GetFreeSlot() {
        for (int i = 0; i < PoolSize; i++) {
            if (!pool[i].active) return i;
        }
        return -1;
    }

    private void BuildPool() {
        // Canvas
        GameObject canvasObj = new GameObject("DamageNumbersCanvas");
        canvasObj.transform.SetParent(transform, false);
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 6500; // below cooldown bar (7000) but above game world
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        // No GraphicRaycaster - purely visual, must never eat clicks.
        canvasRect = canvasObj.GetComponent<RectTransform>();

        pool = new Slot[PoolSize];
        Font font = RuntimeUIFont.Get();

        for (int i = 0; i < PoolSize; i++) {
            GameObject go = new GameObject("DMG_" + i);
            go.transform.SetParent(canvasObj.transform, false);

            RectTransform rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(80f, 40f);
            rt.pivot = new Vector2(0.5f, 0f);

            Text t = go.AddComponent<Text>();
            t.font = font;
            t.fontSize = 24;
            t.fontStyle = FontStyle.Bold;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = Color.white;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.supportRichText = false;

            Shadow sh = go.AddComponent<Shadow>();
            sh.effectColor = new Color(0f, 0f, 0f, 0.85f);
            sh.effectDistance = new Vector2(1f, -1f);

            go.SetActive(false);

            pool[i] = new Slot {
                rect = rt,
                text = t,
                active = false
            };
        }
    }
}
