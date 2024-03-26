using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class Enemy : MonoBehaviour
{
    #region Setup

    // Enemy Spawner Reference
    [HideInInspector] public EnemySpawner spawner;

    // Enemy Reference
    [HideInInspector] public Health enemyHealth;

    // Player References
    [HideInInspector] public PlayerController playerController;
    [HideInInspector] public PlayerResources playerResources;
    [HideInInspector] public PlayerMovement playerMovement;
    [HideInInspector] public Transform playerTransform;

    [Header("Death")]
    [SerializeField] private float deathDelay;

    [Header("Imp Specific")]
    [SerializeField] private bool isImp;
    [SerializeField] private GameObject explosionPrefab;

    // For spawning and granting gold
    [Header("Spawning")]
    public GameObject enemyPrefab;
    public int cost;

    #endregion

    //

    private void Awake()
    {
        // Get component from parent spawner
        spawner = GetComponentInParent<EnemySpawner>();

        // Get health component attached to gameObject
        enemyHealth = GetComponent<Health>();

        // Retrieve player references from the parent gameObject
        playerController = spawner.playerController;
        playerResources = spawner.playerResources;
        playerTransform = spawner.playerTransform;
        playerMovement = spawner.playerMovement;
    }

    //

    #region Death

    public void Die()
    {
        StartCoroutine(Death());
    }

    private IEnumerator Death()
    {
        yield return new WaitForSeconds(deathDelay);

        // Check if is an imp
        if (isImp)
        {
            // If so, spawn the explosion prefab. This explosion damages enemies
            GameObject explosion = Instantiate(explosionPrefab, spawner.transform);
            explosion.GetComponent<Explosion>().damagesEnemies = true;
        }

        // Increment the enemiesDead counter
        spawner.enemiesDead++;

        // Grant the player gold
        playerResources.GainGold(cost);

        Destroy(gameObject);
    }

    #endregion

    //
}

