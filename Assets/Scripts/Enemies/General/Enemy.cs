using System.Collections;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    #region Setup

    // Enemy Spawner Reference
    [HideInInspector] public EnemySpawner spawner;

    // Enemy Reference
    [HideInInspector] public Health enemyHealth;

    // Player References
    [HideInInspector] public PlayerController playerController;
    [HideInInspector] public Transform playerTransform;
    [HideInInspector] public Health playerHealth;
    [HideInInspector] public Souls playerSouls;

    // Set up variables
    [Header("Collision")]
    [SerializeField] private LayerMask collidesWith;

    [Header("Death")]
    [SerializeField] private float deathDelay;

    [Header("Imp Specific")]
    [SerializeField] private bool isImp;
    [SerializeField] private GameObject explosionPrefab;
    [SerializeField] private float explosionDamage;
    [SerializeField] private float explosionRadius;

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
        playerTransform = spawner.playerTransform;
        playerHealth = spawner.playerHealth;
        playerSouls = spawner.playerSouls;
    }

    #region Collision

    private void OnCollisionEnter(Collision collision)
    {
        float damage = playerController.knockbackDamage;
        bool knockedBack = enemyHealth.knockedBack;

        // If not knocked back, do not execute this code
        if (!knockedBack) return;

        // Check if the collider is in the collidesWith layer mask
        if ((collidesWith.value & (1 << collision.gameObject.layer)) != 0)
        {
            if (enemyHealth != null) enemyHealth.Damage(damage);

            GameObject collider = collision.gameObject;

            // If the enemy collided with another enemy, damage it too
            if (collider.CompareTag("Enemy"))
            {
                Health eHealth = collider.GetComponent<Health>();
                if (eHealth != null) // Make sure the enemy has a Health component
                {
                    eHealth.Damage(damage);
                }
            }

            // Reset the knockedBack flag to prevent repeated damage
            enemyHealth.knockedBack = false;
        }
    }

    #endregion

    //

    #region Death

    public void Die()
    {
        StartCoroutine(Death());
    }

    private IEnumerator Death()
    {
        yield return new WaitForSeconds(deathDelay);

        // Check if is an imp. If so, cast an explosion
        if (isImp)
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, explosionRadius);
            GameObject explosion = Instantiate(explosionPrefab, spawner.transform);
            explosion.transform.position = transform.position + new Vector3(0, 1, 0);
            
            foreach (Collider hit  in hits)
            {
                Health health = hit.GetComponent<Health>();

                if (health != null) health.Damage(explosionDamage);
            }
        }

        // Increment the enemiesDead counter
        spawner.enemiesDead++;

        Destroy(gameObject);
    }

    #endregion

    //

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}

