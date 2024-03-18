using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using TMPro;
using UnityEngine.Events;
using static UnityEditor.Timeline.TimelinePlaybackControls;

public class PlayerController : MonoBehaviour
{
    #region !SETUP!

    #region REFERENCES

    [Header("REFERENCES")]

    #region GameObjects

    [Header("References: GameObjects")]
    [SerializeField] private GameObject playerCanvas;
    [SerializeField] private Transform attackPoint;
    [SerializeField] private Animator animator;

    private Rigidbody rb;
    private Health health;
    private Souls souls;
    #endregion

    #endregion

    // End of References

    #region VARIABLES

    [Header("VARIABLES")]

    #region Souls

    [Header("Souls")]
    [SerializeField] private int soulsGain;
    #endregion

    #region Other

    [Header("Other")]
    public UnityEvent die;
    private Coroutine invincibleCoroutine;
    #endregion

    #endregion

    // End of Variables

    #region STANCES

    [Header("STANCES")]

    #region General

    [Header("General")]
    [SerializeField] private string[] stances;
    [SerializeField] private Renderer swordRenderer;

    public int stancePosition = 0;
    public string currentStance;
    #endregion

    #region UI

    [Header("UI")]
    [SerializeField] private GameObject[] stanceIcons;
    [SerializeField] private Color[] stanceColors;
    #endregion

    #region Vampire Stance

    [Header("Vampire Stance")]
    [SerializeField] private float bleedDamage;
    [SerializeField] private float bleedTicks;
    [SerializeField] private float bleedIntervals;

    [SerializeField] private Image vampireSpecialGFX;
    [SerializeField] private float vampireSpecialTicks;
    [SerializeField] private float vampireSpecialRange;
    #endregion

    #region Orc Stance

    [Header("Orc Stance")]
    [SerializeField] private float orcDamageMultiplier;
    [SerializeField] private float orcSpecialDuration;
    #endregion

    #region Ghost Stance

    [Header("Ghost Stance")]
    [SerializeField] private float ghostDamageMultiplier;
    [SerializeField] private float ghostSpecialDuration;
    #endregion

    #endregion

    // End of Stances

    #region MOVEMENT

    [Header("MOVEMENT")]
    [SerializeField] private float moveSpeed;
    [SerializeField] private float rotationSpeed;

    private Vector2 move;
    private bool canMove = true;
    private bool canRotate = true;
    #endregion

    // End of Movement

    #region ATTACKING

    private List<GameObject> damagedEnemies = new List<GameObject>();
    private Coroutine attackReset;
    private Coroutine comboReset;
    private int comboCounter = 0;
    private Vector3 attackSize;
    private float attackDamage;
    private float attackSpeed;
    private float attackChargeTime;
    private float movementDelay;
    private bool canAttack = true;
    private bool isAttacking = false;
    #endregion

    // End of Attacking

    #region SPECIAL

    [Header("SPECIAL")]

    #region User Interface
    [Header("User Interface")]
    [SerializeField] private GameObject[] stanceSpecialIcons;
    [SerializeField] private TextMeshProUGUI specialDurationText;
    [SerializeField] private Image specialDurationImage;
    #endregion

    #region Variables

    [HideInInspector] public bool specialActive;

    private float specialDuration;
    private float specialCountdown;
    #endregion

    #endregion

    // End of Special

    #region DASHING

    [Header("Dashing")]

    #region User Interface

    [Header("User Interface")]
    [SerializeField] private TextMeshProUGUI cooldownTextDash;
    [SerializeField] private Image cooldownImageDash;
    #endregion

    #region Variables

    [Header("Variables")]
    [SerializeField] private float dashForce;
    [SerializeField] private float dashCooldown;
    [SerializeField] private float dashDuration;

    private Coroutine dashCoroutine;
    private float dashCooldownTimer;
    private bool isDashing = false;
    private bool canDash = true;
    #endregion

    #endregion

    // End of Dodging

    #endregion

