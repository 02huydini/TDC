/*
    "Create keybind 1-4 for tower selection."

    Alpha1-Alpha4 trigger the same thing clicking the Nth shop button would.
    Deliberately reuses each button's own Button.onClick (which is what's
    actually wired, in the Editor, to PlacementManager.StartPlacing(prefab))
    instead of keeping a second, parallel list of tower prefabs here that could
    quietly drift out of sync with the real shop buttons.

    Shop tower buttons are found via CostDisplay - every tower shop button
    already has one (see CostDisplay.cs) - and ordered by sibling index so
    1-4 map to the same left-to-right order the buttons appear in on screen.

    Re-scans once a second rather than once at startup: cheap, and tolerates the
    shop panel not existing yet on the very first frame after a scene load
    (e.g. DeckSelectionUI's pre-match screen, if that's still up).

    Self-installing, no scene/prefab wiring needed.
*/
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TowerHotkeys : MonoBehaviour {
    private static readonly KeyCode[] Keys = { KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4 };

    private readonly List<Button> shopButtons = new List<Button>();
    private float nextRescan = 0f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install() {
        if (FindObjectOfType<TowerHotkeys>() != null) return;
        GameObject go = new GameObject("~TowerHotkeys");
        DontDestroyOnLoad(go);
        go.AddComponent<TowerHotkeys>();
    }

    private void Update() {
        if (Time.unscaledTime >= nextRescan) {
            Rescan();
            nextRescan = Time.unscaledTime + 1f;
        }

        for (int i = 0; i < Keys.Length; i++) {
            if (!Input.GetKeyDown(Keys[i])) continue;
            if (i >= shopButtons.Count || shopButtons[i] == null) continue;

            // Respects whatever the button itself currently allows (e.g. greyed
            // out / not interactable because the player can't afford it) instead
            // of bypassing that check the way calling StartPlacing() directly
            // would.
            if (shopButtons[i].interactable) shopButtons[i].onClick.Invoke();
        }
    }

    private void Rescan() {
        shopButtons.Clear();

        CostDisplay[] displays = FindObjectsOfType<CostDisplay>();
        Array.Sort(displays, (a, b) => a.transform.GetSiblingIndex().CompareTo(b.transform.GetSiblingIndex()));

        foreach (CostDisplay cd in displays) {
            Button btn = cd.GetComponent<Button>();
            if (btn == null) btn = cd.GetComponentInParent<Button>();
            if (btn == null) btn = cd.GetComponentInChildren<Button>();
            if (btn != null) shopButtons.Add(btn);
        }
    }
}
