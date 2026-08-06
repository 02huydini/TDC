/*
    "Kiểm tra cooldown: Hiển thị một thanh nhỏ trên đầu Tower cho thấy cooldown."
    Controls (toDo2.txt, same for both players): "Hover Cursor vào Tower."

    Self-installing, same pattern as PathHighlighter/BuildResultLog/DevConsole - no
    scene/prefab wiring required. While the mouse hovers a placed tower, a small bar
    appears above it: empty/red right after it fires, filling to full/green as it
    becomes ready to shoot again. Towers with no fire rate (Farm/Buff towers -
    Towers.HasAttackCooldown() == false) never show a bar, since they have nothing
    to cool down.

    Reuses PlacementManager.main.GetTowerUnderCursor() (the same towerMask raycast
    the merge-preview code already relies on) instead of rolling a second, possibly
    inconsistent hover check.
*/
using UnityEngine;
using UnityEngine.UI;

public class TowerCooldownDisplay : MonoBehaviour {
    private static readonly Color ReadyColor = new Color(0.4f, 1f, 0.4f, 0.95f);
    private static readonly Color CoolingColor = new Color(1f, 0.55f, 0.2f, 0.95f);

    private RectTransform canvasRect;
    private RectTransform barRoot;
    private RectTransform barBackground;
    private Image fillImage;

    // Image.Type.Filled (and Sliced/Tiled) silently do nothing without a Sprite -
    // Unity's Image.OnPopulateMesh() falls back to a plain full-rect quad when
    // sprite is null, so .color still visibly updates every frame but .fillAmount
    // has no effect at all. Generated once and shared, rather than depending on
    // any project sprite asset (this codebase has already hit a similar built-in-
    // resource-name gotcha in RuntimeUIFont.cs).
    private static Sprite solidSprite;
    private static Sprite GetSolidSprite() {
        if (solidSprite != null) return solidSprite;
        Texture2D tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        solidSprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        return solidSprite;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install() {
        if (FindObjectOfType<TowerCooldownDisplay>() != null) return;
        GameObject go = new GameObject("~TowerCooldownDisplay");
        DontDestroyOnLoad(go);
        go.AddComponent<TowerCooldownDisplay>();
    }

    private void Awake() {
        BuildUi();
        SetVisible(false);
    }

    private void Update() {
        if (PlacementManager.main == null) { SetVisible(false); return; }

        // Don't show a cooldown bar for the tower under the placement dummy while
        // the player is actively building/merging - it'd sit right on top of the
        // merge-preview tint and just be visual noise.
        if (PlacementManager.main.isPlacing) { SetVisible(false); return; }

        GameObject tower = PlacementManager.main.GetTowerUnderCursor();
        if (tower == null) { SetVisible(false); return; }

        Towers towerComp = tower.GetComponent<Towers>();
        if (towerComp == null || !towerComp.HasAttackCooldown()) { SetVisible(false); return; }

        SetVisible(true);

        float fraction = towerComp.GetCooldownReadyFraction();
        fillImage.fillAmount = fraction;
        fillImage.color = Color.Lerp(CoolingColor, ReadyColor, fraction);

        PositionOverTower(tower);
    }

    private void PositionOverTower(GameObject tower) {
        Vector3 worldPos = GetTopOfTower(tower);
        Vector2 screenPoint = PlacementManager.main.cam.WorldToScreenPoint(worldPos);

        Vector2 localPoint;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, null, out localPoint)) {
            barRoot.anchoredPosition = localPoint;
        }
    }

    // Tries to sit just above the tower's tallest sprite; falls back to a flat
    // offset above the pivot if the tower has no renderers for some reason.
    private Vector3 GetTopOfTower(GameObject tower) {
        SpriteRenderer[] renderers = tower.GetComponentsInChildren<SpriteRenderer>();
        if (renderers.Length == 0) return tower.transform.position + Vector3.up * 0.9f;

        float topY = float.NegativeInfinity;
        foreach (SpriteRenderer r in renderers) {
            if (r.bounds.max.y > topY) topY = r.bounds.max.y;
        }

        Vector3 top = tower.transform.position;
        top.y = topY + 0.08f; // small gap above the sprite
        return top;
    }

    private void SetVisible(bool visible) {
        if (barRoot != null) barRoot.gameObject.SetActive(visible);
    }

    private void BuildUi() {
        GameObject canvasObj = new GameObject("TowerCooldownDisplayCanvas");
        canvasObj.transform.SetParent(transform);
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 7000; // below BuildResultLog (8000) and DevConsole (10000)
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        // No GraphicRaycaster - purely informational, must never eat clicks meant
        // for the tower/tile underneath it.
        canvasRect = canvasObj.GetComponent<RectTransform>();

        GameObject rootObj = new GameObject("CooldownBar");
        rootObj.transform.SetParent(canvasObj.transform, false);
        barRoot = rootObj.AddComponent<RectTransform>();
        barRoot.sizeDelta = new Vector2(64f, 10f);
        barRoot.pivot = new Vector2(0.5f, 0.5f);

        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(rootObj.transform, false);
        barBackground = bgObj.AddComponent<RectTransform>();
        barBackground.anchorMin = Vector2.zero;
        barBackground.anchorMax = Vector2.one;
        barBackground.offsetMin = Vector2.zero;
        barBackground.offsetMax = Vector2.zero;
        Image bgImage = bgObj.AddComponent<Image>();
        bgImage.color = new Color(0f, 0f, 0f, 0.55f);

        GameObject fillObj = new GameObject("Fill");
        fillObj.transform.SetParent(rootObj.transform, false);
        RectTransform fillRect = fillObj.AddComponent<RectTransform>();
        fillRect.anchorMin = new Vector2(0.05f, 0.15f);
        fillRect.anchorMax = new Vector2(0.95f, 0.85f);
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        fillImage = fillObj.AddComponent<Image>();
        fillImage.sprite = GetSolidSprite();
        fillImage.color = ReadyColor;
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Horizontal;
        fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
        fillImage.fillAmount = 1f;
    }
}
