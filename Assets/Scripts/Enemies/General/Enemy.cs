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
    [HideInInspector] public PlayerResources playerResources;
    [HideInInspector] public Transform playerTransform;

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
        playerResources = spawner.playerResources;
        playerTransform = spawner.playerTransform;
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

