using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class Health : MonoBehaviour
{
    public UnityEvent stagger;
    public UnityEvent death;

    #region References

    [Header("References")]
    [SerializeField] private GameObject[] statusIcons;
    [SerializeField] private Transform canvasPrefab;
    [SerializeField] private Animator animator;
    [SerializeField] private Gradient gradient;
    [SerializeField] private Slider slider;
    [SerializeField] private Image fill;

    private References references;
    private Rigidbody rb;

    private PlayerController playerController;
    private Transform playerTransform;
    private Health playerHealth;
    private Souls playerSouls;
    #endregion

    //

    #region Variables

    [Header("Variables")]
    [SerializeField] private float maxHealth;
    [SerializeField] private float startingHealth;
    [SerializeField] private float staggerThreshold;
    [SerializeField] private float deathDelay;

    [HideInInspector] public float currentHealth;
    [HideInInspector] public bool invincible;

    private bool isDying;

    #endregion

    //

    #region Stance Variables and References

    #region Vampire Stance

    [Header("Stance: Vampire")]
    [SerializeField] private float bleedIntervals;
    [SerializeField] private float specialHeal;

    private Coroutine currentCoroutine;
    private bool isCursed;
    private bool isBleeding;

    #endregion

    //

    #region Orc Stance

    [Header("Stance: Orc")]
    [SerializeField] private LayerMask collidesWith;
    [SerializeField] private float knockBackDuration;

    [HideInInspector] public bool knockedBack;

    private float knockBackDamage;

    #endregion

    //

    #region Ghost Stance

    private float damageModifier = 1f;

    #endregion

    //

    #endregion

    //

    #region General

    private void Awake()
    {
        // Initially disable all status icons
        foreach (GameObject icon in statusIcons) icon.SetActive(false);
    }

    private void Start()
    {
        // Initiliase the health settings
        currentHealth = startingHealth;

        SetMaxHealth();

        // Set up some references (Enemies only)
        if (gameObject.CompareTag("Enemy"))
        {
            references = GetComponent<References>();
            rb = GetComponent<Rigidbody>();

            playerController = references.playerController;
            playerTransform = references.playerTransform;
            playerHealth = references.playerHealth;
            playerSouls = references.playerSouls;
        }
    }

    private void Update()
    {
        HealthBarPosition();
    }

    private void OnCollisionEnter(Collision collision)
    {
        // If not knocked back, do not execute this code
        if (!knockedBack) return;

        // Check if the collider is in the collidesWith layermask
        if ((collidesWith & (1 << collision.gameObject.layer)) != 0)
        {
            Damage(knockBackDamage);

            GameObject collider = collision.gameObject;

            // If the enemy collided with another enemy, damage it too
            if (collider.gameObject.CompareTag("Enemy"))
            {
                // Retrieve its health component and damage the enemy
                Health eHealth = collider.GetComponent<Health>();
                eHealth.Damage(knockBackDamage);
            }
        }
    }

    #endregion

    //

    #region Health Bar UI

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

    private void HealthBarPosition()
    {

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
        if (currentHealth <= 0) StartCoroutine(Die());

        // Update the UI
        SetCurrentHealth();

        // If the amount of damage taken was high enough, stagger the enemy
        if (amount >= staggerThreshold) stagger?.Invoke();
    }


    #endregion

    //

    #region Vampire Stance

    public void Bleed(float damage, float duration, bool isSpecial)
    {
        // If it is a special bleed, set cursed to true
        if (isSpecial)
        {
            isCursed = true;
            SetStatusIconActive(0, false);
            SetStatusIconActive(1, true);
            SetBleed(damage, duration);
        }

        // Special bleeds cannot be overridden
        if (isCursed) return;

        // Set a regular bleed
        SetStatusIconActive(0, true);
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
            playerHealth.Heal(damage * damageModifier);
        }

        isBleeding = false;

        // If it was a curse of the vampire, it is now passed. Disable the icon and set the cursed status to false
        if (isCursed)
        {
            isCursed = false;
            SetStatusIconActive(1, false);
        }
        else
        {
            // If it was a regular bleed, set the normal bleed icon to false
            SetStatusIconActive(0, false);
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

        // The enemy is knocked back. Set the icon
        knockedBack = true;
        SetStatusIconActive(2, true);

        // Calculate the direction from the player to the enemy.
        Vector3 knockbackDirection = (transform.position - playerTransform.position).normalized;

        // Apply force to the enemy's Rigidbody in the calculated direction.
        rb.AddForce(knockbackDirection * force, ForceMode.Impulse);

        // After a little while, stop the knockBack and reset the variable
        Invoke(nameof(ResetKnockBack), knockBackDuration * charges);
    }

    private void ResetKnockBack()
    {
        rb.velocity = Vector3.zero;
        knockedBack = false;
        SetStatusIconActive(2, false);
    }

    #endregion

    // 

    #region Ghost Stance

    public void Marked(float damageIncrease)
    {
        // Make the enemy take more damage and set the status icon active
        damageModifier = 1f + damageIncrease;
        SetStatusIconActive(3, true);
    }

    public void RemoveMark()
    {
        // Reset the damage modifier and set the status icon inactive
        damageModifier = 1f;
        SetStatusIconActive(3, false);
    }

    #endregion

    //

    #region Status Effect UI

    private void SetStatusIconActive(int position, bool active)
    {
        // Handle status icon logic
        statusIcons[position].SetActive(active);
    }

    #endregion

    //

    #region Other

    private IEnumerator Die()
    {
        // Prevent several instances
        if (!isDying)
        {
            isDying = true;

            // If the enemy is marked (more damage taken) while it dies, remove it from the list
            if (damageModifier > 1f) playerController.markedEnemies.Remove(this);

            // Invoke the death event
            death?.Invoke();

            yield return new WaitForSeconds(deathDelay);

            Destroy(gameObject);
        }
    }

    #endregion

    //
}
