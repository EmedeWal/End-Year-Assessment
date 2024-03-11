using System.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    #region References

    [Header("References")]
    [SerializeField] private Transform attackPoint;
    [SerializeField] private Animator animator;

    private Rigidbody rb;
    private Health health;
    private Souls souls;

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

    #region Dodging

    [Header("Dodging")]
    [SerializeField] private float dodgeForce;
    [SerializeField] private float dodgeCooldown;
    [SerializeField] private float dodgeDuration;

    private bool canDodge = true;

    #endregion

    // 

    #region Attacking

    [Header("Attack Variables")]
    [SerializeField] private float attackDamage;
    [SerializeField] private float attackOffset;
    [SerializeField] private float attackSpeed;
    [SerializeField] private float attackRange;
    [SerializeField] private float damageDelay;

    private bool canAttack;

    #endregion

    //

    #region Stances

    [Header("Stance: General")]
    [SerializeField] private string[] stances;
    [SerializeField] private GameObject[] stanceIcons;

    public int stancePosition = 0;
    private string currentStance;

    [Header("Stance: Vampire")]
    [SerializeField] private float bleedBaseDamage;

    [Header("Stance: Orc")]
    [SerializeField] private float knockBackBaseDamage;
    [SerializeField] private float knockBackForce;

    #endregion

    //

    #region Souls

    [Header("Souls")]
    [SerializeField] private int soulsGain;

    #endregion

    //

    #region Default

    private void Awake()
    {
        // Retrieve components
        rb = GetComponent<Rigidbody>();
        health = GetComponent<Health>();
        souls = GetComponent<Souls>();  
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
        // Move the player, if he is allowed to
        if (canMove) Move();
    }

    #endregion

    //

    #region Input

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
        Debug.Log("OnSpecial was called");
    }

    public void OnDodge()
    {
        Dodge();
    }

    public void OnInteract()
    {
        Debug.Log("OnInteract was called");
    }

    public void OnSwapStance(InputAction.CallbackContext context)
    {
        // Check if the input was triggered
        if (context.performed)
        {
            // Get the value of the axis input and enter it is a parameter
            DetermineStance(context.ReadValue<float>());
        }
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
            
            // Add the dodge force
            rb.AddForce(transform.forward * dodgeForce, ForceMode.Impulse);

            // Reset the dodge ability depending on the dodge cooldown
            Invoke(nameof(ResetDodge), dodgeCooldown);

            // Stop the dodge after the dodge duration
            Invoke(nameof(EndDodge), dodgeDuration);

            // Make the player invulnerable for the dodge duration
            StartCoroutine(Invulnerability(dodgeDuration));
        }
    }

    private void ResetDodge()
    {
        canDodge = true;
    }

    private void EndDodge()
    {
        rb.velocity = Vector3.zero;
    }

    #endregion

    //

    #region Attacking

    private void Attack()
    {
        if (!canAttack)
        {
            canAttack = true;
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
        canAttack = false;

        /* Wait a little longer before making the player move again, 
         * so that animations do not trigger double and the player can "chain attack" */
        yield return new WaitForSeconds(attackOffset);
        if (!canAttack) canMove = true;
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
                Health health = hit.GetComponent<Health>();

                // Check for active stances, and handle additional logic accordingly

                // Check if the vampire stance is active. If so, inflict bleed upon the enemy
                if (currentStance == "Vampire") health.Bleed(bleedBaseDamage, souls.GetCharges());

                // Check if the orc stance is active. If so, knock the enemy backwards
                if (currentStance == "Orc") health.KnockBack(knockBackBaseDamage, knockBackForce);

                // Apply damage to the enemy and grant the player souls
                health.Damage(attackDamage);
                souls.GainSouls(soulsGain);
            }
        }
    }

    #endregion

    // 

    #region Other

    private IEnumerator Invulnerability(float duration)
    {
        health.invulnerable = true;

        yield return new WaitForSeconds(duration);

        health.invulnerable = false;
    }

    #endregion

    //

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, attackRange);
    }
}
