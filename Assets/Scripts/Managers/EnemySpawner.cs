using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.AI;

public class EnemySpawner : MonoBehaviour
{
    #region Player References

    [Header("Player References")]
    public PlayerController playerController;
    public PlayerResources playerResources;
    public PlayerMovement playerMovement;
    public Transform playerTransform;

    #endregion

    //

    #region Wave System

    [Header("Setup")]
    [SerializeField] private bool active;
    [SerializeField] private List<Enemy> enemies = new List<Enemy>();

    [Header("References")]
    [SerializeField] private TextMeshProUGUI waveText;

    [Header("Spawn Location")]
    [SerializeField] private float minDistanceFromPlayer;
    [SerializeField] private float spawnRadius;
    private bool canSpawn;

    [Header("Modifiers")]
    [SerializeField] private float performanceDelay;
    [SerializeField] private int waveValueBase;
    [SerializeField] private int waveValueModifier;
    [SerializeField] private float waveDuration;
    [SerializeField] private float waveDurationIncrement;
    private int waveValue;

    private List<GameObject> enemiesToSpawn = new List<GameObject>();
    private float waveTimer;
    private float spawnTimer;
    private float spawnInterval;

    // Keep track of relevant information
    [HideInInspector] public int enemiesDead;
    private int enemiesSpawned;
    private int currentWave = 0;

    private void Start()
    {
        if (active) GenerateWave();
    }

    private void Update()
    {
        if (active && canSpawn) SpawnEnemies();
    }

    private void GenerateWave()
    {
        // Make sure enemies do not spawned until they are generated
        canSpawn = false;

        // Reset variables
        enemiesSpawned = 0;
        enemiesDead = 0;

        // Increment currentWave
        currentWave++;
        waveDuration += waveDurationIncrement;

        // Display the current wave on the UI
        waveText.text = "Wave: " + currentWave.ToString();

        // Generate enemies to spawn
        waveValue = (currentWave * waveValueModifier) + waveValueBase;
        StartCoroutine(GenerateEnemies());
    }

    private IEnumerator GenerateEnemies()
    {
        // Generate a random selection of enemies
        List<GameObject> generatedEnemies = new List<GameObject>();

        while (waveValue > 0)
        {
            int randomEnemyID = Random.Range(0, enemies.Count);
            int randomEnemyCost = enemies[randomEnemyID].cost;

            // Generate random enemies till wave value is exhausted. Early waves cannot contain strong enemies
            if ((waveValue >= randomEnemyCost) && (randomEnemyCost <= (currentWave * 2)))
            {
                // Add the chosen enemy to the list
                generatedEnemies.Add(enemies[randomEnemyID].enemyPrefab);
                waveValue -= randomEnemyCost;
            }
            else if (waveValue <= 0)
            {
                // No more enemies can be afforded
                break;
            }

            // Make sure Unity does not instantly explode
            yield return new WaitForSeconds(performanceDelay);
        }

        // Make sure the list is empty. Then, assign the chosen enemies to the list
        enemiesToSpawn.Clear();
        enemiesToSpawn = generatedEnemies;

        // Calulcate the inverval at which enemies should be spawned and start spawning
        spawnInterval = waveDuration / enemiesToSpawn.Count;
        waveTimer = waveDuration;
        canSpawn = true;
    }

    private void SpawnEnemies()
    {
        // If the spawnTimer is less or equal then zero, try to spawn an enemy
        if (spawnTimer <= 0)
        {
            // Check if there are still enemies to spawn
            if (enemiesToSpawn.Count > 0)
            {
                Vector3 spawnPoint = GetValidSpawnPoint();
                if (spawnPoint != Vector3.zero)
                {
                    Instantiate(enemiesToSpawn[0], spawnPoint, Quaternion.identity, transform);
                    enemiesToSpawn.RemoveAt(0);
                    spawnTimer = spawnInterval;
                    enemiesSpawned++;
                }
            }
            // If  the player has killed all enemies, generate the new wave
            else if (enemiesSpawned == enemiesDead) GenerateWave();
        }
        // If no enemy can be spawned yet, decrease the timers
        else
        {
            spawnTimer -= Time.deltaTime;
            waveTimer -= Time.deltaTime;
        }
    }

    private Vector3 GetValidSpawnPoint()
    {
        for (int i = 0; i < 30; i++)  // Try up to 30 times to find a valid location
        {
            Vector3 randomPoint = Random.insideUnitSphere * spawnRadius + transform.position;
            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomPoint, out hit, spawnRadius, NavMesh.AllAreas))
            {
                Vector3 directionToPlayer = playerTransform.position - hit.position;
                if (directionToPlayer.magnitude >= minDistanceFromPlayer)
                {
                    // Check for walls with a BoxCast
                    Collider[] colliders = Physics.OverlapBox(hit.position, new Vector3(5f, 5f, 5f), Quaternion.identity, LayerMask.GetMask("Terrain"));

                    // If no walls are detected near the spawn position
                    if (colliders.Length == 0)
                    {
                        return hit.position;
                    }
                }
            }
        }

        return Vector3.zero;  // Return zero if a valid point is not found after trying 30 times
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
