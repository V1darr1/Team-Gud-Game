using UnityEngine;
using System.Collections;

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

    public bool IsManaFull => _mana >= manaMax;

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
            playerVel.y = 0;
        }
        else
        {
            playerVel.y -= gravity * Time.deltaTime;
        }

        moveDir = (Input.GetAxis("Horizontal") * transform.right) + (Input.GetAxis("Vertical") * transform.forward);

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
            // Pass our damage, direction, and our own GameObject as the owner.
            proj.Init(spell.Damage, direction, gameObject);
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
