/*
    One slot inside DeckPanel - an actual drop target, not just a display box. Accepts:
    - A drag straight from a DeckCardSlot (IDropHandler -> owner.PlaceInSlot).
    - A click, which either places a currently "held" card (click-click flow) or, if
      nothing is held and this slot is filled, pulls that tower back out of the deck.
*/
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DeckPanelSlot : MonoBehaviour, IPointerClickHandler, IDropHandler,
    IBeginDragHandler, IDragHandler, IEndDragHandler {
    public Image icon;
    public Image background;

    [Tooltip("Background color once a tower is placed in this slot. Default (white) clears " +
             "whatever tint the background sprite already has, so the placeholder gray disappears.")]
    public Color filledColor = Color.white;

    [Tooltip("Background color while this slot is empty, so the tray still reads as 4 " +
             "drop targets before anything's been placed, not an empty stretch of screen.")]
    public Color emptyColor = new Color(1f, 1f, 1f, 0.35f);

    [HideInInspector] public int index;
    [HideInInspector] public DeckSelectionUI owner;

    private void Awake() {
        // An empty slot only shows/accepts drops via `background` (icon.enabled is false
        // until filled - see SetEmpty()). If Raycast Target got left unchecked on it, or
        // `background` was never wired up in the Inspector, the slot silently stops being
        // a valid drop target even though it's fully visible - which reads as "dragging a
        // card onto it does nothing". Force raycasting on defensively instead of relying
        // on that checkbox being set correctly by hand.
        if (background != null) {
            background.raycastTarget = true;
        } else if (icon != null) {
            icon.raycastTarget = true;
        } else {
            Debug.LogWarning($"DeckPanelSlot on '{name}' has neither Background nor Icon " +
                "assigned - this slot can never be a drop target. Assign at least Background.", this);
        }
    }

    public void SetTower(Sprite sprite) {
        if (icon != null) {
            icon.sprite = sprite;
            icon.enabled = sprite != null;
        }
        if (background != null) background.color = filledColor;
    }

    public void SetEmpty() {
        if (icon != null) icon.enabled = false;
        if (background != null) background.color = emptyColor;
    }

    public void OnPointerClick(PointerEventData eventData) {
        if (owner == null) return;

        if (owner.MatchStarted) {
            // Click-click build: click the slot to pick up that tower (dummy follows the
            // cursor via PlacementManager), click a tile afterward to place it.
            owner.OnGameplaySlotClicked(index);
        } else {
            owner.OnPanelSlotClicked(index);
        }
    }

    public void OnDrop(PointerEventData eventData) {
        if (owner == null || owner.IsReady || eventData.pointerDrag == null) {
            Debug.Log($"DeckPanelSlot[{index}] OnDrop ignored - owner={owner != null}, " +
                $"ready={owner != null && owner.IsReady}, pointerDrag={eventData.pointerDrag != null}");
            return;
        }

        DeckCardSlot card = eventData.pointerDrag.GetComponent<DeckCardSlot>();
        if (card != null && card.towerPrefab != null) {
            owner.PlaceInSlot(index, card.towerPrefab);
        } else {
            Debug.Log($"DeckPanelSlot[{index}] OnDrop received an object with no DeckCardSlot " +
                $"(or no towerPrefab): '{eventData.pointerDrag.name}'");
        }
    }

    // --- In-match drag-to-build. Once the pick grid is hidden after ready-up, this
    // slot is the only drag source left, so it needs its own handlers - without these,
    // dragging off it never calls BeginGameplayDrag/EndGameplayDrag, which is also what
    // sets DeckSelectionUI.IsDraggingCard for CameraController's drag guard.

    public void OnBeginDrag(PointerEventData eventData) {
        if (owner == null || !owner.MatchStarted) return;
        owner.BeginGameplayDrag(index);
    }

    public void OnDrag(PointerEventData eventData) {
        if (owner == null || !owner.MatchStarted) return;
        owner.UpdateDragPosition(eventData.position);
    }

    public void OnEndDrag(PointerEventData eventData) {
        if (owner == null || !owner.MatchStarted) return;
        owner.EndGameplayDrag();
    }
}