using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private GameObject player;

    [HideInInspector] public Transform playerTransform;

    [HideInInspector] public PlayerController playerController;
    [HideInInspector] public Health playerHealth;
    [HideInInspector] public Souls playerSouls;

    private void Awake()
    {
        playerTransform = player.transform;

        playerController = player.GetComponent<PlayerController>();
        playerHealth = player.GetComponent<Health>();
        playerSouls = player.GetComponent<Souls>();
    }
}
