//Class made by Alex Martinez
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UpgradeManager : MonoBehaviour {
    public static UpgradeManager main;
    public ShopManager shopManager;

    public GameObject menuUi;
    public static GameObject dummyUi;

    public GameObject upgradeButton;
    public static GameObject dummyUpgradeButton;

    // "Bán Tower" and "Active Tower Skill (AS)" now share one button: it reads
    // Sell until the tower is maxed out AND actually has a skill, at which point
    // it switches to Skill. Assign the same GameObject you'd have used for either.
    public GameObject sellSkillButton;
    [Tooltip("Optional - the Text label on sellSkillButton. Left alone if unassigned.")]
    public Text sellSkillLabel;

    // "Đổi mục tiêu của Tower" - shown/hidden the same way upgradeButton is.
    public GameObject cycleTargetButton;
    [Tooltip("Optional - the Text label on cycleTargetButton, updated to the current mode.")]
    public Text cycleTargetLabel;

    [Tooltip("Optional - shows the Wood cost of upgrading the current tower.")]
    public Text upgradeCostText;
    [Tooltip("Optional - shows the Wood the player gets back for selling the current tower.")]
    public Text sellValueText;
    [Tooltip("Optional - shows the damage/range range the next upgrade roll can land in.")]
    public Text upgradePreviewText;

    private static GameObject currentTower;

    // Read-only access for UpgradeInfoDisplay.cs (self-installing, no prefab wiring).
    public static GameObject GetCurrentTower() { return currentTower; }

    // Start is called before the first frame update
    private void Start() {
        if (main == null) main = this;
        currentTower = null;
        Debug.Log(menuUi);
        menuUi.SetActive(false);
        upgradeButton.SetActive(false);
        if (sellSkillButton != null) sellSkillButton.SetActive(false);
        if (cycleTargetButton != null) cycleTargetButton.SetActive(false);
        dummyUpgradeButton = upgradeButton;
        dummyUi = menuUi;//To make sure i dont lose track of the Tower Menu UI
    }

    //This trys to open the UI panel for upgrading and selling towers
    public void Open(GameObject T) {
        currentTower = T;
        Debug.Log("Tower " + currentTower.GetComponent<Towers>().getName() + " loaded");
        Debug.Log(dummyUpgradeButton);
        dummyUi.SetActive(true);//makes UI appear
        RefreshButtons();
    }

    public void Upgrade() {
        if ((shopManager.canUpgradeTower(currentTower)) && (currentTower.GetComponent<Towers>().canUpgrade())) {
            shopManager.upgradeTower(currentTower);
            currentTower.GetComponent<Towers>().upgrade();
            Debug.Log("Upgraded tower");
        }
        this.Close();
    }

    //This sells a tower and gives money back to the player
    public void Sell() {
        Debug.Log(currentTower.GetComponent<Towers>().getName());
        shopManager.sellTower(currentTower);
        Counter.towers.Remove(currentTower);
        PlacementManager.ClearOccupantTower(currentTower); // frees the tile's grid slot
        Destroy(currentTower);
        this.Close();
        Debug.Log("Sold tower");//makes UI disappear
    }

    // Single entry point for the combined button: Skill once the tower is maxed
    // out and actually has a skill, Sell otherwise. Wire this - not Sell() or
    // UseActiveSkill() directly - to sellSkillButton's OnClick().
    public void SellOrSkill() {
        if (currentTower == null) return;
        Towers tower = currentTower.GetComponent<Towers>();
        if (!tower.canUpgrade() && tower.HasActiveSkill()) {
            UseActiveSkill();
        } else {
            Sell();
        }
    }

    // "Đổi mục tiêu của Tower" - wire to a target-mode button in the tower stats UI.
    public void CycleTarget() {
        if (currentTower == null) return;
        currentTower.GetComponent<Towers>().CycleTargetMode();
        RefreshButtons();
    }

    public string GetCurrentTargetLabel() {
        return currentTower != null ? currentTower.GetComponent<Towers>().GetTargetModeLabel() : "";
    }

    // "Active Tower Skill (AS)" - wire to the AS button (only relevant once the
    // tower has hasActiveSkill == true, i.e. after its final upgrade tier).
    public void UseActiveSkill() {
        if (currentTower == null) return;
        currentTower.GetComponent<Towers>().UseActiveSkill();
    }

    // "Switch AS mode" - toggles the tower's skill between Manual and Automatic.
    public void ToggleSkillMode() {
        if (currentTower == null) return;
        currentTower.GetComponent<Towers>().ToggleSkillMode();
    }

    public void Close() {
        dummyUpgradeButton.SetActive(false);
        if (sellSkillButton != null) sellSkillButton.SetActive(false);
        if (cycleTargetButton != null) cycleTargetButton.SetActive(false);
        dummyUi.SetActive(false);
        currentTower = null;
    }

    // Shows/hides + labels every button in the menu based on the current tower's
    // state. Called on Open(), on CycleTarget(), and every FixedUpdate tick.
    private void RefreshButtons() {
        if (currentTower == null) return;
        Towers tower = currentTower.GetComponent<Towers>();

        bool maxed = !tower.canUpgrade();
        bool hasSkill = tower.HasActiveSkill();

        dummyUpgradeButton.SetActive(!maxed);

        // "Upgrade button need to be turn off if player doesn't have enough Wood."
        // SetActive(!maxed) above only handles "nothing left to upgrade" - this
        // additionally greys the button out (without hiding it) when the tower
        // *can* still be upgraded but the player can't currently afford it.
        if (!maxed && shopManager != null) {
            Button upgradeBtnComp = dummyUpgradeButton.GetComponent<Button>();
            if (upgradeBtnComp != null) upgradeBtnComp.interactable = shopManager.canUpgradeTower(currentTower);
        }

        if (!maxed) {
            if (upgradeCostText != null) upgradeCostText.text = "Cost: " + shopManager.GetUpgradeCost(currentTower) + " Wood";
            if (upgradePreviewText != null) upgradePreviewText.text = tower.GetUpgradePreviewText();
        } else {
            if (upgradeCostText != null) upgradeCostText.text = "Maxed";
            // "Tower show current stats after full upgrade" - nothing left to preview,
            // so show the tower's actual final stats in the same space instead.
            if (upgradePreviewText != null) upgradePreviewText.text = tower.GetCurrentStatsText();
        }

        // "how much will get for selling" - sellTower() pays back GetTowerCost()
        // (the tower's original purchase price), not the upgrade cost.
        if (sellValueText != null) sellValueText.text = "Sell: " + shopManager.GetTowerCost(currentTower) + " Wood";

        if (sellSkillButton != null) {
            sellSkillButton.SetActive(true);
            if (sellSkillLabel != null) sellSkillLabel.text = (maxed && hasSkill) ? "Skill" : "Sell";
        }

        if (cycleTargetButton != null) {
            cycleTargetButton.SetActive(true);
            if (cycleTargetLabel != null) cycleTargetLabel.text = "Target: " + tower.GetTargetModeLabel();
        }
    }

    // Update is called once per frame
    private void Update() {
        if (currentTower == null || dummyUi == null || !dummyUi.activeSelf) return;

        // toDo2.txt's P2 upgrade-menu keybinds ("Nâng cấp Tower: [E]", "Bán Tower: [Q]"),
        // applied to the existing mouse-selected tower menu instead of a P2 cursor/grid -
        // just a shortcut for the same Upgrade()/Sell() the buttons already call.
        // Extended with the doc's remaining shortcuts for this menu:
        // "Đổi mục tiêu: [R]", "Active Tower Skill (AS): [F]", "Switch AS mode: [Z]".
        if (Input.GetKeyDown(KeyCode.E)) Upgrade();
        if (Input.GetKeyDown(KeyCode.Q)) SellOrSkill();
        if (Input.GetKeyDown(KeyCode.R)) CycleTarget();
        if (Input.GetKeyDown(KeyCode.F)) UseActiveSkill();
        if (Input.GetKeyDown(KeyCode.Z)) ToggleSkillMode();

        // Click outside the tower (and outside this menu's own UI) closes it.
        // Gated behind IsPointerOverGameObject() so clicking the menu's own
        // buttons doesn't immediately close the menu out from under the click.
        if (Input.GetMouseButtonDown(0)) {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
            GameObject clicked = PlacementManager.main != null ? PlacementManager.main.GetTowerUnderCursor() : null;
            if (clicked == null) Close();
        }
    }

    private void FixedUpdate() {
        if (currentTower != null) {
            RefreshButtons();
        }
        if (Input.GetMouseButtonDown(1)) {
            this.Close();
            Debug.Log("Unclicked a tower");
        }
    }
}
