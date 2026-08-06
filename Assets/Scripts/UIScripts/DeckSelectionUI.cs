/*
    Pre-match Tower Deck selection screen, ported from Build's TowerDeckSelectionUI
    (Build/Assets/GameplayScript/UI/TowerDeckSelectionUI.cs) into TDC, per toDo2.txt's
    "Miêu tả màn chuẩn bị": show tower info, let the player pick up to 4 towers, lock in
    with a ready button, then start the match. Build's version is 2-player (mouse for P1,
    WASD-grid for P2, two colored decks); this version keeps only the P1 half, since TDC
    is staying single-player/mouse-only for now.

    Deliberately does NOT bring in Build's GameDatabase/TowerData/card-prefab-instantiation
    pipeline - TDC has a fixed, small set of tower prefabs with their own existing shop
    buttons in GameCanvas, so this just reuses those buttons (via DeckCardSlot.linkedShopButton)
    instead of generating new UI from a database.

    Follows the same overlay pattern already used by GameOverBG.cs / PauseMenu.cs in TDC:
    hide the gameplay canvas and pause Time.timeScale while open, restore both on lock-in.
    Wave start is gated separately via RoundController.waitForMatchStart + BeginMatch()
    (see RoundController.cs) so the countdown doesn't run out from under the player while
    they're still picking towers.

    Placement (toDo2.txt's "Click-Click hoặc Drag-Drop" pattern) works two ways:
    - Click-click: a single click on a grid card toggles it straight in/out of the deck
      (first open slot). Works even with zero DeckPanelSlot objects in the scene.
    - Drag-drop: drag a grid card straight onto a DeckPanel slot to place it into that
      specific slot; clicking a filled slot with nothing being dragged removes it.
*/
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class DeckSelectionUI : MonoBehaviour {
    [Tooltip("One entry per pickable tower. Set towerPrefab + linkedShopButton on each in the Inspector.")]
    public DeckCardSlot[] cardSlots;

    [Tooltip("Persistent tray the player drags/click-places towers into. See DeckPanel.cs setup notes.")]
    public DeckPanel deckPanel;

    [Tooltip("A small Image under the top-level Canvas used as the floating drag icon. Kept disabled " +
             "until a drag starts. Assumes a Screen Space - Overlay canvas.")]
    public Image dragGhostIcon;

    [Tooltip("Locks in the current picks and drops into the match with waves held (see startWaveButton).")]
    public Button readyButton;

    [Tooltip("Second ready step, shown once the gameplay canvas is up: locks in initial tower " +
             "placement and actually starts Wave 1's countdown. Optional - if left unassigned, " +
             "waves start immediately after readyButton the way they used to.")]
    public Button startWaveButton;

    [Tooltip("Recolored on startWaveButton once pressed. Defaults to startWaveButton's own Image if left empty.")]
    public Image startWaveButtonGraphic;

    [Tooltip("Recolored on the ready button when the player readies up. Defaults to readyButton's own Image if left empty.")]
    public Image readyButtonGraphic;

    [Tooltip("Ready button color before the player has readied up.")]
    public Color readyIdleColor = Color.white;

    [Tooltip("Ready button color once the player has readied up.")]
    public Color readyConfirmedColor = Color.green;

    [Tooltip("How long the button stays visibly green before the match actually starts.")]
    public float readyConfirmDelay = 0.35f;

    [Tooltip("Optional label, e.g. 'Choose up to 4 towers'.")]
    public Text infoLabel;

    [Tooltip("Optional warning shown if the player tries to ready up with an empty deck.")]
    public GameObject emptyDeckWarning;

    [Tooltip("Optional: container holding the pick grid + ready button (+ any backdrop dim). " +
             "If assigned, this whole thing is hidden once the match starts, while deckPanel " +
             "keeps showing as the in-match build tray. If left empty, the cards/ready button/" +
             "info label are hidden individually instead - either way deckPanel is never touched.")]
    public GameObject pickGridRoot;

    [Tooltip("The dim backdrop shown behind the pick grid. Hidden once the player readies up.")]
    public GameObject grayBackdrop;

    public bool IsReady { get; private set; }
    public bool MatchStarted { get; private set; }

    private RectTransform dragGhostRect;

    private void Awake() {
        if (readyButton != null) {
            readyButton.onClick.AddListener(OnReadyClicked);
            if (readyButtonGraphic == null) readyButtonGraphic = readyButton.image;
        }
        if (startWaveButton != null) {
            startWaveButton.onClick.AddListener(OnStartWaveClicked);
            if (startWaveButtonGraphic == null) startWaveButtonGraphic = startWaveButton.image;
            startWaveButton.gameObject.SetActive(false);
        }
        if (dragGhostIcon != null) {
            dragGhostRect = dragGhostIcon.rectTransform;
            dragGhostIcon.raycastTarget = false;
            dragGhostIcon.gameObject.SetActive(false);
        }
        if (deckPanel != null) deckPanel.Setup(this);
    }

    private void OnEnable() {
        Show();
    }

    public void Show() {
        PlayerLoadout.Clear();

        if (CanvasManager.main != null) CanvasManager.main.Hide();
        TimeHandler.PauseGameTime();

        IsReady = false;
        MatchStarted = false;
        SetPickGridVisible(true);
        if (grayBackdrop != null) grayBackdrop.SetActive(true);
        if (readyButton != null) readyButton.interactable = true;
        if (readyButtonGraphic != null) readyButtonGraphic.color = readyIdleColor;
        if (emptyDeckWarning != null) emptyDeckWarning.SetActive(false);
        if (infoLabel != null) infoLabel.text = $"Choose up to {PlayerLoadout.MaxDeckSize} towers";

        foreach (DeckCardSlot card in cardSlots) {
            if (card == null) continue;
            card.Setup(this);
            // Every tower is shown as pickable while this screen is open; the shop only
            // gets narrowed down to the locked-in deck once the player readies up.
            if (card.linkedShopButton != null) card.linkedShopButton.SetActive(true);
        }

        RefreshAll();
        gameObject.SetActive(true);
    }

    // --- Plain click (works with zero DeckPanelSlot objects in the scene) --------

    /// <summary>
    /// A single click on a grid card toggles it straight in/out of the deck into the
    /// first open slot - the same behavior the screen always had. This never depends
    /// on the panel existing, so selection works even before any DeckPanelSlot is set up.
    /// </summary>
    public void OnCardClicked(DeckCardSlot card) {
        if (IsReady || card == null || card.towerPrefab == null) return;
        PlayerLoadout.Toggle(card.towerPrefab);
        RefreshAll();
    }

    /// <summary>A click on a filled panel slot (with nothing being dragged) removes that tower.</summary>
    public void OnPanelSlotClicked(int index) {
        if (IsReady) return;
        PlayerLoadout.RemoveFromSlot(index);
        RefreshAll();
    }

    // --- Drag-drop placement (optional - only matters once DeckPanelSlot objects exist) --

    /// <summary>Called by a DeckPanelSlot's IDropHandler when a card is dropped on it.</summary>
    public void PlaceInSlot(int index, GameObject towerPrefab) {
        if (IsReady || towerPrefab == null) return;
        PlayerLoadout.AssignToSlot(index, towerPrefab);
        RefreshAll();
    }

    /// <summary>
    /// True for the whole span of any card/tower drag (pre-match or in-match), not just
    /// while the cursor happens to sit over a UI element. CameraController checks this
    /// directly - EventSystem.IsPointerOverGameObject() alone flips false the instant the
    /// drag crosses off the deck panel onto the map, which was causing the camera to jump
    /// mid-drag.
    /// </summary>
    public static bool IsDraggingCard { get; private set; }

    public void BeginDrag(Sprite sprite) {
        IsDraggingCard = true;
        if (dragGhostIcon == null) return;
        dragGhostIcon.sprite = sprite;
        dragGhostIcon.gameObject.SetActive(sprite != null);
    }

    public void UpdateDragPosition(Vector2 screenPosition) {
        if (dragGhostRect != null) dragGhostRect.position = screenPosition;
    }

    public void EndDrag() {
        IsDraggingCard = false;
        if (dragGhostIcon != null) dragGhostIcon.gameObject.SetActive(false);
    }

    // --- In-match placement (once MatchStarted) --------------------------------
    // toDo2.txt: "Đặt Tower: Click-Click hoặc Drag-Drop từ Deck vào sân chơi." Both paths
    // just drive TDC's existing PlacementManager the same way the static per-tower shop
    // buttons already did - no changes to PlacementManager's own placement rules needed.

    /// <summary>Click-click: click a filled deck slot to pick up that tower (mouse then
    /// follows it), click a valid grid tile afterward to place it - identical to clicking
    /// one of the old per-tower shop buttons.</summary>
    public void OnGameplaySlotClicked(int index) {
        if (!MatchStarted) return;
        GameObject tower = PlayerLoadout.GetSlot(index);
        if (tower == null || PlacementManager.main == null) return;

        if (PlacementManager.main.isPlacing && PlacementManager.main.CurrentlyPlacing == tower) {
            // Clicking the same slot again while it's the one being placed cancels it -
            // mirrors clicking a held card again during pre-match deck picking.
            PlacementManager.main.EndPlacement();
            return;
        }

        PlacementManager.main.StartPlacing(tower);
    }

    /// <summary>Drag-drop: dragging a deck slot starts placement immediately (dummy tower
    /// follows the cursor via PlacementManager's own hover-tile tracking) plus a small
    /// floating icon for feedback, same ghost used during pre-match deck building.</summary>
    public void BeginGameplayDrag(int index) {
        if (!MatchStarted) return;
        GameObject tower = PlayerLoadout.GetSlot(index);
        if (tower == null || PlacementManager.main == null) return;
        PlacementManager.main.StartPlacing(tower);
        // Passing null keeps IsDraggingCard true (CameraController's drag guard still
        // applies) but leaves dragGhostIcon inactive - the world-space dummy tower from
        // StartPlacing() already tracks the cursor, so the floating UI icon on top of it
        // was redundant and is what showed up as an unwanted "drag icon".
        BeginDrag(null);
    }

    /// <summary>On release, attempt to place at wherever PlacementManager is currently
    /// hovering, then force placement to end either way - a drag always resolves into
    /// either a placed tower or a clean cancel, it never leaves a dummy stuck on the cursor.</summary>
    public void EndGameplayDrag() {
        EndDrag();
        if (PlacementManager.main == null || !PlacementManager.main.isPlacing) return;
        PlacementManager.main.Placement("Drag");
        PlacementManager.main.EndPlacement();
    }

    private void SetPickGridVisible(bool visible) {
        if (pickGridRoot != null) {
            pickGridRoot.SetActive(visible);
            return;
        }
        // No dedicated root assigned - toggle the known pick-grid pieces individually so
        // deckPanel (a sibling, not part of this list) is never affected either way.
        foreach (DeckCardSlot card in cardSlots) {
            if (card != null) card.gameObject.SetActive(visible);
        }
        if (readyButton != null) readyButton.gameObject.SetActive(visible);
        if (infoLabel != null) infoLabel.gameObject.SetActive(visible);
        if (!visible && emptyDeckWarning != null) emptyDeckWarning.SetActive(false);
    }

    // --- Shared refresh --------------------------------------------------------

    private void RefreshAll() {
        foreach (DeckCardSlot card in cardSlots) {
            if (card != null) card.RefreshFrame();
        }

        if (deckPanel != null) deckPanel.Refresh(GetIconFor);
    }

    private Sprite GetIconFor(GameObject towerPrefab) {
        foreach (DeckCardSlot card in cardSlots) {
            if (card != null && card.towerPrefab == towerPrefab && card.icon != null)
                return card.icon.sprite;
        }
        return null;
    }

    // --- Ready / start -----------------------------------------------------------

    private void OnReadyClicked() {
        if (IsReady) return;

        if (PlayerLoadout.SelectedTowers.Count == 0) {
            if (emptyDeckWarning != null) emptyDeckWarning.SetActive(true);
            return;
        }

        // Lock the deck in immediately (no more placing/removing) and flip the button
        // green so the player gets clear "you're ready" feedback before the screen
        // actually closes.
        IsReady = true;
        if (emptyDeckWarning != null) emptyDeckWarning.SetActive(false);
        if (readyButton != null) readyButton.interactable = false;
        if (readyButtonGraphic != null) readyButtonGraphic.color = readyConfirmedColor;

        StartCoroutine(ConfirmAndStart());
    }

    private IEnumerator ConfirmAndStart() {
        // Realtime because TimeHandler.PauseGameTime() has Time.timeScale at 0 here.
        yield return new WaitForSecondsRealtime(readyConfirmDelay);

        // Only the towers that made it into the deck stay buildable for the rest of the
        // match - reuses TDC's existing shop buttons/assets, just gates their visibility.
        foreach (DeckCardSlot card in cardSlots) {
            if (card == null || card.linkedShopButton == null) continue;
            card.linkedShopButton.SetActive(PlayerLoadout.IsSelected(card.towerPrefab));
        }

        // Hide the pick grid + ready button only - deckPanel (a sibling) stays up and now
        // works as the persistent in-match build tray (see OnGameplaySlotClicked/BeginGameplayDrag).
        SetPickGridVisible(false);
        if (grayBackdrop != null) grayBackdrop.SetActive(false);
        MatchStarted = true;

        if (CanvasManager.main != null) CanvasManager.main.Show();
        TimeHandler.StartGameTime();

        // "A ready button for setting up towers at beginning before the game start":
        // the player is now looking at the field with deckPanel available to build
        // from, but Wave 1's countdown does not begin until they explicitly confirm
        // via startWaveButton - RoundController.waitForMatchStart keeps isStartOfRound
        // false (and therefore FixedUpdate() a no-op) the whole time, regardless of
        // Time.timeScale, so there's no rush and no risk of the wave sneaking in.
        if (startWaveButton != null) {
            startWaveButton.gameObject.SetActive(true);
            startWaveButton.interactable = true;
            if (startWaveButtonGraphic != null) startWaveButtonGraphic.color = readyIdleColor;
        } else {
            // No button wired up in the Inspector - build one at runtime instead of
            // silently skipping straight to Wave 1. Same fix pattern as DevConsole/
            // PathHighlighter: a feature shouldn't depend on someone remembering to
            // drag a reference into a field.
            BuildRuntimeStartWaveButton();
        }
    }

    private void OnStartWaveClicked() {
        if (startWaveButton != null) {
            startWaveButton.interactable = false;
            if (startWaveButtonGraphic != null) startWaveButtonGraphic.color = readyConfirmedColor;
            startWaveButton.gameObject.SetActive(false);
        }
        if (RoundController.main != null) RoundController.main.BeginMatch();
    }

    // --- Fallback: build the Start Wave button at runtime if none was wired up ----

    private void BuildRuntimeStartWaveButton() {
        GameObject canvasObj = new GameObject("~StartWaveCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9000;
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasObj.AddComponent<GraphicRaycaster>();

        if (FindObjectOfType<EventSystem>() == null) {
            GameObject esObj = new GameObject("EventSystem");
            esObj.AddComponent<EventSystem>();
            esObj.AddComponent<StandaloneInputModule>();
        }

        GameObject buttonObj = new GameObject("StartWaveButton");
        buttonObj.transform.SetParent(canvasObj.transform, false);
        RectTransform rect = buttonObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(0f, 40f);
        rect.sizeDelta = new Vector2(260f, 70f);

        Image bg = buttonObj.AddComponent<Image>();
        bg.color = readyIdleColor == Color.white ? new Color(0.2f, 0.75f, 0.3f, 1f) : readyIdleColor;

        Button button = buttonObj.AddComponent<Button>();
        button.targetGraphic = bg;

        GameObject labelObj = new GameObject("Label");
        labelObj.transform.SetParent(buttonObj.transform, false);
        RectTransform labelRect = labelObj.AddComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;
        Text label = labelObj.AddComponent<Text>();
        label.text = "Start Wave";
        label.font = RuntimeUIFont.Get();
        label.fontSize = 28;
        label.fontStyle = FontStyle.Bold;
        label.color = Color.black;
        label.alignment = TextAnchor.MiddleCenter;

        button.onClick.AddListener(() => {
            Destroy(canvasObj);
            if (RoundController.main != null) RoundController.main.BeginMatch();
        });
    }
}