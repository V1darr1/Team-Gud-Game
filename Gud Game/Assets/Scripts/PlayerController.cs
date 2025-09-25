using System.Collections;

using UnityEngine;

public class PlayerController : MonoBehaviour, iSpellCaster
{
    [SerializeField] LayerMask ignoreLayer;
    [SerializeField] CharacterController controller;
    [SerializeField] int speed, sprintMod, jumpSpeed, jumpMax, gravity;

    [Header("Resources")]
    [Tooltip("Maximum mana the caster can hold.")]
    [SerializeField] private float manaMax = 100f;
    private float _mana, _cooldownTimer;
    private float damageMultiplier = 1f;

    [Tooltip("Mana regenerated per second.")]
    [SerializeField] private float manaRegenPerSec = 10f;

    [Header("Equipped Spell")]
    [Tooltip("Drop SpellData asset here (e.g., FireboltSpell).")]
    [SerializeField] private SpellData primarySpell;  // Implements iSpell

    public bool IsManaFull => _mana >= manaMax;

    Vector3 moveDir, playerVel;
    public bool inputEnabled = true;
    public void SetInputEnabled(bool v) => inputEnabled = v;

    private int jumpCount;
    private bool isSprinting;
    public bool sprintToggle;

    bool isBursting;
    float baseSpeed;
    float maxSpeedCap = 0f;
    private float speedMultiplier = 1f;
    private float defenseMultiplier = 1f;
    private DamageableHealth healthComponent;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        controller = GetComponent<CharacterController>();
        _mana = manaMax;
        baseSpeed = speed; // remember original speed once
        healthComponent = GetComponent<DamageableHealth>();
    }

    // Update is called once per frame

    void Update()
    {
        if (!inputEnabled) return;
        movement();
        sprint();

        if (Input.GetButtonDown("Fire1"))
            castSpell();

        //Cooldown Timer ticks towards 0
        if (_cooldownTimer > 0f)
            _cooldownTimer -= Time.deltaTime;

        //Regenerate Mana every Second up to Max
        if (_mana < manaMax)
            _mana = Mathf.Min(manaMax, _mana + manaRegenPerSec * Time.deltaTime);
    }

    void movement()
    {
        if (controller.isGrounded)
        {
            jumpCount = 0;
            playerVel.y = 0f;
        }
        else
        {
            playerVel.y -= gravity * Time.deltaTime;
        }


        float ix = Input.GetAxis("Horizontal");
        float iz = Input.GetAxis("Vertical");


        const float deadzone = 0.2f;
        if (Mathf.Abs(ix) < deadzone) ix = 0f;
        if (Mathf.Abs(iz) < deadzone) iz = 0f;


        Vector3 input = new Vector3(ix, 0f, iz);
        if (input.sqrMagnitude > 1f) input.Normalize();


        moveDir = (transform.right * input.x) + (transform.forward * input.z);


        float sprintMult = isSprinting ? sprintMod : 1f;
        var upgrades = GetComponent<PlayerUpgrades>();
        float upgradeMult = upgrades ? (1f + upgrades.speedPercentBonus) : 1f;


        float finalSpeed = baseSpeed * speedMultiplier * sprintMult * upgradeMult;


        if (maxSpeedCap > 0f) finalSpeed = Mathf.Min(finalSpeed, maxSpeedCap);

        // Apply the movement.
        controller.Move(moveDir * finalSpeed * Time.deltaTime);

        jump();
        controller.Move(playerVel * Time.deltaTime);
    }





    void jump()
    {
        if (Input.GetButtonDown("Jump") && jumpCount < jumpMax)
        {
            jumpCount++;
            playerVel.y = jumpSpeed;
        }
    }

    void sprint()
    {
        if (Input.GetButton("Sprint"))
        {
            isSprinting = true;
        } else if(isSprinting && !Input.GetButton("Sprint") && sprintToggle)
        {
            return;
        }
        else { isSprinting = false; }
        
    }


    void castSpell()
    {

        if (primarySpell == null) return;
        if (!CanCast(primarySpell)) return;

        var upgrades = GetComponent<PlayerUpgrades>();

        Vector3 origin = Camera.main.transform.position;
        Vector3 direction = Camera.main.transform.forward;

        if (upgrades != null && upgrades.tripleBurstEnabled)
        {
            ConeCast(primarySpell, origin, direction, upgrades);
        }
        else
        {
            BeginCast(primarySpell, origin, direction);
        }
    }

    // Fires 3 projectiles in a cone, but spends mana/cooldown ONLY ONCE.
    void ConeCast(SpellData spell, Vector3 origin, Vector3 direction, PlayerUpgrades upgrades)
    {
        // Spend resources once
        _mana -= spell.ManaCost;
        _cooldownTimer = spell.Cooldown;

        var yawAxis = Camera.main ? Camera.main.transform.up : Vector3.up;

        float half = Mathf.Abs(upgrades.coneTotalAngle) * 0.5f;

        // Center shot
        SpawnProjectileNoCost(spell, origin, direction);

        // Left / Right shots at �half the cone angle
        Quaternion leftRot = Quaternion.AngleAxis(-half, yawAxis);
        Quaternion rightRot = Quaternion.AngleAxis(+half, yawAxis);
        SpawnProjectileNoCost(spell, origin, leftRot * direction);
        SpawnProjectileNoCost(spell, origin, rightRot * direction);
    }

    // Clone BeginCast projectile spawn without spending mana/cooldown.
    void SpawnProjectileNoCost(SpellData spell, Vector3 origin, Vector3 direction)
    {
        if (spell.ProjectilePrefab == null)
        {
            Debug.LogWarning($"Spell '{spell.Id}' has no projectile prefab assigned.");
            return;
        }

        Vector3 spawnPos = origin + direction;
        GameObject projGO = Instantiate(spell.ProjectilePrefab, spawnPos, Quaternion.LookRotation(direction));

        var projectile = projGO.GetComponent<iProjectile>();
        if (projectile != null)
        {
            projectile.Init(damage: spell.Damage, direction: direction, gameObject);
        }
        else
        {
            Debug.LogWarning("Spawned projectile is missing SimpleProjectile component.");
        }
    }





    // This code for working mana pickups
    public void AddMana(float amount)
    {
        if (amount <= 0f) return;
        float before = _mana;
        _mana = Mathf.Min(manaMax, _mana + amount);
        if (_mana > before)
        {
            Debug.Log($"Mana +{amount}. MP: {_mana}/{manaMax}");
        }
    }

    public IEnumerator ApplyDoubleDamage(float duration)
    {
        damageMultiplier = 2f;

        yield return new WaitForSeconds(duration);

        damageMultiplier = 1f;
    }
    public IEnumerator ApplySpeedBoost(float duration, float speedBoostMultiplier)
    {
        // Multiply the base speed
        baseSpeed = (int)(speed * speedBoostMultiplier);
        Debug.Log($"Speed boosted to: {baseSpeed}.");

        yield return new WaitForSeconds(duration);

        // Reset the base speed to the original value
        baseSpeed = speed;
        Debug.Log($"Speed boost ended. Speed reset to: {baseSpeed}.");
    }
    public IEnumerator ApplyShield(float duration, float newDefenseMultiplier)
    {

        if (healthComponent != null)
        {
            healthComponent.defenseMultiplier = newDefenseMultiplier;
            Debug.Log("Shield applied! Damage taken is reduced.");
        }

        yield return new WaitForSeconds(duration);

        if (healthComponent != null)
        {
            healthComponent.defenseMultiplier = 1f; // Reset to normal
            Debug.Log("Shield expired. Defense is back to normal.");
        }
    }

    // Quick checks: enough mana and cooldown ready.
    public bool CanCast(iSpell spell)
    {
        if (spell == null) return false;
        if (_cooldownTimer > 0f) return false;    // still cooling down
        if (_mana < spell.ManaCost) return false; // not enough mana
        return true;
    }

    // Called when we actually cast the spell.
    // Spends mana, starts cooldown, spawns the projectile, and initializes it.
    public void BeginCast(iSpell spell, Vector3 origin, Vector3 direction)
    {
        // Spend resources and start cooldown.
        _mana -= spell.ManaCost;
        _cooldownTimer = spell.Cooldown;

        // If the spell has no projectile prefab, we can't shoot a projectile.
        // (For hitscan later, we'll add a different path.)
        if (spell.ProjectilePrefab == null)
        {
            Debug.LogWarning($"Spell '{spell.Id}' has no projectile prefab assigned.");
            return;
        }

        // Spawn a bit in front of the camera so we don't collide with ourselves.
        Vector3 spawnPos = origin + direction;

        // Actually spawn the projectile GameObject.
        GameObject projGO = Instantiate(spell.ProjectilePrefab, spawnPos, Quaternion.LookRotation(direction));

        // IMPORTANT: Grab ANY component that implements iProjectile and initialize it.
        var proj = projGO.GetComponent<iProjectile>();
        if (proj != null)
        {
            // Pass our damage, direction, and our own GameObject as the owner
            float scaledDamage = spell.Damage * damageMultiplier;
            proj.Init(scaledDamage, direction, gameObject);
        }
        else
        {
            Debug.LogWarning("Spawned projectile does not implement iProjectile.");
        }
    }


    /* IEnumerator flashDamage()
     {
         gameManager.instance.playerDamageFlash.SetActive(true);
         yield return new WaitForSeconds(0.1f);
         gameManager.instance.playerDamageFlash.SetActive(false);
     }*/


    //Expose current health for UI.
    public float CurrentMana => _mana;
    public float MaxMana => manaMax;
    public float CooldownRemaining => Mathf.Max(0f, _cooldownTimer);

}
