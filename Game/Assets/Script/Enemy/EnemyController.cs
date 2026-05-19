using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public enum EnemyType
{
    Squelette,
    Araignee
}

public class EnemyController : MonoBehaviour
{
    public static int ActiveCount { get; private set; }

    [Header("R�f�rences")]
    public Animator animator;
    public NavMeshAgent agent;

    [Header("Statistiques")]
    public int maxHealth = 100;
    private int currentHealth;
    public int damage = 10;
    public float meleeAttackRange = 3f;
    public float rangedAttackRange = 15f;
    public float attackCooldown = 2f;
    public float detectionRange = 20f;
    public float loseTargetRange = 25f;

    [Header("Zones d'attaque")]
    public float meleeTransitionRange = 10f;

    [Header("Patrouille")]
    public Vector3 patrolCenter;
    public float patrolRadius = 10f;
    public float minPatrolWaitTime = 2f;
    public float maxPatrolWaitTime = 5f;
    public GameObject damageTextPrefab;

    private enum EnemyState { Idle, Patrol, Wait, Chase, Search, MeleeAttack, RangedAttack, Flee, Stunned, Dead }
    private EnemyState currentState;

    private int animIDIsMoving;
    private int animIDIsAttackingMelee;
    private int animIDIsAttackingRanged;
    private int animIDIsHit;
    private int animIDIsDead;
    private int animIDIsStunned;

    private Transform playerTransform;
    private float attackTimer;
    private float patrolWaitTimer;
    private Vector3 nextPatrolPoint;
    public EnemyType enemyType;

    [Header("Projectile Settings")]
    public GameObject projectilePrefab;
    public Transform projectileSpawnPoint;

    private Vector3 lastKnownPlayerPosition;
    private bool hasCalledForBackup = false;

    [Header("Fuite")]
    public float fleeThreshold = 0.3f;
    public float fleeDistance = 10f;
    public float fleeSpeed = 7f;

    [Header("Effets Sonores")]
    public AudioClip meleeAttackSound;
    public AudioClip rangedAttackSound;
    public AudioClip hitSound;
    public AudioClip deathSound;

    private AudioSource audioSource;

    private Coroutine hitCoroutine;
    private bool isInKnockback = false;
    private float defaultSpeed = 3.5f;
    private bool isCounted;

    private void OnEnable()
    {
        if (isCounted) return;
        ActiveCount++;
        isCounted = true;
    }

    private void OnDisable()
    {
        if (!isCounted) return;
        ActiveCount = Mathf.Max(0, ActiveCount - 1);
        isCounted = false;
    }

    private void Awake()
    {
        if (agent == null)
            agent = GetComponent<NavMeshAgent>();

        if (animator == null)
            animator = GetComponent<Animator>();

        currentHealth = maxHealth;

        if (patrolCenter == Vector3.zero)
            patrolCenter = transform.position;

        audioSource = GetComponent<AudioSource>();

        playerTransform = GameObject.FindGameObjectWithTag("Player").transform;

        AssignAnimationIDs();

        defaultSpeed = agent.speed;

        ChangeState(EnemyState.Patrol);
    }

    private void AssignAnimationIDs()
    {
        animIDIsMoving = Animator.StringToHash("isMoving");
        animIDIsAttackingMelee = Animator.StringToHash("isAttackingM");
        animIDIsAttackingRanged = Animator.StringToHash("isAttackingD");
        animIDIsHit = Animator.StringToHash("isHit");
        animIDIsDead = Animator.StringToHash("isDead");
        animIDIsStunned = Animator.StringToHash("isStunned");
    }

    private void Update()
    {
        if (currentState == EnemyState.Dead)
            return;

        if (isInKnockback)
            return;

        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        switch (currentState)
        {
            case EnemyState.Idle:
                HandleIdleState(distanceToPlayer);
                break;

            case EnemyState.Patrol:
                HandlePatrolState(distanceToPlayer);
                break;

            case EnemyState.Wait:
                HandleWaitState(distanceToPlayer);
                break;

            case EnemyState.Chase:
                HandleChaseState(distanceToPlayer);
                break;

            case EnemyState.MeleeAttack:
                HandleMeleeAttackState(distanceToPlayer);
                break;

            case EnemyState.RangedAttack:
                HandleRangedAttackState(distanceToPlayer);
                break;

            case EnemyState.Stunned:
                break;

            case EnemyState.Search:
                HandleSearchState(distanceToPlayer);
                break;

            case EnemyState.Flee:
                HandleFleeState(distanceToPlayer);
                break;
        }
    }

