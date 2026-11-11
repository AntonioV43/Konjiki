using UnityEngine;

/// <summary>
/// 2D enemy movement using the custom grid-based PathAgent (A* over Tilemaps). States: Idle (roams near spawn), Chase, Search (checks the player's likely
/// escape route, then wanders the area), Returning (walks back to spawn). Gated by a
/// field-of-view cone and raycast line-of-sight, so the enemy only reacts to a player it can
/// actually see. Requires GridPathfinder to be built (RoomController.BuildPathGrid).
/// </summary>
[RequireComponent(typeof(PathAgent))]
public class EnemyMovement : MonoBehaviour
{
    private enum EnemyState
    {
        Idle,
        Chase,
        Search,
        Returning
    }

    // Sub-phases within Search: first walk to where the player was last seen, then (if we have
    // a good read on which way they were running) check further along that route, then just
    // wander the immediate area before giving up.
    private enum SearchPhase
    {
        ApproachLastKnown,
        MoveToEscapePoint,
        Wander
    }

    [Header("Target")]
    [Tooltip("Leave empty to auto-find the object tagged 'Player' on Start.")]
    [SerializeField] private Transform target;

    [Header("Vision")]
    [Tooltip("Max distance the enemy can see the player at all.")]
    [SerializeField] private float sightRange = 8f;

    [Tooltip("Total field-of-view angle in degrees, centered on the enemy's facing direction. "
           + "E.g. 90 means 45 degrees to either side.")]
    [SerializeField] private float fovAngle = 90f;

    [Tooltip("Layers that block line of sight (walls, obstacles). Should NOT include the Player layer.")]
    [SerializeField] private LayerMask obstacleMask;

    [Header("Idle Roaming")]
    [Tooltip("How far from the spawn point the enemy wanders while idle.")]
    [SerializeField] private float idleRoamRadius = 3f;

    [Tooltip("How often the enemy picks a new nearby point to roam to while idle.")]
    [SerializeField] private float idleRoamInterval = 3f;

    [Tooltip("Random +/- variance on idleRoamInterval so multiple enemies don't move in sync.")]
    [SerializeField] private float idleRoamIntervalVariance = 1f;

    [Header("Search")]
    [Tooltip("How long the enemy searches (escape route + wandering) before giving up and returning home.")]
    [SerializeField] private float searchDuration = 5f;

    [Tooltip("How far from its search points the enemy wanders while looking.")]
    [SerializeField] private float searchWanderRadius = 2.5f;

    [Tooltip("How often the enemy picks a new nearby point to wander to while searching.")]
    [SerializeField] private float wanderInterval = 1f;

    [Tooltip("If the agent's velocity stays near zero this long while approaching a search point "
           + "(e.g. it's stuck against a wall), treat it as 'arrived' anyway so it doesn't lock up.")]
    [SerializeField] private float stuckTimeout = 1f;

    [Header("Escape Prediction")]
    [Tooltip("How responsive the player-velocity estimate is: 0 = never updates, 1 = instant/noisy.")]
    [SerializeField, Range(0f, 1f)] private float velocitySmoothing = 0.5f;

    [Tooltip("Minimum estimated player speed before the enemy bothers predicting an escape route "
           + "(below this, the player was basically standing still, so just search where they were).")]
    [SerializeField] private float minEscapeSpeed = 0.5f;

    [Tooltip("How far past the last known position to check, along the player's estimated direction of travel.")]
    [SerializeField] private float escapeSearchDistance = 3f;

    [Header("Sprite Facing")]
    [SerializeField] private bool flipSpriteTowardsFacing = true;

    [Header("Debug")]
    [SerializeField] private bool logStateChanges = false;
    [SerializeField] private bool drawVisionGizmos = true;

    private PathAgent agent;
    private SpriteRenderer spriteRenderer;
    private EnemyState currentState = EnemyState.Idle;
    private SearchPhase searchPhase;

    // Facing is tracked independently of the sprite flip, since FOV needs a real direction vector.
    private Vector2 facingDirection = Vector2.right;

    private Vector2 initialPosition;
    private Vector2 lastKnownPosition;
    private Vector2 escapePoint;
    private Vector2 wanderCenter;

    private Vector2 previousTargetPosition;
    private bool hasPreviousTargetPosition;
    private Vector2 playerVelocityEstimate;

