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

public class Towers : MonoBehaviour {
    // Widened from private to protected: WoodFarmTower (a merge result built entirely
    // at runtime, with no prefab asset to hold Inspector values - see
    // WoodFarmMergeSetup.cs) needs to zero these out itself in its own Awake().
    [SerializeField] protected float range;
    [SerializeField] protected float damage;
    [SerializeField] protected float timeBtwShots;     //Time in between shots (in seconds)
    [SerializeField] protected int towerCost;       //Saves Tower Cost
    [SerializeField] protected int upgradeCost;     //Saves Upgrade Cost
    [SerializeField] protected string towerName;    //Saves Tower Name
    [SerializeField] protected BarrelRotation barrelRotation;
    protected List<BoonType> boons = new List<BoonType>();

    private float nextTimeToShoot;

    public GameObject currentTarget;
    public GameObject menu;

    public Transform barrel;
    public GameObject projectile;

    public bool aimReady { get; private set; } = false;

    public bool upgraded;

    // "Đổi mục tiêu của Tower" - which enemy this tower prioritizes.
    [SerializeField] private TargetMode targetMode = TargetMode.Nearest;

    // "Tower có Detection: Gây sát thương lên Enemy có trạng thái ẩn."
    [SerializeField] protected bool hasDetection = false;

    // --- Active Skill (AS) ---
    // "Đến nâng cấp cuối cùng, UI thay thế nút nâng cấp bằng nút AS."
    // Scaffolding only: no tower has hasActiveSkill = true by default, and
    // OnActiveSkill() below has no real effect until a subclass overrides it.
    [SerializeField] protected bool hasActiveSkill = false;
    [SerializeField] protected float activeSkillCooldown = 5f;
    [SerializeField] private ActiveSkillMode skillMode = ActiveSkillMode.Manual;
    private float activeSkillCooldownRemaining = 0f;

    // --- Tower merging ("Ghép Tower") ---
    // One-directional by construction: this recipe list lives on the tower
    // being placed (the one "in hand") and is keyed by the *existing* tower's
    // name, so A->B does not imply B->A.
    [System.Serializable]
    public class TowerMergeRecipe {
        public string withTowerName;
        public GameObject resultTower;
    }
    [SerializeField] protected List<TowerMergeRecipe> mergeRecipes = new List<TowerMergeRecipe>();

    private void Start() {
        nextTimeToShoot = Time.time;
        upgraded = false;
    }

    //Loads in the tower prefab that was selected for the upgrade manager
    private void OnMouseDown() {
        // Guard against clicks that are actually meant for PlacementManager - without
        // this, clicking an occupied tile to merge a tower onto it (both click-click and
        // drag-drop) also fires this tower's own OnMouseDown at the same time, since a
        // merge click necessarily lands exactly on the existing tower's collider. That
        // stole focus into the upgrade menu instead of letting the merge go through, and
        // is why merge clicks looked like they "just opened the tower menu" and did
        // nothing else.
        if (PlacementManager.main != null && PlacementManager.main.isPlacing) return;

        if (Input.GetMouseButtonDown(0)) {
            menu.GetComponent<UpgradeManager>().Open(this.gameObject);
            Debug.Log("Clicked on a tower");
        }
    }

    private void FixedUpdate() {
        StartCoroutine(TowerLogic());
        tickActiveSkill();
    }

    // Counts down the AS cooldown every physics tick, and auto-fires the
    // skill when it's ready if the tower is in Automatic mode.
    private void tickActiveSkill() {
        if (!hasActiveSkill) return;

        if (activeSkillCooldownRemaining > 0f)
            activeSkillCooldownRemaining -= Time.fixedDeltaTime;

        if (skillMode == ActiveSkillMode.Automatic && IsSkillReady())
            UseActiveSkill();
    }

    private IEnumerator TowerLogic() {
        StartCoroutine(updateClosestEnemy());

        if (Time.time >= nextTimeToShoot) {
            if (currentTarget != null && aimReady) {
                shoot();
                nextTimeToShoot = Time.time + timeBtwShots;
            } else if (currentTarget == null && aimReady) {
                aimReady = false;
            }
        }

        yield return null;
    }

    public void triggerAim() {
        aimReady = !aimReady;
    }