    private void HandleIdleState(float distanceToPlayer)
    {
        if (distanceToPlayer <= detectionRange)
        {
            ChangeState(EnemyState.Chase);
        }
        else
        {
            ChangeState(EnemyState.Patrol);
        }
    }

    private void HandlePatrolState(float distanceToPlayer)
    {
        if (!agent.hasPath || agent.remainingDistance < 0.5f)
        {
            ChangeState(EnemyState.Wait);
            return;
        }

        if (distanceToPlayer <= detectionRange)
        {
            ChangeState(EnemyState.Chase);
        }
    }

    private void HandleWaitState(float distanceToPlayer)
    {
        patrolWaitTimer -= Time.deltaTime;

        if (patrolWaitTimer <= 0)
        {
            ChangeState(EnemyState.Patrol);
            return;
        }

        if (distanceToPlayer <= detectionRange)
        {
            ChangeState(EnemyState.Chase);
        }
    }

    private void HandleSearchState(float distanceToPlayer)
    {
        if (!agent.pathPending && (agent.remainingDistance <= agent.stoppingDistance))
        {
            ChangeState(EnemyState.Patrol);
            return;
        }

        if (distanceToPlayer <= detectionRange)
        {
            ChangeState(EnemyState.Chase);
            return;
        }

        agent.isStopped = false;
        agent.SetDestination(lastKnownPlayerPosition);
        animator.SetBool(animIDIsMoving, true);
    }

    private void HandleChaseState(float distanceToPlayer)
    {
        if (!hasCalledForBackup)
        {
            CallForBackup();
        }

        if (distanceToPlayer > loseTargetRange)
        {
            ChangeState(EnemyState.Patrol);
            return;
        }
        else if (distanceToPlayer <= meleeTransitionRange)
        {
            ChangeState(EnemyState.MeleeAttack);
            return;
        }
        else if (distanceToPlayer <= rangedAttackRange)
        {
            ChangeState(EnemyState.RangedAttack);
            return;
        }

        agent.isStopped = false;
        agent.SetDestination(playerTransform.position);
        animator.SetBool(animIDIsMoving, true);
    }

    private void HandleMeleeAttackState(float distanceToPlayer)
    {
        if (distanceToPlayer > meleeTransitionRange)
        {
            if (distanceToPlayer <= rangedAttackRange)
            {
                ChangeState(EnemyState.RangedAttack);
            }
            else if (distanceToPlayer <= detectionRange)
            {
                ChangeState(EnemyState.Chase);
            }
            else
            {
                ChangeState(EnemyState.Patrol);
            }
            return;
        }
        else if (distanceToPlayer > meleeAttackRange)
        {
            agent.isStopped = false;
            agent.SetDestination(playerTransform.position);
            animator.SetBool(animIDIsMoving, true);
        }
        else
        {
            agent.isStopped = true;
            animator.SetBool(animIDIsMoving, false);
            FaceTarget();

            attackTimer -= Time.deltaTime;

            if (attackTimer <= 0f)
            {
                animator.SetBool(animIDIsAttackingMelee, true);
                attackTimer = attackCooldown;
                audioSource.PlayOneShot(meleeAttackSound);
            }
            else
            {
                animator.SetBool(animIDIsAttackingMelee, false);
            }
        }
    }

    private void HandleRangedAttackState(float distanceToPlayer)
    {
        if (distanceToPlayer <= meleeTransitionRange)
        {
            ChangeState(EnemyState.MeleeAttack);
            return;
        }
        else if (distanceToPlayer > rangedAttackRange)
        {
            if (distanceToPlayer <= detectionRange)
            {
                ChangeState(EnemyState.Chase);
            }
            else
            {
                ChangeState(EnemyState.Patrol);
            }
            return;
        }

        agent.isStopped = true;
        animator.SetBool(animIDIsMoving, false);
        FaceTarget();

        attackTimer -= Time.deltaTime;

        if (attackTimer <= 0f)
        {
            animator.SetBool(animIDIsAttackingRanged, true);
            LaunchProjectile();
            attackTimer = attackCooldown;
            audioSource.PlayOneShot(rangedAttackSound);
        }
        else
        {
            animator.SetBool(animIDIsAttackingRanged, false);
        }
    }

