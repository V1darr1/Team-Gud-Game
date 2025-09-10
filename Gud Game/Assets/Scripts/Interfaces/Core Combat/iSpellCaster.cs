using UnityEngine;
public interface iSpellCaster
{
    // Quick check: do we have enough mana, is cooldown ready, etc.?
    // We'll implement the actual checks in a MonoBehaviour later.
    bool CanCast(iSpell spell);

    // Start the cast right now.
    // origin: where to spawn the projectile (usually near the camera)
    // direction: which way to shoot (usually camera forward)
    void BeginCast(iSpell spell, Vector3 origin, Vector3 direction);
}