    // END OF SETUP

    #region !EXECUTION!

    #region DEFAULT

    private void Awake()
    {
        // Retrieve components
        rb = GetComponent<Rigidbody>();
        health = GetComponent<Health>();
        souls = GetComponent<Souls>();

        // Set objects inactive
        playerCanvas.SetActive(false);

        // Make the cursor invisible
        Cursor.visible = false;
    }

    private void Start()
    {
        // Make the editor a little more intuitive
        stancePosition--;

        // Set the cooldown text inactive and make sure the images value is correct
        cooldownTextDash.gameObject.SetActive(false);
        cooldownImageDash.fillAmount = 0;

        specialDurationText.gameObject.SetActive(false);
        specialDurationImage.fillAmount = 1f;

        // Swap to the correct stance at the beginning of the game
        SwapStance();
    }

    private void Update()
    {
        // Check if the player should move
        if (!isAttacking && !isDashing) Move();

        // Handle dodge cooldown
        if (!canDash) DashCooldown(dashCooldown);

        // Handle special timer
        if (specialActive) SpecialTimer(specialDuration);
    }
    #endregion

    // End of Default

    #region INPUT

    public void OnMove(InputAction.CallbackContext context)
    {
        move = context.ReadValue<Vector2>();
    }

    public void OnSwapStance(InputAction.CallbackContext context)
    {
        // When the button is pressed, determine the stance
        if (context.phase == InputActionPhase.Performed) DetermineStance();
    }

    public void OnLightAttack(InputAction.CallbackContext context)
    {
        // When the button is pressed
        if (context.phase == InputActionPhase.Performed)
        {
            // If the player can Attack and is not dashing, cast a light attack
            if (canAttack && !isDashing)
            {
                SetAttackVariables("Light");
                Attack();
            }
        }
    }

    public void OnHeavyAttack(InputAction.CallbackContext context)
    {
        // When the button is pressed
        if (context.phase == InputActionPhase.Performed)
        {
            // If the player can Attack and is not dashing, cast a heavy attack
            if (canAttack && !isDashing)
            {
                SetAttackVariables("Heavy");
                Attack();
            }
        }
    }

    public void OnSpecial(InputAction.CallbackContext context)
    {
        // When the button is pressed
        if (context.phase == InputActionPhase.Performed)
        {
            // If the player has no special active, cast a special
            if (!specialActive) Special();
        }
    }

    public void OnDash(InputAction.CallbackContext context)
    {
        // When the button is pressed
        if (context.phase == InputActionPhase.Performed)
        {
            // If the player is not attacking and the dash is off cooldown, dash
            if (!isAttacking && canDash) Dash();
        }
    }

    public void OnInteract(InputAction.CallbackContext context)
    {
        // When the button is pressed
        if (context.phase == InputActionPhase.Performed)
        {
            Debug.Log("OnInteract was called");
        }
    }

    #endregion

    // End of Input

    #region STANCES

    private void DetermineStance()
    {
        // Initialise the last position in stances[]
        int lastPosition = stances.Length - 1;

        // Modify the position in the list according to the input
        stancePosition += 1;

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
        foreach (GameObject icon in stanceIcons) icon.SetActive(false);
        stanceIcons[stancePosition].SetActive(true);

        // Set the color of the weapon
        swordRenderer.material.color = stanceColors[stancePosition];

        // Check if there is a special going on. If not, then swap to the correct special stance icon
        if (!specialActive)
        {
            foreach (GameObject specialIcon in stanceSpecialIcons) specialIcon.SetActive(false);
            stanceSpecialIcons[stancePosition].SetActive(true);
        }
    }

    #endregion

    // End of Stances

    #region MOVEMENT