    private void FaceTarget()
    {
        if (isInKnockback) return; // Ne pas tourner pendant le knockback
        Vector3 direction = (playerTransform.position - transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);
    }

    public void LaunchProjectile()
    {
        if (projectilePrefab != null && projectileSpawnPoint != null)
        {
            GameObject projectile = Instantiate(projectilePrefab, projectileSpawnPoint.position, Quaternion.identity);
            Projectile projectileScript = projectile.GetComponent<Projectile>();
            if (projectileScript != null)
            {
                float launchSpeed = 10f;
                projectileScript.Initialize(playerTransform.position, launchSpeed);
            }
        }
    }

    // Ajout du knockback
    public void TakeDamage(int damage, Vector3 hitDirection = default, float knockbackForce = 0f)
    {
        if (currentState == EnemyState.Dead)
            return;

        currentHealth -= damage;
        SoundManager.Instance.PlayEnemyHurt();
        GetComponent<HitFeedback>()?.PlayHitFeedback();
        ShowDamageText(damage);

        if (currentHealth <= 0)
        {
            SoundManager.Instance.PlayEnemyDeath();
            Die();
            return;
        }
        if (currentHealth <= maxHealth * fleeThreshold)
        {
            ChangeState(EnemyState.Flee);
            return;
        }

        animator.SetTrigger(animIDIsHit);

        // Immobilisation totale pendant le knockback + anim de hit
        if (hitCoroutine != null)
            StopCoroutine(hitCoroutine);
        hitCoroutine = StartCoroutine(HitStunAndKnockbackCoroutine(hitDirection, knockbackForce));
    }

    private IEnumerator HitStunAndKnockbackCoroutine(Vector3 hitDirection, float knockbackForce)
    {
        isInKnockback = true;
        float savedSpeed = agent.speed;
        agent.speed = 0f;
        agent.isStopped = true;

        // Knockback
        if (hitDirection != Vector3.zero && knockbackForce > 0f)
        {
            float knockbackTime = 0.15f;
            Vector3 start = transform.position;
            Vector3 end = start + hitDirection.normalized * knockbackForce;
            float elapsed = 0f;
            while (elapsed < knockbackTime)
            {
                transform.position = Vector3.Lerp(start, end, elapsed / knockbackTime);
                elapsed += Time.deltaTime;
                yield return null;
            }
            transform.position = end;
        }

        // Attendre un temps fixe pour simuler la dur�e de l'animation de hit
        float hitAnimationDuration = 0.5f; // Dur�e de l'animation de hit en secondes
        yield return new WaitForSeconds(hitAnimationDuration);

        // === RESTAURE LA VITESSE ET LE MOUVEMENT ===
        if (currentHealth > 0 && currentState != EnemyState.Dead)
        {
            agent.speed = savedSpeed;
            agent.isStopped = false;
        }
        isInKnockback = false;
    }


