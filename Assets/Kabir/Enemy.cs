using UnityEngine;

public class EnemyAI2D : MonoBehaviour
{
    public enum EnemyState { Patrol, Chase, Attack }

    public EnemyState currentState;

    [Header("References")]
    public Transform[] patrolPoints;
    public Transform player;
    public Rigidbody2D rb;

    [Header("Detection Settings")]
    public float detectionRange = 10f;
    public float attackRange = 2f;
    public LayerMask playerLayer;

    [Header("Movement Settings")]
    public float moveSpeed = 3f;

    [Header("Attack Settings")]
    public float attackCooldown = 1.5f;
    private float lastAttackTime;

    private int currentPatrolIndex;
    public bool playerInSight;
    public bool playerInAttackRange;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        currentPatrolIndex = 0;
        TransitionToState(EnemyState.Patrol);
    }

    void Update()
    {
        DetectPlayer();
        StateHandler();
    }

    void DetectPlayer()
    {
        Vector2 position = transform.position;

        playerInSight = Physics2D.OverlapCircle(position, detectionRange, playerLayer);
        playerInAttackRange = Physics2D.OverlapCircle(position, attackRange, playerLayer);
    }

    void StateHandler()
    {
        switch (currentState)
        {
            case EnemyState.Patrol:
                Patrol();
                if (playerInSight)
                    TransitionToState(EnemyState.Chase);
                break;

            case EnemyState.Chase:
                Chase();
                if (!playerInSight)
                    TransitionToState(EnemyState.Patrol);
                else if (playerInAttackRange)
                    TransitionToState(EnemyState.Attack);
                break;

            case EnemyState.Attack:
                Attack();
                if (!playerInAttackRange)
                    TransitionToState(EnemyState.Chase);
                break;
        }
    }

    void Patrol()
    {
        if (patrolPoints.Length == 0) return;

        Transform targetPoint = patrolPoints[currentPatrolIndex];
        MoveTowards(targetPoint.position);

        if (Vector2.Distance(transform.position, targetPoint.position) < 0.2f)
        {
            currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
        }
    }

    void Chase()
    {
        MoveTowards(player.position);
    }
    void MoveTowards(Vector2 target)
    {
        Vector2 direction = (target - (Vector2)transform.position).normalized;
        rb.linearVelocity = new Vector2(direction.x * moveSpeed, rb.linearVelocity.y); // keep vertical velocity (gravity)
    }

    void Attack()
    {
        rb.linearVelocity = Vector2.zero;
        Vector2 direction = (player.position - transform.position).normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        rb.rotation = angle;

        if (Time.time > lastAttackTime + attackCooldown)
        {
            Debug.Log("Enemy attacks player!");
            lastAttackTime = Time.time;
        }
    }

    void TransitionToState(EnemyState newState)
    {
        currentState = newState;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