    private IEnumerator updateClosestEnemy() {
        GameObject bestEnemy = null;
        float bestDistance = Mathf.Infinity; // still used as the in-range gate for every mode
        float bestScore = 0f;
        bool haveCandidate = false;

        foreach (GameObject enemy in Counter.enemies) {
            // Hidden enemies are invisible to this tower unless it has Detection.
            Enemy enemyComp = enemy.GetComponent<Enemy>();
            if (enemyComp != null && enemyComp.IsHidden && !hasDetection) continue;

            float _distance = (transform.position - enemy.transform.position).magnitude;
            if (_distance > range) continue;

            bool isBetter;
            float score;

            switch (targetMode) {
                case TargetMode.Farthest:
                    score = _distance;
                    isBetter = !haveCandidate || score > bestScore;
                    break;
                case TargetMode.HighestHP:
                    score = enemyComp != null ? enemyComp.enemyHealth : 0f;
                    isBetter = !haveCandidate || score > bestScore;
                    break;
                case TargetMode.LowestHP:
                    score = enemyComp != null ? enemyComp.enemyHealth : 0f;
                    isBetter = !haveCandidate || score < bestScore;
                    break;
                case TargetMode.Nearest:
                default:
                    score = _distance;
                    isBetter = !haveCandidate || score < bestScore;
                    break;
            }

            if (isBetter) {
                bestScore = score;
                bestDistance = _distance;
                bestEnemy = enemy;
                haveCandidate = true;
            }
        }

        currentTarget = (haveCandidate && bestDistance <= range) ? bestEnemy : null;

        yield return null;
    }

    // "Đổi mục tiêu của Tower" - cycles Nearest -> Farthest -> HighestHP -> LowestHP -> Nearest.
    public void CycleTargetMode() {
        int next = ((int)targetMode + 1) % System.Enum.GetValues(typeof(TargetMode)).Length;
        targetMode = (TargetMode)next;
    }

    public string GetTargetModeLabel() {
        switch (targetMode) {
            case TargetMode.Nearest: return "Nearest";
            case TargetMode.Farthest: return "Farthest";
            case TargetMode.HighestHP: return "Highest HP";
            case TargetMode.LowestHP: return "Lowest HP";
            default: return targetMode.ToString();
        }
    }

    // --- Attack cooldown ("Kiểm tra cooldown") ---
    // "Hiển thị một thanh nhỏ trên đầu Tower cho thấy cooldown." nextTimeToShoot/
    // timeBtwShots were private with no way for a hover-UI to read them - these are
    // read-only accessors so TowerCooldownDisplay can draw a bar without touching
    // shoot timing itself.

    // Towers with no fire rate at all (e.g. pure Farm/Buff towers, timeBtwShots == 0)
    // have no attack cooldown to show.
    public bool HasAttackCooldown() {
        return timeBtwShots > 0f;
    }

    // 0 = just fired (fully on cooldown), 1 = ready to fire. Clamped so a tower that
    // hasn't shot yet (nextTimeToShoot == Time.time from Start()) reads as "ready"
    // instead of NaN/negative.
    public float GetCooldownReadyFraction() {
        if (!HasAttackCooldown()) return 1f;
        float remaining = nextTimeToShoot - Time.time;
        if (remaining <= 0f) return 1f;
        return 1f - Mathf.Clamp01(remaining / timeBtwShots);
    }

    // --- Active Skill (AS) ---

    public bool HasActiveSkill() {
        return hasActiveSkill;
    }

    public bool IsSkillReady() {
        return hasActiveSkill && activeSkillCooldownRemaining <= 0f;
    }

    public void UseActiveSkill() {
        if (!IsSkillReady()) return;

        OnActiveSkill();
        activeSkillCooldownRemaining = activeSkillCooldown;
    }

    public void ToggleSkillMode() {
        skillMode = (skillMode == ActiveSkillMode.Manual) ? ActiveSkillMode.Automatic : ActiveSkillMode.Manual;
    }

    public ActiveSkillMode GetSkillMode() {
        return skillMode;
    }

    // Hook for subclasses (e.g. ElementalTowers) to give the skill a real effect.
    // Intentionally a no-op here - wiring only, per the audit log.
    protected virtual void OnActiveSkill() {
        Debug.Log(getName() + " used its Active Skill (no effect implemented yet).");
    }

    // --- Tower merging ("Ghép Tower") ---

