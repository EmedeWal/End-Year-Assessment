using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class Health : MonoBehaviour
{
    #region !SETUP!

    #region REFERENCES

    [Header("References")]
    [SerializeField] private GameObject canvasPrefab;
    [SerializeField] private Collider objectCollider;
    [SerializeField] private Animator animator;

    // This variable is used to store the instance of the prefab
    private GameObject canvas;

    // Reference to the script managing the healthUI
    private HealthUI healthUI;

    // Components
    private Rigidbody rb;
    private Enemy enemy;

    // Player
    private PlayerResources playerResources;

    #endregion

    // End of References

    #region VARIABLES

    [Header("Variables")]
    [SerializeField] private Vector3 canvasOffset;

    [SerializeField] private float maxHealth;

    [SerializeField] private float staggerThreshold;

    [HideInInspector] public float currentHealth;
    [HideInInspector] public bool invincible;
    #endregion

    // End of Variables

    #region STANCES

    #region Vampire Stance

    private Coroutine currentCoroutine;
    private float bleedIntervals;
    private bool isCursed;
    private bool isBleeding;

    #endregion

    #endregion

    // End of Stances

    #region EVENTS

    public UnityEvent stagger;
    public UnityEvent death;
    #endregion

    // End of Events

    #endregion

    // END OF SETUP

    #region !EXECUTION!

    #region DEFAULT

    private void Start()
    {
        // Initiliase the health settings
        currentHealth = maxHealth;

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
        playerResources = enemy.playerResources;

        // Update UI
        healthUI.SetMaxHealth(currentHealth, maxHealth);
    }

    private void Update()
    {
        HealthBarPosition();
    }

    #endregion

    // End of Default

    #region UI

    private void HealthBarPosition()
    {
        // Make sure the canvas is set to the same position as the enemy + offset;
        if (canvas != null ) canvas.transform.position = transform.position + canvasOffset;
    }

    #endregion

    // End of UI

    #region MODIFICATIONS

    public void Heal(float amount)
    {
        // Modify health and handle out of bounds input
        currentHealth += amount;
        if (currentHealth > maxHealth) currentHealth = maxHealth;

        // Update UI
        healthUI.SetCurrentHealth(currentHealth);
    }

    public void Damage(float amount)
    {
        // Modify health according to amount
        currentHealth -= amount;

        // Handle out of bounds input
        if (currentHealth < 0) currentHealth = 0;
        if (currentHealth == 0) Die();

        // Update UI
        healthUI.SetCurrentHealth(currentHealth);

        // If the amount of damage taken was high enough, stagger the enemy (no death animations overridden
        if ((amount >= staggerThreshold) && (currentHealth > 0))
        {
            // Invoke the event
            stagger?.Invoke();

            // Fix UI
            healthUI.SetStatusIconActive(2, true);
            CancelInvoke(nameof(DisableStagger));
            Invoke(nameof(DisableStagger), 1f);
        }
    }
    #endregion

    // End of Modifications

    #region STANCES

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

    // End of Stances

    #region OTHER

    public void CanvasState()
    {
        if (canvas.activeSelf) canvas.SetActive(false);
        else canvas.SetActive(true);
    }

    private void DisableStagger()
    {
        healthUI.SetStatusIconActive(2, false);
    }

    private void Die()
    {
        // Invoke the death event
        death?.Invoke();

        // Destroy all relevant components
        Destroy(objectCollider);
        Destroy(canvas);
        Destroy(this);
    }
    #endregion

    // End of Other

    #endregion 

    // END OF EXECUTION
}
