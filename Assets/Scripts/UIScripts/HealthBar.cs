/*
     Written by Nathan Granger to handle the updating of the health/lives bar.
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    [SerializeField] private Image health;
    [SerializeField] private Text healthBarTxt;

    // "Nếu HP thấp hơn 30% thì Main Hall sẽ bốc cháy" - toDo2.txt's fire VFX
    // trigger. Assign the Main Hall's fire sprite/particle GameObject here;
    // it's toggled on/off as lives crosses lowHpThreshold, no polling elsewhere.
    [SerializeField] private GameObject mainHallFireVFX;
    [SerializeField] private float lowHpThreshold = 2f;

    public static float lives;

    // Dev-console "burnhall" test hook (see MainHallBurnTest.cs / DevConsole.cs).
    // Forces the fire VFX on independent of the real lives-based trigger below, so
    // QA can preview the effect without draining lives to trigger it for real.
    private static bool forceBurnOverride = false;

    // What DevConsole reports back after a burnhall command - mirrors whatever
    // shouldBurn last resolved to, whether from the real threshold or the override.
    public static bool IsBurning { get; private set; }

    private void Start()
    {
        lives = 5f;
        if (mainHallFireVFX != null) mainHallFireVFX.SetActive(false);
    }

    private void Update()
    {
        updatePlayerHealth(lives);

        bool shouldBurn = forceBurnOverride || (lives > 0f && lives <= lowHpThreshold);
        IsBurning = shouldBurn;

        if (mainHallFireVFX != null) {
            if (mainHallFireVFX.activeSelf != shouldBurn) mainHallFireVFX.SetActive(shouldBurn);
        }
    }

    // --- Dev-console test hook -----------------------------------------------

    public static void SetForceBurn(bool on) {
        forceBurnOverride = on;
    }

    public static bool GetForceBurn() {
        return forceBurnOverride;
    }

    public void updatePlayerHealth(float livesLeft, float max = 5)
    {
        healthBarTxt.text = "Lives: ";
        health.fillAmount = livesLeft / max;
        healthBarTxt.text += livesLeft + "/" + max;
    }
}
