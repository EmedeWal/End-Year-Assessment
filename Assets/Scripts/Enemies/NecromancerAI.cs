using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UIElements;

public class NecromancerAI : MonoBehaviour
{
    #region !SETUP!

    #region ENUM

    public enum EnemyState
    {
        Chasing,
        Charging,
        Attacking,
        Spellcasting,
        Teleporting
    }

    public EnemyState currentState = EnemyState.Chasing;
    #endregion

    // End of Enum

    #region REFERENCES

    [Header("REFERENCES")]

    [Header("General")]
    [SerializeField] private Transform attackPoint;
    [SerializeField] private Transform spellPoint;
    [SerializeField] private Animator animator;
    [SerializeField] private LayerMask playerLayer;

    [HideInInspector] public Transform player;

    private NavMeshAgent agent;
    private Enemy enemy;

    [Header("General")]
    [SerializeField] private AudioClip chargeSpell;
    [SerializeField] private AudioClip meleeClip;
    private AudioSource audioSource;
    #endregion

    // End of References

    #region VARIABLES

    [Header("VARIABLES")]

    [Header("Attacking: Melee")]
    [SerializeField] private Vector3 meleeSize;
    [SerializeField] private float meleeDamage;
    [SerializeField] private float meleeChargeTime;
    [SerializeField] private float meleeDuration;
    [SerializeField] private float meleeRange;
    [SerializeField] private float meleeCD;
    private bool canMelee = true;

    [Header("Spellcasting: Multi")]
    [SerializeField] private GameObject multiPrefab;
    [SerializeField] private float multiDamage;
    [SerializeField] private float multiChargeTime;
    [SerializeField] private float multiDuration;
    [SerializeField] private float multiRange;
    [SerializeField] private float multiCD;
    [SerializeField] private float multiForce;
    [SerializeField] private float multiRotationIncrement;

    [Header("Spellcasting: Tracking")]
    [SerializeField] private GameObject trackingPrefab;
    [SerializeField] private float trackingDamage;
    [SerializeField] private float trackingChargeTime;
    [SerializeField] private float trackingDuration;
    [SerializeField] private float trackingRange;
    [SerializeField] private float trackingCD;

    private GameObject spellPrefab;
    private float spellChargeTime;
    private float spellDuration;
    private float spellRange;
    private float spellCD;
    private float spellForce;

    private int whichSpell;
    private bool canCast = true;

    [Header("Teleporting")]
    [SerializeField] private GameObject teleportVFX;
    [SerializeField] private float teleportCD;
    [SerializeField] private float minTeleportDistance;
    private Vector3 teleportTargetPosition;
    private bool canTeleport = true;

    [Header("Other")]
    [SerializeField] private float rotationSpeed;
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

        audioSource = GetComponent<AudioSource>();

        DetermineSpell();
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
                ChargeAttack();
                break;

            case EnemyState.Spellcasting:
                ChargeSpell();
                break;

