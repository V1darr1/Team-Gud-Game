using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class MageAi : MonoBehaviour
{
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private Renderer model;
    [SerializeField] private Transform headPos;
    [SerializeField] private Transform castPoint;
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Animator animator;

    [SerializeField] private int HP = 50;
    [SerializeField] private int faceTargetSpeed = 10;
    [SerializeField] private int FOV = 180;

    [SerializeField] private float sightRange = 12f;
    [SerializeField] private float attackRange = 12f;
    [SerializeField] private float attackCooldown = 1.5f;

    private bool alreadyAttacked;
    private bool isDead = false;
    private Color colorOrig;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        gameManager.instance.updateGameGoal(1);
        colorOrig = model.material.color;

        if (agent != null) agent.updateRotation = false;
    }

    // Update is called once per frame
    void Update()
    {
        if(isDead) return;

        var player = gameManager.instance.player;
        if (!player) return;

        float dist= Vector3.Distance(transform.position, player.transform.position);
        bool inSight = dist <= sightRange && canSeePlayer();
        bool inRange = dist <= attackRange;

        if (!inSight && !inRange)
            Patrolling();
        if (inSight && !inRange)
            ChasePlayer();
        if (inSight && inRange)
            AttackPlayer();

        if(animator && agent !=null)
            animator.SetFloat("Speed",agent.velocity.magnitude);
    }
    bool canSeePlayer()
    {
        var player = gameManager.instance.player;
        if (!player) return false;
        
        Vector3 playerDir = player.transform.position-headPos.position;
        float angleToPlayer = Vector3.Angle(playerDir, transform.forward);

        if (angleToPlayer > FOV) return false;

        if (Physics.Raycast(headPos.position, playerDir.normalized, out RaycastHit hit, sightRange))
        {
            if(hit.collider.CompareTag("Player"))
            {
                if (agent.remainingDistance <= agent.stoppingDistance)
                    faceTarget();
                return true;
            }
        }
        return false;  
        
    }
    void Patrolling()
    {
        agent.isStopped = false;
    }
    void ChasePlayer()
    {
        var player = gameManager.instance.player;
        if (player) agent.SetDestination(player.transform.position);
    }
    void AttackPlayer ()
    {
        agent.SetDestination(transform.position);

        if(!alreadyAttacked)
        {
            alreadyAttacked = true;
            CastSpell();
            Invoke(nameof(ResetAttack),attackCooldown);
        }
    }
    void CastSpell ()
    {
        if (animator) animator.SetTrigger("Cast");

        Vector3 spawnPos = castPoint.position + castPoint.forward * 2.0f;
        GameObject spell = Instantiate(projectilePrefab, spawnPos, castPoint.rotation);

        Collider mageCol = GetComponent<Collider>();
        Collider spellCol = spell.GetComponent<Collider>();

        if (mageCol && spellCol)
            Physics.IgnoreCollision(mageCol, spellCol);

        Debug.Log("Fireball spawned at: " + spawnPos);

        if (spellCol)
            StartCoroutine(EnableColliderAfterDelay(spellCol, 0.5f));
    }
    void ResetAttack() => alreadyAttacked = false;

    void faceTarget()
    {
        var player = gameManager.instance.player;
        if(!player) return;

        Vector3 flatDir = player.transform.position - transform.position;
        flatDir.y = 0f;
        if (flatDir.sqrMagnitude < 0.0001f) return;
        Quaternion rot = Quaternion.LookRotation(flatDir);
        transform.rotation = Quaternion.Lerp(transform.rotation,rot,Time.deltaTime* faceTargetSpeed);
    }
    public void takeDamage(int amount)
    {
        if (isDead) return;

        HP-=amount;
        StartCoroutine(flashRed());

        if(HP <=0)
        {
            isDead = true;
            gameManager.instance.updateGameGoal(-1);
            if (animator) animator.SetTrigger("Die");
            Destroy(gameObject, 5f);
        }
    }
    IEnumerator flashRed()
    {
        model.material.color = Color.red;
        yield return new WaitForSeconds(0.1f);
        model.material.color = colorOrig;
    }
    IEnumerator EnableColliderAfterDelay(Collider spellCOl, float delay)
    {
        spellCOl.enabled = false;
        yield return new WaitForSeconds(delay);
        spellCOl.enabled = true;
    }
}