    // Looks up this tower's (the one being placed) merge recipe keyed by the
    // name of the tower already on the field. Returns the result prefab, or
    // null if no recipe matches (one-directional: A->B does not imply B->A).
    public GameObject TryGetMergeResult(string otherTowerName) {
        if (otherTowerName == null) return null;
        string wanted = otherTowerName.Trim();

        foreach (TowerMergeRecipe recipe in mergeRecipes) {
            // Trimmed comparison: a stray trailing space on a towerName field in the
            // Inspector used to make a recipe silently never match - one of the causes
            // of merging feeling inconsistent ("works for this pair, not that one").
            if (recipe.withTowerName != null && recipe.withTowerName.Trim() == wanted)
                return recipe.resultTower;
        }
        return null;
    }

    public bool HasAnyMergeRecipes() {
        return mergeRecipes.Count > 0;
    }

    // Lets code (not just the Inspector) register a merge recipe - used by
    // WoodFarmMergeSetup.cs to wire the Archer+Ballista -> Wood Farm Tower merge onto
    // the two existing tower prefab assets at runtime, since mergeRecipes has no other
    // public mutator and that merge result has no hand-authored prefab of its own.
    public void AddMergeRecipe(string withTowerName, GameObject resultTower) {
        mergeRecipes.Add(new TowerMergeRecipe { withTowerName = withTowerName, resultTower = resultTower });
    }

    protected virtual void shoot() {
        GameObject newBullet = Instantiate(projectile, barrel.position, barrelRotation.pivot);
        Bullet currentBullet = newBullet.GetComponent<Bullet>();
        currentBullet.Damage = getDamage();
        currentBullet.Target = currentTarget;
    }

    // Named so UpgradeManager's "next upgrade" preview can use the exact same
    // bounds upgrade() rolls against, instead of a second hardcoded copy that
    // could quietly drift out of sync with the real roll.
    private const float UpgradeMultiplierMin = 1.1f;
    private const float UpgradeMultiplierMax = 2f;

    public virtual void upgrade() {
        upgraded = true;
        damage = damage * UnityEngine.Random.Range(UpgradeMultiplierMin, UpgradeMultiplierMax);
        range = range * UnityEngine.Random.Range(UpgradeMultiplierMin, UpgradeMultiplierMax);
    }

    public bool canUpgrade() {
        return !upgraded;
    }

    // "Upgrade need to show next upgrade." Damage/range gains are randomized per
    // roll (see upgrade() above), so there's no single deterministic "next" value
    // to show - this reports the actual range the roll can land in instead of
    // fabricating a fixed number the real upgrade might not match.
    public virtual string GetUpgradePreviewText() {
        float dmgLow = damage * UpgradeMultiplierMin;
        float dmgHigh = damage * UpgradeMultiplierMax;
        float rangeLow = range * UpgradeMultiplierMin;
        float rangeHigh = range * UpgradeMultiplierMax;

        return "Damage: " + damage.ToString("0.#") + " -> " + dmgLow.ToString("0.#") + "~" + dmgHigh.ToString("0.#") + "\n" +
               "Range: " + range.ToString("0.#") + " -> " + rangeLow.ToString("0.#") + "~" + rangeHigh.ToString("0.#");
    }

    // "Tower show current stats after full upgrade." Once canUpgrade() is false,
    // GetUpgradePreviewText() has nothing left to preview - this reports the tower's
    // actual final stats instead of leaving that space blank.
    public virtual string GetCurrentStatsText() {
        return "Damage: " + damage.ToString("0.#") + "\n" +
               "Range: " + range.ToString("0.#");
    }

    public void addBoon(BoonType boon) {
        boons.Add(boon);
        switch (boon) {
            case BoonType.Power:
                damage = damage * 2;
                break;
            case BoonType.Swiftness:
                timeBtwShots = timeBtwShots / 2;
                break;
            case BoonType.Farsight:
                range = range * 2;
                break;
        }
    }

    public void removeBoon(BoonType boon) {
        switch (boon) {
            case BoonType.Power:
                damage = damage / 2;
                break;
            case BoonType.Swiftness:
                timeBtwShots = timeBtwShots * 2;
                break;
            case BoonType.Farsight:
                range = range / 2;
                break;
        }
        boons.Remove(boon);
    }

    public float getDamage() { return damage; }

    public int getCost() { return towerCost; }

    public string getName() { return towerName; }

    // Used by TowerRangeIndicator to draw the hover range circle without making range
    // public (which would let other scripts accidentally modify it).
    public float GetRange() { return range; }

    public int getUpgradeCost() { return upgradeCost; }
}