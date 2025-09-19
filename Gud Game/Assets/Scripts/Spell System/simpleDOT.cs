using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class SimpleDOT : MonoBehaviour
{
    [Tooltip("Damage per second while inside.")]
    public float damagePerSecond = 15f;

    [Tooltip("Only damage objects with this tag (usually 'Player'). Leave empty to affect any iDamageable.")]
    public string onlyAffectTag = "Player";

    [Tooltip("If true, require the tag above to match.")]
    public bool requireTagMatch = true;

    // Track loops by target root so we can stop them even if colliders get disabled/destroyed.
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

        var damageable = other.GetComponentInParent<iDamageable>() as Component;
        if (!damageable) return;

        var key = damageable.transform.root;
        if (_running.ContainsKey(key)) return;

        var i = damageable.GetComponent<iDamageable>();
        if (i == null) return;

        _running[key] = StartCoroutine(DamageLoop(i));
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