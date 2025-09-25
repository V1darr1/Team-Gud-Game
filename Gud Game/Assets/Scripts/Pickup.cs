using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Pickup : MonoBehaviour
{
    public enum PickupType { Health, Mana, DoubleDamage, DoubleSpeed, Shield }
    public PickupType type = PickupType.Health;
    public float amount = 25f;
    public float speedBoostMultiplier = 2.0f;
    public float speedBoostDuration = 10f;
    public bool consumeIfFull = false;

    public float doubleDamageDuration = 10f;
    public float shieldDefenseMultiplier = 0.5f;
    public float shieldDuration = 10f;

    private void OnTriggerEnter(Collider other)
    {
        var playerRoot = other.transform.root;

        if (!playerRoot.CompareTag("Player")) return;

        bool consumed = false;

        switch (type)
        {
            case PickupType.Health:
                var hp = playerRoot.GetComponentInChildren<DamageableHealth>();
                if (hp != null && hp.IsAlive)
                {
                    float before = hp.CurrentHealth;
                    hp.ApplyHealing(amount);
                    consumed = (hp.CurrentHealth > before || consumeIfFull);
                }
                break;

            case PickupType.Mana:
                var pc = playerRoot.GetComponentInChildren<PlayerController>();
                if (pc != null)
                {
                    float before = pc.CurrentMana;
                    pc.AddMana(amount);
                    consumed = (pc.CurrentMana > before || consumeIfFull);
                }
                break;

            case PickupType.DoubleSpeed:
                var speedController = playerRoot.GetComponentInChildren<PlayerController>();
                if (speedController != null)
                {
                    // Start the new coroutine from the PlayerController.
                    speedController.StartCoroutine(speedController.ApplySpeedBoost(speedBoostDuration, speedBoostMultiplier));
                    consumed = true;
                }
                break;

            case PickupType.Shield:
                var player = playerRoot.GetComponentInChildren<PlayerController>();
                if (player != null)
                {
                    player.StartCoroutine(player.ApplyShield(shieldDuration, shieldDefenseMultiplier));
                    consumed = true;
                }
                break;
        }

        if (consumed)
            Destroy(gameObject);
    }
}