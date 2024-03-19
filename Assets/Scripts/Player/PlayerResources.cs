using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class PlayerResources : MonoBehaviour
{
    #region !SETUP!

    #region EVENTS

    public UnityEvent die;
    #endregion

    // End of Events

    #region REFERENCES

    [Header("REFERENCES")]

    #region Health

    [Header("Canvas Reference")]
    [SerializeField] private HealthUI healthUI;
    #endregion

    #region Souls
    [Header("Soul References")]
    [SerializeField] private Gradient gradient;
    [SerializeField] private Slider slider;
    [SerializeField] private Image fill;

    private PlayerController player;
    #endregion

    #endregion

    // End of References

    #region VARIABLES

    [Header("VARIABLES")]

    #region Health

    [Header("Health Variables")]
    [SerializeField] private float maxHealth;
    [SerializeField] private float startingHealth;
    [HideInInspector] public float currentHealth;
    [HideInInspector] public bool invincible;

    [Header("Healing Variables")]
    [SerializeField] private float healingCooldown;
    [SerializeField] private float healingThreshold;
    [SerializeField] private float healingAmount;
    private Coroutine healingCoroutine;
    #endregion

    #region Souls

    [Header("Soul Variables")]
    [SerializeField] private int maxSouls;
    [SerializeField] private int startingSouls;
    [HideInInspector] public int currentSouls;
    #endregion

    #endregion

    // End of Variables

    #endregion

    // END OF SETUP

    #region !EXECUTION!

    #region DEFAULT

    private void Awake()
    {
        // Get relevant components
        player = GetComponent<PlayerController>();
    }

    void Start()
    {
        // Initiliase the health settings and update UI
        currentHealth = startingHealth;
        healthUI.SetMaxHealth(currentHealth, maxHealth);

        // Initiliase the soul settings
        currentSouls = startingSouls;
        SetMaxSouls();
    }
    #endregion

    // End of Default

    #region UI

    private void SetMaxSouls()
    {
        slider.maxValue = maxSouls;
        slider.value = currentSouls;
        fill.color = gradient.Evaluate(slider.normalizedValue);
    }

    private void SetCurrentSouls()
    {
        slider.value = currentSouls;
        fill.color = gradient.Evaluate(slider.normalizedValue);
    }
    #endregion

    // End of UI

    #region MODIFICATIONS

    #region Health
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

        // If active, interrupt passive healing
        if (healingCoroutine != null) StopCoroutine(healingCoroutine);

        Debug.Log(gameObject.name + " took " + amount + " damage.");

        // Modify health according to amount and the damage modifier and handle out of bounds input
        currentHealth -= amount;
        if (currentHealth <= 0) Die();

        // Update UI
        healthUI.SetCurrentHealth(currentHealth);

        // Check if the player is below the healing threshold.
        // If so, start a timer after which the player is healed back up to the threshold
        if (currentHealth <= (maxHealth / 100 * healingThreshold)) healingCoroutine = StartCoroutine(PassiveHealing());
    }
    #endregion

    #region Souls

    public void GainSouls(int amount)
    {
        // Modify souls and handle out of bounds input
        currentSouls += amount;
        if (currentSouls > maxSouls) currentSouls = maxSouls;

        // Update the UI
        SetCurrentSouls();
    }

    public void SpendSouls(int amount)
    {
        // Modify souls and handle out of bounds input
        currentSouls -= amount;
        if (currentSouls <= 0) currentSouls = 0;

        // Update the UI
        SetCurrentSouls();
    }
    #endregion

    #endregion

    // End of Modifications

    #region OTHER

    private IEnumerator PassiveHealing()
    {
        // Wait for the passive healing to kick in
        yield return new WaitForSeconds(healingCooldown);

        while (currentHealth <= (maxHealth / 100 * healingThreshold))
        {
            Heal(healingAmount);
            yield return new WaitForSeconds(0.1f);
        }
    }

    public int GetCharges()
    {
        // Get the increments of 20 from your souls and return it
        int charges = currentSouls / 20;

        return charges;
    }

    private void Die()
    {
        // Invoke the death event
        die?.Invoke();

        // Destroy all relevant components
        Destroy(this);
    }

    #endregion

    // End of Other

    #endregion

    // END OF EXECUTION
}
