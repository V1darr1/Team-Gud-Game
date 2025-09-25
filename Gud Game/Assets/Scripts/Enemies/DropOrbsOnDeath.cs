using UnityEngine;

public class DropOrbsOnDeath : MonoBehaviour
{
    //[Range(0f, 1f)] public float dropChance = 0.25f;
    [Header("Orb Prefabs")]
    [SerializeField] public GameObject healthPickup;
    [SerializeField] public GameObject manaPickup;
    [SerializeField] public GameObject doubleDmgPickup;
    [SerializeField] public GameObject DoubleSpeedPickup;
    [SerializeField] public GameObject shieldPickup;

    [SerializeField, Range(0, 100)] private int DropChance = 33;
    [SerializeField] private int PickupCount = 5;//count of all usable pickups

    [Header("Spawn tuning")]
    [SerializeField] public float spawnHeight = 0.4f;
    [SerializeField] public float scatter = 0.3f;

    DamageableHealth _hp;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        _hp = GetComponent<DamageableHealth>();
        if (_hp) _hp.OnDied += HandleDeath;
    }

    private void OnDestroy()
    {
        if (_hp) _hp.OnDied -= HandleDeath;
    }

    void HandleDeath(DamageableHealth _)
    {
        // roll between 0–100
        int roll = Random.Range(0, 100);
        GameObject prefab = null;

        if (roll <= DropChance)
        {
            int drop = roll % PickupCount;
            switch (drop)
            {
                case 0: prefab = healthPickup; break;
                case 1: prefab = manaPickup; break;
                case 2: prefab = doubleDmgPickup; break;
                case 3: prefab = DoubleSpeedPickup; break;
                case 4: prefab = shieldPickup; break;
                default: break;
            }

        }


        if (!prefab) return;

        var pos = transform.position
            + Vector3.up * spawnHeight
            + new Vector3(Random.Range(-scatter, scatter), 0f, Random.Range(-scatter, scatter));

        Instantiate(prefab, pos, Quaternion.identity);
    }
}
