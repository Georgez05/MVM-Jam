using UnityEngine;
using UnityEngine.AI;

public class EnemyAI : MonoBehaviour
{
    public enum EnemyState { Patrol, Chase, Attack }

    public EnemyState currentState;

    [Header("References")]
    public Transform[] patrolPoints;
    public Transform player;
    private NavMeshAgent agent;

    [Header("Detection Settings")]
    public float detectionRange = 10f;
    public float attackRange = 2f;
    public LayerMask playerLayer;

    [Header("Attack Settings")]
    public float attackCooldown = 1.5f;
    private float lastAttackTime;


    private int currentPatrolIndex;
    
    public bool playerInSight;
    public bool playerInAttackRange;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
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
        playerInSight = Physics.CheckSphere(transform.position, detectionRange, playerLayer);
        playerInAttackRange = Physics.CheckSphere(transform.position, attackRange, playerLayer);
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

        agent.destination = patrolPoints[currentPatrolIndex].position;

        if (!agent.pathPending && agent.remainingDistance < 0.5f)
        {
            currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
        }
    }

    void Chase()
    {
        agent.SetDestination(player.position);
    }

    void Attack()
    {
        agent.ResetPath();
        transform.LookAt(player);

        if (Time.time > lastAttackTime + attackCooldown)
        {
            // Attack logic here
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
