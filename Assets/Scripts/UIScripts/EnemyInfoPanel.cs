/*
    "Make right click enemy show their info like tower does, which display HP,
    speed, status, have effect (like detection)."

    Mirrors Towers.OnMouseDown's click-to-open persistent panel, rather than
    EnemyHoverInfo's existing hover-driven readout (which is transient and
    disappears the instant the cursor leaves - fine for a quick glance, not for
    "select this enemy and read it at leisure" the way a tower's Upgrade menu
    already works).

    Right-click a live enemy to open; right-click the same enemy again, a
    different enemy, or empty space to close (same enemy re-clicked toggles it
    off, exactly like re-clicking wouldn't reopen a tower's own menu). Left-click/
    [SPACE] still deals EnemyHoverInfo's click-damage independently - the two
    don't collide since this one is strictly bound to Fire2 (right mouse).

    "have effect (like detection)" - read as the enemy's own Hidden/stealth
    state (Enemy.IsHidden), the one enemy-side property this project already
    models under a name related to Detection (Towers.hasDetection is what lets a
    tower see past it).

    Self-installing, no scene/prefab wiring needed - same pattern as
    EnemyHoverInfo/DevConsole/PathHighlighter.
*/
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class EnemyInfoPanel : MonoBehaviour {
    private RectTransform panelRect;
    private Text infoText;
    private GameObject selectedEnemy;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install() {
        if (FindObjectOfType<EnemyInfoPanel>() != null) return;
        GameObject go = new GameObject("~EnemyInfoPanel");
        DontDestroyOnLoad(go);
        go.AddComponent<EnemyInfoPanel>();
    }

    private void Awake() {
        BuildUi();
    }

    private void Update() {
        // Refresh/close first, so a right-click this same frame that clears
        // selectedEnemy doesn't leave a stale panel showing for one extra frame.
        if (selectedEnemy != null) {
            Enemy enemyComp = selectedEnemy.GetComponent<Enemy>();
            if (enemyComp == null) {
                selectedEnemy = null;
            } else {
                RefreshInfoText(enemyComp);
            }
        }
        SetVisible(selectedEnemy != null);

        if (Input.GetMouseButtonDown(1)) {
            bool overUi = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
            if (overUi) return;

            GameObject clicked = GetMouseHoveredEnemy();
            // Same enemy right-clicked again -> close. Different enemy (or none
            // while one was open) -> select/switch. None while none open -> no-op.
            selectedEnemy = (clicked != null && clicked != selectedEnemy) ? clicked : null;
        }
    }

    private void RefreshInfoText(Enemy e) {
        StringBuilder sb = new StringBuilder();
        sb.Append("<b>").Append(selectedEnemy.name).Append("</b>\n");
        sb.Append("HP: ").Append(Mathf.CeilToInt(e.enemyHealth)).Append(" / ").Append(Mathf.CeilToInt(e.getMaxHealth())).Append('\n');
        sb.Append("Speed: ").Append(e.movementSpeed.ToString("0.#")).Append(" / ").Append(e.getMaxSpeed().ToString("0.#")).Append('\n');
        sb.Append("Hidden: ").Append(e.IsHidden ? "Yes (needs Detection)" : "No");

        List<Status> statuses = e.Statuses;
        if (statuses != null && statuses.Count > 0) {
            sb.Append("\nStatus: ");
            bool first = true;
            foreach (Status s in statuses) {
                if (!first) sb.Append(", ");
                first = false;
                sb.Append(s.statusType);
            }
        }

        infoText.text = sb.ToString();
    }

    // Same sprite-bounds hit test as EnemyHoverInfo.GetMouseHoveredEnemy() - kept
    // as its own copy rather than exposing that one, so this file stays fully
    // self-contained (matches this codebase's existing preference for small,
    // independent self-installing scripts over shared cross-file helpers).
    private static GameObject GetMouseHoveredEnemy() {
        if (PlacementManager.main == null) return null;
        Vector2 mouseWorldPos = PlacementManager.main.GetMousePosition();

        foreach (GameObject enemy in Counter.enemies) {
            if (enemy == null) continue;
            SpriteRenderer sr = enemy.GetComponentInChildren<SpriteRenderer>();
            if (sr != null && sr.bounds.Contains(mouseWorldPos)) return enemy;
        }
        return null;
    }

    private void SetVisible(bool visible) {
        if (panelRect != null && panelRect.gameObject.activeSelf != visible)
            panelRect.gameObject.SetActive(visible);
    }

    private void BuildUi() {
        GameObject canvasObj = new GameObject("EnemyInfoPanelCanvas");
        canvasObj.transform.SetParent(transform, false);
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 7600; // above EnemyHoverInfo's hover panel (7500)
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        // No GraphicRaycaster - purely visual, must never eat clicks meant for
        // enemies/tiles below (same reasoning as EnemyHoverInfo's own canvas).

        GameObject panelObj = new GameObject("EnemyInfoPanel");
        panelObj.transform.SetParent(canvasObj.transform, false);
        panelRect = panelObj.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0f, 1f);
        panelRect.anchorMax = new Vector2(0f, 1f);
        panelRect.pivot = new Vector2(0f, 1f);
        panelRect.anchoredPosition = new Vector2(20f, -120f); // left side, clear of the tower-menu UI on the right
        panelRect.sizeDelta = new Vector2(240f, 130f);

        Image bg = panelObj.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.7f);

        GameObject textObj = new GameObject("InfoText");
        textObj.transform.SetParent(panelObj.transform, false);
        RectTransform tr = textObj.AddComponent<RectTransform>();
        tr.anchorMin = Vector2.zero;
        tr.anchorMax = Vector2.one;
        tr.offsetMin = new Vector2(10f, 8f);
        tr.offsetMax = new Vector2(-10f, -8f);

        infoText = textObj.AddComponent<Text>();
        infoText.font = RuntimeUIFont.Get();
        infoText.fontSize = 18;
        infoText.color = Color.white;
        infoText.alignment = TextAnchor.UpperLeft;
        infoText.supportRichText = true;
        infoText.horizontalOverflow = HorizontalWrapMode.Wrap;
        infoText.verticalOverflow = VerticalWrapMode.Overflow;

        Shadow sh = textObj.AddComponent<Shadow>();
        sh.effectColor = new Color(0f, 0f, 0f, 0.8f);
        sh.effectDistance = new Vector2(1f, -1f);

        panelRect.gameObject.SetActive(false);
    }
}
