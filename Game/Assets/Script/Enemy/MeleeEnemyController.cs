using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class MeleeEnemyController : MonoBehaviour
{
    [Header("R�f�rences")]
    public Animator animator;
    public NavMeshAgent agent;

    [Header("Statistiques")]
    public int maxHealth = 100;
    private int currentHealth;
    public int damage = 10;
    public float meleeAttackRange = 3f;
    public float detectionRange = 15f;
    public float attackCooldown = 2f;

    [Header("Patrouille")]
    public Vector3 patrolCenter;
    public float patrolRadius = 10f;
    public float minPatrolWaitTime = 2f;
    public float maxPatrolWaitTime = 5f;

    private enum EnemyState { Idle, Patrol, Wait, Chase, MeleeAttack, Stunned, Dead }
    private EnemyState currentState;

    private Transform playerTransform;
    private float attackTimer;
    private float patrolWaitTimer;
    private Vector3 nextPatrolPoint;
    private float defaultSpeed;

    private int animIDIsDead;

    public GameObject damageTextPrefab;

    private Coroutine hitCoroutine;
    private bool isInKnockback = false;

    private void Awake()
    {
        if (agent == null)
            agent = GetComponent<NavMeshAgent>();

        if (animator == null)
            animator = GetComponent<Animator>();

        currentHealth = maxHealth;

        if (patrolCenter == Vector3.zero)
            patrolCenter = transform.position;

        playerTransform = GameObject.FindGameObjectWithTag("Player").transform;

        AssignAnimationIDs();
        defaultSpeed = agent.speed;

        ChangeState(EnemyState.Patrol);
    }

    private void AssignAnimationIDs()
    {
        animIDIsDead = Animator.StringToHash("Die");
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

            case EnemyState.Chase:
                HandleChaseState(distanceToPlayer);
                break;

            case EnemyState.MeleeAttack:
                HandleMeleeAttackState(distanceToPlayer);
                break;

            case EnemyState.Wait:
                HandleWaitState(distanceToPlayer);
                break;

            case EnemyState.Stunned:
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

        float currentSpeed = agent.velocity.magnitude;
        animator.SetFloat("speed", currentSpeed);
    }
    private void HandleChaseState(float distanceToPlayer)
    {
        if (distanceToPlayer > detectionRange)
        {
            ChangeState(EnemyState.Patrol);
            return;
        }
        else if (distanceToPlayer <= meleeAttackRange)
        {
            ChangeState(EnemyState.MeleeAttack);
            return;
        }

        agent.isStopped = false;
        agent.SetDestination(playerTransform.position);

        FaceTarget();

        float currentSpeed = agent.velocity.magnitude;
        animator.SetFloat("speed", currentSpeed);
    }

    private void HandleMeleeAttackState(float distanceToPlayer)
    {
        if (distanceToPlayer > meleeAttackRange)
        {
            ChangeState(EnemyState.Chase);
            return;
        }

        agent.isStopped = true;

        FaceTarget();

        attackTimer -= Time.deltaTime;

        if (attackTimer <= 0f)
        {
            animator.SetBool("isAttacking", true);
            attackTimer = attackCooldown;
        }
        else
        {
            animator.SetBool("isAttacking", false);
        }
    }

    public void TakeDamage(int damage, Vector3 hitDirection = default, float knockbackForce = 100f)
    {
        if (currentState == EnemyState.Dead)
            return;

        currentHealth -= damage;
        ShowDamageText(damage);

        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            SoundManager.Instance.PlayEnemyHurt();
            animator.SetTrigger("isHit");
            if (hitCoroutine != null)
                StopCoroutine(hitCoroutine);
            hitCoroutine = StartCoroutine(HitStunAndKnockbackCoroutine(hitDirection, knockbackForce));
        }
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
        float hitAnimationDuration = 0.75f; // Dur�e de l'animation de hit en secondes
        yield return new WaitForSeconds(hitAnimationDuration);

        // === RESTAURE LA VITESSE ET LE MOUVEMENT ===
        if (currentHealth > 0 && currentState != EnemyState.Dead)
        {
            agent.speed = savedSpeed;
            agent.isStopped = false;
        }
        isInKnockback = false;
    }


    private void Die()
    {
        currentState = EnemyState.Dead;
        agent.isStopped = true;
        animator.SetTrigger(animIDIsDead);
        Destroy(gameObject, 3f);
    }
    private void FaceTarget()
    {
        if (isInKnockback) return; // Ne pas tourner pendant le knockback
        Vector3 direction = (playerTransform.position - transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0f, direction.z));
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);
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

        switch (newState)
        {
            case EnemyState.Idle:
                agent.isStopped = true;
                agent.speed = defaultSpeed;
                if (SoundManager.Instance != null)
                    SoundManager.Instance.PlayEnemyWalk();
                animator.SetFloat("speed", 0f);
                break;

            case EnemyState.Patrol:
                agent.isStopped = false;
                agent.speed = defaultSpeed;
                if (SoundManager.Instance != null)
                    SoundManager.Instance.PlayEnemyWalk();
                GetNewPatrolPoint();
                break;

            case EnemyState.Wait:
                agent.isStopped = true;
                agent.speed = defaultSpeed;
                patrolWaitTimer = Random.Range(minPatrolWaitTime, maxPatrolWaitTime);
                animator.SetFloat("speed", 0f);
                break;

            case EnemyState.Chase:
                agent.speed = 4;
                agent.isStopped = false;
                if (SoundManager.Instance != null)
                    SoundManager.Instance.PlayEnemyRun();
                break;

            case EnemyState.MeleeAttack:
                agent.isStopped = true;
                agent.speed = defaultSpeed;
                attackTimer = attackCooldown;
                if (SoundManager.Instance != null)
                    SoundManager.Instance.PlayEnemyAttackMelee();
                animator.SetFloat("speed", 0f);
                break;

            case EnemyState.Dead:
                agent.isStopped = true;
                agent.speed = defaultSpeed;
                SoundManager.Instance.PlayEnemyDeath();
                animator.SetTrigger(animIDIsDead);
                break;

            case EnemyState.Stunned:
                agent.isStopped = true;
                agent.speed = 0f;
                animator.SetFloat("speed", 0f);
                animator.SetTrigger("isStunned");
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
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, meleeAttackRange);

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(patrolCenter, patrolRadius);
    }
}
