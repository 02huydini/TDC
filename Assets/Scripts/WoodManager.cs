/*
    Most code in this file was written out by Nathan Granger based on the free tutorial 
    videos posted by youtube user ZeveonHD, found at 
    https://www.youtube.com/playlist?list=PL5AKnriDHZs5a8De2wK_qqrwBUqjZo0hN. Many
    function and variable names may have been changed and some parts of the code may
    have been modified to fit our game scheme, these sections will be marked with 
    comments.

    Renamed from MoneyManager -> WoodManager to match toDo2.txt's resource name ("Wood").
    Fields keep [FormerlySerializedAs] pointing at their old names so existing prefab
    Inspector values (startMoney: 1000, the wired-up Text references, etc.) carry over
    instead of resetting to default the next time the prefab is opened/saved in Unity.
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;

public class WoodManager : MonoBehaviour
{
    [FormerlySerializedAs("playerMoneyTxt")]
    [SerializeField] private Text playerWoodTxt;
    [SerializeField] private Text playerScoreTxt;   //For player score on GO screen

    public static WoodManager main;

    private static int currPlayerWood;

    [FormerlySerializedAs("startMoney")]
    public int startWood;

    private void Start()
    {
        if (main == null) main = this;
        currPlayerWood = startWood;

        // Refresh UI on Start (ported from the uploaded MoneyManager.cs) - without
        // this the Text keeps showing its prefab placeholder value until the first
        // AddWood/RemoveWood call.
        AddWood(0);
    }

    public int GetCurrWood()
    {
        return currPlayerWood;
    }

    //Modified to change and print out the players wood when added to
    //Modified to show player score on game over screen
    public void AddWood(int amount)
    {
        currPlayerWood += amount;
        if (playerWoodTxt != null) playerWoodTxt.text = $"Wood: {currPlayerWood}";
        if (playerScoreTxt != null) playerScoreTxt.text = $"Score: {currPlayerWood}";
    }

    //Modified to change and print out the players wood when taken away
    //Modified to show player score on game over screen
    public void RemoveWood(int amount)
    {
        currPlayerWood -= amount;
        if (playerWoodTxt != null) playerWoodTxt.text = $"Wood: {currPlayerWood}";
        if (playerScoreTxt != null) playerScoreTxt.text = $"Score: {currPlayerWood}";
    }
}
