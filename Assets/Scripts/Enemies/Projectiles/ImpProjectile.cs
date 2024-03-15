using UnityEngine;

public class ImpProjectile : MonoBehaviour
{
    [HideInInspector] public Rigidbody rb;

    private float damage;

    private void OnTriggerEnter(Collider collision)
    {
        GameObject collisionObject = collision.gameObject;

        if (collisionObject.CompareTag("Player"))
        {
            Health pHealth = collisionObject.GetComponent<Health>();
            pHealth.Damage(damage);
        }

        Destroy(gameObject);
    }

    public void SetDamage(float newDamage)
    {
        damage = newDamage;
    }
}
