/*
    "Kiểm tra HP và hướng đi của Enemy: Hiển thị HP và highlight lối đi của Enemy."
        Controls: "Hover Cursor vào Enemy."
        -> HP, active statuses, and any status combos shown in a panel near the
           cursor. The enemy's specific path is highlighted as a cyan LineRenderer.

    "DMG Enemy: Gây sát thương trực tiếp."
        Controls: 1P "Click vào Enemy." / 2P "[SPACE]"
        -> Left-click or Space on a hovered enemy deals clickDamage via takeDamage().

    Path is now a LineRenderer (same approach as PathHighlighter) rather than
    tinting tiles; TileTint is no longer used by this class. Both path systems
    (Shift=all paths via PathHighlighter, hover=this enemy's path via here) draw
    clean lines through tile centers without interfering with each other.

    Status combos displayed (informational - not yet triggering bonus damage):
        Frozen   + Burning       -> Shatter!
        Frozen   + Electrocuted  -> Freeze Arc!
        Burning  + Electrocuted  -> Overload!
        Burning  + Stunned       -> Scorched!
        Electrocuted + Stunned   -> Surge!
        Overcharged  + Burning   -> Ignition!
        Overcharged  + Electrocuted -> Overclock!
*/
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class EnemyHoverInfo : MonoBehaviour {
    [Tooltip("Direct damage per click/[SPACE] while hovering an enemy.")]
    public float clickDamage = 10f;

    // --- UI ------------------------------------------------------------------
    private RectTransform canvasRect;
    private RectTransform panelRoot;
    private Text infoText;

    // --- Path line -----------------------------------------------------------
    private LineRenderer pathLine;
    private int currentPathID = -1;

    // --- State ---------------------------------------------------------------
    private GameObject hoveredEnemy;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install() {
        if (FindObjectOfType<EnemyHoverInfo>() != null) return;
        GameObject go = new GameObject("~EnemyHoverInfo");
        DontDestroyOnLoad(go);
        go.AddComponent<EnemyHoverInfo>();
    }

    private void Awake() {
        BuildUi();
        BuildPathLine();
        SetVisible(false);
    }

    private void Update() {
        GameObject enemy = GetMouseHoveredEnemy();

        if (enemy != hoveredEnemy) {
            hoveredEnemy = enemy;
            currentPathID = -1; // force path redraw on next frame
        }

        if (hoveredEnemy == null) {
            SetVisible(false);
            SetPathVisible(false);
            return;
        }

        Enemy enemyComp = hoveredEnemy.GetComponent<Enemy>();
        if (enemyComp == null) {
            SetVisible(false);
            SetPathVisible(false);
            return;
        }

        // --- Info panel ------------------------------------------------------
        SetVisible(true);
        RefreshInfoText(enemyComp);
        PositionNearCursor();

        // --- Path line -------------------------------------------------------
        int pid = enemyComp.PathID;
        if (pid != currentPathID) {
            currentPathID = pid;
            DrawPathLine(pid);
        }
        SetPathVisible(true);

        // --- DMG Enemy -------------------------------------------------------
        bool clicked = Input.GetButtonDown("Fire1") || Input.GetKeyDown(KeyCode.Space);
        bool overUi  = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        bool placing  = PlacementManager.main != null && PlacementManager.main.isPlacing;

        if (clicked && !overUi && !placing) {
            enemyComp.takeDamage(clickDamage);
            BuildResultLog.Show("Dealt " + clickDamage + " damage.", true);
        }
    }

    // -------------------------------------------------------------------------
    // Info text
    // -------------------------------------------------------------------------

    private void RefreshInfoText(Enemy e) {
        StringBuilder sb = new StringBuilder();

        // HP row
        sb.Append("<b>HP:</b> ")
          .Append(Mathf.CeilToInt(e.enemyHealth))
          .Append(" / ")
          .Append(Mathf.CeilToInt(e.getMaxHealth()))
          .Append('\n');

        // Statuses
        List<Status> statuses = e.Statuses;
        if (statuses != null && statuses.Count > 0) {
            sb.Append(StatusLine(statuses)).Append('\n');

            // Combos
            string combo = ComboLine(statuses);
            if (!string.IsNullOrEmpty(combo))
                sb.Append(combo).Append('\n');
        }

        infoText.text = sb.ToString().TrimEnd('\n');

        // Resize panel height to fit content
        int lineCount = CountLines(infoText.text);
        panelRoot.sizeDelta = new Vector2(210f, 18f + lineCount * 22f);
    }

    private static string StatusLine(List<Status> statuses) {
        StringBuilder sb = new StringBuilder();
        bool first = true;
        foreach (Status s in statuses) {
            if (!first) sb.Append("  ");
            first = false;
            switch (s.statusType) {
                case StatusType.Frozen:       sb.Append("<color=#70cfff>●Frozen</color>"); break;
                case StatusType.Electrocuted: sb.Append("<color=#ffe600>●Electrocuted</color>"); break;
                case StatusType.Burning:      sb.Append("<color=#ff8c00>●Burning</color>"); break;
                case StatusType.Stunned:      sb.Append("<color=#aaaaaa>●Stunned</color>"); break;
                case StatusType.Overcharged:  sb.Append("<color=#ff55ff>●Overcharged</color>"); break;
                case StatusType.Sprinting:    sb.Append("<color=#44ff88>●Sprinting</color>"); break;
                case StatusType.Shielded:     sb.Append("<color=#8899ff>●Shielded</color>"); break;
                default:                      sb.Append("<color=#cccccc>●" + s.statusType + "</color>"); break;
            }
        }
        return sb.ToString();
    }

    private static string ComboLine(List<Status> statuses) {
        bool has(StatusType t) {
            foreach (Status s in statuses) if (s.statusType == t) return true;
            return false;
        }

        bool frozen   = has(StatusType.Frozen);
        bool burning  = has(StatusType.Burning);
        bool electric = has(StatusType.Electrocuted);
        bool stunned  = has(StatusType.Stunned);
        bool overcharged = has(StatusType.Overcharged);

        if (frozen   && burning)  return "<color=#ff4444>★ Shatter!</color>";
        if (frozen   && electric) return "<color=#70cfff>★ Freeze Arc!</color>";
        if (burning  && electric) return "<color=#ffaa00>★ Overload!</color>";
        if (burning  && stunned)  return "<color=#ff6600>★ Scorched!</color>";
        if (electric && stunned)  return "<color=#ffff44>★ Surge!</color>";
        if (overcharged && burning)  return "<color=#ff77ff>★ Ignition!</color>";
        if (overcharged && electric) return "<color=#cc88ff>★ Overclock!</color>";
        return null;
    }

    private static int CountLines(string s) {
        if (string.IsNullOrEmpty(s)) return 1;
        int n = 1;
        foreach (char c in s) if (c == '\n') n++;
        return n;
    }

    // -------------------------------------------------------------------------
    // Path line (LineRenderer)
    // -------------------------------------------------------------------------

    private void BuildPathLine() {
        GameObject lineObj = new GameObject("EnemyPathLine");
        lineObj.transform.SetParent(transform, false);
        pathLine = lineObj.AddComponent<LineRenderer>();
        pathLine.useWorldSpace = true;
        pathLine.loop = false;
        pathLine.startWidth = 0.08f;
        pathLine.endWidth = 0.08f;
        pathLine.startColor = new Color(0.3f, 0.85f, 1f, 0.9f);
        pathLine.endColor   = new Color(0.3f, 0.85f, 1f, 0.4f);
        pathLine.sortingOrder = 4; // above PathHighlighter lines (3)
        pathLine.material = new Material(Shader.Find("Sprites/Default"));
        pathLine.enabled = false;
    }

    private void DrawPathLine(int pathID) {
        if (pathID < 0 || pathID >= MapGenerator.pathTiles.Count) {
            pathLine.positionCount = 0;
            return;
        }
        List<GameObject> path = MapGenerator.pathTiles[pathID];
        if (path == null || path.Count == 0) {
            pathLine.positionCount = 0;
            return;
        }

        pathLine.positionCount = path.Count;
        for (int i = 0; i < path.Count; i++) {
            Vector3 pos = path[i] != null
                ? path[i].transform.position
                : (i > 0 ? pathLine.GetPosition(i - 1) : Vector3.zero);
            pos.z = 0f;
            pathLine.SetPosition(i, pos);
        }
    }

    private void SetPathVisible(bool visible) {
        if (pathLine != null && pathLine.enabled != visible)
            pathLine.enabled = visible;
    }

    // -------------------------------------------------------------------------
    // Hover detection
    // -------------------------------------------------------------------------

    // Uses the sprite's rendered bounds rather than the enemy's Collider2D. That
    // collider is tuned for bullet-hit gameplay (fixed box size, and it rotates
    // with the enemy's travel-direction rotation in Enemy.moveEnemy()), so it
    // frequently doesn't line up with what's actually drawn on screen - hence
    // needing to hunt for a precise cursor angle to trigger the hover panel.
    // Sprite bounds always match what the player visually sees.
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

    // -------------------------------------------------------------------------
    // Panel positioning + visibility
    // -------------------------------------------------------------------------

    private void PositionNearCursor() {
        Vector2 screenPoint = (Vector2)Input.mousePosition + new Vector2(24f, 24f);
        Vector2 local;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, null, out local))
            panelRoot.anchoredPosition = local;
    }

    private void SetVisible(bool visible) {
        if (panelRoot != null && panelRoot.gameObject.activeSelf != visible)
            panelRoot.gameObject.SetActive(visible);
    }

    private void OnDisable() {
        SetPathVisible(false);
    }

    // -------------------------------------------------------------------------
    // UI construction
    // -------------------------------------------------------------------------

    private void BuildUi() {
        GameObject canvasObj = new GameObject("EnemyHoverInfoCanvas");
        canvasObj.transform.SetParent(transform, false);
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 7500;
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        // No GraphicRaycaster - must never eat clicks meant for enemies/tiles below.
        canvasRect = canvasObj.GetComponent<RectTransform>();

        GameObject rootObj = new GameObject("EnemyHoverPanel");
        rootObj.transform.SetParent(canvasObj.transform, false);
        panelRoot = rootObj.AddComponent<RectTransform>();
        panelRoot.sizeDelta = new Vector2(210f, 50f); // height adjusted each frame
        panelRoot.pivot = new Vector2(0f, 1f);

        Image bg = rootObj.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.65f);

        GameObject textObj = new GameObject("InfoText");
        textObj.transform.SetParent(rootObj.transform, false);
        RectTransform tr = textObj.AddComponent<RectTransform>();
        tr.anchorMin = Vector2.zero;
        tr.anchorMax = Vector2.one;
        tr.offsetMin = new Vector2(8f, 4f);
        tr.offsetMax = new Vector2(-8f, -4f);

        infoText = textObj.AddComponent<Text>();
        infoText.font = RuntimeUIFont.Get();
        infoText.fontSize = 18;
        infoText.color = Color.white;
        infoText.alignment = TextAnchor.UpperLeft;
        infoText.supportRichText = true;
        infoText.horizontalOverflow = HorizontalWrapMode.Overflow;
        infoText.verticalOverflow = VerticalWrapMode.Overflow;

        Shadow sh = textObj.AddComponent<Shadow>();
        sh.effectColor = new Color(0f, 0f, 0f, 0.8f);
        sh.effectDistance = new Vector2(1f, -1f);
    }
}
