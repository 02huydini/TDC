/*
    Most code in this file was written out by Nathan Granger based on the free tutorial 
    videos posted by youtube user ZeveonHD, found at 
    https://www.youtube.com/playlist?list=PL5AKnriDHZs5a8De2wK_qqrwBUqjZo0hN. Many
    function and variable names may have been changed and some parts of the code may
    have been modified to fit our game scheme, these sections will be marked with 
    comments.

    Money type changed to Wood (WoodManager) for tower buy/sell/upgrade.
    Boons now cost Ectos (EctoManager) instead of Wood.

    Inspector re-wiring required after this update:
      - woodManager  -> drag WoodManager component here  (was moneyManager:MoneyManager)
      - ectoManager  -> drag EctoManager component here  (new field)
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShopManager : MonoBehaviour
{
    public static ShopManager main;

    // Re-wire to WoodManager in Inspector (was MoneyManager moneyManager).
    public WoodManager woodManager;

    // Wire to EctoManager in Inspector - boons now cost Ectos, not Wood.
    public EctoManager ectoManager;

    private void Start()
    {
        if (main == null) main = this;
    }

    public int GetTowerCost(GameObject towerPrefab)
    {
        return towerPrefab.GetComponent<Towers>().getCost();
    }

    public int GetUpgradeCost(GameObject towerPrefab)
    {
        return towerPrefab.GetComponent<Towers>().getUpgradeCost();
    }

    // Boon cost is now an Ecto cost.
    public int GetBoonCost(GameObject boonPrefab)
    {
        return boonPrefab.GetComponent<Boon>().getCost();
    }

    public void buyTower(GameObject towerPrefab)
    {
        woodManager.RemoveWood(GetTowerCost(towerPrefab));
    }

    public void sellTower(GameObject towerPrefab)
    {
        woodManager.AddWood(GetTowerCost(towerPrefab));
    }

    public void upgradeTower(GameObject towerPrefab)
    {
        woodManager.RemoveWood(GetUpgradeCost(towerPrefab));
    }

    public bool canUpgradeTower(GameObject towerPrefab)
    {
        return woodManager.GetCurrWood() >= GetUpgradeCost(towerPrefab);
    }

    // buyBoon / canBuyBoon now check Ectos, not Wood.
    public void buyBoon(GameObject boonPrefab)
    {
        ectoManager.RemoveEctos(GetBoonCost(boonPrefab));
    }

    public bool canBuyTower(GameObject towerPrefab)
    {
        return woodManager.GetCurrWood() >= GetTowerCost(towerPrefab);
    }

    public bool canBuyBoon(GameObject boonPrefab)
    {
        return ectoManager.GetCurrEctos() >= GetBoonCost(boonPrefab);
    }
}
