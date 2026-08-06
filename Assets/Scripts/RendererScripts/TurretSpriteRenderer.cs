using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class TurretSpriteRenderer : SpriteLoader
{
    public static TurretSpriteRenderer main;
    public static bool activeRenderer = true;

    protected void Start()
    {
        if (main == null) main = this;
        base.LoadDictionary();
    }

    protected void FixedUpdate()
    {
        if (activeRenderer)
        {
            updateSortingLayerValue();
        }
    }

    public void UpdateSortingOrder()
    { // should only be utilized by the inspector
        updateSortingLayerValue();
    }

    // this function is more like a sorting order renderer
    // most layers are dependent on the tile location
    private void updateSortingLayerValue()
    {
        // Used to run its own independent counter (32766 downward) over Counter.towers,
        // a list ordered by placement chronology, not Y position - "same Y as previous
        // entry -> same order" only works if entries are adjacent by row, which isn't
        // guaranteed here. Worse, that counter's scale was never calibrated against
        // MapRenderer's tile-based one (which Home Tile's root sprite - the house - and
        // every regular tile use), so a tower and the tile/hall next to it could get
        // numerically unrelated sortingOrder values on the same "Objects" layer even
        // though only sortingOrder (not layer) separates them. Reading each tower's
        // order directly off the tile it's standing on reuses that already-correct,
        // shared reference frame instead of inventing a second, incompatible one.
        foreach (GameObject tower in Counter.towers)
        {
            if (tower == null) continue;

            GameObject tile = PlacementManager.GetTileOfTower(tower);
            if (tile == null) continue; // not grid-registered yet this frame

            SpriteRenderer tileRenderer = tile.GetComponent<SpriteRenderer>();
            if (tileRenderer == null) continue;

            int tileOrder = tileRenderer.sortingOrder;
            tower.GetComponent<SpriteRenderer>().sortingOrder = tileOrder;
            tower.transform.GetChild(0).GetComponent<SpriteRenderer>().sortingOrder = tileOrder + 1;
        }
    }

    public void UpdateTurretUnitSprite(Transform barrel, string spriteName, float angle)
    {
        if (string.IsNullOrEmpty(spriteName)) return;

        string direction = "_";

        if (angle > 15 && angle <= 45)
            direction += "NE"; // NE
        else if (angle > 45 && angle <= 135)
            direction += "N"; // N
        else if (angle > 135 && angle <= 165)
            direction += "NW"; // NW
        else if (angle > 165 || angle <= -165)
            direction += "W"; // W
        else if (angle > -165 && angle <= -135)
            direction += "SW"; // SW
        else if (angle > -135 && angle <= -45)
            direction += "S"; // S
        else if (angle > -45 && angle <= -15)
            direction += "SE"; // SE
        else
            direction += "E"; // E


        barrel.GetComponent<SpriteRenderer>().sprite =
                base.GetSpriteByName(spriteName + direction);
    }
}