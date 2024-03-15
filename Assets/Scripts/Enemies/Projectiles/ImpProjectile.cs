using System.Collections;
using UnityEngine;

public class ImpProjectile : MonoBehaviour
{
    [HideInInspector] public Rigidbody rb;

    private float damage;
    private bool active = true;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void OnTriggerEnter(Collider collision)
    {
        if (!active) return;

        GameObject collisionObject = collision.gameObject;

        // Allow this projectile to pass through other enemies
        if (collisionObject.CompareTag("Enemy")) return;

        if (collisionObject.CompareTag("Player"))
        {
            Health pHealth = collisionObject.GetComponent<Health>();
            pHealth.Damage(damage);
        }
        
        active = false;

        // Make sure the object stops moving so the fire can catch up
        rb.velocity = Vector3.zero;

        // Destroy the object after 0.5 seconds
        Invoke(nameof(DestroyInstance), 0.5f);
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
