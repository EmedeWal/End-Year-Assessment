using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class OrcAI : MonoBehaviour
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

    private AudioSource audioSource;

    private PlayerMovement playerMovement;
    private NavMeshAgent agent;
    private Enemy enemy;
    #endregion

    // End of References

    #region VARIABLES

    [Header("Variables: Attack (Slash)")]
    [SerializeField] private Vector3 slashSize;
    [SerializeField] private float slashDamage;
    [SerializeField] private float slashChargeTime;
    [SerializeField] private float slashDuration;
    [SerializeField] private float slashCD;
    [SerializeField] private float slashRange;

    [Header("Variables: Attack (Double Slash)")]
    [SerializeField] private Vector3 doubleSlashSize;
    [SerializeField] private float doubleSlashDamage;
    [SerializeField] private float doubleSlashChargeTime;
    [SerializeField] private float doubleSlashDuration;
    [SerializeField] private float doubleSlashCD;
    [SerializeField] private float doubleSlashRange;

    [Header("Variables: Attack (Spin")]
    [SerializeField] private float spinRadius;
    [SerializeField] private float spinDamage;
    [SerializeField] private float spinChargeTime;
    [SerializeField] private float spinDuration;
    [SerializeField] private float spinCD;
    [SerializeField] private float spinRange;

    [Header("Variables: Attacking")]
    [SerializeField] private Transform attackPoint;
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

    private void Awake()
    {
        // Randomize interception behaviors
        shouldIntercept = Random.value > 0.5;
    }

    private void Start()
    {
        // Set up all references
        agent = GetComponent<NavMeshAgent>();
        enemy = GetComponent<Enemy>();

        playerMovement = enemy.playerMovement;
        player = enemy.playerTransform;

        audioSource = GetComponent<AudioSource>();

        // Determine which attack the enemy should use to know how far it should chase the player
        DetermineAttack();

        // Activate the agent after a short delay
        Invoke("ActivateAgent", 1f);
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

        // Play the correct animation
        if (whichAttack == 0) animator.SetTrigger("Attack (Slash)");
        else if (whichAttack == 1) animator.SetTrigger("Attack (Double Slash)");

        CancelMovement();

        // Start charging the attack
        Invoke(nameof(AttackStart), attackChargeTime);
    }

    private void AttackStart()
    {
        // Lock enemy rotation and position
        currentState = EnemyState.Attacking;

        // Use the correct attack logic
        if (whichAttack == 0) SlashAttack();
        else if (whichAttack == 1) StartCoroutine(DoubleSlashAttack());
        else
        {
            // Set the animation for the spin attack here because this shit triggers instantly lol
            animator.SetTrigger("Attack (Spin)");
            SpinAttack();
        }


        // Start recovery of the attack, after the attackduration
        Invoke(nameof(StartRecovery), attackDuration);
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

        StartCoroutine(AttackReset());
    }

    private void SlashAttack()
    {
        // Play audio
        audioSource.Play();

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

    private IEnumerator DoubleSlashAttack()
    {
        // Perform an attack two times
        for (int i = 0; i < 2; i++)
        {
            // Play audio
            audioSource.Play();

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
            yield return new WaitForSeconds(0.35f);
        }
    }

    private void SpinAttack()
    {
        // Play audio
        audioSource.Play();

        // Retrieve all detected colliders
        Collider[] hits = Physics.OverlapSphere(transform.position + new Vector3(0, 1f, 0), spinRadius, playerLayer);

        foreach (Collider hit in hits)
        {
            GameObject hitObject = hit.gameObject;

            // Retrieve the health script on the player and damage him
            PlayerResources pHealth = hitObject.GetComponent<PlayerResources>();
            pHealth.Damage(attackDamage);
        }
    }

    private void DetermineAttack()
    {
        // Rol a random attack by random numbers: 0/1/2, then decide which block of code to execute based on the roll
        whichAttack = Random.Range(0, 3);

        // Based upon the roll, set all variables
        if (whichAttack == 0)
        {
            attackSize = slashSize;
            attackDamage = slashDamage;
            attackChargeTime = slashChargeTime;
            attackDuration = slashDuration;
            attackCD = slashCD;
            attackRange = slashRange;
        }
        else if (whichAttack == 1)
        {
            attackSize = doubleSlashSize;
            attackDamage = doubleSlashDamage;
            attackChargeTime = doubleSlashChargeTime;
            attackDuration = doubleSlashDuration;
            attackCD = doubleSlashCD;
            attackRange = doubleSlashRange;
        }
        else
        {
            attackDamage = spinDamage;
            attackChargeTime = spinChargeTime;
            attackDuration = spinDuration;
            attackCD = spinCD;
            attackRange = spinRange;
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
    //    Gizmos.matrix = Matrix4x4.TRS(attackPoint.position, attackPoint.rotation, Vector3.one);
    //    Gizmos.DrawWireCube(Vector3.zero, attackSize);

    //    //Gizmos.color = Color.red;
    //    //Gizmos.DrawWireSphere(transform.position, spinRadius);
    //}
    //void OnDrawGizmos()
    //{
    //    Gizmos.color = Color.yellow;
    //    Gizmos.DrawWireSphere(transform.position, spinRadius);
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
