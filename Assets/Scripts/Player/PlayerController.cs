using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System.Runtime.InteropServices;

public class PlayerController : MonoBehaviour
{
    #region References

    [Header("References")]
    [SerializeField] private GameObject playerCanvas;
    [SerializeField] private Transform attackPoint;
    [SerializeField] private Animator animator;

    private Rigidbody rb;
    private Health health;
    private Souls souls;

    #endregion

    //

    #region Variables

    private Coroutine currentCoroutine;

    #endregion

    //

    #region Souls

    [Header("Souls")]
    [SerializeField] private int soulsGain;

    #endregion

    //

    #region Stances

    [Header("Stance: General")]
    [SerializeField] private string[] stances;
    [SerializeField] private GameObject[] stanceIcons;

    public int stancePosition = 0;
    private string currentStance;

    [Header("Stance: Vampire - Default")]
    [SerializeField] private float bleedBaseDamage;
    [SerializeField] private float bleedTicks;

    [Header("Stance: Vampire - Special")]
    [SerializeField] private Image vampireSpecialImage;
    [SerializeField] private float vampireSpecialDuration;
    [SerializeField] private float vampireSpecialRange;

    [Header("Stance: Orc - Default")]
    [SerializeField] private float knockBackBaseDamage;
    [SerializeField] private float knockBackForce;

    [Header("Stance: Orc - Special")]
    [SerializeField] private float orcSpecialDuration;

    [Header("Stance: Ghost - Default")]
    [SerializeField] private float damageIncrement;
    [SerializeField] private float healthTax;

    [Header("Stance: Ghost - Special")]
    [SerializeField] private float damageIncrease;
    [SerializeField] private float ghostSpecialDuration;

    [HideInInspector] public List<Health> markedEnemies = new List<Health>();

    private bool ghostSpecialActive;

    #endregion

    //

    #region Movement

    [Header("Movement Variables")]
    [SerializeField] private float moveSpeed;
    [SerializeField] private float rotationSpeed;

    private Vector2 move;
    private bool canMove = true;

    #endregion

    //

    #region Attacking

    [Header("Attack Variables")]
    [SerializeField] private float attackDamage;
    [SerializeField] private float attackOffset;
    [SerializeField] private float attackSpeed;
    [SerializeField] private float attackRange;
    [SerializeField] private float damageDelay;

    private bool canAttack = true;

    #endregion

    //

    #region Special

    [HideInInspector] public bool specialActive;

    #endregion

    //

    #region Dodging

    [Header("Dodging")]
    [SerializeField] private float dodgeForce;
    [SerializeField] private float dodgeCooldown;
    [SerializeField] private float dodgeDuration;

    private bool canDodge = true;
    private bool isDodging;

    #endregion

    // 

    #region Default

    private void Awake()
    {
        // Retrieve components
        rb = GetComponent<Rigidbody>();
        health = GetComponent<Health>();
        souls = GetComponent<Souls>();

        // Set objects inactive
        playerCanvas.SetActive(false);
    }

    private void Start()
    {
        // Make the editor a little more intuitive
        stancePosition--;

        // Swap to the correct stance at the beginning of the game
        SwapStance();
    }

    private void Update()
    {
        // Move the player, if he is allowed to. If not, make sure he stands still
        if (canMove) Move();
        else rb.velocity = Vector3.zero;
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Check to see if you collided with an enemy during your dodge while the ghost special is active
        if (collision.gameObject.CompareTag("Enemy") && isDodging && ghostSpecialActive)
        {
            Debug.Log("Enemy was marked");

            // Retrieve the health script on the enemy
            Health eHealth = collision.gameObject.GetComponent<Health>();

            // Add the reference to the collider to the markedEnemies list
            markedEnemies.Add(eHealth);

            // Mark the enemy. The severity depends on the amount of charges
            eHealth.Marked(damageIncrease * souls.GetCharges());
        }
    }

    #endregion

    //

    #region Input
    public void OnSwapStance(InputAction.CallbackContext context)
    {
        // Check if the input was triggered
        if (context.performed)
        {
            // Get the value of the axis input and enter it is a parameter
            DetermineStance(context.ReadValue<float>());
        }
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        move = context.ReadValue<Vector2>();
    }

    public void OnAttack()
    {
        Attack();
    }

    public void OnSpecial()
    {
        Special();
    }

    public void OnDodge()
    {
        Dodge();
    }

    public void OnInteract()
    {
        Debug.Log("OnInteract was called");
    }

    #endregion

    //

    #region Stances

