/*
    Wood Farm Tower - runtime-only merge result (Archer Tower + Ballista Tower ->
    Wood Farm Tower). See WoodFarmMergeSetup.cs for how it's wired into both
    prefabs' merge recipes; this class only defines what the tower actually does
    once placed.

    No attack: range/damage/timeBtwShots are all forced to 0 by
    ConfigureAsMergeResult() below (called once, right after this component is
    added - see WoodFarmMergeSetup.BuildTemplate()), so
    Towers.updateClosestEnemy() never finds anything in range and TowerLogic()
    never calls shoot(). shoot() is also overridden as a no-op as a second,
    explicit guard against that ever firing.

    Instead it earns Wood at the end of every wave. RoundController exposes no
    "wave complete" event, only a public round int that OnRoundComplete()
    increments right after paying the player's own bonus - watching for that to
    change is the same "read the existing manager from outside" approach this
    project already uses elsewhere (PathHighlighter/MapGenerator,
    EnemyHoverInfo/Counter) rather than adding a new hook into RoundController.cs.

    "as well display how much they make on that tower" - GetCurrentStatsText()/
    GetUpgradePreviewText() overrides mean UpgradeManager's existing stat panel
    (already wired up, no new UI needed) reads "Wood: +N / wave" instead of
    Damage/Range for this tower. Upgrading a Wood Farm tower increases its payout
    using the same random roll Towers.upgrade() applies to damage/range.
*/
using UnityEngine;

public class WoodFarmTower : Towers {
    private int woodPerWave = 10;
    private int lastSeenRound = -1;

    // Mirrors Towers.upgrade()'s own private consts (not accessible to a
    // subclass) - same 1.1x-2x roll, applied to woodPerWave instead of
    // damage/range.
    private const float UpgradeMultiplierMin = 1.1f;
    private const float UpgradeMultiplierMax = 2f;

    // Called once by WoodFarmMergeSetup right after AddComponent<WoodFarmTower>(),
    // before this template is ever Instantiate()'d. Explicitly zeroes the combat
    // stats this tower has no prefab asset to source them from (they'd already be
    // 0 by C#'s float-default either way, but this makes the "no combat" intent
    // an explicit, readable statement rather than relying on that default).
    public void ConfigureAsMergeResult(int startingWoodPerWave) {
        woodPerWave = startingWoodPerWave;
        range = 0f;
        damage = 0f;
        timeBtwShots = 0f;
        towerName = "Wood Farm Tower";
    }

    protected override void shoot() {
        // Intentionally does nothing - Wood Farm towers never attack.
    }

    private void Update() {
        if (RoundController.main == null) return;

        if (lastSeenRound < 0) {
            lastSeenRound = RoundController.main.round; // baseline on spawn - no payout for the round already in progress
            return;
        }

        if (RoundController.main.round != lastSeenRound) {
            lastSeenRound = RoundController.main.round;
            Payout();
        }
    }

    private void Payout() {
        if (WoodManager.main != null) WoodManager.main.AddWood(woodPerWave);
        // Reuses the existing floating-number pool (DamageNumbers.cs) rather than
        // building a third one - a Wood payout reads fine as a "hit" of its own
        // kind of reward, right over the tower that earned it.
        DamageNumbers.Show(transform.position, woodPerWave);
    }

    public override void upgrade() {
        upgraded = true;
        woodPerWave = Mathf.RoundToInt(woodPerWave * Random.Range(UpgradeMultiplierMin, UpgradeMultiplierMax));
    }

    public override string GetUpgradePreviewText() {
        int low = Mathf.RoundToInt(woodPerWave * UpgradeMultiplierMin);
        int high = Mathf.RoundToInt(woodPerWave * UpgradeMultiplierMax);
        return "Wood: +" + woodPerWave + " / wave -> +" + low + "~+" + high + " / wave";
    }

    public override string GetCurrentStatsText() {
        return "Wood: +" + woodPerWave + " / wave";
    }
}
