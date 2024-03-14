using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class SpiderAI : MonoBehaviour
{
    #region References

    [Header("References")]
    [SerializeField] private Transform attackPoint;
    [SerializeField] private Animator animator;
    [SerializeField] private LayerMask playerLayer;

    [HideInInspector] public Transform player;

    private References references;
    private NavMeshAgent agent;
    private Rigidbody rb;

    #endregion

    //

    #region Enum

    public enum State
    {
        Idle,
        Chase,
        Attack
    }

    public State state = State.Chase;

    #endregion

    //

    #region Variables

    [Header("Variables")]
    [SerializeField] private Vector3 attackSize;
    [SerializeField] private float attackDuration;
    [SerializeField] private float attackDamage;
    [SerializeField] private float attackRange;
    [SerializeField] private float attackCD;

    [SerializeField] private float rotationSpeed;

    private Coroutine attackReset;
    private bool canAttack = true;
    private bool isAttacking;

    #endregion

    //

    #region Default

    private void Start()
    {
        // Set up all references
        references = GetComponent<References>();
        agent = GetComponent<NavMeshAgent>();
        rb = GetComponent<Rigidbody>();

        player = references.playerTransform;
    }

    private void Update()
    {
        // Set animations
        animator.SetFloat("Speed", agent.velocity.magnitude / agent.speed);

        // Store the result of the range check
        bool inRange = InRange();

        // If the enemy is attacking, stop checking stuff
        if (isAttacking) return;

        // If the player is in the attackRange and if the enemy can attack
        if (inRange && canAttack) UpdateBehaviour(State.Attack);

        // If the player is in the attackRange, the enemy should idle
        else if (inRange) UpdateBehaviour(State.Idle);

        // The enemy should move towards the player
        else UpdateBehaviour(State.Chase);
    }

    #endregion 

    //

    #region Behavior

    private void UpdateBehaviour(State newState)
    {
        // Determine which state to swap to
        state = newState;

        switch (state)
        {
            case State.Idle:
                Idle();
                break;

            case State.Chase:
                Chase();
                break;

            case State.Attack:
                Attack();
                break;
        }
    }

    private bool InRange()
    {
        // Calculate the distance to the player and check if the player is in the attack range
        if (Vector3.Distance(player.position, transform.position) <= attackRange) return true;
        else return false;
    }

    #endregion

    //

    #region States

    private void Idle()
    {
        // Make sure the enemy is not moving, but rotating towards the player
        agent.SetDestination(transform.position);

        // Determine the target rotation
        Quaternion targetRotation = Quaternion.LookRotation(player.position - transform.position);

        // Interpolate the enemy's rotation towards the target rotation
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
    }

    private void Chase()
    {
        // make sure the enemy moves towards the player
        agent.SetDestination(player.position);
    }

    #region Attacking

    private void Attack()
    {
        canAttack = false;
        isAttacking = true;

        // Make the sure the enemy stands still
        agent.SetDestination(transform.position);

        // Play the animation
        animator.SetTrigger("Attack");

        // Reset the attack after the cooldown has passed
        attackReset = StartCoroutine(AttackReset());

        // Calculate when the enemy is done attacking
        StartCoroutine(AttackTracking());

        // Apply the damage
        StartCoroutine(ApplyDamage());
    }

    private IEnumerator AttackReset()
    {
        yield return new WaitForSeconds(attackCD);
        canAttack = true;
    }

    private IEnumerator AttackTracking()
    {
        yield return new WaitForSeconds(attackDuration);
        isAttacking = false;
    }

    private IEnumerator ApplyDamage()
    {
        // Apply the damage halfway through the animation
        yield return new WaitForSeconds(attackDuration / 2);

        Collider[] hits = Physics.OverlapBox(attackPoint.position, attackSize);

        foreach (Collider hit in hits)
        {
            if (hit.gameObject.CompareTag("Player"))
            {
                Health pHealth = hit.GetComponent<Health>();
                pHealth.Damage(attackDamage);
            }
        }
    }

    #endregion

    //

    #endregion

    //

    #region Events

    public void Stagger()
    {
        // Reset AttackReset()
        if (attackReset != null && !isAttacking)
        {
            animator.SetTrigger("Stagger");

            StopCoroutine(attackReset);
            attackReset = StartCoroutine(AttackReset());
        }
    }

    public void Death()
    {
        // Play the animation and remove enemy intelligence
        animator.SetTrigger("Death");
        Destroy(this);
    }

    #endregion

    //

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        Gizmos.matrix = Matrix4x4.TRS(attackPoint.position, attackPoint.rotation, Vector3.one);
        Gizmos.DrawWireCube(Vector3.zero, attackSize);
    }
}
