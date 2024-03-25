using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

public class DragonAI : MonoBehaviour
{
    #region !SETUP!

    #region ENUM

    public enum EnemyState
    {
        Chasing,
        Charging,
        Attacking,
        Firing,
        Flying,
        Moving
    }

    public EnemyState currentState = EnemyState.Chasing;
    #endregion

    // End of Enum

    #region REFERENCES

    [Header("REFERENCES")]

    [Header("General")]
    [SerializeField] private GameObject GFX;
    [SerializeField] private Collider dragonCollider;
    [SerializeField] private Animator animator;
    [SerializeField] private LayerMask playerLayer;

    [HideInInspector] public Transform player;

    private NavMeshAgent agent;
    private Enemy enemy;

    [Header("Audio")]
    [SerializeField] private AudioClip roar;
    [SerializeField] private AudioClip bite;
    [SerializeField] private AudioClip tail;
    private AudioSource audioSource;
    #endregion

    // End of References

    #region VARIABLES

    [Header("MELEE")]

    [Header("Bite Attack")]
    [SerializeField] private Transform bitePoint;
    [SerializeField] private Vector3 biteSize;
    [SerializeField] private float biteDamage;
    [SerializeField] private float biteChargeTime;
    [SerializeField] private float biteDuration;
    [SerializeField] private float biteRange;
    [SerializeField] private float biteCD;

    [Header("Tail Attack")]
    [SerializeField] private Transform tailPoint;
    [SerializeField] private Vector3 tailSize;
    [SerializeField] private float tailDamage;
    [SerializeField] private float tailChargeTime;
    [SerializeField] private float tailDuration;
    [SerializeField] private float tailRange;
    [SerializeField] private float tailCD;

    private Transform meleePoint;
    private Vector3 meleeSize;
    private int whichMelee;
    private float meleeDamage;
    private float meleeChargeTime;
    private float meleeDuration;
    private float meleeRange;
    private float meleeCD;
    private bool canMelee = true;

    [Header("RANGED")]
    [SerializeField] private Transform firePointGrounded;
    [SerializeField] private Transform firePointFlying;
    [SerializeField] private GameObject fireballPrefab;
    [SerializeField] private float fireballDamage;
    [SerializeField] private float fireChargeTime;
    [SerializeField] private float fireDuration;
    [SerializeField] private float fireRange;
    [SerializeField] private float fireCD;
    [SerializeField] private float fireForce;
    [SerializeField] private float maxRotation;
    private bool canFire = true;

    private Transform firePoint;

    [Header("FLYING")]
    public UnityEvent canvasState;

    [SerializeField] private int maxStateTime;
    [SerializeField] private float ascendDistance;
    [SerializeField] private float ascendIncrement;
    [SerializeField] private float rangeBoost;
    private int stateTime = 0;
    private float originalPositionY;
    private bool isFlying = false;
    private bool isAscendingOrDescending = false;

    [Header("MOVING")]
    [SerializeField] private float minRepositionDistance;
    private Vector3 reposition;
    private bool isMoving = false;

    [Header("Other")]
    [SerializeField] private float rotationSpeed;
    [SerializeField] private float activationTime;
    private bool active;
    #endregion

    // End of Attacking

    #endregion

    // END OF SETUP

    #region !EXECUTION!

    #region DEFAULT

    private void Awake()
    {
        // Determine which melee to cast
        DetermineMelee();
    }

