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
    [SerializeField] private Transform headPos;      // eyes/aim origin
    [SerializeField] private Animator animator;
    [SerializeField] private DamageableHealth health; // your shared health

    [Header("Perception & Combat")]
    [SerializeField] private int faceTargetSpeed = 10;
    [SerializeField] private int FOV = 180;
    [SerializeField] private float sightRange = 12f;
    [SerializeField] private float attackRange = 2.2f;   // set higher for mages
    [SerializeField] private float timeBetweenAttacks = 1.25f;

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

    void Start()
    {
        gameManager.instance.updateGameGoal(1);

        if (!agent) agent = GetComponent<NavMeshAgent>();
            agent.stoppingDistance = Mathf.Max(0.05f, attackRange * 0.75f);
        if (!model) model = GetComponentInChildren<Renderer>();
        if (!headPos) headPos = transform;
        if (!health) health = GetComponent<DamageableHealth>();

        if (agent) agent.updateRotation = false;
        if (model) colorOrig = model.material.color;

        if (behavior == BehaviorType.Mage && !castPoint) castPoint = headPos;
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

        if (!inSight && !inRange) Patrolling();
        else if (inSight && !inRange) ChasePlayer();
        else if (inSight && inRange) AttackPlayer();

        if (animator && agent) animator.SetFloat("Speed", agent.velocity.magnitude);
    }

    bool CanSeePlayer()
    {
        var player = gameManager.instance.player;
        if (!player) return false;

        Vector3 toPlayer = player.transform.position - headPos.position;
        if (Vector3.Angle(toPlayer, transform.forward) > FOV) return false;

        if (Physics.Raycast(headPos.position, toPlayer.normalized, out RaycastHit hit, sightRange))
        {
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
        var player = gameManager.instance.player;
        if (!player || !agent) return;

        agent.isStopped = false;
        agent.SetDestination(player.transform.position);

        if (behavior == BehaviorType.Melee)
            FaceTargetHard();
        else
            FaceMoveDirection();
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
        if (!model) return;
        StartCoroutine(FlashRed());
    }

    IEnumerator FlashRed()
    {
        var mat = model.material;
        var prev = mat.color;
        mat.color = Color.red;
        yield return new WaitForSeconds(0.1f);
        mat.color = prev;
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