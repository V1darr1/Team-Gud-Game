using UnityEngine;

public class PlayerEffects : MonoBehaviour
{
    public static PlayerEffects Instance { get; private set; }

    [Header("AOE on Hit")]
    public bool aoeOnHit = false;     // enabled by reward
    public float aoeRadius = 3f;
    public int aoeDamage = 10;
    public LayerMask enemyMask;       // set to Enemy layer(s) in Inspector
    public GameObject aoeVFX;

    void Awake() => Instance = this;

    public void OnPlayerHitEnemy(Vector3 hitPos)
    {
        if (!aoeOnHit) return;

        if (aoeVFX) Instantiate(aoeVFX, hitPos, Quaternion.identity);

        // If mask is not set, scan everything; otherwise use mask
        bool useMask = enemyMask.value != 0;
        Collider[] cols = useMask
            ? Physics.OverlapSphere(hitPos, aoeRadius, enemyMask, QueryTriggerInteraction.Collide)
            : Physics.OverlapSphere(hitPos, aoeRadius, ~0, QueryTriggerInteraction.Collide);

        int applied = 0;
        foreach (var c in cols)
        {
            var t = c.GetComponentInParent<iDamageable>();
            if (t != null && t.IsAlive) { t.ApplyDamage(aoeDamage); applied++; }
        }
        
        Destroy(aoeVFX);
    }
}

