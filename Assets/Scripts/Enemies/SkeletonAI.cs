using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class SkeletonAI : MonoBehaviour
{
    #region !SETUP!

    #region ENUM

    public enum EnemyState
    {
        Chasing,
        Charging,
        Attacking,
    }

    public EnemyState currentState = EnemyState.Chasing;
    #endregion

    // End of Enum

    #region REFERENCES

    [Header("References")]
    [SerializeField] private Transform attackPoint;
    [SerializeField] private Animator animator;
    [SerializeField] private LayerMask playerLayer;

    [HideInInspector] public Transform player;

    private NavMeshAgent agent;
    private Enemy enemy;
    #endregion

    // End of References

    #region VARIABLES

    [Header("Variables: Attacking")]
    [SerializeField] private Vector3 attackSize;
    [SerializeField] private float attackDamage;
    [SerializeField] private float attackChargeTime;
    [SerializeField] private float attackDuration;
    [SerializeField] private float attackRange;
    [SerializeField] private float attackCD;

    private bool canAttack = true;

    [Header("Variables: Other")]
    [SerializeField] private float rotationSpeed;
    [SerializeField] private float rotationModifier;
    [SerializeField] private float deathDelay;
    #endregion

    // End of Attacking

    #endregion

    // END OF SETUP

    #region !EXECUTION!

    #region DEFAULT

    private void Start()
    {
        // Set up all references
        agent = GetComponent<NavMeshAgent>();
        enemy = GetComponent<Enemy>();

        player = enemy.playerTransform;
    }

    private void Update()
    {
        UpdateBehaviour();
    }

    #endregion 

    // End of Default

    #region BEHAVIOUR

    private void UpdateBehaviour()
    {
        switch (currentState)
        {
            case EnemyState.Chasing:
                Chase();
                break;

            case EnemyState.Charging:
                RotateTowardsPlayer();
                break;

            case EnemyState.Attacking:
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

    // End of Behavior

    #region STATES

    #region Chasing

    private void Chase()
    {
        // If the player is recharging from an attack, only rotate towards the player but do not follow him
        if (!canAttack)
        {
            RotateTowardsPlayer();
            return;
        }

        // If the player is in range, start to attack
        if (InRange())
        {
            currentState = EnemyState.Attacking;
            return;
        }

        // The enemy moves towards the player
        agent.SetDestination(player.position);
        animator.SetFloat("Speed", agent.velocity.magnitude);
    }
    #endregion

    #region Charging

    private void RotateTowardsPlayer()
    {
        // Rotate towards the player
        Vector3 direction = (player.position - transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * rotationSpeed);
    }
    #endregion

    #region Attacking

    private void Attack()
    {
        if (!canAttack) return;

        // Enemy rotates towards the player while charging
        currentState = EnemyState.Charging;
        canAttack = false;

        CancelMovement();

        animator.SetTrigger("Attack");

        // Make the enemy rotate faster to give more tracking
        rotationSpeed *= rotationModifier;

        // Start charging the attack
        Invoke(nameof(AttackStart), attackChargeTime);
    }

    private void AttackStart()
    {
        // Lock enemy rotation and position
        currentState = EnemyState.Attacking;

        DealDamage();

        // Start recovery of the attack, after the attackduration
        Invoke(nameof(StartRecovery), attackDuration);

        // Reset to the default rotation speed 
        rotationSpeed /= rotationModifier;
    }

    private IEnumerator AttackReset()
    {
        // Wait for the attackCD to attack again
        yield return new WaitForSeconds(attackCD);

        // Booleans
        canAttack = true;
    }

    private void StartRecovery()
    {
        currentState = EnemyState.Chasing;
        StartCoroutine(AttackReset());
    }

    private void DealDamage()
    {
        // Retrieve all detected colliders
        Collider[] hits = Physics.OverlapBox(attackPoint.position, attackSize);

        foreach (Collider hit in hits)
        {
            GameObject hitObject = hit.gameObject;

            // Check if the hit is an enemy and it has not been damaged yet
            if (hitObject.CompareTag("Player"))
            {
                // Retrieve the health script on the player and damage him
                PlayerResources pHealth = hitObject.GetComponent<PlayerResources>();
                pHealth.Damage(attackDamage);
            }
        }
    }
    #endregion

    #endregion

    // End of States

    #region OTHER

    private void CancelMovement()
    {
        agent.SetDestination(transform.position);
        animator.SetFloat("Speed", 0f);
    }
    #endregion

    // End of Other

    #region Events

    public void Stagger()
    {
        // Reset the enemy's attack CD, but make sure to reset it again later
        StopAllCoroutines();
        StartCoroutine(AttackReset());
        canAttack = false;

        // Animation
        animator.SetTrigger("Stagger");

        // If the enemy is interrupted while charing his attack, execute the following the code
        if (currentState == EnemyState.Charging)
        {
            // Cancel the attack start
            CancelInvoke(nameof(AttackStart));

            // Start recovery of the attack, after the attackduration
            Invoke(nameof(StartRecovery), attackDuration);

            // Reset to the default rotation speed 
            rotationSpeed /= rotationModifier;
        }

        // If the enemy is hit while moving, stop him in his tracks
        else if (currentState == EnemyState.Chasing) CancelMovement();
    }

    public void Death()
    {
        // Play the animation and remove enemy intelligence
        animator.SetTrigger("Death");
        agent.SetDestination(transform.position);
        enemy.Die();
        Destroy(this);
    }

    #endregion

    // End of Events

    #endregion

    // END OF EXECUTION

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        Gizmos.matrix = Matrix4x4.TRS(attackPoint.position, attackPoint.rotation, Vector3.one);
        Gizmos.DrawWireCube(Vector3.zero, attackSize);
    }
}

