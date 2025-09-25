using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(UnifiedEnemyAI))]
[RequireComponent (typeof(DamageableHealth))]
public class DifficultyScaler : MonoBehaviour
{
    [SerializeField] private float healthPerLevel = 10f;

    [SerializeField] private float damagePerLevel = 2f;

    private UnifiedEnemyAI ai;
    private DamageableHealth hp;

    private float baseHealth;
    private float baseDamage;

    void Awake()
    {
        ai=GetComponent<UnifiedEnemyAI>();
        hp=GetComponent<DamageableHealth>();

        baseHealth = hp.MaxHealth;
        baseDamage = ai.Damage; 
        
    }
    private void OnEnable()
    {
        ApplyScaling();
    }

    private void ApplyScaling()
    {
        int level = gameManager.instance ? gameManager.instance.CurrentLevel : 1;

        float newHealth = baseHealth + (healthPerLevel *(level - 1));
        float newDamage = baseDamage + (damagePerLevel *(level -1));

        hp.SetMaxHealth(newHealth, refillCurrent: true);
        ai.Damage = newDamage;
    }
}
