using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Explosion : MonoBehaviour
{
    [Header("Explosion Variables")]
    public float damage = 25;
    public float radius = 5;
    public bool damagesEnemies = false;

    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    private void Start()
    {
        audioSource.Play();

        // Destroy the object after some seconds
        Invoke(nameof(DestroyInstance), 3f);

        // Cast an explosion that damages the player (and enemies?)
        CastExplosion();
    }

    private void CastExplosion()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, radius);

        // Damage the player caught in the blast
        foreach (Collider hit in hits)
        {
            PlayerResources pHealth = hit.GetComponent<PlayerResources>();
            if (pHealth != null) pHealth.Damage(damage);
        }

        if (!damagesEnemies) return;

        // Damage all enemies caught in the blast
        foreach (Collider hit in hits)
        {
            Health eHealth = hit.GetComponent<Health>();
            if (eHealth != null) eHealth.Damage(damage);
        }
    }

    private void DestroyInstance()
    {
        Destroy(gameObject);
    }
}
