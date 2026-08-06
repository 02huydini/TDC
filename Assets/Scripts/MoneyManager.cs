/*
    Most code in this file was written out by Nathan Granger based on the free tutorial 
    videos posted by youtube user ZeveonHD, found at 
    https://www.youtube.com/playlist?list=PL5AKnriDHZs5a8De2wK_qqrwBUqjZo0hN. Many
    function and variable names may have been changed and some parts of the code may
    have been modified to fit our game scheme, these sections will be marked with 
    comments.

    Class/field/method names kept as-is (Money-prefixed) on purpose - only the display
    text changed to "Wood". Renaming the actual symbols broke cross-prefab scene wiring
    (ShopManager.moneyManager, this component's own startMoney/playerMoneyTxt) since
    those links are stored in Scene.unity keyed by the old field names. Not worth
    repeating that breakage just to rename internals the player never sees.
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MoneyManager : MonoBehaviour
{
    [SerializeField] private Text playerMoneyTxt;
    [SerializeField] private Text playerScoreTxt;   //For player score on GO screen

    public static MoneyManager main;

    private static int currPlayerMoney;
    public int startMoney;

    private void Start()
    {
        if (main == null) main = this;
        currPlayerMoney = startMoney;
    }

    public int GetCurrMoney()
    {
        return currPlayerMoney;
    }

    //Modified to change and print out the players wood when added to
    //Modified to show player score on game over screen
    public void addMoney(int amount)
    {
        currPlayerMoney += amount;
        playerMoneyTxt.text = $"Wood: {currPlayerMoney}";
        playerScoreTxt.text = $"Score: {currPlayerMoney}";
    }

    //Modified to change and print out the players wood when taken away
    //Modified to show player score on game over screen
    public void removeMoney(int amount)
    {
        currPlayerMoney -= amount;
        playerMoneyTxt.text = $"Wood: {currPlayerMoney}";
        playerScoreTxt.text = $"Score: {currPlayerMoney}";
    }
}
