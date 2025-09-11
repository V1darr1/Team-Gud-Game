using UnityEngine;


[RequireComponent(typeof(Collider))]
public class SimpleDOT : MonoBehaviour
{
    [Tooltip("How much damage to apply per second while inside the lava.")]
    public float damagePerSecond = 15f;

    [Tooltip("Only affect objects with this tag (usually 'Player'). Leave empty to affect any iDamageable.")]
    public string onlyAffectTag = "Player";

    [Tooltip("If true, we require the tag above to match. If false, anything implementing iDamageable takes damage.")]
    public bool requireTagMatch = true;

    [Tooltip("Limit how often we look up iDamageable on the same object (micro-optimization).")]
    public bool cacheDamageablePerCollider = true;

    // Simple cache so we don't call GetComponentInParent every physics step for the same collider.
    private readonly System.Collections.Generic.Dictionary<Collider, iDamageable> _cache
        = new System.Collections.Generic.Dictionary<Collider, iDamageable>();

    private Collider _trigger;

    private void Reset()
    {
        // Make sure the collider is a trigger for OnTriggerStay to fire.
        var col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    private void Awake()
    {
        _trigger = GetComponent<Collider>();
        if (_trigger != null) _trigger.isTrigger = true;
    }

    // NOTE: OnTriggerStay runs every physics step while another collider remains inside this trigger.
    private void OnTriggerStay(Collider other)
    {
        // At least one of the two colliders (this or the other) must be on a Rigidbody for trigger callbacks to fire.
        // Usually the Player has a Rigidbody, so the lava object can stay static.

        if (requireTagMatch && !string.IsNullOrEmpty(onlyAffectTag) && !other.CompareTag(onlyAffectTag))
            return;

        // Try to get iDamageable from the object or its parents.
        iDamageable damageable = null;

        if (cacheDamageablePerCollider)
        {
            if (!_cache.TryGetValue(other, out damageable))
            {
                damageable = other.GetComponentInParent<iDamageable>();
                _cache[other] = damageable; // may store null; that's fine
            }
        }
        else
        {
            damageable = other.GetComponentInParent<iDamageable>();
        }

        if (damageable == null)
            return;

        // Convert DPS to per-step damage. OnTriggerStay runs in FixedUpdate steps.
        float damageThisStep = damagePerSecond * Time.fixedDeltaTime;

        if (damageThisStep <= 0f)
            return;

        // Call your interface. If your method name differs, rename here (e.g., TakeDamage / ApplyDamage).
        damageable.ApplyDamage(damageThisStep);
    }

    private void OnTriggerExit(Collider other)
    {
        // Clean up cache when something leaves the lava.
        if (cacheDamageablePerCollider && _cache.ContainsKey(other))
            _cache.Remove(other);
    }

    // Optional: draw the lava volume in the editor for clarity.
    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.3f, 0f, 0.35f);
        var col = GetComponent<Collider>();
        if (col is BoxCollider box)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(box.center, box.size);
        }
        else if (col is SphereCollider sphere)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawSphere(sphere.center, sphere.radius);
        }
        // (Capsule/mesh omitted for brevity)
    }
}