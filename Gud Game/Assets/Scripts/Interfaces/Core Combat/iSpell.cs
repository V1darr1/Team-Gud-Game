using UnityEngine;

public interface iSpell
{
    // A unique id string. Example: "firebolt", "ice_nova".
    // Helpful for UI, saving, or analytics.
    string Id { get; }

    // How much mana (or resource) this spell consumes when cast.
    float ManaCost { get; }

    // How long (in seconds) you must wait before casting this spell again.
    float Cooldown { get; }

    // The base damage this spell deals on hit (we’ll keep it simple for now).
    float Damage { get; }

    // A prefab for the projectile this spell fires.
    // If a spell is hitscan (no projectile), we can leave this null later.
    GameObject ProjectilePrefab { get; }
}
