using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class Health : MonoBehaviour
{
    public UnityEvent stagger;
    public UnityEvent death;

    #region References

    [Header("Player Only")]
    [SerializeField] private HealthUI healthUI;

    [Header("References")]
    [SerializeField] private GameObject canvasPrefab;
    [SerializeField] private Collider objectCollider;
    [SerializeField] private Animator animator;

    // This variable is used to store the instance of the prefab
    private GameObject canvas;

    // Components
    private Rigidbody rb;
    private Enemy enemy;

    // Player
    private PlayerController playerController;
    private PlayerResources playerResources;
    private Transform playerTransform;

    #endregion

    //

    #region Variables

    [Header("Variables")]
    [SerializeField] private Vector3 canvasOffset;

    [SerializeField] private float maxHealth;

    [SerializeField] private float staggerThreshold;

    [HideInInspector] public float currentHealth;
    [HideInInspector] public bool invincible;

    private bool isEnemy;

    #endregion

    // End of Variables

    #region Stance Variables and References

    #region Vampire Stance

    [Header("Stance: Vampire")]
    [SerializeField] private float specialHeal;

    private Coroutine currentCoroutine;
    private float bleedIntervals;
    private bool isCursed;
    private bool isBleeding;

    #endregion

    //


    #endregion

    // End of Stance Variables and References

    #region General

    private void Awake()
    {
        if (gameObject.CompareTag("Enemy")) isEnemy = true;
    }

    private void Start()
    {
        // Initiliase the health settings
        currentHealth = maxHealth;

        // Set up some references (Enemies only)
        if (isEnemy)
        {
            // Get Components
            rb = GetComponent<Rigidbody>();
            enemy = GetComponent<Enemy>();

            // Make a temporary gameObject of the spawner reference
            GameObject spawnerObject = enemy.spawner.gameObject;

            // Spawn the enemy's canvas under the EnemySpawner to avoid complicated rotations
            canvas = Instantiate(canvasPrefab, spawnerObject.transform);

            // Get the healthUI component on the canvas
            healthUI = canvas.GetComponent<HealthUI>();

            // Assign Player References
            playerController = enemy.playerController;
            playerResources = enemy.playerResources;
            playerTransform = enemy.playerTransform;
        }

        // Update UI
        healthUI.SetMaxHealth(currentHealth, maxHealth);
    }

    private void Update()
    {
        if (isEnemy) HealthBarPosition();
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

        Debug.Log(gameObject.name + " took " + amount + " damage.");

        // Modify health according to amount and the damage modifier and handle out of bounds input
        currentHealth -= amount;
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

    public void Bleed(float damage, float ticks, float intervals, bool isSpecial)
    {
        // If it is a special bleed, set cursed to true
        if (isSpecial)
        {
            isCursed = true;
            healthUI.SetStatusIconActive(0, false);
            healthUI.SetStatusIconActive(1, true);
            SetBleed(damage, ticks, intervals, true);
        }

        // Ultimate bleeds cannot be overridden
        if (isCursed) return;

        // Set a regular bleed
        healthUI.SetStatusIconActive(0, true);
        SetBleed(damage, ticks, intervals, false);
    }

    private void SetBleed(float damage, float ticks, float intervals, bool isSpecial)
    {
        bleedIntervals = intervals;

        // If the enemy is not yet bleeding, make him bleed
        if (isBleeding) StopCoroutine(currentCoroutine);
        currentCoroutine = StartCoroutine(ApplyBleed(damage, ticks, isSpecial));
    }

    private IEnumerator ApplyBleed(float damage, float ticks, bool isSpecial)
    {
        isBleeding = true;

        /* The enemy takes one second of damage for the bleed duration
         * The damage is multiplied by severity */
        for (int i = 0; i < ticks; i++) 
        {
            // Wait for the bleed interval to start damaging the enemy
            yield return new WaitForSeconds(bleedIntervals);

            // Damage the enemy
            Damage(damage);

            // Determine the amount of healing based on special or not
            float heal = damage;
            if (!isSpecial) heal /= 4;
            playerResources.Heal(heal);
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

    #endregion

    //

    #region Other

    private void Die()
    {
        // Invoke the death event
        death?.Invoke();

        // Destroy all relevant components
        Destroy(objectCollider);
        Destroy(canvas);
        Destroy(rb);
        Destroy(this);
    }

    #endregion

    //
}
