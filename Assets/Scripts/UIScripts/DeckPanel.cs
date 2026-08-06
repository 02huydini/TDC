/*
    Persistent deck tray for the pre-match screen (toDo2.txt: "Players được phép chọn
    đến 4 Tower cho mỗi Tower Deck"). Each slot is a real input target (see
    DeckPanelSlot) - the tray isn't just a readout of the pick grid, it's where the
    click-click and drag-drop placements actually land.

    Setup in the Inspector:
    - Add exactly 4 child slot objects (icon + background Image each, with a
      DeckPanelSlot component), assign them to `slots` in slot order.
    - All 4 stay visible/active at all times (empty ones show a dim placeholder via
      DeckPanelSlot.SetEmpty()) so they're always valid drop targets - lay them out
      at fixed positions/sizes rather than relying on a layout group to pack around
      (de)activated children.
*/
using System;
using UnityEngine;

public class DeckPanel : MonoBehaviour {
    [Tooltip("Exactly PlayerLoadout.MaxDeckSize (4) slots.")]
    public DeckPanelSlot[] slots;

    public void Setup(DeckSelectionUI owner) {
        if (slots == null) return;
        for (int i = 0; i < slots.Length; i++) {
            if (slots[i] == null) continue;
            slots[i].index = i;
            slots[i].owner = owner;
        }
    }

    /// <summary>
    /// Rebuilds the tray straight from PlayerLoadout's per-slot assignment. iconLookup
    /// resolves a tower prefab to the sprite shown for it on the pick grid
    /// (DeckCardSlot.icon.sprite).
    ///
    /// Slots stay active/visible at all times, filled or not. An inactive GameObject
    /// can't receive Unity UI raycasts, so a hidden empty slot was never actually a
    /// valid drop target - there was nothing on screen to drag a card onto in the first
    /// place. Empty slots now show a dim placeholder (DeckPanelSlot.SetEmpty()) instead,
    /// so all 4 tray slots are visible and droppable from the moment the screen opens.
    /// </summary>
    public void Refresh(Func<GameObject, Sprite> iconLookup) {
        if (slots == null) return;

        for (int i = 0; i < slots.Length; i++) {
            if (slots[i] == null) continue;

            GameObject tower = PlayerLoadout.GetSlot(i);
            if (tower != null) {
                slots[i].SetTower(iconLookup?.Invoke(tower));
            } else {
                slots[i].SetEmpty();
            }
        }
    }
}