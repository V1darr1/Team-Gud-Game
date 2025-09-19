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

        if (agent) agent.updateRotation = false;
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
        if (!agent) return;

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
        float rx = Random.Range(-walkPointRange, walkPointRange);
        float rz = Random.Range(-walkPointRange, walkPointRange);
        Vector3 candidate = new Vector3(transform.position.x + rx, transform.position.y + 2f, transform.position.z + rz);

        if (Physics.Raycast(candidate, Vector3.down, out RaycastHit hit, 4f, whatIsGround))
        {
            walkPoint = hit.point;
            walkPointSet = true;
        }
    }

    void ChasePlayer()
    {
        if (!agent) return;
        var player = gameManager.instance.player;
        if (!player) return;

        // Sample near the player to avoid invalid destinations on edges
        if (UnityEngine.AI.NavMesh.SamplePosition(player.transform.position, out var hit, 1.0f, agent.areaMask))
            agent.SetDestination(hit.position);
        else
            agent.SetDestination(player.transform.position);

        agent.isStopped = false;

        if (behavior == BehaviorType.Melee) FaceTargetHard();
        else FaceMoveDirection();

        // If path is invalid or partial for too long, pick a nearby patrol point to unstick
        if (!agent.hasPath || agent.pathStatus == NavMeshPathStatus.PathInvalid)
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
        if (!agent) return;
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

        if (spell.ProjectilePrefab == null)
        {
            Debug.LogWarning($"Spell '{spell.Id}' has no projectile prefab.");
            return;
        }

        Vector3 spawnPos = origin + direction; // slight offset
        GameObject proj = Instantiate(spell.ProjectilePrefab, spawnPos, Quaternion.LookRotation(direction));

        var projectile = proj.GetComponent<SimpleProjectile>();
        if (projectile != null)
        {
            projectile.Init(damage: spell.Damage, direction: direction);
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

        if (agent)
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
}