    private float idleRoamTimer;
    private float searchTimer;
    private float wanderTimer;
    private float stuckTimer;

    private void Awake()
    {
        agent = GetComponent<PathAgent>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        initialPosition = transform.position;

        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                target = player.transform;
            else
                Debug.LogWarning($"{name}: Movement has no target and no 'Player' tagged object was found.");
        }
    }

    private void Update()
    {
        EvaluateState();
        UpdateFacing();
        UpdateSpriteFacing();
    }

    // ---------------- State Machine ----------------

    private void EvaluateState()
    {
        bool canSee = target != null && CanSeePlayer();

        switch (currentState)
        {
            case EnemyState.Idle:
                if (canSee)
                {
                    TrackLastKnownPosition();
                    ChangeState(EnemyState.Chase);
                }
                else
                {
                    UpdateRoam(initialPosition, idleRoamRadius, ref idleRoamTimer,
                               idleRoamInterval, idleRoamIntervalVariance);
                }
                break;

            case EnemyState.Chase:
                if (canSee)
                {
                    TrackLastKnownPosition(); // keep it fresh, and keep refining the velocity estimate
                    agent.SetDestination(lastKnownPosition);
                }
                else
                {
                    ChangeState(EnemyState.Search); // lastKnownPosition already holds the last visible spot
                }
                break;

            case EnemyState.Search:
                if (canSee)
                {
                    TrackLastKnownPosition();
                    ChangeState(EnemyState.Chase);
                    break;
                }

                UpdateSearch();
                break;

            case EnemyState.Returning:
                if (canSee)
                {
                    TrackLastKnownPosition();
                    ChangeState(EnemyState.Chase);
                    break;
                }

                if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
                    ChangeState(EnemyState.Idle);
                break;
        }
    }

    private void ChangeState(EnemyState newState)
    {
        if (newState == currentState) return;

        currentState = newState;

        switch (newState)
        {
            case EnemyState.Idle:
                agent.isStopped = false;
                idleRoamTimer = 0f; // pick a roam point right away
                break;

            case EnemyState.Chase:
                agent.isStopped = false;
                agent.SetDestination(target.position);
                break;

            case EnemyState.Search:
                agent.isStopped = false;
                searchPhase = SearchPhase.ApproachLastKnown;
                agent.SetDestination(lastKnownPosition);
                searchTimer = 0f;
                stuckTimer = 0f;
                break;

            case EnemyState.Returning:
                agent.isStopped = false;
                agent.SetDestination(initialPosition);
                break;
        }

        if (logStateChanges)
            Debug.Log($"{name}: {currentState}");
    }

    // ---------------- Search ----------------

    private void UpdateSearch()
    {
        switch (searchPhase)
        {
            case SearchPhase.ApproachLastKnown:
                if (HasArrivedOrStuck())
                {
                    if (playerVelocityEstimate.magnitude >= minEscapeSpeed)
                    {
                        escapePoint = lastKnownPosition + playerVelocityEstimate.normalized * escapeSearchDistance;
                        agent.SetDestination(escapePoint);
                        searchPhase = SearchPhase.MoveToEscapePoint;
                        stuckTimer = 0f;
                    }
                    else
                    {
                        BeginWander(lastKnownPosition);
                    }
                }
                break;

            case SearchPhase.MoveToEscapePoint:
                if (HasArrivedOrStuck())
                    BeginWander(escapePoint);
                break;

            case SearchPhase.Wander:
                UpdateRoam(wanderCenter, searchWanderRadius, ref wanderTimer, wanderInterval, 0f);
                break;
        }

        searchTimer += Time.deltaTime;
        if (searchTimer >= searchDuration)
            ChangeState(EnemyState.Returning);
    }

    private void BeginWander(Vector2 center)
    {
        wanderCenter = center;
        wanderTimer = 0f; // pick a wander point immediately
        searchPhase = SearchPhase.Wander;
        stuckTimer = 0f;
    }

    /// <summary>True once the agent has reached its current destination, or has been stuck
    /// (near-zero velocity) for stuckTimeout seconds - e.g. the destination is against a wall.</summary>
    private bool HasArrivedOrStuck()
    {
        bool arrived = !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance;

        if (agent.velocity.sqrMagnitude < 0.01f)
            stuckTimer += Time.deltaTime;
        else
            stuckTimer = 0f;

        return arrived || stuckTimer >= stuckTimeout;
    }

    // ---------------- Roaming (shared by Idle and Search-wander) ----------------

    private void UpdateRoam(Vector2 center, float radius, ref float timer, float interval, float intervalVariance)
    {
        timer -= Time.deltaTime;

        bool needsNewPoint = timer <= 0f
            || (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance);

        if (needsNewPoint)
        {
            Vector2 point = center + Random.insideUnitCircle * radius;
            agent.SetDestination(point);
            timer = interval + Random.Range(-intervalVariance, intervalVariance);
        }
    }

    // ---------------- Perception ----------------

    /// <summary>True if the player is within range, inside the FOV cone, and not blocked by an obstacle.</summary>
    private bool CanSeePlayer()
    {
        Vector2 toTarget = (Vector2)target.position - (Vector2)transform.position;
        float distance = toTarget.magnitude;

        if (distance > sightRange) return false;

        Vector2 directionToTarget = toTarget.normalized;
        float angle = Vector2.Angle(facingDirection, directionToTarget);
        if (angle > fovAngle * 0.5f) return false;

        // Raycast only as far as the target - anything blocking in between counts as cover.
        RaycastHit2D hit = Physics2D.Raycast(transform.position, directionToTarget, distance, obstacleMask);
        if (hit.collider != null) return false;

        return true;
    }

    /// <summary>Updates lastKnownPosition and refines a smoothed estimate of the player's velocity,
    /// used later to guess which way they ran off to.</summary>
    private void TrackLastKnownPosition()
    {
        Vector2 currentPos = target.position;

        if (hasPreviousTargetPosition && Time.deltaTime > 0f)
        {
            Vector2 instantVelocity = (currentPos - previousTargetPosition) / Time.deltaTime;
            playerVelocityEstimate = Vector2.Lerp(playerVelocityEstimate, instantVelocity, velocitySmoothing);
        }

        previousTargetPosition = currentPos;
        hasPreviousTargetPosition = true;
        lastKnownPosition = currentPos;
    }

    // ---------------- Facing ----------------

    private void UpdateFacing()
    {
        // Derive facing from the agent's actual movement velocity, in any state -
        // idle roaming now moves too, so this covers it naturally.
        if (agent.velocity.sqrMagnitude > 0.01f)
            facingDirection = agent.velocity.normalized;
    }

    private void UpdateSpriteFacing()
    {
        if (!flipSpriteTowardsFacing || spriteRenderer == null) return;

        if (Mathf.Abs(facingDirection.x) > 0.01f)
            spriteRenderer.flipX = facingDirection.x < 0f;
    }

    // ---------------- Debug ----------------

    private void OnDrawGizmosSelected()
    {
        if (!drawVisionGizmos) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, sightRange);

        // FOV cone
        Gizmos.color = Color.cyan;
        Vector3 forward = facingDirection.normalized;
        Vector3 leftEdge = Quaternion.Euler(0, 0, fovAngle * 0.5f) * forward;
        Vector3 rightEdge = Quaternion.Euler(0, 0, -fovAngle * 0.5f) * forward;
        Gizmos.DrawLine(transform.position, transform.position + leftEdge * sightRange);
        Gizmos.DrawLine(transform.position, transform.position + rightEdge * sightRange);

        // Idle roam area (only meaningful once initialPosition is set, i.e. at runtime)
        if (Application.isPlaying)
        {
            if (currentState == EnemyState.Idle)
            {
                Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
                Gizmos.DrawWireSphere(initialPosition, idleRoamRadius);
            }
            else if (currentState == EnemyState.Search)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawWireSphere(lastKnownPosition, 0.3f);

                if (searchPhase == SearchPhase.MoveToEscapePoint || searchPhase == SearchPhase.Wander)
                {
                    Gizmos.color = Color.blue;
                    Gizmos.DrawLine(lastKnownPosition, escapePoint);
                    Gizmos.DrawWireSphere(escapePoint, 0.3f);
                }

                Gizmos.color = new Color(1f, 0f, 1f, 0.3f);
                Gizmos.DrawWireSphere(wanderCenter, searchWanderRadius);
            }
            else if (currentState == EnemyState.Returning)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(initialPosition, 0.3f);
            }
        }
    }
}