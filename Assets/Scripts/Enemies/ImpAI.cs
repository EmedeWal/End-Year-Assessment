using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class ImpAI : MonoBehaviour
{
    #region !SETUP!

    #region ENUM

    public enum EnemyState
    {
        Chasing,
        Charging,
        Attacking,
        Retreating
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
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private float attackDamage;
    [SerializeField] private float attackChargeTime;
    [SerializeField] private float attackDuration;
    [SerializeField] private float attackRange;
    [SerializeField] private float attackCD;
    [SerializeField] private float attackForce;

    private bool canAttack = true;

    [Header("Variables: Retreating")]
    [SerializeField] private float retreatDistance;
    [SerializeField] private float safeDistance;
    private Vector3 retreatTargetPosition;
    private bool isRetreating;

    [Header("Variables: Other")]
    [SerializeField] private float rotationSpeed;
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

            case EnemyState.Retreating:
                RetreatToNewPosition();
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

        // Start charging the attack
        Invoke(nameof(AttackStart), attackChargeTime);
    }

    private void AttackStart()
    {
        // Lock enemy rotation and position
        currentState = EnemyState.Attacking;

        ShootProjectile();

        // Start recovery of the attack, after the attackduration
        Invoke(nameof(StartRecovery), attackDuration);
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
        currentState = EnemyState.Retreating;
        StartCoroutine(AttackReset());
    }

    private void ShootProjectile()
    {
        // Shoot a projectile (enemySpawner is its parent), set its position, set the damage and apply a force
        GameObject projectile = Instantiate(projectilePrefab, attackPoint.position, attackPoint.rotation);
        projectile.GetComponent<ImpProjectile>().SetDamage(attackDamage);
        projectile.GetComponent<Rigidbody>().AddForce(transform.forward * attackForce * 100, ForceMode.Force);
    }
    #endregion

    #region Retreating
    private void RetreatToNewPosition()
    {
        animator.SetFloat("Speed", agent.velocity.magnitude);
    
        // If the enemy is retreating, check if it has reached its destination. If so, cancel the retreat
        if (isRetreating && (Vector3.Distance(transform.position, retreatTargetPosition) <= 0.1f))
        {
            CancelRetreat();
            return;
        }

        // Chose a valid retreat position
        if (!isRetreating && (Vector3.Distance(transform.position, player.position) <= safeDistance))
        {
            isRetreating = true;
            bool validPositionFound = false;
            int maxAttempts = 10; 
            Vector3 potentialRetreatPosition = Vector3.zero;
            NavMeshHit hit;

            for (int i = 0; i < maxAttempts && !validPositionFound; i++)
            {
                // Try to find a valid retreat position not close to walls
                Vector3 retreatDirection = (transform.position - player.position).normalized + Random.insideUnitSphere * 0.3f; // Add randomness to avoid patterns
                potentialRetreatPosition = transform.position + retreatDirection.normalized * retreatDistance;

                if (NavMesh.SamplePosition(potentialRetreatPosition, out hit, retreatDistance, NavMesh.AllAreas))
                {
                    // Check for walls with a BoxCast
                    Collider[] colliders = Physics.OverlapBox(hit.position, new Vector3(1f, 1f, 1f), Quaternion.identity, LayerMask.GetMask("Terrain"));

                    if (colliders.Length == 0) // No walls detected
                    {
                        validPositionFound = true;
                        retreatTargetPosition = hit.position;
                        agent.SetDestination(hit.position);
                    }
                }
            }

            if (!validPositionFound)
            {
                // No valid positions found. Cancel the retreat action
                CancelRetreat();
            }
        }

        // If the player never entered the range of the enemy and the enemy can attack again, start to chase the player
        if (!isRetreating && canAttack) currentState = EnemyState.Chasing;
    }

    private void CancelRetreat()
    {
        isRetreating = false;

        agent.SetDestination(transform.position);

        // The enemy is done retreating and should rotate until the faces the player, before starting the chase
        currentState = EnemyState.Charging;

        Invoke(nameof(StartChase), 1f);    
    }

    private void StartChase()
    {
        currentState = EnemyState.Chasing;
    }

    #endregion

    #region Other

    private void CancelMovement()
    {
        agent.SetDestination(transform.position);
        animator.SetFloat("Speed", 0f);
    }
    #endregion

    #endregion

    // End of States

    #region EVENTS

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
        }

        // If the enemy is hit while moving, stop him in his tracks
        else if (currentState == EnemyState.Chasing) CancelMovement();

        // If the enemy is retreating
        else if (currentState == EnemyState.Retreating)
        {
            CancelInvoke(nameof(StartChase));
            CancelRetreat();
        }
    }

    public void Death()
    {
        // Play the animation and remove enemy intelligence
        agent.SetDestination(transform.position);
        animator.SetTrigger("Death");
        enemy.Die();
        Destroy(this);
    }

    #endregion

    //End of Events

    #endregion

    // END OF EXECUTION
}