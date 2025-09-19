using UnityEngine;

public interface iProjectile
{
    //Interface for all projectiles spawned in game by spellcasters. Collects the spellsData on Initialization.
    void Init(float damage, Vector3 direction, GameObject owner);
}