            case EnemyState.Teleporting:
                Teleport();
                break;
        }
    }

    private bool InMeleeRange()
    {
        // Calculate the distance to the player and check if the player is in melee range
        if (Vector3.Distance(player.position, transform.position) <= meleeRange) return true;
        else return false;
    }

    private bool InSpellRange()
    {
        // Calculate the distance to the player and check if the player is in spellcasting range
        if (Vector3.Distance(player.position, transform.position) <= spellRange) return true;
        else return false;
    }

    #endregion

    // End of Behavior

    #region STATES

    #region Chasing

    private void Chase()
    {
        // If the player is in melee range, attack in melee. Also check if it can attack, otherwise use a ranged spell
        if (InMeleeRange())
        {
            CancelMovement();
            currentState = EnemyState.Attacking; 
            return;
        }

        // If the player is in spell range, cast spell.
        if (InSpellRange())
        {
            CancelMovement();
            currentState = EnemyState.Spellcasting;
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

        animator.SetTrigger("Attack (Melee)");

        // The enemy tracks the player until damage is dealt
        currentState = EnemyState.Charging;

        // Start charging the attack
        Invoke(nameof(MeleeStart), meleeChargeTime);
    }

    private void MeleeStart()
    {
        CastMelee();

        // Play audio
        audioSource.clip = meleeClip;
        audioSource.volume = 0.2f;
        audioSource.Play();

        // Tracking is disabled
        currentState = EnemyState.Attacking;    

        // Start recovery of the attack, after the attackduration
        Invoke(nameof(MeleeRecovery), meleeDuration);

        // Reset attack CD
        StartCoroutine(MeleeReset());
    }

    private IEnumerator MeleeReset()
    {
        // Wait for the spellCD to attack again
        yield return new WaitForSeconds(meleeCD);

        // Booleans
        canMelee = true;
    }

    private void MeleeRecovery()
    {
        // After every melee attack, the enemy teleport away, if the teleport is off cooldown
        currentState = EnemyState.Teleporting;
    }

    private void CastMelee()
    {
        Collider[] hits = Physics.OverlapBox(attackPoint.position, meleeSize);

        foreach (Collider hit in hits)
        {
            GameObject hitObject = hit.gameObject;

            if (hitObject.CompareTag("Player"))
            {
                PlayerResources pHealth = hitObject.GetComponent<PlayerResources>();

                pHealth.Damage(meleeDamage);
            }
        }
    }
    #endregion

    #region Spellcasting

    private void ChargeSpell()
    {
        if (!canCast) return;

        canCast = false;

        animator.SetTrigger("Attack (Ranged)");

        // Play an audio cue to notify the player 
        audioSource.clip = chargeSpell;
        audioSource.volume = 0.05f;
        audioSource.time = 0.3f;
        audioSource.Play();

        // The enemy should rotate towards the player until the projectile is launched
        currentState = EnemyState.Charging;

        // Start the spell after short delay
        Invoke(nameof(SpellStart), spellChargeTime);
    }

    private void SpellStart()
    {
        // Lock enemy rotation
        currentState = EnemyState.Spellcasting;

        // Cast the correct spell
        if (whichSpell == 0) CastMultiSpell();
        else CastTrackingSpell();

        // Start additional behavior after the spell has ended
        Invoke(nameof(SpellRecovery), spellDuration);

        // The enemy can cast spells again after the cooldown
        StartCoroutine(SpellReset());
    }

    private IEnumerator SpellReset()
    {
        yield return new WaitForSeconds(spellCD);
        canCast = true;
    }

    private void SpellRecovery()
    {
        // The spell was done charging, stop the audio
        audioSource.Stop();

        // Additional behavior
        currentState = EnemyState.Chasing;

        DetermineSpell();
    }

    private void CastMultiSpell()
    {
        // Starting at -15 degrees for the first projectile.
        float currentRotationOffset = -multiRotationIncrement;

        // Shoot three projectiles
        for (int i = 0; i < 3; i++)
        {
            // Calculate the rotation for this projectile
            Quaternion projectileRotation = Quaternion.Euler(spellPoint.eulerAngles.x, spellPoint.eulerAngles.y + currentRotationOffset, spellPoint.eulerAngles.z);

            // Instantiate the projectile with the adjusted rotation
            GameObject projectile = Instantiate(spellPrefab, spellPoint.position, projectileRotation);
            projectile.GetComponent<ForwardProjectile>().SetDamage(multiDamage);
            projectile.GetComponent<Rigidbody>().AddForce(projectile.transform.forward * spellForce * 100, ForceMode.Force);

            // Increment the offset for the next projectile
            currentRotationOffset += multiRotationIncrement;
        }
    }

    private void CastTrackingSpell()
    {
        // Instantiate the projectile
        GameObject projectile = Instantiate(spellPrefab, spellPoint.position, spellPoint.rotation);
        TrackingProjectile projectileScript = projectile.GetComponent<TrackingProjectile>();

        // Initialise damage variables
        projectileScript.player = player.transform;
        projectileScript.explosionDamage = trackingDamage;
    }

    private void DetermineSpell()
    {
        // The enemy chooses from two spells
        whichSpell = Random.Range(0, 2);

        // Initialise multi spell
        if (whichSpell == 0)
        {
            spellPrefab = multiPrefab;
            spellChargeTime = multiChargeTime;
            spellDuration = multiDuration;
            spellForce = multiForce;
            spellCD = multiCD;
            spellRange = multiRange;
        }
        else
        {
            spellPrefab = trackingPrefab;
            spellChargeTime = trackingChargeTime;
            spellDuration = trackingDuration;
            spellCD = trackingCD;
            spellRange = trackingRange;
        }

        // The teleport distance should be equal to the enemy's range, so he can immediately attack again
        minTeleportDistance = spellRange;
    }

    #endregion

    #region Teleporting
    private void Teleport()
    {
        // If the enemy can teleport, teleport away
        if (canTeleport)
        {
            // The enemy should play the idle animation
            animator.SetFloat("Speed", 0f);

            // Spawn some VFX for better feedback
            StartCoroutine(TeleportVFX());

            canTeleport = false;
            bool validPositionFound = false;
            int maxAttempts = 50;
            Vector3 potentialRetreatPosition = Vector3.zero;
            NavMeshHit hit;

            for (int i = 0; i < maxAttempts && !validPositionFound; i++)
            {
                // Find a valid retreat position not close to walls
                Vector3 randomDirection = Random.insideUnitSphere;
                randomDirection.y = 0;
                potentialRetreatPosition = transform.position + randomDirection.normalized * spellRange;

                // Ensure this potential position is at least the minimum teleport distance
                if ((potentialRetreatPosition - transform.position).magnitude < minTeleportDistance)
                {
                    continue; // Skip this iteration if the position is too close
                }

                if (NavMesh.SamplePosition(potentialRetreatPosition, out hit, spellRange, NavMesh.AllAreas))
                {
                    // Check for walls with a BoxCast
                    Collider[] colliders = Physics.OverlapBox(hit.position, new Vector3(1f, 1f, 1f), Quaternion.identity, LayerMask.GetMask("Terrain"));

                    if (colliders.Length == 0) // No walls detected
                    {
                        validPositionFound = true;

                        // Teleport to the location, and instantly rotate towards the player
                        teleportTargetPosition = hit.position;
                        transform.position = hit.position;
                        transform.LookAt(player.position);

                        // Start chase
                        StartChase();
                        break;
                    }
                }
            }

        }
        else
        {
            // If the enemy cannot teleport, but should, start chasing
            currentState = EnemyState.Chasing;
        }
    }

    private void TeleportReset()
    {
        canTeleport = true;
    }

    private void StartChase()
    {
        currentState = EnemyState.Chasing;

        // Start reseting the teleport cooldown
        Invoke(nameof(TeleportReset), teleportCD);
    }

    private IEnumerator TeleportVFX()
    {
        GameObject VFX = Instantiate(teleportVFX, transform.position + new Vector3(0, 3, 0), Quaternion.identity);

        yield return new WaitForSeconds(0.7f);

        Destroy(VFX);
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
        // Stop and reset all coroutines
        StopAllCoroutines();
        StartCoroutine(MeleeReset());
        StartCoroutine(SpellReset());

        canMelee = false;
        canCast = false;

        // Animation
        animator.SetTrigger("Stagger");

        // If the enemy is interrupted while charing his attack, execute the following the code
        if (currentState == EnemyState.Charging)
        {
            // Cancel the start of the attacks
            CancelInvoke(nameof(MeleeStart));
            CancelInvoke(nameof(SpellStart));

            // Start recovery of the attack, after the attackduration
            Invoke(nameof(MeleeRecovery), spellDuration);
            Invoke(nameof(SpellRecovery), spellDuration);
        }

        // If the enemy is hit while moving, stop him in his tracks
        else if (currentState == EnemyState.Chasing) CancelMovement();
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

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(attackPoint.position, meleeSize);
    }
}
