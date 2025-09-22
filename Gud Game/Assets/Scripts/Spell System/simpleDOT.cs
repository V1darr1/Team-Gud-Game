using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class SimpleDOT : MonoBehaviour
{
    public float damagePerSecond = 15f;
    public string onlyAffectTag = "Player";
    public bool requireTagMatch = true;

    // Track by root transform, so one coroutine per target
    private readonly Dictionary<Transform, Coroutine> _running = new();

    private void Reset()
    {
        var c = GetComponent<Collider>();
        if (c) c.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.isTrigger) return;
        if (requireTagMatch && !string.IsNullOrEmpty(onlyAffectTag) && !other.CompareTag(onlyAffectTag))
            return;

        var comp = other.GetComponentInParent<iDamageable>() as Component;
        if (!comp) return;

        var key = comp.transform.root;
        if (_running.ContainsKey(key)) return; // already ticking

        var dmg = comp.GetComponent<iDamageable>();
        if (dmg == null) return;

        _running[key] = StartCoroutine(DamageLoop(dmg));
    }

    private void OnTriggerExit(Collider other)
    {
        var root = other.transform.root;
        if (_running.TryGetValue(root, out var co))
        {
            StopCoroutine(co);
            _running.Remove(root);
        }
    }

    private void OnDisable()
    {
        foreach (var kv in _running)
            if (kv.Value != null) StopCoroutine(kv.Value);
        _running.Clear();
    }

    private IEnumerator DamageLoop(iDamageable target)
    {
        while (target != null && target.IsAlive)
        {
            float dmg = damagePerSecond * Time.deltaTime;
            if (dmg > 0f) target.ApplyDamage(dmg);
            yield return null;
        }
    }
}