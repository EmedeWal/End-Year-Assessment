using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Player References")]
    public Transform playerTransform;
    public PlayerController playerController;
    public Health playerHealth;
    public Souls playerSouls;
}
