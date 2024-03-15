using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class ImpAI : MonoBehaviour
{
    #region References

    [Header("References")]
    [SerializeField] private Transform attackPoint;
    [SerializeField] private Animator animator;
    [SerializeField] private LayerMask playerLayer;

    [HideInInspector] public Transform player;

    private NavMeshAgent agent;
    private Enemy enemy;

    #endregion

    //

    #region Enum

    public enum State
    {
        Idle,
        Chase,
        Attack,
        Retreat
    }

    public State state = State.Chase;

    #endregion

    //

    #region Variables

    [Header("Variables: Attacking")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private float attackDuration;
    [SerializeField] private float attackDamage;
    [SerializeField] private float attackRange;
    [SerializeField] private float attackForce;
    [SerializeField] private float attackCD;

    [Header("Variables: Other")]
    [SerializeField] private float safeDistance;
    [SerializeField] private float rotationSpeed;

    private Coroutine attackReset;
    private bool canAttack = true;
    private bool isAttacking;

    private bool hasRetreated;
    private bool isRetreating;

    #endregion

    //

    #region Default

    private void Start()
    {
        // Set up all references
        agent = GetComponent<NavMeshAgent>();
        enemy = GetComponent<Enemy>();

        player = enemy.playerTransform;
    }

    private void Update()
    {
        // If the enemy isRetreating, check whether it has reached its destination
        if (isRetreating)
        {
            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
            {
                if (!agent.hasPath || agent.velocity.sqrMagnitude == 0f)
                {
                    StartCoroutine(TurnToPlayer()); 
                }
            }
        }

        // If the enemy is attacking or retreating, return
        if (isAttacking || isRetreating) return;

        // Set animations
        animator.SetFloat("Speed", agent.velocity.magnitude / agent.speed);

        // Store the result of the distance check in a variable
        float distance = DistanceToPlayer();

        // Check if the player is inRange.
        bool inRange = distance <= attackRange;

        // If the player is very close, the enemy should run away and reposition
        if (distance <= safeDistance && !hasRetreated) UpdateBehaviour(State.Retreat);

        // If the player is in the attackRange and if the enemy can attack
        else if (inRange && canAttack) UpdateBehaviour(State.Attack);

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

            case State.Retreat:
                Retreat();
                break;
        }
    }

    private float DistanceToPlayer()
    {
        // Calculate the distance to the player
        return Vector3.Distance(player.position, transform.position);
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

        StartCoroutine(ShootProjectile());
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

    private IEnumerator ShootProjectile()
    {
        yield return new WaitForSeconds(attackDuration / 2);

        // Shoot a projectile (enemySpawner is its parent), set its position, set the damage and apply a force
        GameObject projectile = Instantiate(projectilePrefab, enemy.spawner.transform);
        projectile.transform.position = attackPoint.position;
        projectile.GetComponent<ImpProjectile>().SetDamage(attackDamage);
        projectile.GetComponent<Rigidbody>().AddForce(transform.forward * attackForce * 100, ForceMode.Force);
    }

    #endregion

    //

    private void Retreat()
    {
        Debug.Log("Retreat has been called");

        isRetreating = true;
        hasRetreated = true;

        Vector3 retreatDirection = transform.position - player.position;
        Vector3 retreatPosition = transform.position + retreatDirection.normalized * Random.Range(safeDistance, safeDistance * 2);

        NavMeshHit hit;
        if (NavMesh.SamplePosition(retreatPosition, out hit, safeDistance * 2, NavMesh.AllAreas)) agent.SetDestination(hit.position);

        // Cooldown before next possible retreat
        Invoke(nameof(ResetRetreat), 5f);
    }

    private void ResetRetreat()
    {
        hasRetreated = false;
    }

    private IEnumerator TurnToPlayer()
    {
        UpdateBehaviour(State.Idle);

        yield return new WaitForSeconds(1);

        isRetreating = false;
    }

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

        // MAke sure the enemy stops moving
        agent.SetDestination(transform.position);

        enemy.Die();

        Destroy(this);
    }

    #endregion

    //
}