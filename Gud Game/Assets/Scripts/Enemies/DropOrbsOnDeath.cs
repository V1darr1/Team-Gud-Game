
using UnityEngine;

public class DropOrbsOnDeath : MonoBehaviour
{
    [Range(0f, 1f)] public float dropChance = 0.25f;
    [Header("Orb Prefabs")]
    [SerializeField] public GameObject healthPickup;
    [SerializeField] public GameObject manaPickup;
    [SerializeField] public GameObject doubleDmgPickup;
    [SerializeField] public GameObject DoubleSpeedPickup;
    [SerializeField] public GameObject shieldPickup;

    [SerializeField, Range(0f, 1f)] private float healthChance = 0.25f;
    [SerializeField, Range(0f, 1f)] private float manaChance = 0.25f;
    [SerializeField, Range(0f, 1f)] private float doubleChance = 0.15f;
    [SerializeField, Range(0f, 1f)] private float doublespeedChance = 0.15f;
    [SerializeField, Range(0f, 1f)] private float shieldChance = 0.15f;

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
        // roll between 0–1
        float roll = Random.value;
        GameObject prefab = null;

        if (roll < healthChance)
        {
            prefab = healthPickup;
        }
        else if (roll < healthChance + manaChance)
        {
            prefab = manaPickup;
        }
        else if (roll < healthChance + manaChance + doubleChance)
        {
            prefab = doubleDmgPickup;
        }
        else if (roll < healthChance + manaChance + doubleChance + doublespeedChance)
        {
            prefab = DoubleSpeedPickup;
        }
        else if (roll < healthChance + manaChance + doubleChance + doublespeedChance + shieldChance)
        {
            prefab = shieldPickup;
        }
        else
        {
            // no drop
            return;
        }

        if (!prefab) return;

        var pos = transform.position
            + Vector3.up * spawnHeight
            + new Vector3(Random.Range(-scatter, scatter), 0f, Random.Range(-scatter, scatter));

        Instantiate(prefab, pos, Quaternion.identity);
    }
}
