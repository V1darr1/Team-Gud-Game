using UnityEngine;

public class DropOrbsOnDeath : MonoBehaviour
{
    [Range(0f, 1f)] public float dropChance = 0.25f;
    [Header("Orb Prefabs")]
    public GameObject healthPickup;
    public GameObject manaPickup;

    [Header("Spawn tuning")]
    public float spawnHeight = 0.4f;
    public float scatter = 0.3f;

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
        if (Random.value >= dropChance) return;

        bool dropHealth = Random.value < 0.5f;
        var prefab = dropHealth ? healthPickup : manaPickup;
        if (!prefab) return;

        var pos = transform.position
            +Vector3.up * spawnHeight
            + new Vector3(Random.Range(-scatter, scatter), 0f, Random.Range(-scatter, scatter));

        Instantiate(prefab, pos, Quaternion.identity);
    }
}
