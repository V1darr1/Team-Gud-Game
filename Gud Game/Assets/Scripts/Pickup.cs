using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Pickup : MonoBehaviour
{
    public enum PickupType { Health, Mana, Firejolt}
    public PickupType type = PickupType.Health;
    public float amount = 25f;
    public bool consumeIfFull = false;

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
                if(pc != null)
                {
                    float before = pc.CurrentMana;
                    pc.AddMana(amount);
                    consumed = (pc.CurrentMana > before || consumeIfFull);
                }
                break;

                
        }
        if (consumed)
            Destroy(gameObject);
    }
}
