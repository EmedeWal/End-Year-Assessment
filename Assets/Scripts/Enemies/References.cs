using UnityEngine;
using System.Collections;

public class References : MonoBehaviour
{
    // Enemy Spawner References
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

    #region Death

    public void Die(float delay)
    {
        StartCoroutine(Death(delay));
    }

    private IEnumerator Death(float delay)
    {
        yield return new WaitForSeconds(delay);

        Destroy(gameObject);
    }

    #endregion

    //
}
