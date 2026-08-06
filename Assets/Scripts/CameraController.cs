/*
    Developing the camera boundary limits, scroll wheel and middle mouse
    is based on this tutorial: https://www.youtube.com/watch?v=IfbMKe6p9nM
    More work needs to be done with this.
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class CameraController : MonoBehaviour {
    public static CameraController main;

    private Vector3 MouseScrollStartPos;
    private Camera mainCamera;

    // True only for a press that legitimately started a camera drag (i.e. wasn't over
    // UI/placing at mouse-down). The held-check below used to re-evaluate eligibility
    // fresh every frame instead of remembering this, so if a placement/UI drag ended
    // mid-hold, the held branch would fire using a MouseScrollStartPos that was never
    // set for this press - producing a "movement" equal to the whole absolute camera-
    // to-mouse distance instead of a small per-frame delta, flinging the camera.
    private bool isDraggingCamera;


    // different speed values, serialized for editor adjustment to save in the code
    [SerializeField] private float MoveSpeed = 20f;
    [SerializeField] private float ZoomSpeed = 80f;

    private float spriteSize = 1f;

    // Start is called before the first frame update
    private void Start() {
        if (main == null) main = this;
        mainCamera = GetComponent<Camera>();
        spriteSize = MapGenerator.main.getSpriteSize();
        mainCamera.orthographicSize = spriteSize * 5;
    }

    // Update is called once per frame
    private void Update() {
        if (!HandleKeyInput()) {
            HandleMouseInput();
        }
        HandleWheelScroll();

        // "Make a way to return camera to main hall, incase of bugged camera like
        // camera flying off to corner that can't find map anymore." There's no
        // boundary clamp on camera position anywhere in this file (see the class
        // comment above) - WASD/drag genuinely has no limit, so this is a real way
        // to get stuck. Home key as a quick keyboard escape hatch; ReturnToMainHall()
        // below is also called by CameraResetButton.cs's on-screen button.
        if (Input.GetKeyDown(KeyCode.Home)) ReturnToMainHall();
    }

    // Snaps the camera back over the home tile (Main Hall), keeping its current
    // zoom/z depth. Safe to call even if MapGenerator/endTile isn't ready yet.
    public void ReturnToMainHall() {
        if (mainCamera == null || MapGenerator.endTile == null) return;
        Vector3 target = MapGenerator.endTile.transform.position;
        mainCamera.transform.position = new Vector3(target.x, target.y, mainCamera.transform.position.z);
    }

    private bool HandleKeyInput() {
        Vector3 movement = Vector3.zero;
        if (Input.GetKey("w") || Input.GetKey("up")) {
            movement = new Vector3(0, MoveSpeed * Time.deltaTime, 0);
            mainCamera.transform.position += movement;
        }
        if (Input.GetKey("s") || Input.GetKey("down")) {
            movement = new Vector3(0, -MoveSpeed * Time.deltaTime, 0);
            mainCamera.transform.position += movement;
        }
        if (Input.GetKey("a") || Input.GetKey("left")) {
            movement = new Vector3(-MoveSpeed * Time.deltaTime, 0, 0);
            mainCamera.transform.position += movement;
        }
        if (Input.GetKey("d") || Input.GetKey("right")) {
            movement = new Vector3(MoveSpeed * Time.deltaTime, 0, 0);
            mainCamera.transform.position += movement;
        }

        if (movement != Vector3.zero)
            return true;

        return false;
    }

    private bool HandleMouseInput() {
        // Don't pan the camera while the player is dragging/clicking UI (the deck panel,
        // shop buttons, etc.) or actively placing a tower - otherwise a UI drag also drags
        // the map underneath it, since this only ever reads raw mouse input and has no idea
        // a UI drag is in progress.
        bool interactingWithUI = (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            || (PlacementManager.main != null && PlacementManager.main.isPlacing)
            || DeckSelectionUI.IsDraggingCard;

        if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(2)) {
            isDraggingCamera = !interactingWithUI;
            if (isDraggingCamera) MouseScrollStartPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        }

        // If UI/placement starts up mid-hold, drop out of the drag entirely rather than
        // just skipping a frame - resuming later would still use the old, now-stale
        // MouseScrollStartPos and produce the same kind of jump.
        if (interactingWithUI) isDraggingCamera = false;

        if (Input.GetMouseButtonUp(0) || Input.GetMouseButtonUp(2)) {
            isDraggingCamera = false;
        }

        if ((Input.GetMouseButton(0) || Input.GetMouseButton(2)) && isDraggingCamera) {
            Vector3 movement = mainCamera.ScreenToWorldPoint(Input.mousePosition) - MouseScrollStartPos;
            mainCamera.transform.position -= movement;
            return true;
        }
        return false;
    }

    private bool HandleWheelScroll() {
        if (Input.mouseScrollDelta.y != 0) {
            mainCamera.orthographicSize += Input.mouseScrollDelta.y * Time.deltaTime * ZoomSpeed;
            mainCamera.orthographicSize = Mathf.Clamp(mainCamera.orthographicSize, spriteSize * 2, spriteSize * 8);
            return true;
        }
        return false;
    }
}