using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class Health : MonoBehaviour
{
    public UnityEvent stagger;
    public UnityEvent death;

    #region References

    [Header("References")]
    [SerializeField] private GameObject canvasPrefab;
    [SerializeField] private Collider objectCollider;
    [SerializeField] private Animator animator;
    [SerializeField] private HealthUI healthUI;

    // This variable is used to store the instance of the prefab
    private GameObject canvas;

    // Components
    private References references;
    private Rigidbody rb;

    // Player
    private PlayerController playerController;
    private Transform playerTransform;
    private Health playerHealth;
    private Souls playerSouls;

    #endregion

    //

    #region Variables

    [Header("Variables")]
    [SerializeField] private Vector3 canvasOffset;

    [SerializeField] private float maxHealth;
    [SerializeField] private float startingHealth;

    [SerializeField] private float staggerThreshold;

    [HideInInspector] public float currentHealth;
    [HideInInspector] public bool invincible;

    private bool isEnemy;

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
        if (gameObject.CompareTag("Enemy")) isEnemy = true;
    }

    private void Start()
    {
        // Initiliase the health settings
        currentHealth = startingHealth;

        // Set up some references (Enemies only)
        if (isEnemy)
        {
            // Get Components
            references = GetComponent<References>();
            rb = GetComponent<Rigidbody>();

            // Make a temporary gameObject of the spawner reference
            GameObject spawnerObject = references.spawner.gameObject;

            // Spawn the enemy's canvas under the EnemySpawner to avoid complicated rotations
            canvas = Instantiate(canvasPrefab, spawnerObject.transform);

            // Get the healthUI component on the canvas
            healthUI = canvas.GetComponent<HealthUI>();

            // Assign Player References
            playerController = references.playerController;
            playerTransform = references.playerTransform;
            playerHealth = references.playerHealth;
            playerSouls = references.playerSouls;
        }

        // Update UI
        healthUI.SetMaxHealth(currentHealth, maxHealth);
    }

    private void Update()
    {
        if (isEnemy) HealthBarPosition();
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

    private void HealthBarPosition()
    {
        // Make sure the canvas is set to the same position as the enemy + offset;
        if (canvas != null ) canvas.transform.position = transform.position + canvasOffset;
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

        // Update UI
        healthUI.SetCurrentHealth(currentHealth);
    }

    public void Damage(float amount)
    {
        // Check if the player is invulnerable
        if (invincible) return;

        Debug.Log(gameObject.name + " took " + amount * damageModifier + " damage.");

        // Modify health according to amount and the damage modifier and handle out of bounds input
        currentHealth -= amount * damageModifier;
        if (currentHealth <= 0) Die();

        // Update UI
        healthUI.SetCurrentHealth(currentHealth);

        // If the amount of damage taken was high enough, stagger the enemy
        if (amount >= staggerThreshold) stagger?.Invoke();
    }


    #endregion

    //

    #region Stances

    #region Vampire Stance

    public void Bleed(float damage, float duration, bool isSpecial)
    {
        // If it is a special bleed, set cursed to true
        if (isSpecial)
        {
            isCursed = true;
            healthUI.SetStatusIconActive(0, false);
            healthUI.SetStatusIconActive(1, true);
            SetBleed(damage, duration);
        }

        // Special bleeds cannot be overridden
        if (isCursed) return;

        // Set a regular bleed
        healthUI.SetStatusIconActive(0, true);
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
            healthUI.SetStatusIconActive(1, false);
        }
        else
        {
            // If it was a regular bleed, set the normal bleed icon to false
            healthUI.SetStatusIconActive(0, false);
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
        healthUI.SetStatusIconActive(2, true);

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
        healthUI.SetStatusIconActive(2, false);
    }

    #endregion

    // 

    #region Ghost Stance

    public void Marked(float damageIncrease)
    {
        // Make the enemy take more damage and set the status icon active
        damageModifier = 1f + damageIncrease;
        healthUI.SetStatusIconActive(3, true);
    }

    public void RemoveMark()
    {
        // Reset the damage modifier and set the status icon inactive
        damageModifier = 1f;
        healthUI.SetStatusIconActive(3, false);
    }

    #endregion

    //

    #endregion

    //

    #region Other

    private void Die()
    {
        // If the enemy is marked (more damage taken) while it dies, remove it from the list
        if (isEnemy && damageModifier > 1f) playerController.markedEnemies.Remove(this);

        // Invoke the death event
        death?.Invoke();

        // Destroy all relevant components
        Destroy(objectCollider);
        Destroy(canvas);
        Destroy(this);
    }

    #endregion

    //
}
