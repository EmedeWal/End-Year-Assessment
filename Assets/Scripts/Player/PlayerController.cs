using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform attackPoint;
    [SerializeField] private Animator animator;

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

    private bool canAttack;

    #endregion

    //

    #region Stances

    [Header("Stance Related")]
    [SerializeField] private string[] stances;
    [SerializeField] private string currentStance;

    private int stancePosition = 0;

    #endregion

    //

    #region Default

    private void Start()
    {
        currentStance = stances[stancePosition];
    }

    private void Update()
    {
        if (canMove)
        MovePlayer();
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
        Debug.Log("OnDodge was called");
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

    #region Functionality

    private void MovePlayer()
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

        // Update the currentStance accordingly
        currentStance = stances[stancePosition];
    }

    private void Attack()
    {
        if (!canAttack)
        {
            canAttack = true;
            canMove = false;

            animator.SetTrigger("Attack");

            StartCoroutine(ApplyDamage());
            StartCoroutine(ResetAttack());
        }   
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
                // Retrieve the health script on the enemy and damage it
                Health health = hit.GetComponent<Health>();
                health.Damage(attackDamage);
            }
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

    #endregion

    //

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, attackRange);
    }
}
