using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(Collider))]
public class SimpleDOT : MonoBehaviour
{
    [Tooltip("Damage per second while inside.")]
    public float damagePerSecond = 15f;

    [Tooltip("Only damage objects with this tag (usually 'Player'). Leave empty to affect any iDamageable.")]
    public string onlyAffectTag = "Player";

    [Tooltip("If true, require the tag above to match. If false, anything implementing iDamageable takes damage.")]
    public bool requireTagMatch = true;

    // Store running coroutines so we can stop them on exit
    private readonly Dictionary<Collider, Coroutine> active = new Dictionary<Collider, Coroutine>();

    private void Reset()
    {
        var col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    private void OnTriggerStay(Collider other)
    {
        if (requireTagMatch && !string.IsNullOrEmpty(onlyAffectTag) && !other.CompareTag(onlyAffectTag) || other.isTrigger)
            return;

        iDamageable dmg = other.GetComponentInParent<iDamageable>();
        if (dmg == null) return;

        if (!active.ContainsKey(other))
        {
            Coroutine c = StartCoroutine(DamageLoop(dmg));
            active[other] = c;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (active.TryGetValue(other, out Coroutine c))
        {
            StopCoroutine(c);
            active.Remove(other);
        }
    }

    private IEnumerator DamageLoop(iDamageable target)
    {
        while (true)
        {
            float dmgThisFrame = damagePerSecond * Time.deltaTime;
            if (dmgThisFrame > 0f)
                target.ApplyDamage(dmgThisFrame);

            yield return null; // next frame
        }
    }
}
