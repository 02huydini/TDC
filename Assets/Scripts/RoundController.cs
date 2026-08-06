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

public class RoundController : MonoBehaviour
{
    public static RoundController main;

    [SerializeField]
    private GameObject[] enemyPrefabs;
    [SerializeField]
    private GameObject bossPrefab;

    [Tooltip("If true, the first wave will not start automatically - something else " +
             "(DeckSelectionUI.BeginMatch) must call BeginMatch() once the player is ready. " +
             "Leave false to keep existing behavior (e.g. MapGenPlaytesting).")]
    public bool waitForMatchStart = false;

    public float timeBtwWaves;
    public float timeBeforeRoundStarts;
    public float timeVar;

    public bool isRoundGoing;
    public bool isIntermission;
    public bool isStartOfRound;

    public int round;

    // Wave-complete bonus, adapted from the uploaded RoundController.cs's
    // OnRoundComplete() concept - scaled to this project's existing "round" field
    // and its actual currencies (WoodManager/EctoManager), not the MoneyManager/
    // "currentRound" names that upload used.
    [Header("Wave Reward Settings")]
    [SerializeField] private int baseRoundReward = 50;
    [SerializeField] private int rewardPerRound = 20;
    [SerializeField] private int ectoRewardPerRound = 100;

    private const byte minionValue = 1;
    private const byte specialValue = 3;
    private const byte tankValue = 5;
    private const byte spawnerValue = 7;
    private int spawnerCount = 0;

    private void Start()
    {
        if (main == null) main = this;
        isRoundGoing = false;
        isIntermission = false;

        if (waitForMatchStart)
        {
            // Wait for DeckSelectionUI (or anything else) to call BeginMatch() before
            // the first wave timer starts counting down.
            isStartOfRound = false;
        }
        else
        {
            isStartOfRound = true;
            timeVar = 0f + timeBeforeRoundStarts;
        }

        Debug.Log($"RoundController reports timescale as: {Time.timeScale}");
        if (!waitForMatchStart && Time.timeScale == 0) TimeHandler.StartGameTime();

        round = 1;
    }

    private void FixedUpdate()
    {
        if (isStartOfRound)
        {
            if (Time.time >= timeVar)
            {
                isStartOfRound = false;
                isRoundGoing = true;

                spawnEnemies();
                return;
            }
        }
        else if (isIntermission)
        {
            if (Time.time >= timeVar)
            {
                isIntermission = false;
                isRoundGoing = true;

                MapGenerator.main.randomExpand(); // this needs to be replaced by buttons
                spawnEnemies();
            }
        }
        else if (isRoundGoing)
        {
            if (!(Counter.enemies.Count > 0))
            {
                OnRoundComplete();
            }
        }
    }

    // Adapted from the uploaded RoundController.cs's OnRoundComplete(): awards the
    // wave-complete Wood/Ecto bonus, advances round state. Called from two places -
    // Enemy.cs/SpecialEnemy.cs's enemyDead() (event-driven, matching the uploaded
    // script's own architecture: the last enemy dying calls this directly) AND this
    // class's own FixedUpdate() poll above (kept as a fallback in case something
    // ever clears Counter.enemies without going through enemyDead(), e.g. a scene
    // reset). The isRoundGoing guard makes both callers safe together: whichever
    // fires first does the work and flips isRoundGoing false; the other becomes a
    // no-op instead of double-awarding the bonus.
    public void OnRoundComplete()
    {
        if (!isRoundGoing) return;

        isIntermission = true;
        isRoundGoing = false;

        AwardRoundCompleteBonus();

        timeVar = Time.time + timeBtwWaves;
        round += 1;
    }

    // Wood scales with the round just finished (round 1 -> baseRoundReward, each
    // round after adds rewardPerRound); Ectos are a flat bonus per wave. Called
    // exactly once per round from OnRoundComplete() above, regardless of which of
    // its two callers actually triggered it.
    private void AwardRoundCompleteBonus()
    {
        int woodReward = baseRoundReward + ((round - 1) * rewardPerRound);

        if (WoodManager.main != null) WoodManager.main.AddWood(woodReward);
        if (EctoManager.main != null) EctoManager.main.AddEctos(ectoRewardPerRound);

        Debug.Log($"RoundController: round {round} complete - awarded {woodReward} Wood, {ectoRewardPerRound} Ectos.");
    }

    // Called by DeckSelectionUI once the player has locked in their tower deck.
    // No-op if waitForMatchStart was false (round already started itself in Start()).
    public void BeginMatch()
    {
        if (!waitForMatchStart || !isStartOfRound && (isRoundGoing || isIntermission)) return;

        isStartOfRound = true;
        timeVar = Time.time + timeBeforeRoundStarts;
    }

    private List<GameObject> getEnemySpawnOrder()
    {
        List<GameObject> enemies = new List<GameObject>();
        int points = round;
        byte minionCount = 0;

        if (points < 5)
        {
            for (int i = 0; i < points; i++)
            {
                enemies.Add(enemyPrefabs[0]);
            }
        }
        else
        {
            while (points > 0)
            {
                if (points >= (spawnerValue + (spawnerCount * spawnerValue)) && minionCount >= (3 + (byte)(round / 5)))
                {
                    GameObject enemy = enemyPrefabs[1];
                    enemies.Add(enemy);
                    points -= spawnerValue;
                    spawnerCount++;
                }
                else if (points >= tankValue && minionCount >= (3 + (byte)(round / 5)))
                {   // should be tank type
                    GameObject enemy = enemyPrefabs[2];
                    enemies.Add(enemy);
                    points -= tankValue;
                }
                else if (points >= specialValue && minionCount >= (3 + (byte)(round / 5)))
                {   // should be spawner type
                    GameObject enemy = enemyPrefabs[UnityEngine.Random.Range(3, enemyPrefabs.Length)];
                    enemies.Add(enemy);
                    points -= specialValue;
                }
                else if (points >= minionValue)
                {   // should be easiest enemy to beat
                    GameObject enemy = enemyPrefabs[0];
                    enemies.Add(enemy);
                    points -= minionValue;
                    minionCount++;
                }
            }
        }

        return enemies;
    }

    private void spawnEnemies()
    {
        StartCoroutine("ISpawnEnemies");
    }

    private IEnumerator ISpawnEnemies()
    {
        List<GameObject> enemies = getEnemySpawnOrder();

        for (int i = 0; i < enemies.Count; i++)
        {
            GameObject selectedEnemy = enemies[i];

            for (int j = 0; j < MapGenerator.spawnTiles.Count; j++)
            {
                GameObject newEnemy = Instantiate(selectedEnemy,
                    MapGenerator.spawnTiles[j].transform.position, Quaternion.identity);
                Enemy script = newEnemy.GetComponent<Enemy>();
                script.levelUpMaxHealth(round / 5);
                script.initializeEnemy(MapGenerator.spawnTiles[j], j);
            }
            yield return new WaitForSeconds(1f);
        }

        if (round % 10 == 0)
        {
            // time to spawn boss prefab
            GameObject boss = Instantiate(bossPrefab,
                MapGenerator.spawnTiles[0].transform.position, Quaternion.identity);
            Enemy bossEnemy = boss.GetComponent<Enemy>();
            bossEnemy.levelUpMaxHealth(round / 10);
            bossEnemy.initializeEnemy(MapGenerator.spawnTiles[0], 0);
        }
    }
}
