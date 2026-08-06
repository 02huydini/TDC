/*
    "Hiển thị lối đi của Enemy: [SHIFT]" - holding Shift tints every tile in
    MapGenerator.pathTiles so the player can see where enemies will walk.
    Released Shift reverts every tile back to its original color.

    Drop this on any persistent GameObject in the gameplay scene (e.g. the same
    one PlacementManager or GameController lives on) - it needs no Inspector
    wiring, it just reads MapGenerator.pathTiles at runtime.
*/
using System.Collections.Generic;
using UnityEngine;

public class PathHighlighter : MonoBehaviour {
    public Color highlightColor = new Color(1f, 0.95f, 0.2f, 1f);

    private readonly Dictionary<SpriteRenderer, Color> originalColors = new Dictionary<SpriteRenderer, Color>();
    private bool highlighting = false;

    // Self-installing, same as DevConsole - last time this was left as "drop it on any
    // GameObject in the scene", which meant it never actually ran because nothing was
    // ever dropped on anything. No Editor step needed now.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install() {
        if (FindObjectOfType<PathHighlighter>() != null) return;
        GameObject go = new GameObject("~PathHighlighter");
        DontDestroyOnLoad(go);
        go.AddComponent<PathHighlighter>();
    }

    private void Update() {
        if (Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift)) {
            SetHighlight(true);
        } else if (Input.GetKeyUp(KeyCode.LeftShift) || Input.GetKeyUp(KeyCode.RightShift)) {
            SetHighlight(false);
        }
    }

    private void SetHighlight(bool on) {
        if (on == highlighting) return;
        highlighting = on;

        if (on) {
            originalColors.Clear();
            foreach (List<GameObject> path in MapGenerator.pathTiles) {
                foreach (GameObject tile in path) {
                    if (tile == null) continue;
                    SpriteRenderer renderer = tile.GetComponent<SpriteRenderer>();
                    if (renderer == null || originalColors.ContainsKey(renderer)) continue;

                    originalColors[renderer] = renderer.color;
                    renderer.color = highlightColor;
                }
            }
        } else {
            foreach (KeyValuePair<SpriteRenderer, Color> entry in originalColors) {
                if (entry.Key != null) entry.Key.color = entry.Value;
            }
            originalColors.Clear();
        }
    }

    // Safety net: if this object is disabled/destroyed mid-highlight, don't leave
    // path tiles stuck tinted.
    private void OnDisable() {
        SetHighlight(false);
    }
}
