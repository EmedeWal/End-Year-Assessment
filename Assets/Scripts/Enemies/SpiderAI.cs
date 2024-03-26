using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class SpiderAI : MonoBehaviour
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
    [SerializeField] private Animator animator;
    [SerializeField] private LayerMask playerLayer;

    [HideInInspector] public Transform player;

    private PlayerMovement playerMovement;
    private NavMeshAgent agent;
    private Enemy enemy;

    [Header("Audio")]
    [SerializeField] private AudioClip lungeClip;
    [SerializeField] private AudioClip flurryClip;

    private AudioSource audioSource;
    #endregion

    // End of References

    #region VARIABLES

    [Header("Variables: Attack (Lunge)")]
    [SerializeField] private Transform lungeAttackPoint;
    [SerializeField] private Vector3 lungeAttackSize;
    [SerializeField] private float lungeAttackDamage;
    [SerializeField] private float lungeAttackChargeTime;
    [SerializeField] private float lungeAttackDuration;
    [SerializeField] private float lungeAttackCD;
    [SerializeField] private float lungeAttackRange;

    [Header("Variables: Attack (Flurry)")]
    [SerializeField] private Transform flurryAttackPoint;
    [SerializeField] private Vector3 flurryAttackSize;
    [SerializeField] private float flurryAttackDamage;
    [SerializeField] private float flurryAttackChargeTime;
    [SerializeField] private float flurryAttackDuration;
    [SerializeField] private float flurryAttackCD;
    [SerializeField] private float flurryAttackRange;

    private Transform attackPoint;
    private Vector3 attackSize;
    private float attackDamage;
    private float attackChargeTime;
    private float attackDuration;
    private float attackCD;
    private float attackRange;

    private int whichAttack = 1;
    private bool canAttack = true;

    [Header("Variables: Movement")]
    [SerializeField] private bool shouldIntercept;
    [SerializeField][Range(-1, 1)] public float MovementPredictionThreshold = 0;
    [SerializeField][Range(0.25f, 2f)] public float MovementPredictionTime = 1f;
    private Coroutine chaseCoroutine;

    [Header("Variables: Other")]
    [SerializeField] private float rotationSpeed;
    private bool active = false;

    private Vector3 expectedPosition;
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

        playerMovement = enemy.playerMovement;
        player = enemy.playerTransform;

        // Determine which attack the enemy should use to know how far it should chase the player
        DetermineAttack();

        // Activate the agent after a short delay
        Invoke("ActivateAgent", 1f);

        audioSource = GetComponent<AudioSource>();
    }

    private void Update()
    {
        if (active) UpdateBehaviour();
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
            if (chaseCoroutine != null) StopCoroutine(chaseCoroutine);
            RotateTowardsPlayer();
            return;
        }

        // If the player is in range, start to attack
        if (InRange())
        {
            if (chaseCoroutine != null) StopCoroutine(chaseCoroutine);
            currentState = EnemyState.Attacking;
            return;
        }

        // The enemy moves towards the player
        if (chaseCoroutine != null) StopCoroutine(chaseCoroutine);
        chaseCoroutine = StartCoroutine(MoveToPlayer());

        animator.SetFloat("Speed", agent.velocity.magnitude);
    }

    private IEnumerator MoveToPlayer()
    {
        WaitForSeconds repathingDelay = new WaitForSeconds(0.15f);

        float timeToPlayer = Vector3.Distance(player.position, transform.position) / agent.speed;

        while (true)
        {
            // Check if the enemy should intercept the player and act accordingly
            if (!shouldIntercept)
            {
                agent.SetDestination(player.position);
            }
            else
            {
                if (timeToPlayer > MovementPredictionTime)
                {
                    timeToPlayer = MovementPredictionTime;
                }

                Vector3 targetPosition = player.position + playerMovement.AverageVelocity * timeToPlayer;
                Vector3 directionToTarget = (targetPosition - transform.position).normalized;
                Vector3 directionToPlayer = (player.transform.position - transform.position).normalized;

                // Check if the player is close enough for a direct approach
                float dot = Vector3.Dot(directionToPlayer, directionToTarget);

                if (dot < MovementPredictionThreshold)
                {
                    targetPosition = player.position;
                }

                expectedPosition = targetPosition;

                agent.SetDestination(targetPosition);
            }

            yield return repathingDelay;
        }
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

        // Play the correct animation and audio
        if (whichAttack == 0)
        {
            audioSource.clip = lungeClip;
            audioSource.Play();

            animator.SetTrigger("Attack (Lunge)");
        }
        else
        {
            audioSource.clip = flurryClip;
            audioSource.Play();

            animator.SetTrigger("Attack (Flurry)");
        }

        CancelMovement();

        // Start charging the attack
        Invoke(nameof(AttackStart), attackChargeTime);
    }

    private void AttackStart()
    {
        // Lock enemy rotation and position
        currentState = EnemyState.Attacking;

        // Use the correct attack logic
        if (whichAttack == 0) LungeAttack();
        else StartCoroutine(FlurryAttack());

        // Start recovery of the attack, after the attackduration
        Invoke(nameof(StartRecovery), attackDuration);

        // Reset attack CD
        StartCoroutine(AttackReset());
    }

    private IEnumerator AttackReset()
    {
        // Wait for the spellCD to attack again
        yield return new WaitForSeconds(attackCD);

        // Booleans
        canAttack = true;
    }

    private void StartRecovery()
    {
        currentState = EnemyState.Chasing;

        DetermineAttack();
    }

    private void LungeAttack()
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

    private IEnumerator FlurryAttack()
    {
        // Perform an attack four times
        for (int i = 0; i < 4; i++)
        {
            // Check if something was hit
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

            // Wait a tiny bit for the next attack in the flurry
            yield return new WaitForSeconds(0.3f);
        }
    }

    private void DetermineAttack()
    {
        // Rol a random attack by random numbers: 0 or 1, then decide which block of code to execute based on the roll
        whichAttack = Random.Range(0, 2);

        // Based upon the roll, set all variables
        if (whichAttack == 0)
        {
            attackPoint = lungeAttackPoint; 
            attackSize = lungeAttackSize;
            attackDamage = lungeAttackDamage;
            attackChargeTime = lungeAttackChargeTime;
            attackDuration = lungeAttackDuration;
            attackCD = lungeAttackCD;
            attackRange = lungeAttackRange;
        }
        else
        {
            attackPoint = flurryAttackPoint;
            attackSize = flurryAttackSize;
            attackDamage = flurryAttackDamage;
            attackChargeTime = flurryAttackChargeTime;
            attackDuration = flurryAttackDuration;
            attackCD = flurryAttackCD;
            attackRange = flurryAttackRange;
        }
    }
    #endregion

    #endregion

    // End of States

    #region OTHER

    private void ActivateAgent()
    {
        active = true;
    }

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

        // Play idle animation
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

    //private void OnDrawGizmosSelected()
    //{
    //    Gizmos.color = Color.blue;
    //    Gizmos.matrix = Matrix4x4.TRS(meleePoint.position, meleePoint.rotation, Vector3.one);
    //    Gizmos.DrawWireCube(Vector3.zero, attackSize);
    //}

    //void OnDrawGizmos()
    //{
    //    if (agent != null && agent.isActiveAndEnabled)
    //    {
    //        // Draw a line from the enemy to the predicted player position
    //        Gizmos.color = Color.yellow;
    //        Gizmos.DrawLine(transform.position, expectedPosition);

    //        // Draw a sphere at the predicted player position
    //        Gizmos.color = Color.red;
    //        Gizmos.DrawSphere(expectedPosition, 0.5f);

    //        // Optionally, draw a line from the enemy to its current destination
    //        Gizmos.color = Color.green;
    //        Gizmos.DrawLine(transform.position, agent.destination);
    //    }
    //}
}
