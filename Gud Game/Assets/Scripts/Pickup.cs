using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Pickup : MonoBehaviour
{
    public enum PickupType { Health, Mana, DoubleDamage }
    public PickupType type = PickupType.Health;
    public float amount = 25f;
    public bool consumeIfFull = false;

    public float doubleDamageDuration = 10f;

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

            case PickupType.DoubleDamage:
                var controller = playerRoot.GetComponentInChildren<PlayerController>();
                if (controller != null)
                {
                    controller.StartCoroutine(controller.ApplyDoubleDamage(doubleDamageDuration));
                    consumed = true;
                }
                break;
        }
        if (consumed)
            Destroy(gameObject);
    }
}
