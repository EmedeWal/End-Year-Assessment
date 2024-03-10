using UnityEngine;

public class Health : MonoBehaviour
{
    [Header("Health Variables")]
    [SerializeField] private float maxHealth;

    [HideInInspector] public float currentHealth;

    private void Start()
    {
        currentHealth = maxHealth;
    }

    public void Heal(float amount)
    {
        currentHealth += amount;
        if (currentHealth > maxHealth) currentHealth = maxHealth;
    }

    public void Damage(float amount)
    {
        Debug.Log(gameObject.name + " took " + amount + " damage.");

        currentHealth -= amount;
        if (currentHealth <= 0) Die();
    }

    private void Die()
    {
        Destroy(gameObject);
    }

}
