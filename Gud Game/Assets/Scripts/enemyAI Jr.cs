using UnityEngine;
using System.Collections;
using UnityEngine.AI;


public class enemyAI : MonoBehaviour, IDamageable
{
    [SerializeReference] NavMeshAgent agent;
    [SerializeField] Renderer model;
    [SerializeField] Transform headPos;
    [SerializeField] int HP;
    [SerializeField] int faceTargetSpeed;
    [SerializeField] int FOV;

    
    [SerializeField] LayerMask whatIsGround;
    [SerializeField] LayerMask whatIsPlayer; 

    
    
    [SerializeField] Transform weaponHitboxRoot; 
    [SerializeField] LayerMask targetMask;       
    [SerializeField] int damage;
    [SerializeField] float attackHitWindow; 
    bool weaponActive;
    readonly Collider[] _hits = new Collider[4];

    Color colorOrig;

    float angleToPlayer;

    Vector3 walkPoint;
    bool walkPointSet;
    [SerializeField] float walkPointRange;

    [SerializeField] float timeBetweenAttacks;
    bool alreadyAttacked;

    [SerializeField] float sightRange, attackRange;
    bool playerInSightRange, playerInAttackRange;

    bool playerInTrigger;

    Vector3 playerDir;

    void Start()
    {

        gameManager.instance.updateGameGoal(1);
        ToggleWeapon(false);

        if (weaponHitboxRoot)
        {
            foreach (var hb in weaponHitboxRoot.GetComponentsInChildren<EnemyWeaponHitbox>(true))
            {
                hb.owner = this;
                hb.damage = damage;
            }
        }
        AutoWireWeaponHitboxes();
    }

    void AutoWireWeaponHitboxes()
    {
        if (!weaponHitboxRoot)
        {
            Debug.LogWarning($"{name}: 'weaponHitboxRoot' is not assigned on enemyAI.");
            return;
        }

        var hitboxes = weaponHitboxRoot.GetComponentsInChildren<EnemyWeaponHitbox>(true);
        if (hitboxes == null || hitboxes.Length == 0)
        {
            
        }

        foreach (var hb in hitboxes)
        {
            if (hb == null) continue;
            hb.owner = this;
            hb.damage = damage; // keep your Inspector value in sync
        }
    }



    void Update()
    {
        var player = gameManager.instance.player;
        if (!player) return;

        
        float dist = Vector3.Distance(transform.position, player.transform.position);
        playerInSightRange = dist <= sightRange && canSeePlayer(); 
        playerInAttackRange = dist <= attackRange;

        if (playerInTrigger && playerInSightRange)
        {
            
        }

        if (!playerInSightRange && !playerInAttackRange)
            Patrolling();

        
        if (playerInSightRange && playerInAttackRange)
            AttackPlayer();
        else if (playerInSightRange && !playerInAttackRange)
            ChasePlayer(); 
    }

    bool canSeePlayer()
    {
        var player = gameManager.instance.player;
        if (!player) return false;

        playerDir = player.transform.position - headPos.position;
        angleToPlayer = Vector3.Angle(playerDir, transform.forward);
        Debug.DrawRay(headPos.position, playerDir);

        if (angleToPlayer > FOV) return false;

        
        if (Physics.Raycast(headPos.position, playerDir.normalized, out RaycastHit hit, sightRange))
        {
            if (hit.collider.CompareTag("Player"))
            {
                if (agent && agent.remainingDistance <= agent.stoppingDistance)
                    faceTarget();
                return true;
            }
        }
        return false;
    }

    void Patrolling()
    {
        if (!walkPointSet) SearchWalkPoint();

        if (walkPointSet && agent)
            agent.SetDestination(walkPoint);

        Vector3 distanceToWalkPoint = transform.position - walkPoint;

        if (distanceToWalkPoint.magnitude < 1f)
            walkPointSet = false;
    }

    void AttackPlayer()
    {
        if (agent)
        {
            agent.SetDestination(transform.position);
        }

        agent.SetDestination(transform.position);

        if (!alreadyAttacked)
        {
            alreadyAttacked = true;
            
            StartCoroutine(MeleeSwingWindow());
            Invoke(nameof(ResetAttack), timeBetweenAttacks);
        }
    }

    IEnumerator MeleeSwingWindow()
    {

        yield return new WaitForSeconds(0.05f);
        ToggleWeapon(true);                     
        yield return new WaitForSeconds(attackHitWindow);
        ToggleWeapon(false);
    }

    void ResetAttack()
    {
        alreadyAttacked = false;
    }

    void ChasePlayer()
    {
       
        var player = gameManager.instance.player;
        if (player) agent.SetDestination(player.transform.position);
    }

    void SearchWalkPoint()
    {
        float randomZ = Random.Range(-walkPointRange, walkPointRange);
        float randomX = Random.Range(-walkPointRange, walkPointRange);

        
        walkPoint = new Vector3(transform.position.x + randomX, transform.position.y, transform.position.z + randomZ);

        if (Physics.Raycast(walkPoint + Vector3.up * 2f, Vector3.down, 2f, whatIsGround))
            walkPointSet = true;
    }

    void faceTarget()
    {
        var player = gameManager.instance.player;
        if (!player) return;

        Vector3 flatDir = player.transform.position - transform.position;
        flatDir.y = 0f;
        if (flatDir.sqrMagnitude < 0.0001f) return;
        Quaternion rot = Quaternion.LookRotation(flatDir);
        transform.rotation = Quaternion.Lerp(transform.rotation, rot, Time.deltaTime * faceTargetSpeed);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
            playerInTrigger = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
            playerInTrigger = false;
    }

    public void takeDamage(int amount)
    {
        if (HP > 0)
        {
            HP -= amount;
            StartCoroutine(flashRed());
        }
        if (HP <= 0)
        {
            gameManager.instance.updateGameGoal(-1);
            Destroy(gameObject);
        }
    }

    IEnumerator flashRed()
    {
        model.material.color = Color.red;
        yield return new WaitForSeconds(0.1f);
        model.material.color = colorOrig;
    }

    
    void ToggleWeapon(bool active)
    {
        if (!weaponHitboxRoot) return;
        if (weaponActive == active) return;
        weaponActive = active;

        foreach (var col in weaponHitboxRoot.GetComponentsInChildren<Collider>())
        {
            if (col && col.isTrigger) col.enabled = active;
        }
    }

}
