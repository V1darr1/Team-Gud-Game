using UnityEngine;

// How it works (in plain words):
// - On Init(...), we remember our damage, set our starting direction, and who spawned us (owner).
// - Every frame, we either move straight or steer toward a target if we found one.
// - We look for targets inside a sphere around us (acquireRadius) on a timer (reacquireInterval).
// - We prefer targets in front of us (optional) so it doesn't whip around behind the player.
// - On trigger hit, if the thing has iDamageable and is alive, we apply damage and destroy ourselves.

[RequireComponent(typeof(Collider))]
public class simpleHoming : MonoBehaviour , iProjectile
{
    [Header("Movement")]
    [Tooltip("How fast the projectile travels forward (units/second).")]
    [SerializeField] private float speed = 20f;

    [Tooltip("How quickly it can turn toward the target (degrees/second).")]
    [SerializeField] private float turnDegreesPerSecond = 360f;

    [Tooltip("How many seconds this projectile lives before auto-destroying.")]
    [SerializeField] private float lifetime = 8f;

    [Header("Targeting")]
    [Tooltip("How far to search for targets.")]
    [SerializeField] private float acquireRadius = 30f;

    [Tooltip("How often (seconds) we search for the nearest target.")]
    [SerializeField] private float reacquireInterval = 0.25f;

    [Tooltip("Layer mask for valid targets (e.g., Enemy). Leave as Everything to test quickly.")]
    [SerializeField] private LayerMask targetLayers = ~0; // Everything by default

    [Tooltip("Only lock onto targets generally in front of the projectile (prevents instant 180° turns).")]
    [SerializeField] private bool requireInFront = true;

    [Tooltip("How 'in front' a target must be to consider (0=any direction, 1=straight ahead). 0.4 is forgiving.")]
    [SerializeField, Range(0f, 1f)] private float forwardBiasDot = 0.4f;

    // --- Runtime state filled by Init(...) ---
    private float _damage;
    private bool _initialized;
    private GameObject _owner;

    // Target tracking
    private Transform _target;      // where to steer
    private float _seekTimer;       // counts down to 0, then we scan again

    public void Init(float damage, Vector3 direction, GameObject owner)
    {
        _damage = damage;
        _owner = owner;

        // Point our forward along the given direction.
        transform.rotation = Quaternion.LookRotation(direction);

        // Force an immediate first scan for targets.
        _seekTimer = 0f;

        _initialized = true;
    }

    private void Update()
    {
        if (!_initialized) return;

        // Lifetime countdown (prevents projectiles from living forever)
        lifetime -= Time.deltaTime;
        if (lifetime <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        // Periodically try to find a target (if we don't have one or it became invalid)
        _seekTimer -= Time.deltaTime;
        if (_seekTimer <= 0f)
        {
            // Re-scan if we lost target or it died/out-of-range.
            if (_target == null || !IsTargetValid(_target))
            {
                _target = FindNearestValidTarget();
            }
            _seekTimer = reacquireInterval;
        }

        // If we have a valid target, steer toward it.
        if (_target != null)
        {
            Vector3 toTarget = (_target.position - transform.position);
            if (toTarget.sqrMagnitude > 0.001f)
            {
                Quaternion desired = Quaternion.LookRotation(toTarget.normalized);
                // Rotate gradually (smooth turn) toward the target.
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation, desired, turnDegreesPerSecond * Time.deltaTime);
            }
        }

        // Move forward along our current facing direction.
        transform.position += transform.forward * speed * Time.deltaTime;
    }

    /// <summary>
    /// Returns true if a candidate transform still looks like a good target.
    /// Checks for iDamageable + alive, within radius, and (optionally) in front.
    /// </summary>
    private bool IsTargetValid(Transform candidate)
    {
        var damageable = candidate.GetComponentInParent<iDamageable>();
        if (damageable == null || !damageable.IsAlive) return false;

        // Still in range?
        float dist = Vector3.Distance(transform.position, candidate.position);
        if (dist > acquireRadius) return false;

        // If we require "in front", check a dot-product bias.
        if (requireInFront)
        {
            Vector3 to = (candidate.position - transform.position).normalized;
            float dot = Vector3.Dot(transform.forward, to);
            if (dot < forwardBiasDot) return false;
        }

        return true;
    }
}
