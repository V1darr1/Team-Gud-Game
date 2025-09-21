using System;
using System.Collections.Generic;
using UnityEngine;

public class KillAllEnemiesObjective : MonoBehaviour, IRoomObjective
{
    [SerializeField] List<DamageableHealth> enemies = new(); // leave empty to auto-find
    public bool IsComplete { get; private set; }
    public event Action<IRoomObjective> OnCompleted;

    int _alive;

    void Awake()
    {
        if (enemies.Count == 0)
            enemies.AddRange(GetComponentsInChildren<DamageableHealth>(true));

        enemies.RemoveAll(e => e == null);
        Debug.Log($"[Objective] Found {enemies.Count} enemies.", this);
    }

    void OnEnable()
    {
        _alive = 0;
        foreach (var e in enemies)
        {
            if (!e) continue;
            if (e.IsAlive) _alive++;

            e.OnDied -= HandleEnemyDied; // avoid double-subscribe
            e.OnDied += HandleEnemyDied;
        }

        Debug.Log($"[Objective] Alive at start: {_alive}", this);

        if (gameManager.instance) gameManager.instance.SetEnemiesRemaining(_alive);

        if (_alive == 0) Complete();
    }

    void OnDisable()
    {
        foreach (var e in enemies)
            if (e) e.OnDied -= HandleEnemyDied;
    }

    void HandleEnemyDied(DamageableHealth e)
    {
        if (IsComplete) return;
        _alive = Mathf.Max(0, _alive - 1);
        Debug.Log($"[Objective] Enemy died → remaining: {_alive}", this);
        if (gameManager.instance) gameManager.instance.SetEnemiesRemaining(_alive);
        if (_alive == 0) Complete();
    }

    void Complete()
    {
        if (IsComplete) return;
        IsComplete = true;
        Debug.Log("[Objective] COMPLETE", this);
        if (gameManager.instance) gameManager.instance.SetEnemiesRemaining(0);
        OnCompleted?.Invoke(this);
    }
}