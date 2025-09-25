using System.Collections;
using UnityEngine;

public class DamageableHealth : MonoBehaviour, iDamageable
{
    public event System.Action<DamageableHealth> OnDied;
    bool _deathInvoked;

    [Header("Health Settings")]
    [Tooltip("Health this object starts with.")]
    [SerializeField] private float maxHealth = 100f;

    [Tooltip("If true, the object is destroyed when health reaches 0.")]
    [SerializeField] private bool destroyOnDeath = true;

    [Tooltip("If true, the object can be healed by any target.")]
    [SerializeField] private bool canBeHealed = false;

    // Tracks the current health at runtime.
    private float _health;

    // Other scripts read this to know if we should still take damage.
    public bool IsAlive => _health > 0f;

    private void Awake()
    {
        // When the object spawns, set current health to the maximum.
        _health = Mathf.Max(1f, maxHealth); // Ensure it's at least 1 to avoid starting dead by mistake.
    }
    public float defenseMultiplier = 1f;

    public void ApplyDamage(float amount)
    {
        // If already dead, ignore any further hits.
        if (!IsAlive) return;

        float finalDamag = amount * defenseMultiplier;

        // Reduce health by the damage amount (never below 0).
        _health = Mathf.Max(0f, _health - Mathf.Max(0f, amount));

        // Print simple feedback to the Console so you can see it working.
        Debug.Log($"{name} took {amount} damage. HP: {_health}/{maxHealth}");

        var ai = GetComponent<UnifiedEnemyAI>();
        if (ai)
        {
            ai.OnDamaged(gameManager.instance.player.transform.position);
        }

        //Flash Screen to notify player of damage || Flash Enemy to notify player of damage.
        if (gameObject.tag == "Player") { StartCoroutine(damageFlash()); }
        else if (gameObject.GetComponent<iEnemy>() != null) { gameObject.GetComponent<iEnemy>().FlashDamage(); }

        // If health hit 0, handle "death".
        if (_health <= 0f)
        {
            OnDeath();
        }
    }

    // Called once when health reaches 0.
    private void OnDeath()
    {
        // You can hook death animation, sound, or events here.
        Debug.Log($"{name} died.");

        if (!_deathInvoked)
        {
            _deathInvoked = true;
            OnDied?.Invoke(this);
        }

        if (destroyOnDeath)
        {
            // Destroy this entire GameObject (basic behavior).
            // If you have a ragdoll or animation, replace this with custom logic.
            Destroy(gameObject);
            //Add any additional logic needed for game here.
        }
        if (gameObject.tag == "Player") { gameManager.instance.OpenLoseMenu(); }
    }

    public void ApplyHealing(float amount)
    {
        if (!IsAlive || !canBeHealed) return; // No healing if dead || No Healing if Not Healable)
        _health = Mathf.Min(maxHealth, _health + Mathf.Max(0f, amount));
        Debug.Log($"{name} healed {amount}. HP: {_health}/{maxHealth}");
    }
    public void SetMaxHealth(float newMax, bool refillCurrent)
    {
        maxHealth = Mathf.Max(1f, newMax);

        if (refillCurrent)
        {
            _health = maxHealth;
        }
        else
        {
            _health = Mathf.Min(_health, maxHealth);
        }
    }
    IEnumerator damageFlash()
    {
        gameManager.instance.playerDamageFlash.SetActive(true);
        yield return new WaitForSeconds(0.1f);
        gameManager.instance.playerDamageFlash.SetActive(false);
    }
    /*void EnemyDamageFlash()
    {
        Color orig = gameObject.GetComponent<Renderer>().material.color;
        gameObject.GetComponent<Renderer>().material.color = Color.red;
        gameObject.GetComponent<Renderer>().material.color = orig;
    }*/

    //Expose current health for UI.
    public float CurrentHealth => _health;
    public float MaxHealth => maxHealth;
}