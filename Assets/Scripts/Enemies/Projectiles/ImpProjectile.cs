using System.Collections;
using UnityEngine;

public class ImpProjectile : MonoBehaviour
{
    [SerializeField] private GameObject fire;

    [HideInInspector] public Rigidbody rb;

    private float damage;
    private bool active = true;

    private void Awake()
    {
        fire.SetActive(false);

        StartCoroutine(StartFire());
    }

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void OnTriggerEnter(Collider collision)
    {
        if (!active) return;

        GameObject collisionObject = collision.gameObject;

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

    private IEnumerator StartFire()
    {
        yield return new WaitForEndOfFrame();

        fire.SetActive(true);
    }

    private void DestroyInstance()
    {
        Destroy(gameObject);
    }
}