    private void Start()
    {
        // Set up all references
        agent = GetComponent<NavMeshAgent>();
        enemy = GetComponent<Enemy>();

        player = enemy.playerTransform;

        audioSource = GetComponent<AudioSource>();

        // Make sure the dragon roars before activating
        audioSource.clip = roar;
        audioSource.Play();
        Invoke(nameof(Activate), activationTime);
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
                ChargeAttack();
                break;

            case EnemyState.Firing:
                Fire();
                break;

            case EnemyState.Flying:
                SwapAvialState();
                break;

            case EnemyState.Moving:
                Reposition();
                break;
        }
    }

    private bool InMeleeRange()
    {
        // Calculate the distance to the player and check if the player is in melee range
        if (Vector3.Distance(player.position, transform.position) <= meleeRange) return true;
        else return false;
    }

    private bool InFireRange()
    {
        // Calculate the distance to the player and check if the player is in spellcasting range
        if (Vector3.Distance(player.position, transform.position) <= fireRange) return true;
        else return false;
    }

    #endregion

    // End of Behavior

    #region STATES

    #region Chasing

    private void Chase()
    {
        // Check if the enemy is grounded
        if (!isFlying)
        {
            //If so, increment TrackStateTime
            if (TrackStateTime())
            {
                CancelMovement();
                currentState = EnemyState.Flying;
                return;
            }
        }

        // If the player is in melee range and the dragon is not flying, attack in melee. 
        if (InMeleeRange() && !isFlying)
        {
            CancelMovement();
            currentState = EnemyState.Attacking;
            return;
        }

        // If the player is in the fire range, fire at him
        if (InFireRange())
        {
            CancelMovement();
            currentState = EnemyState.Firing;
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

    private void ChargeAttack()
    {
        if (!canMelee) return;

        canMelee = false;

        // Animations
        if (whichMelee == 0)
        {
            animator.SetTrigger("Attack (Bite)");
            audioSource.clip = bite;
            audioSource.volume = 0.1f;

            // The enemy tracks the player until damage is dealt
            currentState = EnemyState.Charging;
        }
        else
        {
            animator.SetTrigger("Attack (Tail)");
            audioSource.clip = tail;

            // No tracking for the tail attack cuz it is kinda bullshit
        }

        // Play audio
        audioSource.Play();

        // Start charging the attack
        Invoke(nameof(MeleeStart), meleeChargeTime);
    }

    private void MeleeStart()
    {
        MeleeAttack();

        // Tracking is disabled
        currentState = EnemyState.Attacking;

        // Start recovery of the attack, after the attackduration
        Invoke(nameof(MeleeRecovery), meleeDuration);
    }

    private IEnumerator MeleeReset()
    {
        // Wait for the spellCD to attack again
        yield return new WaitForSeconds(meleeCD);

        // Booleans
        canMelee = true;

        currentState = EnemyState.Chasing;
    }

    private void MeleeRecovery()
    {
        // Determine the next attack
        DetermineMelee();

        // And start chasing the enemy until the melee can be used again
        currentState = EnemyState.Charging;
        StartCoroutine(MeleeReset());
    }

    private void MeleeAttack()
    {
        Collider[] hits = Physics.OverlapBox(meleePoint.position, meleeSize);

        foreach (Collider hit in hits)
        {
            GameObject hitObject = hit.gameObject;

            if (hitObject.CompareTag("Player"))
            {
                PlayerResources pHealth = hitObject.GetComponent<PlayerResources>();

                pHealth.Damage(meleeDamage);
            }
        }

        // Reset the volume of general audio
        audioSource.volume = 0.25f;
    }

    private void DetermineMelee()
    {
        // Choose one of two attacks
        whichMelee = Random.Range(0, 2);

        if (whichMelee == 0)
        {
            meleePoint = bitePoint;
            meleeSize = biteSize;
            meleeDamage = biteDamage;
            meleeChargeTime = biteChargeTime;
            meleeDuration = biteDuration;
            meleeRange = biteRange;
            meleeCD = biteCD;
        }
        else
        {
            meleePoint = tailPoint;
            meleeSize = tailSize;
            meleeDamage = tailDamage;
            meleeChargeTime = tailChargeTime;
            meleeDuration = tailDuration;
            meleeRange = tailRange;
            meleeCD = tailCD;
        }

    }
    #endregion

    #region Firing

    private void Fire()
    {
        if (!canFire) return;

        canFire = false;

        animator.SetTrigger("Fire");

        // Play audio cue
        audioSource.clip = tail;
        audioSource.Play();

        // The enemy should rotate towards the player until the projectile is launched
        currentState = EnemyState.Charging;

        // Start the spell after short delay
        Invoke(nameof(FireStart), fireChargeTime);
    }

    private void FireStart()
    {
        // Lock enemy rotation
        currentState = EnemyState.Firing;

        // Shoot a fireball
        ShootFireball();

        // Start additional behavior after the spell has ended
        Invoke(nameof(FireRecovery), fireDuration);
    }

    private void FireReset()
    {
        canFire = true;
    }

    private void FireRecovery()
    {
        // The dragon can fire again after the cooldown
        Invoke(nameof(FireReset), fireCD);

        // Determine whether the enemy should chase or find a new position
        if (isFlying) currentState = EnemyState.Moving;
        else currentState = EnemyState.Chasing;
    }

    private void ShootFireball()
    {
        // Determine which firePoint should be used
        if (isFlying) firePoint = firePointFlying;
        else firePoint = firePointGrounded;

        // If the dragon is flying, adjust the firePoint rotation to aim at the player
        if (isFlying)
        {
            // Calculate the direction to the player
            Vector3 directionToPlayer = (player.position - firePoint.position).normalized;

            // Create a look rotation in the direction of the player
            Quaternion lookRotation = Quaternion.LookRotation(directionToPlayer);

            // Extract the pitch (x rotation) from the look rotation
            float pitch = lookRotation.eulerAngles.x;

            // Clamp the pitch to desired range
            float clampedPitch = Mathf.Clamp(pitch, 0, maxRotation);

            // Update only the x component of the firePoint's rotation
            firePoint.rotation = Quaternion.Euler(clampedPitch, firePoint.eulerAngles.y, firePoint.eulerAngles.z);
        }

        // Instantiate the projectile with the adjusted rotation
        GameObject projectile = Instantiate(fireballPrefab, firePoint.position, firePoint.rotation);
        ForwardProjectile projectileScript = projectile.GetComponent<ForwardProjectile>();
        projectileScript.SetDamage(fireballDamage);
        projectileScript.shouldExplode = true;
        projectile.GetComponent<Rigidbody>().AddForce(projectile.transform.forward * fireForce * 100, ForceMode.Force);
    }

    #endregion

    #region Flying

    private void SwapAvialState()
    {
        // Check if the dragon is in the process of ascending or descending
        if (isAscendingOrDescending) return;

        // The dragon is now in the process of ascending or descending
        isAscendingOrDescending = true;

        // Check if the enemy should ascend or descend
        if (!isFlying)
        {
            StartCoroutine(Ascend());
        }
        else
        {
            StartCoroutine(Descend());
        }
    }

    private IEnumerator Ascend()
    {
        isFlying = true;
        animator.SetBool("Is Flying", isFlying);

        // Save the original position of the GFX
        originalPositionY = GFX.transform.position.y;

        animator.SetTrigger("Ascend");

        // The dragon can fire further now that he is high up in the air
        fireRange += rangeBoost;

        ManageAvialState();

        while (GFX.transform.position.y <= ascendDistance)
        {
            GFX.transform.position += new Vector3(0, ascendIncrement * 2, 0);

            yield return new WaitForSeconds(ascendIncrement);
        }

        // At the end of flying, go back to regular behavior
        currentState = EnemyState.Chasing;

        // The enemy is no longer ascending or descending
        isAscendingOrDescending = false;
    }

    private IEnumerator Descend()
    {
        animator.SetTrigger("Descend");

        yield return null;

        // The dragon can fire further now that he is high up in the air
        fireRange -= rangeBoost;

        // While the GFX are not back at their original position
        while (originalPositionY <= GFX.transform.position.y)
        {
            GFX.transform.position -= new Vector3(0, ascendIncrement * 2, 0);

            yield return new WaitForSeconds(ascendIncrement);
        }

        ManageAvialState();

        // The enemy is no longer ascending, descending, or flying
        isFlying = false;
        isAscendingOrDescending = false;
        animator.SetBool("Is Flying", isFlying);

        // At the end of flying, go back to regular behavior
        currentState = EnemyState.Charging;
        Invoke(nameof(StartChase), 1f);
    }

    private void ManageAvialState()
    {
        // Disable or enable the canvas
        canvasState?.Invoke();

        // Disable or enable the boxCollider
        if (dragonCollider.enabled) dragonCollider.enabled = false;
        else dragonCollider.enabled = true;
    }
    #endregion

    #region Moving

    private void Reposition()
    {
        animator.SetFloat("Speed", agent.velocity.magnitude);

        if (isMoving && agent.remainingDistance <= 0.1f)
        {
            CancelReposition();
            return;
        }

        if (isMoving) return;

        isMoving = true;
        bool validPositionFound = false;
        int maxAttempts = 10;


        for (int i = 0; i < maxAttempts && !validPositionFound; i++)
        {
            Vector3 randomDirection = Random.insideUnitSphere.normalized * fireRange;
            randomDirection += transform.position;
            randomDirection.y = transform.position.y; // Keep the altitude consistent

            if (NavMesh.SamplePosition(randomDirection, out NavMeshHit hit, fireRange, NavMesh.AllAreas))
            {
                Vector3 toPlayer = player.position - hit.position;
                Vector3 fromDragon = transform.position - hit.position;

                // Check if the new position is within fire range and not too close to the current position
                if (toPlayer.magnitude <= fireRange && fromDragon.magnitude >= minRepositionDistance)
                {
                    // Ensure the target position is not surrounded by walls
                    Collider[] colliders = Physics.OverlapBox(hit.position, new Vector3(5f, 5f, 5f), Quaternion.identity, LayerMask.GetMask("Terrain"));

                    if (colliders.Length == 0)
                    {
                        validPositionFound = true;
                        reposition = hit.position;
                        agent.SetDestination(reposition);
                    }
                }
            }
        }

        if (!validPositionFound)
        {
            CancelReposition();
        }
    }

    private void CancelReposition()
    {
        isMoving = false;

        agent.SetDestination(transform.position);

        if (TrackStateTime()) currentState = EnemyState.Flying;
        else
        {
            // The enemy is done retreating and should rotate until the faces the player, before starting the chase
            currentState = EnemyState.Charging;
            Invoke(nameof(StartChase), 1f);
        }
    }

    private void StartChase()
    {
        currentState = EnemyState.Chasing;
    }
    #endregion

    #region Other

    private bool TrackStateTime()
    {
        stateTime++;

        // If the enemy has exceeded its time in the air or the ground, swap state
        if (stateTime >= maxStateTime) return true;
        else return false;
    }

    private void Activate()
    {
        active = true;
    }

    private void CancelMovement()
    {
        agent.SetDestination(transform.position);
        animator.SetFloat("Speed", 0f);
    }
    #endregion

    #endregion

    // End of States

    #region EVENTS

    //public void Stagger()
    //{
    //    // Reset the enemy's attack CD, but make sure to reset it again later
    //    StopAllCoroutines();
    //    StartCoroutine(MeleeReset());
    //    canFire = false;

    //    // Animation
    //    animator.SetTrigger("Stagger");

    //    // If the enemy is interrupted while charing his attack, execute the following the code
    //    if (currentState == EnemyState.Charging)
    //    {
    //        // Cancel the attack start
    //        CancelInvoke(nameof(MeleeStart));

    //        // Start recovery of the attack, after the attackduration
    //        Invoke(nameof(MeleeRecovery), spellDuration);
    //    }

    //    // If the enemy is hit while moving, stop him in his tracks
    //    else if (currentState == EnemyState.Chasing) CancelMovement();
    //}

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

    private void OnDrawGizmosSelected()
    {
        //Gizmos.color = Color.red;
        //Gizmos.DrawWireCube(meleePoint.position, meleeSize);
    }
}
