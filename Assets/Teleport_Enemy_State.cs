using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class TeleportingEnemyStateMachine : MonoBehaviour
{
    public enum EnemyState { Idle, Patrol, Attack, Teleport }

    public EnemyState currentState = EnemyState.Idle;

    [Header("Detection Settings")]
    public float rayDistance = 10f;
    public float circleCheckRadius = 2f;
    public LayerMask playerLayer;

    [Header("Movement Settings")]
    public float moveSpeed = 3f;
    public float idleDuration = 2f; // Original idle duration
    public Transform[] waypoints;

    [Header("Health")]
    public float Health;
    [Header("Debug Info")]
    public bool check = false; // auto-updated: true if player in circle
    private GameObject detectedPlayer;

    private int currentWaypointIndex = 0;
    private float idleTimer = 0f;
    private float idleWaitTime = 1f; // Wait 1 second in idle before moving
    private Rigidbody2D rb;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        if(Health<=0)
        {
            Destroy(gameObject);
        }
        UpdateCircleCheck(); // always update circle detection

        switch (currentState)
        {
            case EnemyState.Idle:
                IdleState();
                break;
            case EnemyState.Patrol:
                PatrolState();
                break;
            case EnemyState.Attack:
                AttackState();
                break;
            case EnemyState.Teleport:
                TeleportState();
                break;
        }
    }

    // ---------------- UNIVERSAL CHECK FUNCTIONS ----------------

    bool IsPlayerInRaycast()
    {
        RaycastHit2D hit = Physics2D.Raycast(transform.position, transform.right, rayDistance, playerLayer);
        return hit.collider != null && hit.collider.CompareTag("Player");
    }

    void UpdateCircleCheck()
    {
        Collider2D hit = Physics2D.OverlapCircle(transform.position, circleCheckRadius, playerLayer);
        check = (hit != null && hit.CompareTag("Player"));
    }

    // ---------------- STATE FUNCTIONS ----------------

    void IdleState()
    {
        idleTimer += Time.deltaTime;

        if (idleTimer >= idleWaitTime) // Wait for 1 second
        {
            idleTimer = 0f;
            currentState = EnemyState.Patrol;
        }
    }

    void PatrolState()
    {
        if (waypoints.Length == 0) return;

        Transform target = waypoints[currentWaypointIndex];
        float distance = Vector2.Distance(transform.position, target.position);

        if (distance > 0.1f)
        {
            MoveTowards(target.position);
        }
        else
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y); // stop horizontal movement
            currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
            currentState = EnemyState.Idle; // switch to Idle when done patrolling
        }

        if (check)
        {
            detectedPlayer = GameObject.FindGameObjectWithTag("Player");
            currentState = EnemyState.Attack;
        }

        else if (IsPlayerInRaycast())
        {
            TeleportState();
        }
    }

    void AttackState()
    {
        Debug.Log("Enemy attacking...");

        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y); // stop horizontal movement while attacking

        if (IsPlayerInRaycast())
        {
            Debug.Log("Player inside teleport circle!");
            currentState = EnemyState.Teleport;
        }
    }
    void TeleportState()
    {
        if (waypoints.Length > 0 && detectedPlayer != null)
        {
            // Initialize the farthest distance and the index of the farthest waypoint
            float maxDistance = 0f;
            int farthestWaypointIndex = 0;

            // Loop through all waypoints and find the one farthest from the player
            for (int i = 0; i < waypoints.Length; i++)
            {
                float distanceToPlayer = Vector2.Distance(detectedPlayer.transform.position, waypoints[i].position);

                if (distanceToPlayer > maxDistance)
                {
                    maxDistance = distanceToPlayer;
                    farthestWaypointIndex = i;
                }
            }

            // Teleport the enemy to the farthest waypoint
            transform.position = waypoints[farthestWaypointIndex].position;
        }

        // Switch state to Attack after teleporting
        currentState = EnemyState.Attack;
    }


    // ---------------- MOVEMENT ----------------

    void MoveTowards(Vector2 target)
    {
        Vector2 direction = (target - (Vector2)transform.position).normalized;
        rb.linearVelocity = new Vector2(direction.x * moveSpeed, rb.linearVelocity.y); // maintain vertical velocity (gravity, etc.)
    }

    // ---------------- DEBUG GIZMOS ----------------
    public void Damage(float value)
    {
        Health -= value;
    }
    void OnDrawGizmos()
    {
        // Raycast (thicker line using a wire cube)
        Gizmos.color = Color.red;
        Gizmos.DrawCube(transform.position + transform.right * rayDistance / 2, new Vector3(rayDistance, 0.2f, 0)); // makes the ray a bit thicker

        // Circle detection
        Gizmos.color = check ? Color.green : Color.gray;
        Gizmos.DrawWireSphere(transform.position, circleCheckRadius);

        // Waypoints
        if (waypoints != null)
        {
            Gizmos.color = Color.yellow;
            foreach (var point in waypoints)
            {
                if (point != null)
                    Gizmos.DrawWireSphere(point.position, 0.2f);
            }
        }
    }
}
