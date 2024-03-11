using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class Health : MonoBehaviour
{
    #region References

    [Header("References")]
    [SerializeField] private Gradient gradient;
    [SerializeField] private Slider slider;
    [SerializeField] private Image fill;

    #endregion

    //

    #region Variables

    [Header("Variables")]
    [SerializeField] private float maxHealth;
    [SerializeField] private float startingHealth;

    [HideInInspector] public float currentHealth;
    [HideInInspector] public bool invulnerable;

    private EnemySpawner spawner;
    private Rigidbody rb;
    private Health playerHealth;

    #endregion

    //

    #region Vampire Stance

    [Header("Stance: Vampire")]
    [SerializeField] private float bleedIntervals;
    private Coroutine currentCoroutine;
    private float bleedDuration = 5;
    private bool isBleeding;

    #endregion

    //

    #region Orc Stance

    [Header("Stance: Orc")]
    [SerializeField] private LayerMask collidesWith;
    private float knockBackDamage;
    private bool knockedBack;

    #endregion

    //

    #region General

    private void Awake()
    {
        // Set up some references (Enemies only)
        if (gameObject.CompareTag("Enemy"))
        {
            spawner = GetComponentInParent<EnemySpawner>();
            rb = GetComponent<Rigidbody>(); 
            playerHealth = spawner.playerHealth;
        }
    }

    private void Start()
    {
        // Initiliase the health settings
        currentHealth = startingHealth;

        SetMaxHealth();
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Check if the script is attached to an enemy and if it is being knocked back
        if (gameObject.CompareTag("Enemy") && knockedBack)
        {
            // Check if the collider is in the collidesWith layermask
            // ...
        }
    }

    #endregion

    //

    #region UI

    private void SetMaxHealth()
    {
        slider.maxValue = maxHealth;
        slider.value = currentHealth;
        fill.color = gradient.Evaluate(slider.normalizedValue);
    }


    private void SetCurrentHealth()
    {
        slider.value = currentHealth;
        fill.color = gradient.Evaluate(slider.normalizedValue);
    }

    #endregion

    //

    #region Modifications

    public void Heal(float amount)
    {
        Debug.Log(gameObject.name + " healed " + amount + " health.");

        // Modify health and handle out of bounds input
        currentHealth += amount;
        if (currentHealth > maxHealth) currentHealth = maxHealth;

        // Update the UI
        SetCurrentHealth();
    }

    public void Damage(float amount)
    {
        // Check if the enemy is invulnerable
        if (!invulnerable)
        {
            Debug.Log(gameObject.name + " took " + amount + " damage.");

            // Modify health and handle out of bounds input
            currentHealth -= amount;
            if (currentHealth <= 0) Die();

            // Update the UI
            SetCurrentHealth();
        }
    }


    #endregion

    //

    #region Vampire Stance

    public void Bleed(float damage, int severity)
    {
        // If the enemy is not yet bleeding, make him bleed
        if (isBleeding) StopCoroutine(currentCoroutine);
        currentCoroutine = StartCoroutine(ApplyBleed(damage, severity));
    }

    private IEnumerator ApplyBleed(float initialDamage, int severity)
    {
        isBleeding = true;

        // Calculate the final damage
        float damage = initialDamage * severity;

        /* The enemy takes one second of damage for the bleed duration
         * The damage is multiplied by severity */
        for (int i = 0; i < bleedDuration; i++) 
        {
            // Wait for the bleed interval to start damaging the enemy
            yield return new WaitForSeconds(bleedIntervals);

            // Damage the enemy and heal the player
            Damage(damage);
            playerHealth.Heal(damage);
        }

        isBleeding = false;
    }

    #endregion

    //

    #region Orc Stance

    public void KnockBack(float damage, float force)
    {
        // Set the knockBackDamage
        knockBackDamage = damage;

        // Apply a negative force, to make the enemy fly backwards
        rb.AddForce(transform.forward *  force * -1, ForceMode.Impulse);
    }

    #endregion

    // 

    #region Other

    private void Die()
    {
        Destroy(gameObject);
    }

    #endregion

    //
}
