using UnityEngine;

public interface iSpell
{
    string Id { get; }
    float ManaCost { get; }
    float Cooldown { get; }
    float Damage { get; }
    UnityEngine.GameObject ProjectilePrefab { get; }
}
