using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using TMPro;
using UnityEngine.Events;

public class PlayerController : MonoBehaviour
{
    #region !SETUP!

    #region REFERENCES

    [Header("REFERENCES")]

    #region GameObjects

    [Header("References: GameObjects")]
    [SerializeField] private GameObject playerCanvas;
    [SerializeField] private Transform lightAttackPoint;
    [SerializeField] private Transform heavyAttackPoint;
    [SerializeField] private Animator animator;

    private PlayerResources resources;
    private Transform attackPoint;
    private Rigidbody rb;
    #endregion

    #region Audio Sources
    [Header("AUDIO")]
    [SerializeField] private AudioSource[] audioSources;
    [SerializeField] private AudioSource music;
    #endregion

    #endregion

    // End of References

    #region VARIABLES
    private Coroutine invincibleCoroutine;
    private int soulGain;

    private float audioOffset = 0;
    private float audioVolume = 0;
    #endregion 

    // End of Variables

    #region EVENTS

    [Header("EVENTS")]
    public UnityEvent die;
    #endregion

    // End of Events

    #region STANCES

    [Header("STANCES")]

    #region General

    [Header("General")]
    [SerializeField] private string[] stances;
    [SerializeField] private Renderer swordRenderer;

    private int stancePosition = 0;
    private string currentStance;
    #endregion

    #region UI

    [Header("UI")]
    [SerializeField] private GameObject[] stanceIcons;
    [SerializeField] private Color[] stanceColors;
    #endregion

    #region Vampire Stance

    [Header("Vampire Stance: Audio")]
    [SerializeField] private AudioClip vampireClip;
    [SerializeField] private float vampireAudioOffset;
    [SerializeField] private float vampireAudioVolume;

    [Header("Vampire Stance: Variables")]
    [SerializeField] private float bleedDamage;
    [SerializeField] private float bleedTicks;
    [SerializeField] private float bleedIntervals;

    [SerializeField] private Image vampireUltGFX;
    [SerializeField] private float vampireUltTicks;
    [SerializeField] private float vampireUltRange;
    #endregion

    #region Orc Stance

    [Header("Orc Stance: Audio")]
    [SerializeField] private AudioClip orcClip;
    [SerializeField] private float orcAudioOffset;
    [SerializeField] private float orcAudioVolume;

    [Header("Orc Stance: Variables")]
    [SerializeField] private GameObject shockwaveCanvas;
    [SerializeField] private float orcDamageMultiplier;
    [SerializeField] private float orcUltDuration;
    [SerializeField] private float shockwaveRange;
    [SerializeField] private float shockwaveModifier;
    private bool orcUltActive = false;
    #endregion

    #region Ghost Stance

    [Header("Ghost Stance: Audio")]
    [SerializeField] private AudioClip ghostClip;
    [SerializeField] private float ghostAudioOffset;
    [SerializeField] private float ghostAudioVolume;

    [Header("Ghost Stance: Variables")]
    [SerializeField] private float ghostDamageMultiplier;
    [SerializeField] private float ghostUltDuration;
    [SerializeField] private float ghostDashCooldown;
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

    [Header("ATTACKING")]
    [SerializeField] private AudioClip lightAttackClip;
    [SerializeField] private AudioClip heavyAttackClip;
    [SerializeField] private int lightAttackSoulGain;
    [SerializeField] private int heavyAttackSoulGain;

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

    #region ULTIMATE

    [Header("ULTIMATE")]

    #region User Interface
    [Header("User Interface")]
    [SerializeField] private GameObject[] stanceUltIcons;
    [SerializeField] private TextMeshProUGUI ultDurationText;
    [SerializeField] private Image ultDurationImage;
    #endregion

    #region Variables

    [HideInInspector] public bool ultActive;

    private float ultDuration;
    private float ultCountdown;
    #endregion

    #endregion

    // End of Ultimate

    #region DASHING

    [Header("DASHING")]

    #region User Interface

