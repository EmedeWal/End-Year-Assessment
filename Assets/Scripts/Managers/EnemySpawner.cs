using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    #region Player References

    [Header("Player References")]
    public Transform playerTransform;
    public PlayerController playerController;
    public Health playerHealth;
    public Souls playerSouls;

    #endregion

    //

    #region Wave System

    [Header("Wave System")]

    public List<EnemyToSpawn> enemies = new List<EnemyToSpawn>();
    public int currentWave = 0;
    public int waveValue;
    public int waveValueModifier = 10;

    public List<GameObject> enemiesToSpawn = new List<GameObject>();

    public Transform spawnLocation;
    public float waveDuration = 60;
    private float spawnTimer;
    private float waveTimer;
    private float spawnInterval;

    private void Start()
    {
        GenerateWave();
    }

    private void Update()
    {
        SpawnEnemies();
    }

    private void GenerateWave()
    {
        // Increment currentWave
        currentWave++;

        // Generate enemies to spawn
        waveValue = currentWave * waveValueModifier;
        GenerateEnemies();

        // Calulcate the inverval at which enemies should be spawned
        spawnInterval = waveDuration / enemiesToSpawn.Count;
        waveTimer = waveDuration;
    }

    private void GenerateEnemies()
    {
        // Generate a random selection of enemies
        List<GameObject> generatedEnemies = new List<GameObject>();

        while (waveValue > 0)
        {
            int randomEnemyID = Random.Range(0, enemies.Count + 1);
            int randomEnemyCost = enemies[randomEnemyID].cost;

            Debug.Log("Random Enemy Id: " + randomEnemyID);
            Debug.Log("Chosen enemy cost: " + randomEnemyCost);

            if (waveValue > randomEnemyCost)
            {
                generatedEnemies.Add(enemies[randomEnemyID].enemyPrefab);
                waveValue -= randomEnemyCost;
            }
            else
            {
                break;
            }

            Debug.Log("The wave value is: " + waveValue);
        }

        Debug.Log("The amount of generated enemies are: " + generatedEnemies.Count);
        Debug.Log("The wave value is: " + waveValue);


        //// While the wave has money to spend
        //while (waveValue > 0)
        //{
        //    Debug.Log($"Wave Value: {waveValue}, Generated Enemies: {generatedEnemies.Count}");

        //    // Generate a random enemy
        //    int randomEnemyID = Random.Range(0, enemies.Count);
        //    int randomEnemyCost = enemies[randomEnemyID].cost;

        //    // Check if the enemy can be afforded
        //    if (waveValue > randomEnemyCost)
        //    {
        //        generatedEnemies.Add(enemies[randomEnemyID].enemyPrefab);
        //        waveValue -= randomEnemyCost;
        //    }
        //    else if (waveValue == 0)
        //    {
        //        break;
        //    }
        //}

        // Make sure the list is empty. Then, assign the chosen enemies to the list
        enemiesToSpawn.Clear();
        enemiesToSpawn = generatedEnemies;
    }

    private void SpawnEnemies()
    {
        // If the spawnTimer is less or equal then zero, try to spawn an enemy
        if (spawnTimer <= 0)
        {
            // Check if there are still enemies to spawn
            if (enemiesToSpawn.Count > 0)
            {
                // Spawn the enemy at the spawn location
                Instantiate(enemiesToSpawn[0], transform);

                // Remove them from the list and reset the spawntimer
                enemiesToSpawn.RemoveAt(0);
                spawnTimer = spawnInterval;
            }
            else if (waveTimer  <= 0)
            {
                // Generate the new wave
                GenerateWave();
            }
        }
        else
        {
            spawnTimer -= Time.deltaTime;
            waveTimer -= Time.deltaTime;
        }
    }

    #endregion

    //
}

[System.Serializable]
public class EnemyToSpawn
{
    public GameObject enemyPrefab;
    public int cost;
}
