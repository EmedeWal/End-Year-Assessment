using UnityEngine;

public class ForwardProjectile : MonoBehaviour
{
    [SerializeField] private float particleDelay;

    [HideInInspector] public Rigidbody rb;

    private float damage;
    private bool active = true;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();

        // Get the layer indices
        int enemyLayer = LayerMask.NameToLayer("Enemy");
        int projectileLayer = LayerMask.NameToLayer("Projectile");

        // Ignore collisions between Enemy and Projectile layers
        Physics.IgnoreLayerCollision(enemyLayer, projectileLayer, true);
        // Optionally, ignore collisions between projectiles themselves
        Physics.IgnoreLayerCollision(projectileLayer, projectileLayer, true);
    }

    private void OnTriggerEnter(Collider collision)
    {
        if (!active) return;

        GameObject collisionObject = collision.gameObject;

        if (collisionObject.CompareTag("Player"))
        {
            PlayerResources pHealth = collisionObject.GetComponent<PlayerResources>();
            if (pHealth != null) pHealth.Damage(damage);
        }

        active = false;

        // Make sure the object stops moving so the fire can catch up
        rb.velocity = Vector3.zero;

        // Destroy the object after a delay
        Invoke(nameof(DestroyInstance), particleDelay);
    }

    public void SetDamage(float newDamage)
    {
        damage = newDamage;
    }

    private void DestroyInstance()
    {
        Destroy(gameObject);
    }
}