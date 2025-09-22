using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class StartupDiag : MonoBehaviour
{
    [Tooltip("Stagger start of FX/AI by frames to avoid JobTemp spikes.")]
    public int fxDelayFrames = 2;
    public int navDelayFrames = 2;

    ParticleSystem[] _fx;
    NavMeshAgent[] _agents;

    IEnumerator Start()
    {
        // Grab everything up front
        _fx = Object.FindObjectsByType<ParticleSystem>(
                  FindObjectsInactive.Include, FindObjectsSortMode.None);

        _agents = Object.FindObjectsByType<UnityEngine.AI.NavMeshAgent>(
                  FindObjectsInactive.Include, FindObjectsSortMode.None);

        // 1) Stop ALL particles so nothing spawns on frame 0
        foreach (var ps in _fx) if (ps) { ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear); ps.gameObject.SetActive(false); }

        // 2) Disable ALL agents so they won't query while navmesh builds
        foreach (var a in _agents) if (a && a.enabled) a.enabled = false;

        // Let the scene settle 1 frame
        yield return null;

        // ====== Re-enable in a staggered way ======

        // A) Re-enable particles after a couple frames (to avoid frame-0 geometry jobs)
        for (int i = 0; i < fxDelayFrames; i++) yield return null;
        foreach (var ps in _fx) if (ps) { ps.gameObject.SetActive(true); ps.Play(); }

        // B) Re-enable agents after nav is surely ready
        for (int i = 0; i < navDelayFrames; i++) yield return null;
        foreach (var a in _agents)
        {
            if (!a) continue;
            a.enabled = true;
            if (NavMesh.SamplePosition(a.transform.position, out var hit, 3f, a.areaMask))
                a.Warp(hit.position);
        }
    }
}