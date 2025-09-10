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

    [Tooltip("Mana regenerated per second.")]
    [SerializeField] private float manaRegenPerSec = 10f;

    [Header("Equipped Spell")]
    [Tooltip("Drop SpellData asset here (e.g., FireboltSpell).")]
    [SerializeField] private SpellData primarySpell;  // Implements iSpell


    Vector3 moveDir, playerVel;

    private int jumpCount;
    private bool isSprinting, sprintToggle;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        controller = GetComponent<CharacterController>();
        _mana = manaMax;
    }

    // Update is called once per frame
    void Update()
    {
        movement();
        sprint();
        
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
            playerVel = Vector3.zero;
        }
        else
        {
            playerVel.y -= gravity * Time.deltaTime;
        }

        moveDir = (Input.GetAxis("Horizontal") * transform.right)
            + (Input.GetAxis("Vertical") * transform.forward);

        controller.Move(moveDir * speed * Time.deltaTime);

        jump();

        controller.Move(playerVel * Time.deltaTime);

        if (Input.GetButtonDown("Fire1"))
        {
            castSpell();
        }

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
        if (Input.GetButtonDown("Sprint"))
        {
            speed *= sprintMod;
            isSprinting = true;
        }
        else if (Input.GetButtonUp("Sprint") && !sprintToggle)
        {
            speed /= sprintMod;
            isSprinting = false;
        }
        else if (isSprinting && Input.GetButtonDown("Sprint"))
        {
            speed /= sprintMod;
            isSprinting = false;

        }
    }
    void castSpell()
    {
        if (primarySpell == null) return;          // No spell equipped
        if (!CanCast(primarySpell)) return;        // Not ready (mana/cooldown)

        // Get origin (camera position) and forward direction (where we're looking)
        Vector3 origin = Camera.main.transform.position;
        Vector3 direction = Camera.main.transform.forward;

        // Actually begin the cast
        BeginCast(primarySpell, origin, direction);
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
            projectile.Init(damage: spell.Damage, direction: direction);
        }
        else
        {
            // If you rename the projectile script later, update this type here.
            Debug.LogWarning("Spawned projectile is missing FireboltProjectile component.");
        }
    }

    //Expose current health for UI.
    public float CurrentMana => _mana;
    public float MaxMana => manaMax;
    public float CooldownRemaining => Mathf.Max(0f, _cooldownTimer);
}