    private void Move()
    {
        // Calculate movement
        Vector3 movement = new Vector3(move.x, 0f, move.y);
        
        // Rotate the player
        if (canRotate) Rotate(movement);

        // If the player cannot move, make sure he stands still
        if (canMove)
        {
            // Move the player
            transform.Translate(movement * moveSpeed * Time.deltaTime, Space.World);

            // Set the correct animation
            animator.SetFloat("Speed", movement.magnitude);

            // If the player is not movingdo this to stop him from moving after collisions
            if (movement.magnitude == 0) rb.velocity = Vector3.zero;
        }
        else
        {
            rb.velocity = Vector3.zero;
        }
    }

    private void Rotate(Vector3 movement)
    {
        // First check if the player is moving to avoid snapping back to its default rotation upon being idle.
        if (movement.magnitude > 0)
        {
            // The rotation of the transform is equal to the direction the player is moving
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(movement), rotationSpeed);
        }
    }

    #endregion

    // End of Movement

    #region ATTACKING

    private void Attack()
    {
        // Reset certain coroutines and clear the damaged enemies list
        if (attackReset != null) StopCoroutine(attackReset);
        damagedEnemies.Clear();

        // The player cannot attack or move, but can rotate
        canMove = false;
        canRotate = true;
        canAttack = false;

        // The player is starting his attack. He can still rotate
        Invoke(nameof(AttackStart), attackChargeTime);

        // Reset canAttack after the duration of attackSpeed
        attackReset = StartCoroutine(AttackReset());
    }

    private void AttackStart()
    {
        // The player has started his attack and can no longer rotate
        canRotate = false;
        isAttacking = true;

        // Retrieve all hits and deal damage
        DealDamage();
    }

    private IEnumerator AttackReset()
    {
        // Wait for the attackSpeed
        yield return new WaitForSeconds(attackSpeed - movementDelay);

        // The player is done attacking and can attack again
        canAttack = true;
        isAttacking = false;

        // Allow chain attacks
        yield return new WaitForSeconds(movementDelay);

        // The player can move again
        canMove = true;
        canRotate = true;
    }

    private void DealDamage()
    {
        // Determine whether the player was lunging or not
        bool isLunge = false;
        if (isDashing) isLunge = true;

        // Retrieve all detected colliders
        Collider[] hits = Physics.OverlapBox(attackPoint.position, attackSize);

        foreach (Collider hit in hits)
        {
            GameObject hitObject = hit.gameObject;

            // Check if the hit is an enemy and it has not been damaged yet
            if (hitObject.CompareTag("Enemy") && !damagedEnemies.Contains(hitObject))
            {
                // Increment the combo counter
                AttackCombo();

                // Retrieve the health script on the enemy
                Health eHealth = hit.GetComponent<Health>();

                // Initialise the base attack damage of the player
                float damage = attackDamage;

                #region Stance Related Logic

                // If the player is in the Vampire stance, attacks inflicts bleed status
                if (currentStance == "Vampire") eHealth.Bleed(bleedDamage, bleedTicks, bleedIntervals, false);

                // If the player is in the Orc stance, every third attack deals double damage
                if (currentStance == "Orc" && (comboCounter % 3 == 0)) damage *= orcDamageMultiplier;

                // If the player is in the Ghost Stance, and the player used a lunge attack, it deals double damage
                if (currentStance == "Ghost" && isLunge) damage *= ghostDamageMultiplier;
                #endregion

                // Damage the enemy and add it to the list
                eHealth.Damage(damage);
                damagedEnemies.Add(hitObject);

                // Grant the player souls
                souls.GainSouls(soulsGain);
            }
        }
    }

    private void AttackCombo()
    {
        if (comboReset != null) StopCoroutine(comboReset);
        comboReset = StartCoroutine(ComboReset());

        comboCounter++;
    }

    private IEnumerator ComboReset()
    {
        yield return new WaitForSeconds(attackSpeed + movementDelay);

        comboCounter = 0;
    }

    private void SetAttackVariables(string attackType)
    {
        if (attackType == "Light")
        {
            // Set the right animation, attack size, and attackDuration variables
            animator.SetTrigger("Attack - Slash");
            attackSize = new Vector3(2, 2, 3);
            attackDamage = 10f;
            attackChargeTime = 0.3f;
            attackSpeed = 1f;
            movementDelay = 0.3f;
        }

        if (attackType == "Heavy")
        {
            // Set the right animation, attack size, and attackDuration variables
            animator.SetTrigger("Attack - Pierce");
            attackSize = new Vector3(1, 2, 4);
            attackDamage = 15f;
            attackChargeTime = 0.5f;
            attackSpeed = 1.5f;
            movementDelay = 0.5f;
        }
    }
    #endregion

    // End of Attacking

    #region SPECIAL

    private void Special()
    {
        // Retrieve soul charges for convenience
        int charges = souls.GetCharges();

        // Then spend all available soul charges
        souls.SpendSouls(charges * 20);

        // Check if the player is not attacking, dodging, and if he has more than 0 soul charges
        if (canAttack && !isDashing && charges > 0)
        {
            specialActive = true;

            // Determine which Special to cast
            if (currentStance == "Vampire") VampireSpecial(charges);
            if (currentStance == "Orc") OrcSpecial(charges);
            if (currentStance == "Ghost") GhostSpecial(charges);
        }
    }

    private void VampireSpecial(int charges)
    {
        // Local variables to keep track of the base amount of bleed ticks for the special
        float ticks = vampireSpecialTicks * charges;

        // Set up special timer
        SpecialTimerSetup(ticks);

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
                 * The damage is much smaller than a normal bleed, but the duration depends on charges 
                 * Two bleed ticks per charge. (max 10 ticks of 1 damage) */
                eHealth.Bleed(bleedDamage, ticks, 1f, true);
            }
        }

        // Regardless of enemies hit, call EndSpecial after the duration
        Invoke(nameof(SpecialEnd), ticks);
    }

    private IEnumerator VampireSpecialGFX()
    {
        playerCanvas.SetActive(true);

        // Some local variables to keep things cleaner
        RectTransform size = vampireSpecialGFX.rectTransform;
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
        // Set up variables
        float finalSpecialDuration = orcSpecialDuration * charges;

        // Set up special timer
        SpecialTimerSetup(finalSpecialDuration);

        // Become invulnerable to damage for the duration of the special
        invincibleCoroutine = StartCoroutine(Invincible(finalSpecialDuration));
        Invoke(nameof(SpecialEnd), finalSpecialDuration);
    }

    private void GhostSpecial(int charges)
    {
        // Set up special timer and start the effect
        SpecialTimerSetup(ghostSpecialDuration * charges);
        StartCoroutine(GhostSpecialEffect(charges));
    }

    private IEnumerator GhostSpecialEffect(int charges)
    {
        // Upon activation, make sure the dodge's cooldown is reset
        DashReset();

        // Reduce the dodge cooldown by 1 second for each charge and allow the player to pass through enemies
        dashCooldown -= 1 * charges;
        Physics.IgnoreLayerCollision(LayerMask.NameToLayer("Player"), LayerMask.NameToLayer("Enemy"), true);

        // This takes effect for as long as the duration * charges
        yield return new WaitForSeconds(ghostSpecialDuration * charges);

        // Revert effects
        dashCooldown += 1 * charges;
        Physics.IgnoreLayerCollision(LayerMask.NameToLayer("Player"), LayerMask.NameToLayer("Enemy"), false);

        // End the special
        SpecialEnd();
    }

    private void SpecialTimerSetup(float duration)
    {
        // Modify variables
        specialDuration = duration;
        specialCountdown = specialDuration;
        specialActive = true;

        // Set the right color for the text component
        Color color = stanceColors[stancePosition];
        specialDurationText.color = color;

        // Set the text active
        specialDurationText.gameObject.SetActive(true);
    }

    private void SpecialTimer(float duration)
    {
        specialCountdown -= Time.deltaTime;

        // Is the duration zero?
        if (specialCountdown <= 0)
        {
            // End the special
            SpecialEnd();
        }
        else
        {
            // Set the UI to accurate values
            specialDurationText.text = Mathf.RoundToInt(specialCountdown).ToString();
            specialDurationImage.fillAmount = 1 - (specialCountdown / duration);
        }
    }

    public void SpecialEnd()
    {
        // Reset variables
        specialDurationText.gameObject.SetActive(false);
        specialDurationImage.fillAmount = 1;
        specialActive = false;

        // Set the text inactive       
        specialDurationText.gameObject.SetActive(false);

        // Swap back to the correct special stance icon
        foreach (GameObject specialIcon in stanceSpecialIcons) specialIcon.SetActive(false);
        stanceSpecialIcons[stancePosition].SetActive(true);
    }

    #endregion

    // End of Special

    #region DASHING

    private void Dash()
    {
        // If the player is dashing out of an attack, the player overrides movement constraints and the animation
        if (canAttack && !isAttacking && !canMove && !canRotate)
        {
            animator.SetTrigger("Dash");

            canMove = true;
            canRotate = true;

            // Calculate movement
            Vector3 movement = new Vector3(move.x, 0f, move.y);
            transform.rotation = Quaternion.LookRotation(movement);
        }

        // Booleans
        canDash = false;
        isDashing = true;

        // Rigidbody Interpolation for better fluency while dashing, then add dashForce;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.AddForce(transform.forward * dashForce, ForceMode.Impulse);

        // Stop the dash after the dash duration to prevent leftover motion
        Invoke(nameof(DashEnd), dashDuration);

        // Make the player invulnerable for the dash duration, unless the player is already invincible
        if (invincibleCoroutine == null) StartCoroutine(Invincible(dashDuration));

        // Set up UI elements
        cooldownTextDash.gameObject.SetActive(true);
        dashCooldownTimer = dashCooldown;
    }

    private void DashCooldown(float cooldown)
    {
        // Count down
        dashCooldownTimer -= Time.deltaTime;

        // Is the cooldown zero?
        if (dashCooldownTimer <= 0)
        {
            // The player can dash again
            DashReset();
        }
        else
        {
            // Set the cooldownText accurate
            cooldownTextDash.text = Mathf.RoundToInt(dashCooldownTimer).ToString();
            cooldownImageDash.fillAmount = dashCooldownTimer / cooldown;
        }
    }
    private void DashEnd()
    {
        // Reset rigidBody interpolation and reset player velocity
        rb.interpolation = RigidbodyInterpolation.None;
        rb.velocity = Vector3.zero;

        // If the dash has ended while the player is attacking, DealDamage()
        if (isAttacking) DealDamage();

        // Reset booleans
        isDashing = false;
    }

    private void DashReset()
    {
        // Reset UI variables
        cooldownTextDash.gameObject.SetActive(false);
        cooldownImageDash.fillAmount = 0;

        // Reset the ability to dash
        canDash = true;
    }

    #endregion

    // End of Dashing

    #region Other

    private IEnumerator Invincible(float duration)
    {
        // Make the player invincible for the duration
        health.invincible = true;
        yield return new WaitForSeconds(duration);
        health.invincible = false;

        // If a coroutine is assigned, unassign it
        if (invincibleCoroutine != null) invincibleCoroutine = null;
    }

    public void Die()
    {
        animator.SetTrigger("Death");

        die?.Invoke();

        rb.velocity = Vector3.zero;
        Destroy(this);
    }

    #endregion

    //

    private void OnDrawGizmosSelected()
    {
        // For the attack
        Gizmos.color = Color.red;

        // Draw the cube using the attackPoint's position and rotation
        Gizmos.matrix = Matrix4x4.TRS(attackPoint.position, attackPoint.rotation, Vector3.one);
        Gizmos.DrawWireCube(Vector3.zero, attackSize);

        //For the vampire special
        //Gizmos.color = Color.red;
        //Gizmos.DrawWireSphere(transform.position, vampireSpecialRange);
    }

    #endregion

    // END OF EXECUTION
}
