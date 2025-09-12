using UnityEngine;

public interface iDamageable
{
    // Apply some amount of damage. We'll implement the health logic later.
    void ApplyDamage(float amount);

    // If false, we can ignore further hits (already dead/destroyed).
    bool IsAlive { get; }
}
