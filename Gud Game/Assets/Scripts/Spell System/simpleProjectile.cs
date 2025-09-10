using UnityEngine;

[RequireComponent(typeof(Collider))] // Ensures there is ALWAYS a Collider on this object.
public class SimpleProjectile : MonoBehaviour
{
    [Header("Movement")]
    [Tooltip("How fast the projectile moves forward (units/second).")]
    [SerializeField] private float speed = 30f;

    [Tooltip("How many seconds this projectile lives before auto-destroying.")]
    [SerializeField] private float lifetime = 5f;

    [Header("Debug/Effects (optional)")]
    [Tooltip("If true, draws a short gizmo ray in Scene view to show forward direction.")]
    [SerializeField] private bool drawGizmoForward = false;

    // --- Runtime fields (set by the caster when spawning) ---
    private float _damage;     // How much damage to apply on hit
    private bool _initialized; // We won't move until Init(...) is called

    /// <summary>
    /// Called by the caster immediately after Instantiate().
    /// Sets the damage and the forward direction.
    /// </summary>
    public void Init(float damage, Vector3 direction)
    {
        _damage = damage;

        // Rotate the projectile so its "forward" points along the direction we want to travel.
        transform.rotation = Quaternion.LookRotation(direction);

        _initialized = true;
    }

    private void Update()
    {
        if (!_initialized) return; // Safety: don’t move if not set up yet.

        // Move forward in the direction we're facing.
        transform.position += transform.forward * speed * Time.deltaTime;

        // Count down lifetime and destroy when time runs out (prevents infinite objects).
        Destroy(gameObject, lifetime);
    }

    /// <summary>
    /// Triggered when our trigger-collider touches another collider.
    /// Note: This requires our Collider to have "Is Trigger" checked.
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        // Find an IDamageable component on the thing we hit (or its parents).
        var damageable = other.GetComponentInParent<iDamageable>();

        // If the target can take damage and is still alive, apply damage.
        if (damageable != null && damageable.IsAlive)
        {
            damageable.ApplyDamage(_damage);
        }

        // (Optional) You can spawn impact VFX/SFX here later.

        // Destroy the projectile after any hit to keep it simple.
        Destroy(gameObject);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (!drawGizmoForward) return;
        Gizmos.color = Color.white;
        Gizmos.DrawRay(transform.position, transform.forward * 0.5f);
    }
#endif
}