    private void DetermineStance(float inputValue)
    {
        // Initialise the last position in stances[]
        int lastPosition = stances.Length - 1;

        /* Turn the float inputValue into an integer, so we can work with arrays
         * Round the float value to the nearest integer */
        int intInput = Mathf.RoundToInt(inputValue);

        // Modify the position in the list according to the input
        stancePosition += intInput;

        // Handle out-of-bounds inputs
        if (stancePosition < 0) stancePosition = lastPosition;
        else if (stancePosition > lastPosition) stancePosition = 0;

        SwapStance();
    }

    private void SwapStance()
    {
        // Update the currentStance accordingly
        currentStance = stances[stancePosition];

        // Disable all stance icons. Then, set the correct one active
        foreach (GameObject icon in stanceIcons)
        {
            icon.SetActive(false);
        }

        stanceIcons[stancePosition].SetActive(true);
    }

    #endregion

    // 

    #region Movement

    private void Move()
    {
        Vector3 movement = new Vector3(move.x, 0f, move.y);

        // First check if the player is moving to avoid snapping back to its default rotation upon being idle.
        if (movement.magnitude > 0)
        {
            // The rotation of the transform is equal to the direction the player is moving
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(movement), rotationSpeed);
        }

        // This apparently works really well so let's remember transform.Translate
        transform.Translate(movement * moveSpeed * Time.deltaTime, Space.World);

        // Check if the player is running or not, then set the correct animation
        animator.SetFloat("Movement", movement.magnitude);

