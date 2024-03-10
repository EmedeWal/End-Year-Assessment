using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;

    [Header("Variables")]
    [SerializeField] private float moveSpeed;
    [SerializeField] private float rotationSpeed;

    private Vector2 move;
    public bool canMove = true;

    public float attackSpeed;
    public float attackOffset;
    public bool hasAttacked;

    private void Update()
    {
        if (canMove)
        MovePlayer();
    }

    #region Input

    public void OnMove(InputAction.CallbackContext context)
    {
        move = context.ReadValue<Vector2>();
    }

    public void OnAttack()
    {
        Attack();
    }

    #endregion



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

    private void Attack()
    {
        if (!hasAttacked)
        {
            hasAttacked = true;
            canMove = false;
            animator.SetTrigger("Attack");
            StartCoroutine(ResetAttack());
        }   
    }

    private IEnumerator ResetAttack()
    {
        // Wait for the attackSpeed of the player, before the player can attack again
        yield return new WaitForSeconds(attackSpeed - attackOffset);
        hasAttacked = false;

        // Wait a little longer before making the player move again, so that animations do not trigger double and the player can "chain attack"
        yield return new WaitForSeconds(attackOffset);
        if (!hasAttacked) canMove = true;
    }

    #endregion


}
