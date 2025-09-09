using UnityEngine;

public interface iSpellCaster
{
    bool CanCast(iSpell spell);
    void BeginCast(iSpell spell, UnityEngine.Vector3 origin, UnityEngine.Vector3 direction);
}

