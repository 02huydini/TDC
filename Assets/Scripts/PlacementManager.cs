/*
    Most code in this file was written out by Nathan Granger based on the free tutorial 
    videos posted by youtube user ZeveonHD, found at 
    https://www.youtube.com/playlist?list=PL5AKnriDHZs5a8De2wK_qqrwBUqjZo0hN. Many
    function and variable names may have been changed and some parts of the code may
    have been modified to fit our game scheme, these sections will be marked with 
    comments. 
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlacementManager : MonoBehaviour {
    public static PlacementManager main;

    public ShopManager shopManager;


    private GameObject currObjPlacing;

    private GameObject dummyPlacement;

    private GameObject hoverTile;

    public Camera cam;

    public LayerMask mask;
    public LayerMask towerMask;

    private bool flag = false;

    public bool isPlacing;

    // Grid-cell occupancy, keyed by the tile GameObject itself rather than a live
    // physics raycast. Each map tile is a stable, persistent GameObject (MapGenerator
    // reuses an existing tile if one is already at a given position rather than
    // creating a duplicate), so its identity IS the grid coordinate - no separate
    // Vector2Int/row-col system needed.
    //
    // This replaces using GetTowerUnderCursor() (a second, independent Physics2D
    // raycast against towerMask) to decide "is this tile occupied". That raycast
    // and the one that finds hoverTile (against the map layer) query different
    // colliders at the same mouse position, and tower sprites are taller/wider than
    // one grid cell in this isometric view - so near a tile's edge the two raycasts
    // could disagree about which cell/tower was under the cursor. That's what made
    // merging work from one edge of a tower and not the other, and let a second
    // tower get instantiated overlapping the first instead of merging or blocking.
    // Every placement decision below now reads/writes this dictionary instead.
    private static readonly Dictionary<GameObject, GameObject> towerAtTile = new Dictionary<GameObject, GameObject>();
    private static readonly Dictionary<GameObject, GameObject> tileOfTower = new Dictionary<GameObject, GameObject>();

    public static GameObject GetTowerAtTile(GameObject tile) {
        if (tile == null) return null;
        return towerAtTile.TryGetValue(tile, out GameObject tower) ? tower : null;
    }

    // Lets TurretSpriteRenderer calibrate a tower's sortingOrder against the exact
    // tile it's standing on (see TurretSpriteRenderer.updateSortingLayerValue()).
    public static GameObject GetTileOfTower(GameObject tower) {
        if (tower == null) return null;
        return tileOfTower.TryGetValue(tower, out GameObject tile) ? tile : null;
    }

    private static void SetTileOccupant(GameObject tile, GameObject tower) {
        if (tile == null || tower == null) return;
        towerAtTile[tile] = tower;
        tileOfTower[tower] = tile;
    }

    // Called by UpgradeManager.Sell() so a sold tower's tile is freed up again.
    // Without this, selling a tower would leave the grid dictionary thinking that
    // tile is still occupied forever, permanently blocking rebuilding on it.
    public static void ClearOccupantTower(GameObject tower) {
        if (tower == null) return;
        if (tileOfTower.TryGetValue(tower, out GameObject tile)) {
            towerAtTile.Remove(tile);
            tileOfTower.Remove(tower);
        }
    }

    /// <summary>The tower/boon prefab currently being placed, or null. Used by deck-slot
    /// clicks to detect "clicking the same slot again" as a cancel rather than a restart.</summary>
    public GameObject CurrentlyPlacing => currObjPlacing;

    private void Start() {
        if (main == null) main = this;
    }

    private void Update() {
        if (isPlacing == true) {
            StartCoroutine(GetCurrentHoverTile());

            if (dummyPlacement != null) {
                if (hoverTile != null) {
                    dummyPlacement.transform.position = hoverTile.transform.position;

                    // Match PlaceBuilding()/PlaceBoon()'s sortingOrder convention for the
                    // real tower. Without this the dummy keeps whatever sortingOrder its
                    // prefab shipped with, which can tie with the map tile underneath and
                    // flip-flop in draw order frame to frame - the other half of the
                    // flicker, alongside the disabled colliders above.
                    SpriteRenderer hoverRenderer = hoverTile.GetComponent<SpriteRenderer>();
                    SpriteRenderer dummyRenderer = dummyPlacement.GetComponent<SpriteRenderer>();
                    if (hoverRenderer != null && dummyRenderer != null) {
                        dummyRenderer.sortingOrder = hoverRenderer.sortingOrder;

                        if (!flag && dummyPlacement.transform.childCount > 0) {
                            SpriteRenderer childRenderer = dummyPlacement.transform.GetChild(0).GetComponent<SpriteRenderer>();
                            if (childRenderer != null) childRenderer.sortingOrder = hoverRenderer.sortingOrder + 1;
                        }
                    }

                    SetDummyVisible(true);
                    UpdateMergePreview();
                } else {
                    // No valid tile under the cursor yet (e.g. placement was just started
                    // by clicking a deck slot, before the mouse has ever been over the
                    // map) - hide the dummy instead of leaving it sitting at whatever
                    // raw position Instantiate() gave it, which is what looked like the
                    // tower "flinging away" or disappearing off in empty space.
                    SetDummyVisible(false);
                }
            }

            if (Input.GetButtonDown("Fire1"))
                Placement("Click");

            if (Input.GetButtonDown("Fire2") || Input.GetKeyDown(KeyCode.Escape))
                EndPlacement();
        }
    }

    private void SetDummyVisible(bool visible) {
        if (dummyPlacement == null) return;
        foreach (SpriteRenderer renderer in dummyPlacement.GetComponentsInChildren<SpriteRenderer>()) {
            renderer.enabled = visible;
        }
    }

    // "Nếu Tháp trong tay ghép được, hiển thị Preview Icon/Color" - tints every
    // sprite on the ghost/preview green while hovering a tower it can merge with,
    // red while hovering one it can't, and back to normal otherwise.
    private void UpdateMergePreview() {
        if (flag || dummyPlacement == null) return; // Boons don't merge

        // Uses hoverTile (the same tile the dummy is snapped to), not a second
        // independent raycast - so the preview always matches what an actual click
        // right now would do, including at tile edges.
        GameObject towerUnderCursor = GetTowerAtTile(hoverTile);
        Color tint = Color.white;

        if (towerUnderCursor != null) {
            Towers incoming = currObjPlacing.GetComponent<Towers>();
            Towers existing = towerUnderCursor.GetComponent<Towers>();
            bool mergeable = incoming != null && existing != null &&
                incoming.TryGetMergeResult(existing.getName()) != null;

            tint = mergeable ? new Color(0.4f, 1f, 0.4f, 0.85f) : new Color(1f, 0.4f, 0.4f, 0.85f);
        }

        foreach (SpriteRenderer renderer in dummyPlacement.GetComponentsInChildren<SpriteRenderer>()) {
            renderer.color = tint;
        }
    }

    public Vector2 GetMousePosition() {
        return cam.ScreenToWorldPoint(Input.mousePosition);
    }

    public IEnumerator GetCurrentHoverTile() {
        Vector2 mousePosition = GetMousePosition();

        RaycastHit2D hit = Physics2D.Raycast(mousePosition, new Vector2(0, 0), 0.1f, mask, -100, 100);

        // Default to "nothing valid under the cursor right now". Previously hoverTile
        // was only ever assigned, never cleared, so if the cursor wasn't over a valid
        // tile this frame it silently kept whatever tile was hovered during the *last*
        // placement - possibly clear across the map. That stale reference is what made
        // a new dummy/placement snap to the wrong spot when placement started before
        // the mouse had been over the map at all this time.
        hoverTile = null;

        if (hit.collider != null) {
            if (MapGenerator.mapTiles.Contains(hit.collider.gameObject) &&
                !Counter.towers.Contains(hit.collider.gameObject))        //Check if obj is mapTile
            {
                bool isPathTile = false;
                foreach (List<GameObject> path in MapGenerator.pathTiles)       //Check if obj is pathTile
                {
                    if (path.Contains(hit.collider.gameObject))  //Check that mapTile is not pathTile
                    {
                        isPathTile = true;
                        break;
                    }
                }

                if (!isPathTile) hoverTile = hit.collider.gameObject;
            }
        }

        yield return null;
    }

    public bool checkForTower() {
        // Grid-based, same reasoning as PlaceBuilding()/TryMergeTower() below.
        return GetTowerAtTile(hoverTile) != null;
    }

    // Returns the actual Tower GameObject under the cursor (or null), so callers
    // like PlaceBuilding() can inspect it for a possible merge instead of just
    // knowing "something is there".
    public GameObject GetTowerUnderCursor() {
        Vector2 mousePosition = GetMousePosition();
        RaycastHit2D hit = Physics2D.Raycast(mousePosition, new Vector2(0, 0), 0.1f, towerMask, -100, 100);

        if (hit.collider == null) return null;

        // The raycast can land on a child collider (e.g. a sprite/hitbox object one
        // level under the tower root) that never got moved onto the Tower layer by
        // SetLayerRecursively below - most likely on any tower placed/merged before
        // this fix. Walk up to the root Towers component so merge detection doesn't
        // silently miss it. Falls back to the hit object itself if there's no Towers
        // anywhere in its parent chain.
        Towers towerComp = hit.collider.GetComponentInParent<Towers>();
        return towerComp != null ? towerComp.gameObject : hit.collider.gameObject;
    }

    // Recursively assigns a layer to a GameObject and every child under it. Tower
    // prefabs can have their visible sprite/collider on a child object; only setting
    // the layer on the root (as PlaceBuilding()/TryMergeTower() used to) left that
    // child on its original layer, so towerMask raycasts (GetTowerUnderCursor, used by
    // merge detection) would hit-or-miss depending on prefab structure - the source of
    // "merging works sometimes, not other times".
    private static void SetLayerRecursively(GameObject obj, int layer) {
        obj.layer = layer;
        foreach (Transform child in obj.transform) {
            SetLayerRecursively(child.gameObject, layer);
        }
    }

    public void Placement(string source = "Click") {
        placementSource = source;

        if (hoverTile == null) {
            LogBuildAttempt(DiagnoseNoHoverTile());
            return;
        }

        if (flag)
            PlaceBoon();
        else
            PlaceBuilding();
    }

    // Small, consistent Unity-console log line for every build attempt, tagged with
    // which input method triggered it (click-click vs drag-drop) so the two are easy
    // to tell apart while testing. Separate from BuildResultLog's on-screen toast -
    // this one is specifically for the Console window.
    private string placementSource = "Click";

    private void LogBuildAttempt(string result) {
        Debug.Log("[Build:" + placementSource + "] " + result);
    }

    // Re-runs the same raycast GetCurrentHoverTile() does, but reports exactly which
    // check failed instead of just "hoverTile was null". Only called on an actual
    // failed placement attempt (not every frame), so it can afford to be verbose.
    // The most common cause: 'mask' is a LayerMask field on this component - if it
    // was never set in the Inspector it defaults to 0 ("Nothing"), which makes every
    // raycast miss regardless of where the cursor is, on every single attempt. This
    // reports that explicitly instead of leaving you to guess.
    private string DiagnoseNoHoverTile() {
        Vector2 mousePosition = GetMousePosition();
        RaycastHit2D hit = Physics2D.Raycast(mousePosition, new Vector2(0, 0), 0.1f, mask, -100, 100);

        if (hit.collider == null) {
            return "no valid tile under cursor - raycast hit nothing at all. " +
                   "PlacementManager.mask is currently set to: " + MaskToLayerNames(mask) +
                   ". MapGenerator.mapTiles currently has " + MapGenerator.mapTiles.Count + " tiles registered" +
                   (MapGenerator.mapTiles.Count == 0 ? " (0 means the map likely hasn't generated yet)." : ".");
        }

        GameObject hitObj = hit.collider.gameObject;

        if (!MapGenerator.mapTiles.Contains(hitObj)) {
            return "no valid tile under cursor - raycast hit '" + hitObj.name +
                   "' (layer '" + LayerMask.LayerToName(hitObj.layer) +
                   "'), which is not in MapGenerator.mapTiles (" + MapGenerator.mapTiles.Count + " tiles registered).";
        }

        if (Counter.towers.Contains(hitObj)) {
            return "no valid tile under cursor - raycast hit a GameObject that's also tracked as a tower in Counter.towers, unexpectedly.";
        }

        foreach (List<GameObject> path in MapGenerator.pathTiles) {
            if (path.Contains(hitObj)) {
                return "no valid tile under cursor - hovering '" + hitObj.name + "', which is a path tile (not buildable).";
            }
        }

        return "no valid tile under cursor - unexplained: hoverTile should have resolved to '" + hitObj.name + "' but didn't. Report this.";
    }

    private static string MaskToLayerNames(LayerMask mask) {
        if (mask.value == 0) return "<none>";
        List<string> names = new List<string>();
        for (int i = 0; i < 32; i++) {
            if ((mask.value & (1 << i)) != 0) {
                string layerName = LayerMask.LayerToName(i);
                names.Add(string.IsNullOrEmpty(layerName) ? ("Layer" + i) : layerName);
            }
        }
        return string.Join(",", names);
    }

    // Friendly name for whatever's currently in hand, for BuildResultLog messages.
    private string GetPlacingName() {
        if (currObjPlacing == null) return "?";
        Towers t = currObjPlacing.GetComponent<Towers>();
        if (t != null) return t.getName();
        Boon b = currObjPlacing.GetComponent<Boon>();
        if (b != null) return b.getName();
        return currObjPlacing.name;
    }

    public void PlaceBoon() {
        if (hoverTile != null) {
            // Boon.cs is a pure area-effect object - it buffs whatever towers/enemies
            // fall within its radius each tick (see Boon.updateTowersInRange/
            // updateEnemiesInRange), and never reads what's actually on the tile it
            // sits on. The old checkForTower() gate blocked placing a boon on any
            // occupied tile for no functional reason - it doesn't attach to or
            // interact with that tile's tower at all, so there's nothing to conflict
            // with. Boons also aren't registered in the tile-occupancy dictionary
            // (no SetTileOccupant call here), so they can't block a tower from being
            // built/merged on the same tile either, in either direction.
            if (shopManager.canBuyBoon(currObjPlacing) == true) {
                GameObject newTowerObj = Instantiate(currObjPlacing);
                newTowerObj.layer = LayerMask.NameToLayer("Tower");
                newTowerObj.GetComponent<SpriteRenderer>().sortingOrder = hoverTile.GetComponent<SpriteRenderer>().sortingOrder;
                newTowerObj.transform.position = hoverTile.transform.position;

                BuildResultLog.Show("Placed " + GetPlacingName() + ".", true);
                LogBuildAttempt("SUCCESS - placed Boon " + GetPlacingName());
                EndPlacement();
                shopManager.buyBoon(currObjPlacing);
            } else {
                Debug.Log("Not enough money for Boon.. \n");
                BuildResultLog.Show("Not enough Wood for " + GetPlacingName() + ".", false);
                LogBuildAttempt("BLOCKED - not enough Wood for Boon " + GetPlacingName());
                EndPlacement();
            }
        }
    }

    public void PlaceBuilding() {
        if (hoverTile != null) {
            // Grid-based occupancy check (see towerAtTile above), not a raycast.
            GameObject existingTower = GetTowerAtTile(hoverTile);

            if (existingTower == null) {
                if (shopManager.canBuyTower(currObjPlacing) == true) {
                    GameObject newTowerObj = Instantiate(currObjPlacing);
                    SetLayerRecursively(newTowerObj, LayerMask.NameToLayer("Tower"));
                    newTowerObj.GetComponent<SpriteRenderer>().sortingOrder = hoverTile.GetComponent<SpriteRenderer>().sortingOrder;
                    newTowerObj.transform.GetChild(0).GetComponent<SpriteRenderer>().sortingOrder =
                        hoverTile.GetComponent<SpriteRenderer>().sortingOrder + 1;
                    newTowerObj.transform.position = hoverTile.transform.position;

                    Counter.towers.Add(newTowerObj);
                    SetTileOccupant(hoverTile, newTowerObj);

                    Counter.towers.Sort((x, y) => x.transform.position.y.CompareTo(y.transform.position.y));

                    BuildResultLog.Show("Built " + GetPlacingName() + ".", true);
                    LogBuildAttempt("SUCCESS - built " + GetPlacingName());
                    EndPlacement();
                    shopManager.buyTower(currObjPlacing);

                    // "Make tower be selected when place down." Opens the same menu a
                    // click on the tower would (UpgradeManager.Open), so the freshly
                    // built tower reads as selected right away instead of needing an
                    // extra click.
                    if (UpgradeManager.main != null) UpgradeManager.main.Open(newTowerObj);
                } else {
                    Debug.Log("Not enough money for Tower.. \n");
                    BuildResultLog.Show("Not enough Wood for " + GetPlacingName() + ".", false);
                    LogBuildAttempt("BLOCKED - not enough Wood for " + GetPlacingName());
                    EndPlacement();
                }
            } else {
                // "Ghép Tower": a tower already occupies this tile - try to merge
                // instead of the old silent no-op.
                TryMergeTower(existingTower, hoverTile);
            }
        }
    }

    // "Ghép Tower: Ghi đè 2 Tower với nhau và tạo ra một Tower khác." One-directional:
    // the recipe lives on currObjPlacing (the tower in hand), keyed by the name of
    // the tower already on the field (see Towers.TryGetMergeResult).
    private void TryMergeTower(GameObject existingTower, GameObject tile) {
        Towers incoming = currObjPlacing.GetComponent<Towers>();
        Towers existing = existingTower.GetComponent<Towers>();

        if (incoming == null || existing == null) {
            BuildResultLog.Show("Can't merge - missing Towers component.", false);
            LogBuildAttempt("BLOCKED - missing Towers component");
            EndPlacement();
            return;
        }

        GameObject resultPrefab = incoming.TryGetMergeResult(existing.getName());

        if (resultPrefab == null) {
            Debug.Log("These two towers can't be merged.. \n");
            BuildResultLog.Show(incoming.getName() + " can't merge with " + existing.getName() + ".", false);
            LogBuildAttempt("BLOCKED - " + incoming.getName() + " has no merge recipe with " + existing.getName());
            EndPlacement();
            return;
        }

        if (shopManager.canBuyTower(currObjPlacing) == false) {
            Debug.Log("Not enough Wood to merge.. \n");
            BuildResultLog.Show("Not enough Wood to merge into " + resultPrefab.name + ".", false);
            LogBuildAttempt("BLOCKED - not enough Wood to merge into " + resultPrefab.name);
            EndPlacement();
            return;
        }

        Vector3 spot = existingTower.transform.position;
        int baseOrder = existingTower.GetComponent<SpriteRenderer>().sortingOrder;

        Counter.towers.Remove(existingTower);
        ClearOccupantTower(existingTower);
        Destroy(existingTower);

        GameObject mergedTowerObj = Instantiate(resultPrefab);
        SetLayerRecursively(mergedTowerObj, LayerMask.NameToLayer("Tower"));
        mergedTowerObj.GetComponent<SpriteRenderer>().sortingOrder = baseOrder;
        mergedTowerObj.transform.GetChild(0).GetComponent<SpriteRenderer>().sortingOrder = baseOrder + 1;
        mergedTowerObj.transform.position = spot;

        Counter.towers.Add(mergedTowerObj);
        SetTileOccupant(tile, mergedTowerObj);
        Counter.towers.Sort((x, y) => x.transform.position.y.CompareTo(y.transform.position.y));

        shopManager.buyTower(currObjPlacing); // merging spends the in-hand tower's cost, same as a normal placement

        BuildResultLog.Show("Merged " + existing.getName() + " -> " + mergedTowerObj.GetComponent<Towers>().getName() + ".", true);
        LogBuildAttempt("SUCCESS - merged " + existing.getName() + " + " + incoming.getName() + " -> " + mergedTowerObj.GetComponent<Towers>().getName());
        EndPlacement();

        // Same "select on place" behavior as a normal build - see PlaceBuilding() above.
        if (UpgradeManager.main != null) UpgradeManager.main.Open(mergedTowerObj);
    }

    public void StartPlacing(GameObject towerToBuild) {
        // Clicking a different deck slot before placing the current one (click-click
        // build) would otherwise leave the previous dummy orphaned in the scene.
        if (isPlacing) EndPlacement();

        // GetCurrentHoverTile() is a coroutine - calling it directly here (without
        // StartCoroutine) never actually ran its body, so hoverTile was never really
        // cleared/refreshed at this point. Update()'s own StartCoroutine call handles
        // that properly; here we just need hoverTile to not be pointing at whatever
        // tile was hovered during a previous placement.
        hoverTile = null;

        isPlacing = true;

        currObjPlacing = towerToBuild;

        dummyPlacement = Instantiate(currObjPlacing);
        SetDummyVisible(false); // hidden until Update() finds a real tile under the cursor

        if (dummyPlacement.GetComponent<Towers>() != null)
            Destroy(dummyPlacement.GetComponent<Towers>());


        if (dummyPlacement.GetComponent<BarrelRotation>() != null)
            Destroy(dummyPlacement.GetComponent<BarrelRotation>());


        if (dummyPlacement.GetComponent<Boon>() != null) {
            Destroy(dummyPlacement.GetComponent<Boon>());
            flag = true;
        }

        // The dummy is a straight clone of the real tower prefab, colliders and all. If
        // one of those colliders sits on the same layer the hover/tower raycasts query,
        // it can compete with the actual map tile directly underneath it for the hit -
        // since both are at the exact same position, which one wins can flip frame to
        // frame, showing up as the dummy sprite flickering. It's only a preview, so it
        // never needs to be physically detectable.
        foreach (Collider2D dummyCollider in dummyPlacement.GetComponentsInChildren<Collider2D>()) {
            dummyCollider.enabled = false;
        }
    }

    public void EndPlacement() {
        isPlacing = false;

        if (dummyPlacement != null) {
            Destroy(dummyPlacement);
        }

        hoverTile = null;
        flag = false;
    }
}