        // If the player is not moving, do this to stop him from moving after collisions
        if (movement.magnitude == 0) rb.velocity = Vector3.zero;
    }

    #endregion

    //

    #region Attacking

    private void Attack()
    {
        if (canAttack && !isDodging)
        {
            canAttack = false;
            canMove = false;

            animator.SetTrigger("Attack");

            StartCoroutine(ResetAttack());
            StartCoroutine(ApplyDamage());
        }   
    }

    private IEnumerator ResetAttack()
    {
        // Wait for the attackSpeed of the player, before the player can attack again
        yield return new WaitForSeconds(attackSpeed - attackOffset);
        canAttack = true;

        /* Wait a little longer before making the player move again, 
         * so that animations do not trigger double and the player can "chain attack" */
        yield return new WaitForSeconds(attackOffset);
        if (canAttack) canMove = true;
    }

    private IEnumerator ApplyDamage()
    {
        // Wait a moment for the swing to start
        yield return new WaitForSeconds(attackSpeed / damageDelay);

        // Retrieve all detected colliders
        Collider[] hits = Physics.OverlapSphere(attackPoint.position, attackRange);

        foreach (Collider hit in hits)
        {
            // Check if the hit is an enemy
            if (hit.gameObject.CompareTag("Enemy"))
            {
                // Retrieve the health script on the enemy
                Health eHealth = hit.GetComponent<Health>();

                // Retrieve the current Soul Charges
                int charges = souls.GetCharges();

                // Make a variable for damage handling, to avoid ridiculous stacking from the Ghost stance
                float damage = attackDamage;

                /* Check for active stances, and handle additional logic accordingly
                 * Check if the player has charges. If not, do not bother with stance logic */
                if (charges != 0)
                {
                    // Check if the vampire stance is active. If so, inflict bleed upon the enemy
                    if (currentStance == "Vampire") eHealth.Bleed(bleedBaseDamage * charges, bleedTicks, false);

                    // Check if the orc stance is active. If so, knock the enemy backwards
                    if (currentStance == "Orc") eHealth.KnockBack(knockBackBaseDamage * charges, knockBackForce * charges, charges);

                    // Check if the ghost stance is active. If so, deal more damage at the cost of health
                    if (currentStance == "Ghost")
                    {
                        damage = damageIncrement * charges;
                        health.Damage(healthTax * charges);
                    }
                }

                // Apply damage to the enemy
                eHealth.Damage(damage);

                // If the special is not active, grant the player souls
                if (!specialActive) souls.GainSouls(soulsGain);
            }
        }
    }

    #endregion

    // 

    #region Special

    private void Special()
    {
        // If the special is active, do not execute all of this stuff
        if (specialActive) return;

        // Retrieve soul charges for convenience
        int charges = souls.GetCharges();

        // Check if the player is not attacking, dodging, and if he has more than 0 soul charges
        if (canAttack && !isDodging && charges > 0)
        {
            specialActive = true;

            // Determine which Special to cast
            if (currentStance == "Vampire") VampireSpecial(charges);
            if (currentStance == "Orc") OrcSpecial(charges);
            if (currentStance == "Ghost") GhostSpecial(charges);
        }
    }

    public void SpecialEnd()
    {
        specialActive = false;
        souls.SpendSouls(souls.GetCharges() * 20);
    }

    private void VampireSpecial(int charges)
    {
        specialActive = true;

        // Local variable to keep track of the base amount of bleed ticks for the special
        int baseTicks = 2;

        // Trigger visuals
        StartCoroutine(VampireSpecialGFX());

        // Cast a big ass circle and collect all colliders
        Collider[] hits = Physics.OverlapSphere(transform.position, vampireSpecialRange);

        foreach (Collider hit in hits)
        {
            // Check if it is an enemy
            if (hit.gameObject.CompareTag("Enemy"))
            {
                // Collect their health scripts
                Health eHealth = hit.GetComponent<Health>();

                /* Inflict bleed upon all enemies hit. 
                 * The damage is half as a normal bleed, but the duration depends on charges
                 * Two bleed ticks per charge */
                eHealth.Bleed(bleedBaseDamage * charges / baseTicks, baseTicks  * charges, true);
            }
        }
    }

    private IEnumerator VampireSpecialGFX()
    {
        playerCanvas.SetActive(true);

        // Some local variables to keep things cleaner
        RectTransform size = vampireSpecialImage.rectTransform;
        Vector2 nativeSize = size.sizeDelta;
        Vector2 maxSize = new Vector2(vampireSpecialRange, vampireSpecialRange);
        float increment = 0.3f;
        float pause = 0.01f;

        // While the circle is not yet as big as the boxCast, increase its size
        while (size.sizeDelta.magnitude < maxSize.magnitude)
        {
            size.sizeDelta += new Vector2(increment, increment);
            yield return new WaitForSeconds(pause);
        }

        // Wait a tiny while before dissappearing
        yield return new WaitForSeconds(pause * 10);

        // Reset the native size
        size.sizeDelta = nativeSize;

        playerCanvas.SetActive(false);
    }

    private void OrcSpecial(int charges)
    {
        specialActive = true;

        // Become invulnerable to damage for the duration of the special
        currentCoroutine = StartCoroutine(Invincible(orcSpecialDuration * charges));
    }

    private void GhostSpecial(int charges)
    {
        StartCoroutine(GhostSpecialEffect(charges));
    }

    private IEnumerator GhostSpecialEffect(int charges)
    {
        // Set booleans
        specialActive = true;
        ghostSpecialActive = true;

        // Reduce the dodge cooldown by 1 second for each charge
        dodgeCooldown -= 1 * charges;

        // This takes effect for as long as the duration * charges
        yield return new WaitForSeconds(ghostSpecialDuration * charges);

        // Revert dodge cooldown
        dodgeCooldown += 1 * charges;

        // Unmark all enemies in the markedEnemies list
        foreach (Health eHealth in markedEnemies) eHealth.UnMark();

        // Set booleans
        ghostSpecialActive = false;

        SpecialEnd();
    }

    #endregion

    //

    #region Dodging

    private void Dodge()
    {
        // If the player can move and the dodge is off cooldown, dodge
        if (canMove && canDodge)
        {
            canDodge = false;
            isDodging = true;

            // Add the dodge force
            rb.AddForce(transform.forward * dodgeForce, ForceMode.Impulse);

            // Reset the dodge ability depending on the dodge cooldown
            Invoke(nameof(ResetDodge), dodgeCooldown);

            // Stop the dodge after the dodge duration
            Invoke(nameof(EndDodge), dodgeDuration);

            // Make the player invulnerable for the dodge duration, unless the player is already invincible
            if (currentCoroutine == null) StartCoroutine(Invincible(dodgeDuration));
        }
    }

    private void ResetDodge()
    {
        canDodge = true;
    }

    private void EndDodge()
    {
        rb.velocity = Vector3.zero;
        isDodging = false;
    }

    #endregion

    //

    #region Other

    private IEnumerator Invincible(float duration)
    {
        // Make the player invincible for the duration
        health.invincible = true;

        yield return new WaitForSeconds(duration);

        health.invincible = false;

        // If a coroutine is assigned, unassign it and set specialActive to false
        if (currentCoroutine != null)
        {
            SpecialEnd();
            currentCoroutine = null;
        }
    }

    #endregion

    //

    private void OnDrawGizmosSelected()
    {
        // For the attack
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(attackPoint.position, attackRange);

        // For the vampire special
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, vampireSpecialRange);

    }
}
