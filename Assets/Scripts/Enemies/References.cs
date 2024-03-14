using UnityEngine;

public class References : MonoBehaviour
{
    // Enemy Spawner Reference
    [HideInInspector] public EnemySpawner spawner;

    // Player References
    [HideInInspector] public PlayerController playerController;
    [HideInInspector] public Transform playerTransform;
    [HideInInspector] public Health playerHealth;
    [HideInInspector] public Souls playerSouls;

    private void Awake()
    {
        spawner = GetComponentInParent<EnemySpawner>();

        playerController = spawner.playerController;
        playerTransform = spawner.playerTransform;
        playerHealth = spawner.playerHealth;
        playerSouls = spawner.playerSouls;
    }
}
