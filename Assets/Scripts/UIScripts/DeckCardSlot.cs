/*
    One card on the pre-match Tower Deck screen. Ported from Build's DeckPickCardUI
    (Build/Assets/GameplayScript/UI/DeckPickCardUI.cs), trimmed down from that file's
    2-player (mouse + keyboard-grid) version to single-player mouse-only, matching
    the "keep TDC mouse-only for now" scope.

    Reuses TDC's own tower assets: `icon`/`nameLabel` are meant to be wired to the same
    sprites/text already used by the existing shop buttons (e.g. BuildingSniperButton),
    not new Build artwork. `linkedShopButton` is a reference to that existing shop button
    GameObject, which DeckSelectionUI shows/hides based on what got picked.

    Supports both input patterns from toDo2.txt's "Click-Click hoặc Drag-Drop" phrasing:
    - Click-click: clicking a card "picks it up" (owner.OnCardClicked), then clicking a
      DeckPanelSlot places it there.
    - Drag-drop: dragging the card straight onto a DeckPanelSlot places it there via
      that slot's IDropHandler.
*/
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DeckCardSlot : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler,
    IBeginDragHandler, IDragHandler, IEndDragHandler {
    [Tooltip("Tower prefab this card represents (one of TDC's existing tower prefabs, e.g. 'Archer Tower').")]
    public GameObject towerPrefab;

    [Tooltip("The existing in-match shop button that builds this tower (e.g. BuildingSniperButton in GameCanvas). " +
             "Hidden for the rest of the match if this card isn't picked.")]
    public GameObject linkedShopButton;

    [Header("UI refs (point these at the same icon/name assets the shop button already uses)")]
    public Image icon;
    public Text nameLabel;
    public Image selectionFrame;

    [Header("Colors (matches Build's Player 1 hover/selected colors)")]
    public Color hoverColor = Color.cyan;
    public Color selectedColor = Color.blue;

    private DeckSelectionUI owner;
    private bool hovering;

    public void Setup(DeckSelectionUI ownerUI) {
        owner = ownerUI;

        if (towerPrefab != null && nameLabel != null) {
            Towers towerData = towerPrefab.GetComponent<Towers>();
            if (towerData != null) nameLabel.text = towerData.getName();
        }

        hovering = false;
        RefreshFrame();
    }

    public void OnPointerEnter(PointerEventData eventData) {
        hovering = true;
        RefreshFrame();
    }

    public void OnPointerExit(PointerEventData eventData) {
        hovering = false;
        RefreshFrame();
    }

    public void OnPointerClick(PointerEventData eventData) {
        if (owner != null) owner.OnCardClicked(this);
    }

    public void OnBeginDrag(PointerEventData eventData) {
        if (owner == null || towerPrefab == null || owner.IsReady) return;
        owner.BeginDrag(icon != null ? icon.sprite : null);
    }

    public void OnDrag(PointerEventData eventData) {
        if (owner == null || towerPrefab == null) return;
        owner.UpdateDragPosition(eventData.position);
    }

    public void OnEndDrag(PointerEventData eventData) {
        if (owner == null) return;
        // If we were released over a DeckPanelSlot, its OnDrop already placed the
        // tower - this just cleans up the floating drag icon either way.
        owner.EndDrag();
    }

    public void RefreshFrame() {
        if (selectionFrame == null) return;

        bool selected = PlayerLoadout.IsSelected(towerPrefab);
        selectionFrame.gameObject.SetActive(selected || hovering);
        selectionFrame.color = selected ? selectedColor : hoverColor;
    }
}