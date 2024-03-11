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

    private EnemySpawner spawner;
    private Rigidbody rb;

    private PlayerController playerController;
    private Health playerHealth;
    private Souls playerSouls;
    #endregion

    //

    #region Variables

    [Header("Variables")]
    [SerializeField] private float maxHealth;
    [SerializeField] private float startingHealth;

    [HideInInspector] public float currentHealth;
    [HideInInspector] public bool invincible;

    #endregion

    //

    #region Vampire Stance

    [Header("Stance: Vampire")]
    [SerializeField] private float bleedIntervals;

    private Coroutine currentCoroutine;
    private bool vampireCursed;
    private bool isBleeding;

    #endregion

    //

    #region Orc Stance

    [Header("Stance: Orc")]
    [SerializeField] private LayerMask collidesWith;
    [SerializeField] private float knockBackDuration;

    private float knockBackDamage;
    private bool knockedBack;

    #endregion

    //

    #region Ghost Stance

    private float damageModifier = 1f;

    #endregion

    //

    #region General

    private void Start()
    {
        // Initiliase the health settings
        currentHealth = startingHealth;

        SetMaxHealth();

        // Set up some references (Enemies only)
        if (gameObject.CompareTag("Enemy"))
        {
            spawner = GetComponentInParent<EnemySpawner>();
            rb = GetComponent<Rigidbody>();
            playerController = spawner.playerController;
            playerHealth = spawner.playerHealth;
            playerSouls = spawner.playerSouls;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Check if the script is attached to an enemy and if it is being knocked back
        if (gameObject.CompareTag("Enemy") && knockedBack)
        {
            // Check if the collider is in the collidesWith layermask
            if ((collidesWith & (1 << collision.gameObject.layer)) != 0)
            {
                Damage(knockBackDamage);
            }
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
        // Check if the player is invulnerable
        if (invincible) return;

        Debug.Log(gameObject.name + " took " + amount * damageModifier + " damage.");

        // Modify health according to amount and the damage modifier and handle out of bounds input
        currentHealth -= amount * damageModifier;
        if (currentHealth <= 0) Die();

        // Update the UI
        SetCurrentHealth();
    }


    #endregion

    //

    #region Vampire Stance

    public void Bleed(float damage, float duration, bool isSpecial)
    {
        if (isSpecial)
        {
            SetBleed(damage, duration);

            vampireCursed = true;
        }

        // Special bleeds cannot be overridden
        if (vampireCursed) return;

        SetBleed(damage, duration);
    }

    private void SetBleed(float damage, float duration)
    {
        // If the enemy is not yet bleeding, make him bleed
        if (isBleeding) StopCoroutine(currentCoroutine);
        currentCoroutine = StartCoroutine(ApplyBleed(damage, duration));
    }

    private IEnumerator ApplyBleed(float damage, float duration)
    {
        isBleeding = true;

        /* The enemy takes one second of damage for the bleed duration
         * The damage is multiplied by severity */
        for (int i = 0; i < duration; i++) 
        {
            // Wait for the bleed interval to start damaging the enemy
            yield return new WaitForSeconds(bleedIntervals);

            // Damage the enemy and heal the player
            Damage(damage);
            playerHealth.Heal(damage);
        }

        isBleeding = false;

        /* If it was a curse of the vampire, it is now passed
         * Remove all soul charges from the player
         * And set specialActive to false */
        if (vampireCursed)
        {
            vampireCursed = false;
            playerController.SpecialEnd();
        }
    }

    #endregion

    //

    #region Orc Stance

    public void KnockBack(float damage, float force, int charges)
    {
        /* Do not ask what on earth this random calculation is supposed to do.
         * It just makes the force less exponential and thus, feel better */
        if (charges > 1) force = force - ((charges - 1) * 8);

        // Set the knockBackDamage
        knockBackDamage = damage;

        // The enemy is knocked back
        knockedBack = true;

        // Apply a negative force, to make the enemy fly backwards
        rb.AddForce(transform.forward *  force * -1, ForceMode.Impulse);

        // After a little while, stop the knockBack and reset the variable
        Invoke(nameof(ResetKnockBack), knockBackDuration * charges);
    }

    private void ResetKnockBack()
    {
        rb.velocity = Vector3.zero;
        knockedBack = false;
    }

    #endregion

    // 

    #region Ghost Stance

    public void Marked(float damageIncrease)
    {
        damageModifier = 1f + damageIncrease;
    }

    public void UnMark()
    {
        damageModifier = 1f;
    }

    #endregion

    //

    private void Die()
    {
        // If the enemy is marked while it dies, remove it from the list
        if (damageModifier == 1f) playerController.markedEnemies.Remove(this);

        Destroy(gameObject);
    }
}
