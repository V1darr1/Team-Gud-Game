using System.Collections;
using UnityEditor.Experimental.GraphView;
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
    private bool isSprinting, sprintToggle;

    bool isBursting;
    float baseSpeed;
    float maxSpeedCap = 0f;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        controller = GetComponent<CharacterController>();
        _mana = manaMax;
        baseSpeed = speed; // remember original speed once
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

        
        float finalSpeed = baseSpeed * sprintMult * upgradeMult;

        
        if (maxSpeedCap > 0f) finalSpeed = Mathf.Min(finalSpeed, maxSpeedCap);

       
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
        isSprinting = Input.GetButton("Sprint");
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

        // Left / Right shots at ±half the cone angle
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

        var projectile = projGO.GetComponent<SimpleProjectile>();
        if (projectile != null)
        {
            projectile.Init(damage: spell.Damage, direction: direction);
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
    
    // Quick checks: enough mana and cooldown ready.
    public bool CanCast(iSpell spell)
    {
        if (spell == null) return false;
        if (_cooldownTimer > 0f) return false;    // still cooling down
        if (_mana < spell.ManaCost) return false; // not enough mana
        return true;
    }

    // Spend mana, start cooldown, spawn projectile, initialize it.
    public void BeginCast(iSpell spell, Vector3 origin, Vector3 direction)
    {
        // Spend resources and start cooldown.
        _mana -= spell.ManaCost;
        _cooldownTimer = spell.Cooldown;

        // If the spell has no projectile prefab, we can't shoot a projectile.
        // (Later you can add hitscan logic here.)
        if (spell.ProjectilePrefab == null)
        {
            Debug.LogWarning($"Spell '{spell.Id}' has no projectile prefab assigned.");
            return;
        }

        // Compute a spawn position slightly in front of the camera
        Vector3 spawnPos = origin + direction;

        // Instantiate (spawn) the projectile
        GameObject projGO = Instantiate(spell.ProjectilePrefab, spawnPos, Quaternion.LookRotation(direction));

        // Give it damage + forward direction
        var projectile = projGO.GetComponent<SimpleProjectile>();
        if (projectile != null)
        {
            float scaledDamage = spell.Damage * damageMultiplier;
            projectile.Init(scaledDamage, direction);
        }
        else
        {
            // If you rename the projectile script later, update this type here.
            Debug.LogWarning("Spawned projectile is missing FireboltProjectile component.");
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
