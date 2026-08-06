/*
    Draws a translucent circle showing a tower's attack range while the mouse
    hovers over it. Self-installing, no scene wiring needed.

    Uses a LineRenderer (world-space circle, 64 segments) rather than a canvas
    UI element because range is a world-space radius and a circle needs to scale
    with camera zoom. sortingOrder is set so it sits above the map tiles but
    below towers and enemies.

    Reuses PlacementManager.GetTowerUnderCursor() - same towerMask raycast the
    merge system uses, so hover detection is consistent across all tower UI.
*/
using UnityEngine;

public class TowerRangeIndicator : MonoBehaviour {
    private const int Segments = 64;

    private LineRenderer line;
    private GameObject trackedTower;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install() {
        if (FindObjectOfType<TowerRangeIndicator>() != null) return;
        GameObject go = new GameObject("~TowerRangeIndicator");
        DontDestroyOnLoad(go);
        go.AddComponent<TowerRangeIndicator>();
    }

    private void Awake() {
        line = gameObject.AddComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.loop = true;
        line.positionCount = Segments;
        line.startWidth = 0.06f;
        line.endWidth = 0.06f;
        line.startColor = new Color(0.4f, 0.9f, 1f, 0.7f);
        line.endColor   = new Color(0.4f, 0.9f, 1f, 0.7f);
        // sortingOrder alone can't fix this: Unity sorts by sortingLayer FIRST,
        // sortingOrder only breaks ties within the same layer. Map tiles render on
        // the "Ground" layer and towers on "Objects" (see Map Tile.prefab / Fire
        // Tower.prefab) - a LineRenderer left on the default "Default" layer draws
        // behind both regardless of sortingOrder, however high. Matching towers'
        // "Objects" layer, with a high order within it, is what actually fixes it.
        line.sortingLayerID = SortingLayer.NameToID("Objects");
        line.sortingOrder = 40000;
        // Sprites/Default renders in 2D without depth-fighting.
        line.material = new Material(Shader.Find("Sprites/Default"));
        line.enabled = false;
    }

    private void Update() {
        if (PlacementManager.main == null || PlacementManager.main.isPlacing) {
            SetVisible(false);
            return;
        }

        GameObject tower = PlacementManager.main.GetTowerUnderCursor();

        if (tower == null) {
            SetVisible(false);
            trackedTower = null;
            return;
        }

        // Redraw only when the hovered tower changes (avoids recreating 64 points
        // every frame when nothing has moved).
        if (tower != trackedTower) {
            trackedTower = tower;
            DrawCircle(tower);
        }

        SetVisible(true);
    }

    private void DrawCircle(GameObject tower) {
        Towers t = tower.GetComponent<Towers>();
        if (t == null) { SetVisible(false); return; }

        float radius = t.GetRange();
        Vector3 center = tower.transform.position;
        center.z = 0f;

        for (int i = 0; i < Segments; i++) {
            float angle = (i / (float)Segments) * Mathf.PI * 2f;
            float x = center.x + Mathf.Cos(angle) * radius;
            float y = center.y + Mathf.Sin(angle) * radius;
            line.SetPosition(i, new Vector3(x, y, 0f));
        }
    }

    private void SetVisible(bool visible) {
        if (line.enabled != visible) line.enabled = visible;
    }
}
