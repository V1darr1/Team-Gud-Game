using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class UnifiedEnemyAI : MonoBehaviour, iEnemy
{
    public enum BehaviorType { Melee, Mage }

    [Header("Core")]
    [SerializeField] private BehaviorType behavior = BehaviorType.Melee;
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private Renderer model;
    private Material _cachedMat;
    [SerializeField] private Transform headPos;      // eyes/aim origin
    [SerializeField] private Animator animator;
    [SerializeField] private DamageableHealth health; // your shared health

    [Header("Perception & Combat")]
    [SerializeField] private int faceTargetSpeed = 10;
    [SerializeField] private int FOV = 180;
    [SerializeField] private float sightRange = 12f;
    [SerializeField] private float attackRange = 2.2f;   // set higher for mages
    [SerializeField] private float timeBetweenAttacks = 1.25f;
    [SerializeField] private LayerMask playerLayer = 1 << 7;        // set to your Player layer
    [SerializeField] private LayerMask visionBlockers = ~0;          // set to: Default, Walls, etc (NOT Player)
    [SerializeField, Range(0.05f, 0.3f)] private float eyeRadius = 0.12f; // spherecast thickness

    [Header("Alert / Investigate")]
    [SerializeField] private float investigateTime = 3f;
    private float _alertTimer;
    private Vector3 _lastKnownPlayerPos;

    [Header("Patrol")]
    [SerializeField] private float walkPointRange = 8f;
    [SerializeField] private LayerMask whatIsGround = ~0;

    // ---------- Melee ----------
    [Header("Melee (Behavior = Melee)")]
    [SerializeField] private Transform weaponHitboxRoot;
    [SerializeField] private Transform hitFrom;            // optional start point
    [SerializeField] private Transform hitTo;              // optional end point
    [SerializeField] private LayerMask targetMask = ~0;
    [SerializeField] private float damage = 10f;
    [SerializeField] private float attackHitWindow = 0.15f;
    [SerializeField] private float meleeHitRadius = 0.9f;  // bump up for big enemy (CHUNK!!)
    private readonly Collider[] _hits = new Collider[6];
    private bool weaponActive;
   

    // ---------- Mage ----------
    [Header("Mage (Behavior = Mage)")]
    [SerializeField] private Transform castPoint;
    [SerializeField] private SpellData primarySpell;  // Implements iSpell
    private float _cooldownTimer;

    // ---------- Runtime ----------
    private bool alreadyAttacked;
    private Color colorOrig;
    private Vector3 walkPoint;
    private bool walkPointSet;
    private bool _didPostBakeSnap;

    // --- Unstuck ---
    [SerializeField] private float stuckCheckInterval = 0.25f;
    [SerializeField] private float stuckIfSpeedBelow = 0.05f;
    [SerializeField] private float stuckForSeconds = 0.8f;
    [SerializeField] private float sideStepDistance = 1.2f;

    float _stillTimer;
    Vector3 _lastPos;

    bool EnsureAgentOnNavMesh()
    {
        if (!agent || !agent.isActiveAndEnabled) return false;

        if (agent.isOnNavMesh) return true;

        // Try to snap to the nearest valid point under/near us
        if (NavMesh.SamplePosition(transform.position, out var hit, 3f, agent.areaMask))
        {
            agent.Warp(hit.position);        // Warp does not require being on-navmesh beforehand
            return agent.isOnNavMesh;
        }

        return false; // nowhere valid nearby yet
    }

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        health = GetComponent<DamageableHealth>();

        if (!model) model = GetComponentInChildren<Renderer>();
        if (model)
        {
            _cachedMat = model.material;
            colorOrig = _cachedMat.color;
        }

        if (agent)
        {
            agent.updateRotation = false;
            agent.autoRepath = true;
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
            agent.stoppingDistance = Mathf.Max(agent.stoppingDistance, 0.6f);

            // ensure we’re actually placed on the NavMesh after the room finishes baking
            StartCoroutine(SnapAgentNextFrame());
        }
        else
        {
            Debug.LogWarning($"{name}: NavMeshAgent missing.", this);
        }

        InvokeRepeating(nameof(CheckStuck), stuckCheckInterval, stuckCheckInterval);
    }

    void OnEnable()
    {
        // if this enemy is enabled after a room transition, snap again next frame
        if (agent && agent.enabled) StartCoroutine(SnapAgentNextFrame());
    }

    private IEnumerator SnapAgentNextFrame()
    {
        yield return null;

        if (!agent) yield break;

        //try to fins navmesh under the current transform
        if (NavMesh.SamplePosition(agent.transform.position, out var hit, 3.0f, agent.areaMask))
        {
            agent.Warp(hit.position);
            _didPostBakeSnap = true;
        }
    }

    void Update()
    {
        if (!EnsureAgentOnNavMesh()) return;
        
        // Let DamageableHealth own alive/dead state
        if (health && !health.IsAlive) { HandleDeath(); return; }

        var player = gameManager.instance.player;
        if (!player) return;

        if (_cooldownTimer > 0f) _cooldownTimer -= Time.deltaTime;

        float dist = Vector3.Distance(transform.position, player.transform.position);
        bool inSight = dist <= sightRange && CanSeePlayer();
        bool inRange = dist <= attackRange;

        // Decay alert
        if (_alertTimer > 0f) _alertTimer -= Time.deltaTime;

        // If alerted (recently hit) but no sight, move to last known position
        if (_alertTimer > 0f && agent && !CanSeePlayer())
        {
            if (NavMesh.SamplePosition(_lastKnownPlayerPos, out var hit, 1.2f, agent.areaMask))
                agent.SetDestination(hit.position);
            else
                agent.SetDestination(_lastKnownPlayerPos);

            FaceMoveDirection();
            if (animator) animator.SetFloat("Speed", agent.velocity.magnitude);
            return; // skip regular logic this frame
        }

        if (!inSight && !inRange) Patrolling();
        else if (inSight && !inRange) ChasePlayer();
        else if (inSight && inRange) AttackPlayer();

        if (animator && agent) animator.SetFloat("Speed", agent.velocity.magnitude);
    }

    bool CanSeePlayer()
    {
        var player = gameManager.instance.player;
        if (!player) return false;

        // angle check (use half-FOV)
        Vector3 toPlayer = (player.transform.position + Vector3.up * 0.9f) - headPos.position;
        if (Vector3.Angle(toPlayer, transform.forward) > (FOV * 0.5f)) return false;

        float dist = toPlayer.magnitude;
        Vector3 dir = toPlayer / Mathf.Max(dist, 0.0001f);

        // SphereCast against both player + blocking geometry and see what we hit first
        int mask = playerLayer | visionBlockers;
        if (Physics.SphereCast(headPos.position, eyeRadius, dir, out RaycastHit hit, sightRange, mask, QueryTriggerInteraction.Ignore))
        {
            // Must hit the player FIRST; if a wall is closer, LOS is blocked
            if (hit.collider.CompareTag("Player"))
            {
                if (agent && agent.remainingDistance <= agent.stoppingDistance) FaceTarget();
                return true;
            }
        }
        return false;
    }

    // --------- Locomotion ---------
    void Patrolling()
    {
        if (!agent || !EnsureAgentOnNavMesh()) return;


        if (!walkPointSet) SearchWalkPoint();
        if (walkPointSet) agent.SetDestination(walkPoint);

        FaceMoveDirection();

        if (agent.hasPath && agent.pathStatus == NavMeshPathStatus.PathPartial)
        {
            // re-pick point if partial path
            walkPointSet = false;
        }

        if ((transform.position - walkPoint).magnitude < 1f)
            walkPointSet = false;
    }

    void SearchWalkPoint()
    {
        // pick a random direction on XZ plane
        Vector2 r = Random.insideUnitCircle * walkPointRange;
        Vector3 candidate = new Vector3(transform.position.x + r.x, transform.position.y, transform.position.z + r.y);

        // snap to the NavMesh near the candidate
        if (NavMesh.SamplePosition(candidate, out var hit, 2.0f, agent ? agent.areaMask : NavMesh.AllAreas))
        {
            walkPoint = hit.position;
            walkPointSet = true;
            return;
        }

        walkPointSet = false;
    }

    void ChasePlayer()
    {
        if (!agent || !EnsureAgentOnNavMesh()) return;

        var player = gameManager.instance.player;
        if (!player) return;

        // aim a point a bit before the player, so we don't try to overlap them or walls behind them
        Vector3 toPlayer = player.transform.position - transform.position;
        Vector3 desired = player.transform.position - toPlayer.normalized * Mathf.Max(agent.stoppingDistance, 0.6f);

        // sample on the mesh
        if (NavMesh.SamplePosition(desired, out var hit, 1.5f, agent.areaMask))
            agent.SetDestination(hit.position);
        else if (NavMesh.SamplePosition(player.transform.position, out hit, 1.5f, agent.areaMask))
            agent.SetDestination(hit.position);
        else
            agent.ResetPath();

        agent.isStopped = false;

        if (behavior == BehaviorType.Melee) FaceTargetHard();
        else FaceMoveDirection();

        // if the path is partial/invalid for long, fall back to a patrol point
        if (!agent.hasPath || agent.pathStatus != NavMeshPathStatus.PathComplete)
        {
            SearchWalkPoint();
            if (walkPointSet) agent.SetDestination(walkPoint);
        }
    }

    void FaceTarget()
    {
        var player = gameManager.instance.player;
        if (!player) return;

        Vector3 flat = player.transform.position - transform.position;
        flat.y = 0f; if (flat.sqrMagnitude < 0.0001f) return;

        Quaternion target = Quaternion.LookRotation(flat);
        transform.rotation = Quaternion.Lerp(transform.rotation, target, Time.deltaTime * faceTargetSpeed);
    }

    public void OnDamaged(Vector3 attackerPos)
    {
        _lastKnownPlayerPos = attackerPos;
        _alertTimer = investigateTime;
    }

    // --------- Attack ---------
    void AttackPlayer()
    {
        if (!agent || !EnsureAgentOnNavMesh()) return;

        agent.isStopped = true;

        agent.SetDestination(transform.position); // stop to attack
        FaceTarget();

        if (alreadyAttacked) return;
        alreadyAttacked = true;

        if (behavior == BehaviorType.Melee)
        {
            if (animator) animator.SetTrigger("Attack");
            StartCoroutine(MeleeSwingWindow());
        }
        else // Mage
        {
            CastSpell();
        }

        Invoke(nameof(ResetAttack), timeBetweenAttacks);
    }
   
    void ResetAttack() => alreadyAttacked = false;

    // --- Melee ---
    IEnumerator MeleeSwingWindow()
    {
        yield return new WaitForSeconds(0.05f); // small wind-up
        ToggleWeapon(true);
        DoMeleeHit();
        yield return new WaitForSeconds(attackHitWindow);
        ToggleWeapon(false);
    }

    void ToggleWeapon(bool active)
    {
        if (!weaponHitboxRoot || weaponActive == active) return;
        weaponActive = active;

        foreach (var col in weaponHitboxRoot.GetComponentsInChildren<Collider>())
            if (col && col.isTrigger) col.enabled = active;
    }

    void DoMeleeHit()
    {
        // Decide capsule endpoints
        Vector3 a, b;
        if (hitFrom && hitTo)
        {
            a = hitFrom.position;
            b = hitTo.position;
        }
        else
        {
            Vector3 basePos = weaponHitboxRoot ? weaponHitboxRoot.position : transform.position + transform.forward * 0.8f;
            a = basePos;
            b = basePos + transform.forward * 0.8f;
        }

        int count = Physics.OverlapCapsuleNonAlloc(
            a, b, meleeHitRadius,
            _hits,
            targetMask,
            QueryTriggerInteraction.Ignore // don't hit our own trigger colliders
        );

        Transform myRoot = transform.root;
        var dedupe = new System.Collections.Generic.HashSet<Transform>();

        for (int i = 0; i < count; i++)
        {
            var col = _hits[i];
            if (!col) continue;

            Transform tgtRoot = col.transform.root;
            if (tgtRoot == myRoot) continue;        // ignore self
            if (!dedupe.Add(tgtRoot)) continue;     // avoid multi-hit on same target

            var dmg = col.GetComponentInParent<iDamageable>();
            if (dmg != null && dmg.IsAlive)
                dmg.ApplyDamage(damage);
        }
    }

    // --- Mage ---
    void CastSpell()
    {
        if (animator) animator.SetTrigger("Cast");
        if (!primarySpell) return;
        if (!CanCast(primarySpell)) return;

        Vector3 origin = castPoint ? castPoint.position : headPos.position;
        Vector3 dir = (gameManager.instance.player.transform.position - origin).normalized;

        BeginCast(primarySpell, origin, dir);
    }

    public bool CanCast(iSpell spell)
    {
        if (spell == null) return false;
        if (_cooldownTimer > 0f) return false;
        return true;
    }

    public void BeginCast(iSpell spell, Vector3 origin, Vector3 direction)
    {
        _cooldownTimer = spell.Cooldown;

        // If the spell has no projectile prefab, we can't shoot a projectile.
        // (For hitscan later, we'll add a different path.)
        if (spell.ProjectilePrefab == null)
        {
            Debug.LogWarning($"Spell '{spell.Id}' has no projectile prefab assigned.");
            return;
        }

        // Actually spawn the projectile GameObject.
        Vector3 spawnPos = origin + direction; // slight offset
        GameObject proj = Instantiate(spell.ProjectilePrefab, spawnPos, Quaternion.LookRotation(direction));

        // IMPORTANT: Grab ANY component that implements iProjectile and initialize it.
        var projectile = proj.GetComponent<iProjectile>();
        if (projectile != null)
        {
            projectile.Init(damage: spell.Damage, direction: direction, gameObject);
        }
    }

    // --------- Feedback / Death ---------
    public void FlashDamage()
    {
        if (!_cachedMat) return;
        StartCoroutine(FlashRed());
    }

    private IEnumerator FlashRed()
    {
        var prev = _cachedMat.color;
        _cachedMat.color = Color.red;
        yield return new WaitForSeconds(0.1f);
        _cachedMat.color = prev;
    }

    private void HandleDeath()
    {
        gameManager.instance.updateGameGoal(-1);

        if (agent && agent.isOnNavMesh)
        {
            agent.ResetPath();
            agent.isStopped = true;
        }

        if (animator) animator.SetTrigger("Die");

        Destroy(gameObject, 5f); // let death anim play
    }

    // Gizmos
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(transform.position, sightRange);
        Gizmos.color = Color.red; Gizmos.DrawWireSphere(transform.position, attackRange);
        if (weaponHitboxRoot) { Gizmos.color = Color.magenta; Gizmos.DrawWireSphere(weaponHitboxRoot.position, 0.9f); }
    }

    void FaceMoveDirection()
    {
        if (!agent) return;
        Vector3 dir = agent.velocity.sqrMagnitude > 0.0001f
            ? agent.velocity
            : (agent.hasPath ? (agent.steeringTarget - transform.position) : Vector3.zero);
        if (dir.sqrMagnitude < 0.0001f) return;
        dir.y = 0f;
        transform.rotation = Quaternion.Lerp(
            transform.rotation,
            Quaternion.LookRotation(dir),
            Time.deltaTime * faceTargetSpeed
        );
    }

    // Rotate to face the player (used while chasing for melee)
    void FaceTargetHard()
    {
        var player = gameManager.instance.player; if (!player) return;
        Vector3 dir = player.transform.position - transform.position; dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) return;
        transform.rotation = Quaternion.Lerp(
            transform.rotation,
            Quaternion.LookRotation(dir),
            Time.deltaTime * faceTargetSpeed
        );
    }

    void CheckStuck()
    {
        if (!agent || !agent.isOnNavMesh) return;

        // should we be moving?
        bool shouldMove = agent.hasPath && !agent.pathPending && agent.remainingDistance > agent.stoppingDistance;

        float moved = (transform.position - _lastPos).magnitude;
        _lastPos = transform.position;

        if (!shouldMove) { _stillTimer = 0f; return; }

        if (moved < stuckIfSpeedBelow) _stillTimer += stuckCheckInterval;
        else _stillTimer = 0f;

        if (_stillTimer < stuckForSeconds) return;

        // --- escape: side-step and re-path ---
        _stillTimer = 0f;
        agent.ResetPath();

        Vector3 left = Vector3.Cross(Vector3.up, transform.forward).normalized;
        Vector3 right = -left;

        if (!TryStep(left) && !TryStep(right))
            TryStep(Random.insideUnitSphere);

        // after a short delay, try again to the true target (player or patrol)
        StartCoroutine(RepathSoon());
    }

    bool TryStep(Vector3 dir)
    {
        Vector3 target = transform.position + dir.normalized * sideStepDistance;
        if (NavMesh.SamplePosition(target, out var hit, 1.5f, agent.areaMask))
        {
            agent.SetDestination(hit.position);
            return true;
        }
        return false;
    }

    // ----HEAD TRACKING----
    void OnAnimatorIK(int layerIndex)
    {
        if (!animator) return;

        var player = gameManager.instance?.player;
        if (!player) return;

        // Point of interest (you can offset upward a bit to aim at the chest/head)
        Vector3 targetPos = player.transform.position + Vector3.up * 1.5f;

        // How strongly the enemy looks at the target (blendable)
        float weight = 1.0f;

        // IK settings
        animator.SetLookAtWeight(
            weight,        // global weight
            0.3f,          // body weight
            0.6f,          // head weight
            1.0f,          // eyes weight
            0.5f           // clamp weight (limits max rotation)
        );

        // Apply look-at target
        animator.SetLookAtPosition(targetPos);
    }

    IEnumerator RepathSoon()
    {
        yield return new WaitForSeconds(0.35f);

        var player = gameManager.instance.player;
        if (player && CanSeePlayer())
            ChasePlayer();
        else
            Patrolling();
    }
    public float Damage  // public accessor for scaling
    { get => damage;
    set => damage = value; }
}