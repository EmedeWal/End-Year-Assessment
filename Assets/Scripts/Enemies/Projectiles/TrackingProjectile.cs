using UnityEngine;

public class TrackingProjectile : MonoBehaviour
{
    [Header("Movement Tacking")]
    [SerializeField] private float moveSpeed;
    [SerializeField] private float rotationSpeed;

    [Header("Explosion")]
    [SerializeField] private GameObject explosionPrefab;
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private float explosionRange;
    [SerializeField] private float shockwaveRange;
    [SerializeField] private float explosionDelay;

    [Header("Other")]
    [SerializeField] private GameObject VFX;
    [SerializeField] private float activationTime;
    [SerializeField] private float destroyDelay;
    [SerializeField] private float lifespan;

    [HideInInspector] public Transform player;
    [HideInInspector] public float explosionDamage;

    private bool canExplode = true;
    private bool active = false;

    private void Awake()
    {
        // Get the layer indices
        int enemyLayer = LayerMask.NameToLayer("Enemy");
        int projectileLayer = LayerMask.NameToLayer("Projectile");

        // Ignore collisions between Enemy and Projectile layers
        Physics.IgnoreLayerCollision(enemyLayer, projectileLayer, true);
        // Optionally, ignore collisions between projectiles themselves
        Physics.IgnoreLayerCollision(projectileLayer, projectileLayer, true);
    }

    void Start()
    {
        // Activate the spell after a short delay
        Invoke(nameof(Activate), activationTime);

        // Count down the lifespan of the spell. Upon it reaching zero, explode instantly
        Invoke(nameof(LifeSpan), lifespan);
    }

    void Update()
    {
        if (!active) return;

        // If the player is within the detonation distance, do boom
        if (CalculateDistance() <= explosionRange)
        {
            // Deactivate the spell and cast an explosion after the explosion delay
            Deactivate(explosionDelay);
            return;
        }

        SpellMovement();
    }

    private void OnTriggerEnter(Collider other)
    {
        // Upon collision, explode instantly
        if (active) Deactivate(0f);
    }

    private float CalculateDistance()
    {
        // Return the distance from the spell to the player
        return Vector3.Distance(transform.position, player.position);
    }

    private void SpellMovement()
    {
        // Move the spell forward constantly
        transform.Translate(Vector3.forward * moveSpeed * Time.deltaTime);

        // Calculate the rotation needed to look at the player
        Vector3 directionToPlayer = (player.position - transform.position).normalized;
        directionToPlayer.y = 0; // Ignore y difference for horizontal rotation only
        Quaternion lookRotation = Quaternion.LookRotation(directionToPlayer);

        // Rotate the spell towards the player over time
        transform.rotation = Quaternion.RotateTowards(transform.rotation, lookRotation, rotationSpeed * Time.deltaTime);
    }

    private void Explosion()
    {
        // BOOM 
        Instantiate(explosionPrefab, transform);

        // Cast the innor explosion and outer explosions
        Collider[] hits = Physics.OverlapSphere(transform.position, explosionRange, playerLayer);

        PlayerResources pHealth;

        // Damage all collider's of the inner explosion
        foreach (Collider hit in hits)
        {
            pHealth = hit.gameObject.GetComponent<PlayerResources>();
            pHealth.Damage(explosionDamage);
        }
    }

    private void Activate()
    {
        active = true;
    }

    private void Deactivate(float delay)
    {
        // Make sure the spell stops updating
        active = false;
        transform.position = transform.position;

        // Explode after a short delay
        Invoke(nameof(Explode), delay);
    }

    private void Explode()
    {
        // Make sure no double triggers happen
        if (!canExplode) return;
        canExplode = false;

        VFX.SetActive(false);

        // Cast the explosion
        Explosion();

        // Destroy the entire spell after the explosion has finished (approx)
        Invoke(nameof(DestroyInstance), destroyDelay);
    }

    private void LifeSpan()
    {
        Deactivate(0f);
    }

    private void DestroyInstance()
    {
        Destroy(gameObject);
    }
}
