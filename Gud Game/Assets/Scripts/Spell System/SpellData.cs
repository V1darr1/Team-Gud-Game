// SpellData.cs
// A ScriptableObject that holds the DATA for a spell.
// This implements iSpell so everything that expects an ISpell can use it.
//
// Why ScriptableObject?
// - Lets you create many spell assets (Firebolt, Ice Nova, etc.) without writing new code.
// - Easy to tweak values in the Inspector without recompiling.

using UnityEngine;

[CreateAssetMenu(
    fileName = "NewSpell",
    menuName = "Spells/Spell Data",
    order = 0)]
public class SpellData : ScriptableObject, iSpell
{
    [Header("Identity")]
    [SerializeField] private string id = "firebolt";  // unique key, good for UI/saving

    [Header("Costs & Timing")]
    [Tooltip("Mana spent when casting this spell.")]
    [SerializeField, Min(0f)] private float manaCost = 10f;

    [Tooltip("Cooldown in seconds before you can cast again.")]
    [SerializeField, Min(0f)] private float cooldown = 0.75f;

    [Header("Effect")]
    [Tooltip("Base damage dealt on a successful hit.")]
    [SerializeField, Min(0f)] private float damage = 25f;

    [Header("Delivery")]
    [Tooltip("Projectile prefab to spawn when casting. Leave empty for hitscan-style spells.")]
    [SerializeField] private GameObject projectilePrefab;

    // --- ISpell interface implementation (read-only public access) ---
    public string Id => id;
    public float ManaCost => manaCost;
    public float Cooldown => cooldown;
    public float Damage => damage;
    public GameObject ProjectilePrefab => projectilePrefab;

    // Simple validation to catch mistakes during editing.
#if UNITY_EDITOR
    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(id))
            id = name.ToLower().Replace(" ", "_"); // auto-fill id from asset name

        // Clamp to safe values
        if (manaCost < 0f) manaCost = 0f;
        if (cooldown < 0f) cooldown = 0f;
        if (damage < 0f) damage = 0f;
    }
#endif
}
