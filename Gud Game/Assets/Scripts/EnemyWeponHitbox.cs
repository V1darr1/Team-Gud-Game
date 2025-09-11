using UnityEngine;

public class EnemyWeaponHitbox : MonoBehaviour
{
    public enemyAI owner;       
    public int damage = 15;     

    private void OnTriggerEnter(Collider other)
    {
        if (!owner) return;
        if (!other.CompareTag("Player")) return;

        // Find something that can take damage on the player hierarchy
        var idmg = other.GetComponentInParent<IDamage>();
        if (idmg != null)
        {
            idmg.takeDamage(damage);
        }
    }

}