    private void HandleFleeState(float distanceToPlayer)
    {
        Vector3 fleeDirection = (transform.position - playerTransform.position).normalized;
        Vector3 fleeTarget = transform.position + fleeDirection * fleeDistance;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(fleeTarget, out hit, fleeDistance, NavMesh.AllAreas))
        {
            agent.isStopped = false;
            agent.speed = fleeSpeed;
            agent.SetDestination(hit.position);
            animator.SetBool(animIDIsMoving, true);

            if (distanceToPlayer >= loseTargetRange)
            {
                ChangeState(EnemyState.Patrol);
            }
        }
        else
        {
            ChangeState(EnemyState.Patrol);
        }
    }

    public void CallForBackup()
    {
        if (hasCalledForBackup)
            return;

        hasCalledForBackup = true;

        Collider[] hitColliders = Physics.OverlapSphere(transform.position, 15f);
        foreach (var hitCollider in hitColliders)
        {
            EnemyController ally = hitCollider.GetComponent<EnemyController>();
            if (ally != null && ally != this)
            {
                ally.ReceiveBackupCall(playerTransform.position);
            }
        }
    }

    public void ReceiveBackupCall(Vector3 playerPosition)
    {
        if (currentState == EnemyState.Patrol || currentState == EnemyState.Wait || currentState == EnemyState.Search)
        {
            lastKnownPlayerPosition = playerPosition;
            ChangeState(EnemyState.Chase);
        }
    }

    private void Die()
    {
        animator.SetTrigger(animIDIsDead);
        currentState = EnemyState.Dead;
        agent.isStopped = true;

        QuestManager.Instance.RegisterKill(enemyType);

        StartCoroutine(DieAfterAnimation());
    }

    private IEnumerator DieAfterAnimation()
    {
        yield return new WaitForSeconds(2f);

        Destroy(gameObject);
    }

    private void GetNewPatrolPoint()
    {
        Vector2 randomCircle = Random.insideUnitCircle * patrolRadius;
        Vector3 randomDirection = new Vector3(randomCircle.x, 0, randomCircle.y);

        Vector3 targetPosition = patrolCenter + randomDirection;

        if (NavMesh.SamplePosition(targetPosition, out NavMeshHit hit, patrolRadius, NavMesh.AllAreas))
        {
            nextPatrolPoint = hit.position;
            agent.SetDestination(nextPatrolPoint);
        }
        else
        {
            nextPatrolPoint = transform.position;
        }
    }

    private void ChangeState(EnemyState newState)
    {
        if (currentState == newState)
            return;
        
        // Safety check: ensure agent is enabled before trying to control it
        if (agent == null || !agent.isOnNavMesh || !agent.enabled)
            return;

        animator.SetBool(animIDIsAttackingMelee, false);
        animator.SetBool(animIDIsAttackingRanged, false);

        switch (newState)
        {
            case EnemyState.Idle:
                agent.isStopped = true;
                break;

            case EnemyState.Patrol:
                agent.isStopped = false;
                agent.speed = defaultSpeed;
                agent.stoppingDistance = 0f;
                animator.SetBool(animIDIsMoving, true);
                if (SoundManager.Instance != null)
                    SoundManager.Instance.PlayEnemyWalk();
                GetNewPatrolPoint();
                break;

            case EnemyState.Wait:
                agent.isStopped = true;
                patrolWaitTimer = Random.Range(minPatrolWaitTime, maxPatrolWaitTime);
                break;

            case EnemyState.Chase:
                agent.isStopped = false;
                agent.speed = 5f;
                agent.stoppingDistance = meleeTransitionRange;
                if (SoundManager.Instance != null)
                    SoundManager.Instance.PlayEnemyRun();
                animator.SetBool(animIDIsMoving, true);
                break;

            case EnemyState.MeleeAttack:
                agent.isStopped = false;
                agent.speed = 5f;
                agent.stoppingDistance = meleeAttackRange;
                if (SoundManager.Instance != null)
                    SoundManager.Instance.PlayEnemyAttackMelee();
                animator.SetBool(animIDIsMoving, true);
                attackTimer = attackCooldown;
                break;

            case EnemyState.RangedAttack:
                agent.isStopped = true;
                agent.stoppingDistance = rangedAttackRange;
                if (SoundManager.Instance != null)
                    SoundManager.Instance.PlayEnemyAttackRanged();
                animator.SetBool(animIDIsMoving, false);
                attackTimer = attackCooldown;
                break;

            case EnemyState.Stunned:
                agent.isStopped = true;
                animator.SetTrigger(animIDIsStunned);
                break;

            case EnemyState.Dead:
                agent.isStopped = true;
                animator.SetTrigger(animIDIsDead);
                Collider enemyCollider = GetComponent<Collider>();
                enemyCollider.enabled = false;
                break;

            case EnemyState.Search:
                agent.isStopped = false;
                animator.SetBool(animIDIsMoving, true);
                break;

            case EnemyState.Flee:
                agent.isStopped = false;
                agent.speed = fleeSpeed;
                animator.SetBool(animIDIsMoving, true);
                break;
        }

        currentState = newState;
    }

    public void Stun(float duration)
    {
        if (currentState == EnemyState.Dead)
            return;

        StartCoroutine(StunCoroutine(duration));
    }

    private IEnumerator StunCoroutine(float duration)
    {
        ChangeState(EnemyState.Stunned);

        yield return new WaitForSeconds(duration);

        ChangeState(EnemyState.Chase);
    }
    private void ShowDamageText(int damage)
    {
        if (damageTextPrefab != null)
        {
            GameObject damageTextInstance = Instantiate(damageTextPrefab, transform.position + Vector3.up * 2f, Quaternion.identity);

            DamageText damageText = damageTextInstance.GetComponent<DamageText>();
            if (damageText != null)
            {
                damageText.SetDamageText(damage.ToString());
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(patrolCenter, patrolRadius);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, meleeAttackRange);

        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, rangedAttackRange);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, meleeTransitionRange);

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, loseTargetRange);
    }
}
