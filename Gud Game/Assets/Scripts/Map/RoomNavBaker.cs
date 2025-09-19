using System.Collections;
using UnityEngine;
using Unity.AI.Navigation;   // Requires NavMeshComponents package (NavMeshSurface)

/// <summary>
/// Attach this to any spawned room (or let EnsureAndBuild add it for you).
/// It builds each NavMeshSurface in the room ONCE (synchronous) and exposes a
/// SafeDestroy helper you can use when culling old rooms.
/// </summary>
public class RoomNavBaker : MonoBehaviour
{
    private NavMeshSurface[] _surfaces;
    private bool _built;

    void Awake()
    {
        // Cache all surfaces in this room (children included)
        _surfaces = GetComponentsInChildren<NavMeshSurface>(true);
    }

    /// <summary>
    /// Build all surfaces exactly once. Synchronous (no async API required).
    /// </summary>
    public void BuildOnce()
    {
        if (_built) return;

        if (_surfaces == null || _surfaces.Length == 0)
            _surfaces = GetComponentsInChildren<NavMeshSurface>(true);

        // Optional: reduce runtime allocations by baking from colliders
        // (set this in the Inspector on each surface: Use Geometry = Physics Colliders)

        foreach (var s in _surfaces)
        {
            if (!s) continue;
            // Clear any stale baked data first (important when rooms are cloned)
            s.RemoveData();
            s.BuildNavMesh();  // synchronous build
        }

        _built = true;
    }

    /// <summary>
    /// Convenience: ensures a RoomNavBaker exists on the room and builds once.
    /// Call this right AFTER you've positioned/rotated the spawned room.
    /// </summary>
    public static void EnsureAndBuild(GameObject room)
    {
        if (!room) return;
        var baker = room.GetComponent<RoomNavBaker>();
        if (!baker) baker = room.AddComponent<RoomNavBaker>();
        baker.BuildOnce();
    }

    /// <summary>
    /// Safe destroy helper used when culling rooms. With synchronous builds there
    /// is nothing to wait for, but we yield one frame to be extra cautious if you
    /// call this from within physics/trigger callbacks.
    /// Usage: StartCoroutine(RoomNavBaker.SafeDestroy(roomGO));
    /// </summary>
    public static IEnumerator SafeDestroy(GameObject room)
    {
        if (!room) yield break;
        yield return null; // let current frame finish
        if (room) Destroy(room);
    }
}