    [Header("User Interface")]
    [SerializeField] private TextMeshProUGUI cooldownTextDash;
    [SerializeField] private Image cooldownImageDash;
    #endregion

    #region Variables

    [Header("Variables")]
    [SerializeField] private AudioClip dashClip;
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
        resources = GetComponent<PlayerResources>();
        rb = GetComponent<Rigidbody>();

        // Set objects inactive
        playerCanvas.SetActive(false);

        // Make the cursor invisible
        Cursor.visible = false;
    }

    private void Start()
    {
        // Set the attackpoint for gizmos
        attackPoint = lightAttackPoint;

        // Set the cooldown text inactive and make sure the images value is correct
        cooldownTextDash.gameObject.SetActive(false);
        cooldownImageDash.fillAmount = 0;

        ultDurationText.gameObject.SetActive(false);
        ultDurationImage.fillAmount = 1f;

        // Swap to the correct stance at the beginning of the game
        SwapStance();
    }

    private void Update()
    {
        // Check if the player should move
        if (!isAttacking && !isDashing) Move();

        // If the player is performing a lunging attack, damage all enemies in his path (provided he does not collide and stuff)
        if (isAttacking && isDashing) DealDamage();

        // Handle dodge cooldown and special timer
        if (!canDash) DashCooldown(dashCooldown);
        if (ultActive) UltimateTimer(ultDuration);
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
            // If the player can Attacking and is not dashing, cast a light attack
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
            // If the player can Attacking and is not dashing, cast a heavy attack
            if (canAttack && !isDashing)
            {
                SetAttackVariables("Heavy");
                Attack();
            }
        }
    }

    public void OnUltimate(InputAction.CallbackContext context)
    {
        // When the button is pressed
        if (context.phase == InputActionPhase.Performed)
        {
            // If the player has no special active, cast a special
            if (!ultActive) Ultimate();
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
        if (!ultActive)
        {
            foreach (GameObject specialIcon in stanceUltIcons) specialIcon.SetActive(false);
            stanceUltIcons[stancePosition].SetActive(true);
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

        // Play the correct clip
        audioSources[0].Play();

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
        // Create this variable to handle double hits in Orc stance
        bool attackLanded = false;

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
                // Add the enemy to the list
                damagedEnemies.Add(hitObject);

                // Increment combo counter before the attack lands
                if (!attackLanded) AttackCombo();              

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

                // This code is only executed once per block
                if (!attackLanded)
                {
                    attackLanded = true;

                    // Grant the player souls
                    resources.GainSouls(soulGain);

                    // If the Orc Ult is active is active, attacks cause a small wave around the target that damages enemies
                    if (orcUltActive) OrcUltimateShockwave(hitObject, damage);
                }

                // Damage the enemy
                eHealth.Damage(damage);
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
            audioSources[0].clip = lightAttackClip;
            attackPoint = lightAttackPoint;
            attackSize = new Vector3(2f, 1f, 2f);
            soulGain = lightAttackSoulGain;
            attackDamage = 10f;
            attackChargeTime = 0.3f;
            attackSpeed = 1f;
            movementDelay = 0.3f;
        }

        if (attackType == "Heavy")
        {
            // Set the right animation, attack size, and attackDuration variables
            animator.SetTrigger("Attack - Pierce");
            audioSources[0].clip = heavyAttackClip;
            attackPoint = heavyAttackPoint;
            attackSize = new Vector3(1.5f, 1f, 3f);
            soulGain = heavyAttackSoulGain;
            attackDamage = 15f;
            attackChargeTime = 0.5f;
            attackSpeed = 1.5f;
            movementDelay = 0.5f;
        }
    }
    #endregion

    // End of Attacking

    #region ULTIMATE

    private void Ultimate()
    {
        // Get a reference to the audioSource that is used for special sound effects
        AudioSource audioSource = audioSources[1];

        // Check if the player is not attacking, dodging, and if he has maximum souls
        if (canAttack && !isDashing && resources.currentSouls == 100)
        {
            // Then spend all souls
            resources.SpendSouls();

            ultActive = true;

            // Determine which Ultimate to cast and assign corresponding audio
            if (currentStance == "Vampire")
            {
                audioOffset = vampireAudioOffset;
                audioVolume = vampireAudioVolume;
                audioSource.clip = vampireClip;
                VampireUltimate();
            }

            if (currentStance == "Orc")
            {
                audioOffset = orcAudioOffset;
                audioVolume = orcAudioVolume;
                audioSource.clip = orcClip;
                OrcUltimate();
            }

            if (currentStance == "Ghost")
            {
                audioOffset = ghostAudioOffset;
                audioVolume = ghostAudioVolume;
                audioSource.clip = ghostClip;
                GhostUltimate();
            }

            // Play the assigned audio clip
            audioSource.volume = audioVolume;
            audioSource.time = audioOffset;
            audioSource.Play();

            // Make the default music softer
            music.volume = 0.01f;
        }
    }

    private void VampireUltimate()
    {
        // Set up special timer
        UltimateTimerSetup(vampireUltTicks);

        // Trigger visuals
        StartCoroutine(VampireUltimateGFX());

        // Cast a big ass circle and collect all colliders
        Collider[] hits = Physics.OverlapSphere(transform.position, vampireUltRange);

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
                eHealth.Bleed(bleedDamage, vampireUltTicks, 1f, true);
            }
        }

        // Regardless of enemies hit, call EndSpecial after the duration
        Invoke(nameof(UltimateEnd), vampireUltTicks);
    }

    private IEnumerator VampireUltimateGFX()
    {
        playerCanvas.SetActive(true);

        // Some local variables to keep things cleaner
        RectTransform size = vampireUltGFX.rectTransform;
        Vector2 nativeSize = size.sizeDelta;
        Vector2 maxSize = new Vector2(vampireUltRange, vampireUltRange);
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

    private void OrcUltimate()
    {
        // Set up special timer
        UltimateTimerSetup(orcUltDuration);

        // Become invulnerable to damage for the duration of the special
        invincibleCoroutine = StartCoroutine(Invincible(orcUltDuration));

        // Your attacks cause a small area of effect around your target
        orcUltActive = true;

        // End the special
        Invoke(nameof(UltimateEnd), orcUltDuration);
    }

    private void OrcUltimateShockwave(GameObject target, float damage)
    {
        // Set up correct layer detection
        int enemyLayer = LayerMask.NameToLayer("Enemy");
        int layerMask = 1 << enemyLayer;

        // Retrieve the VFX origin and instantiate the VFX
        Transform origin = target.transform.Find("VFX Origin");
        StartCoroutine(OrcUltimateGFX(origin));

        // Store all enemies hit by the shockwave in a collider array
        Collider[] hits = Physics.OverlapSphere(target.transform.position, shockwaveRange, layerMask);

        foreach (Collider hit in hits)
        {
            // Ensure the shockwave doesn't affect the target itself
            if (hit.gameObject == target) continue;

            // Check for a health script and ensure the object is an enemy but not the original target
            Health eHealth = hit.GetComponent<Health>();

            // Damage the enemies by the shockwave
            if (eHealth != null && !damagedEnemies.Contains(hit.gameObject)) eHealth.Damage(damage / shockwaveModifier);
        }
    }

    private IEnumerator OrcUltimateGFX(Transform origin)
    {
        // Instantiate the VFX at the target location
        GameObject VFX = Instantiate(shockwaveCanvas, origin);

        // Wait a tiny while before dissappearing
        yield return new WaitForSeconds(1f);

        Destroy(VFX); 
    }

    private void GhostUltimate()
    {
        // Set up special timer and start the effect
        UltimateTimerSetup(ghostUltDuration);
        StartCoroutine(GhostUltimateEffect());
    }

    private IEnumerator GhostUltimateEffect()
    {
        // Upon activation, make sure the dodge's cooldown is reset
        DashReset();

        // Reduce dash cooldown and allow the player to pass through enemies
        dashCooldown -= ghostDashCooldown;
        Physics.IgnoreLayerCollision(LayerMask.NameToLayer("Player"), LayerMask.NameToLayer("Enemy"), true);

        // This takes effect for as long as the duration * charges
        yield return new WaitForSeconds(ghostUltDuration);

        // Revert effects
        dashCooldown += ghostDashCooldown;
        Physics.IgnoreLayerCollision(LayerMask.NameToLayer("Player"), LayerMask.NameToLayer("Enemy"), false);

        // End the special
        UltimateEnd();
    }

    private void UltimateTimerSetup(float duration)
    {
        // Modify variables
        ultDuration = duration;
        ultCountdown = ultDuration;
        ultActive = true;

        // Set the right color for the text component
        Color color = stanceColors[stancePosition];
        ultDurationText.color = color;

        // Set the text active
        ultDurationText.gameObject.SetActive(true);
    }

    private void UltimateTimer(float duration)
    {
        ultCountdown -= Time.deltaTime;

        // Is the duration zero?
        if (ultCountdown <= 0)
        {
            // End the special
            UltimateEnd();
        }
        else
        {
            // Set the UI to accurate values
            ultDurationText.text = Mathf.RoundToInt(ultCountdown).ToString();
            ultDurationImage.fillAmount = 1 - (ultCountdown / duration);
        }
    }

    public void UltimateEnd()
    {
        // Reset variables
        ultDurationText.gameObject.SetActive(false);
        ultDurationImage.fillAmount = 1;
        ultActive = false;

        // If the orc ult was active, it is now not
        if (orcUltActive) orcUltActive = false;

        // Set the text inactive       
        ultDurationText.gameObject.SetActive(false);

        // Swap back to the correct special stance icon
        foreach (GameObject ultIcon in stanceUltIcons) ultIcon.SetActive(false);
        stanceUltIcons[stancePosition].SetActive(true);

        // The audio fades out and normal audio fades in
        StartCoroutine(UltimateAudioFadeOut());
    }

    private IEnumerator UltimateAudioFadeOut()
    {
        // Get a reference to the audioSource that is used for special sound effects
        AudioSource audioSource = audioSources[1];

        // Local floats. Audio decreases relative to the specific volume
        float audioModification = audioSource.volume / 100;
        float audioDelay = 0.01f;

        // Gradually decrease the volume
        while (audioSource.volume > 0)
        {
            audioSource.volume -= audioModification;
            yield return new WaitForSeconds(audioDelay);
        }

        audioSource.Stop();

        // Make the default music louder
        while (music.volume < 0.1f)
        {
            music.volume += 0.001f;
            yield return new WaitForSeconds(audioDelay);
        }
    }

    #endregion

    // End of Ultimate

    #region DASHING

    private void Dash()
    {
        // Play the audio
        AudioSource audioSource = audioSources[2];
        audioSource.clip = dashClip;
        audioSource.time = 0.05f;
        audioSource.Play();

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
        // Reset rigidBody states and reset player velocity
        rb.interpolation = RigidbodyInterpolation.None;
        rb.velocity = Vector3.zero;

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
        resources.invincible = true;
        yield return new WaitForSeconds(duration);
        resources.invincible = false;

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

    //private void OnDrawGizmosSelected()
    //{
    //    if (attackPoint != null)
    //    {
    //        Gizmos.color = Color.red;
    //        Gizmos.matrix = Matrix4x4.TRS(attackPoint.position, attackPoint.rotation, Vector3.one);
    //        Gizmos.DrawWireCube(Vector3.zero, attackSize);

    //    }

    //    Gizmos.color = Color.green;
    //    Gizmos.DrawWireSphere(transform.position, shockwaveRange);

    //    Gizmos.color = Color.red;
    //    Gizmos.DrawWireSphere(transform.position, vampireUltRange);
    //}

    #endregion

    // END OF EXECUTION
}
