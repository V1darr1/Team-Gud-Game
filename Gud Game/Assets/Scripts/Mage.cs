using UnityEngine;
using UnityEngine.AI;
using System.Collections;
public class SkeletonMage : MonoBehaviour
{
    public GameObject player;
    public GameObject projectilePrefab;
    public Transform castPoint;
    private NavMeshAgent agent;
    private Animator animator;

  
   [SerializeField] float detectionRange = 12f;
   [SerializeField] float attackRange = 8f;
   [SerializeField] float attackCooldown = 2f;
   [SerializeField] float attackTimer = 0f;

    [SerializeField] float projectileSpeed = 12f;

    private bool isDead = false;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
<<<<<<< Updated upstream
    {
        player = GameObject.FindWithTag("Player");
        currentHealth = maxHealth;
=======
    {       
>>>>>>> Stashed changes
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        if(isDead) return;

        float distance = Vector3.Distance(transform.position, player.transform.position);

        if(distance <= detectionRange)
        {
            agent.SetDestination(player.transform.position);

            if (distance <= attackRange)
            {
                agent.isStopped = true;
                FaceTarget();

                if(attackTimer <=0f)
                {
                    CastSpell();
                    attackTimer = attackCooldown;
                }
            }
            else
            {
                agent.isStopped = false;
            }
        }
        if (attackTimer > 0f)
       attackTimer -= Time.deltaTime;
    }
    void FaceTarget()
    {
        Vector3 direction = (player.transform.position - transform.position).normalized;
        direction.y = 0;
        Quaternion lookRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);
    }
    void CastSpell ()
    {
        if (animator) animator.SetTrigger("Cast");

        Vector3 spawnPos = castPoint.position + castPoint.forward * 0.3f;

        GameObject spell = Instantiate(projectilePrefab, castPoint.position, castPoint.rotation);
        Rigidbody rb = spell.GetComponent<Rigidbody>();

        Collider collider= GetComponent<Collider>();
        Collider spellcoll = spell.GetComponent<Collider>();
        if(collider&&spellcoll)
        {
            Physics.IgnoreCollision(spellcoll, collider);
        }
        if (rb)
        {

            rb.linearVelocity = Vector3.zero;

            Vector3 direction = (player.transform.position - castPoint.position).normalized;
            rb.linearVelocity = direction * projectileSpeed;
        }
        Destroy(spell, 5f);
    }
   
   private IEnumerator DeathRoutine()
    {
        isDead = true;

        if(agent) agent.enabled = false;

        Collider col = GetComponent<Collider>();
        if (col) col.enabled = false;

        if (animator) animator.SetTrigger("Die");
        yield return new WaitForSeconds(5f);

        Destroy(gameObject);
    }
}
