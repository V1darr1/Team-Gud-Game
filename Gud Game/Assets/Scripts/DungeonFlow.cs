using UnityEngine;
using System.Collections.Generic;
using Unity.AI.Navigation;

public class DungeonFlow : MonoBehaviour
{
    [Header("Rooms")]
    [SerializeField] private RoomTemplate startRoom;
    [SerializeField] private List<RoomTemplate> pool;

    [Header("Transition")]
    [SerializeField] private float doorOffset = 0.1f;
    [SerializeField] private int keepRooms = 1;

    private readonly List<GameObject> _spawned = new();
    private Transform _player;

    void BuildRoomNavMesh(GameObject roomGO)
    {
        var surfaces = roomGO.GetComponentsInChildren<NavMeshSurface>(true);
        foreach (var s in surfaces)
        {
            s.RemoveData();   // safety: clear stale data
            s.BuildNavMesh(); // build for this instance at its final position/rotation
        }

        // After building, force agents under this room to sample the new mesh
        var agents = roomGO.GetComponentsInChildren<UnityEngine.AI.NavMeshAgent>(true);
        foreach (var a in agents)
        {
            bool wasEnabled = a.enabled;
            a.enabled = false;
            a.enabled = true; // re-enable to resample
            a.Warp(a.transform.position); // snap to nearest poly of the new mesh
        }

        Debug.Log($"[Flow] Built {surfaces.Length} NavMeshSurface(s) and warped {agents.Length} agent(s) in '{roomGO.name}'.");
    }

    void Start()
    {
        // find the player by tag
        var playerGO = GameObject.FindGameObjectWithTag("Player");
        if (!playerGO)
        {
            Debug.LogError("[DungeonFlow] No GameObject tagged 'Player' in the scene.");
            return;
        }
        _player = playerGO.transform;

        // spawn the initial room from the template
        if (!startRoom || !startRoom.roomPrefab)
        {
            Debug.LogError("[DungeonFlow] Start Room template or prefab missing.");
            return;
        }

        var firstRoom = Instantiate(startRoom.roomPrefab, Vector3.zero, Quaternion.identity);
        _spawned.Add(firstRoom);
        Debug.Log($"[DungeonFlow] Spawned START room '{firstRoom.name}'");

        // 3) teleport the player to the room's PlayerSpawn
        var spawn = FindPlayerSpawn(firstRoom) ?? FallbackSpawn(firstRoom);
        if (spawn)
        {
            _player.position = spawn.position;
            _player.rotation = spawn.rotation;
        }
        else
        {
            Debug.LogWarning("[DungeonFlow] No PlayerSpawn found in start room; leaving player where they are.");
        }
    }

    int CountUsableDoors(GameObject roomGO)
    {
        // count active, enabled triggers that actually have a Door ref
        int count = 0;
        foreach (var d in roomGO.GetComponentsInChildren<DoorTrigger>(true))
        {
            if (!d || !d.isActiveAndEnabled) continue;
            var col = d.GetComponent<Collider>();
            if (col && col.enabled && d.Door != null) count++;
        }
        return count;
    }

    void SealDoor(DoorTrigger trig, bool permanent = true)
    {
        if (!trig) return;
        // permanently lock so RoomController cannot reopen it
        if (trig.Door && permanent) trig.Door.SetPermanentLock(true);

        var col = trig.GetComponent<Collider>();
        if (col) col.enabled = false;
        trig.enabled = false;
    }

    System.Collections.IEnumerator ReenableDoorLater(DoorTrigger trig, float delay)
    {
        if (!trig) yield break;
        var col = trig.GetComponent<Collider>();
        yield return new WaitForSeconds(delay);
        if (trig) trig.enabled = true;
        if (col) col.enabled = true;
    }

    Transform FindPlayerSpawn(GameObject roomGO)
    {
        // looks for a child named "PlayerSpawn"
        var t = roomGO.transform.Find("PlayerSpawn");
        if (t) return t;

        var all = roomGO.GetComponentsInChildren<Transform>(true);
        foreach (var x in all)
            if (x.CompareTag("PlayerSpawn")) return x;

        return null;
    }

    Transform FallbackSpawn(GameObject roomGO)
    {
        // If you forgot to create PlayerSpawn, use the first door’s SpawnPoint.
        var doors = roomGO.GetComponentsInChildren<DoorTrigger>(true);
        if (doors.Length > 0 && doors[0].SpawnPoint) return doors[0].SpawnPoint;
        return null;
    }

    public void RequestTransitionFromDoor(DoorTrigger fromDoor, Transform player)
    {
        var next = ChooseNextTemplate(fromDoor.SocketId);
        if (next == null)
        {
            Debug.LogError($"[Flow] No template in pool exposes socket '{fromDoor.SocketId}'.");
            return;
        }

        var nextGO = Instantiate(next.roomPrefab);
        _spawned.Add(nextGO);

        // Find matching door in the new room (logs all sockets)
        var nextDoor = FindMatchingDoor(nextGO, fromDoor.SocketId);

        // ---------- PLACE THE ROOM ----------
        if (nextDoor)
        {
            // Rotate so nextDoor faces opposite of fromDoor
            float angle = Vector3.SignedAngle(
                nextDoor.transform.forward,
                -fromDoor.transform.forward,
                Vector3.up
            );
            nextGO.transform.RotateAround(nextDoor.transform.position, Vector3.up, angle);

            // Snap positions
            Vector3 deltaPos = fromDoor.transform.position - nextDoor.transform.position;
            nextGO.transform.position += deltaPos;

            // Extra spacing
            nextGO.transform.position += fromDoor.transform.forward * doorOffset;
        }
        else
        {
            // Fallback placement if no match (still spawn somewhere sensible)
            nextGO.transform.position = fromDoor.transform.position + fromDoor.transform.forward * (2f + doorOffset);
            Debug.LogWarning("[Flow] Using fallback placement (no matching door).");
        }

        // Build navmesh for newly placed room
        BuildRoomNavMesh(nextGO);

        // ---------- TELEPORT (with safe fallback) ----------
        Vector3 spawnPos;
        Quaternion spawnRot;

        if (nextDoor && nextDoor.SpawnPoint)
        {
            spawnPos = nextDoor.SpawnPoint.position;
            spawnRot = nextDoor.SpawnPoint.rotation;
        }
        else
        {
            // Compute a safe spawn: one meter "into" the new room, facing inward
            Vector3 intoRoom = nextDoor ? -nextDoor.transform.forward : fromDoor.transform.forward;
            Vector3 refPos = nextDoor ? nextDoor.transform.position : fromDoor.transform.position;
            spawnPos = refPos + intoRoom * 1.0f;
            spawnRot = Quaternion.LookRotation(intoRoom, Vector3.up);
            Debug.LogWarning("[Flow] Using computed spawn (no SpawnPoint / no match).");
        }

        // Small nudge to ensure we're not inside the trigger volume
        Vector3 nudgeDir = nextDoor ? -nextDoor.transform.forward : fromDoor.transform.forward;
        spawnPos += nudgeDir * 0.15f;

        player.SetPositionAndRotation(spawnPos, spawnRot);
        Physics.SyncTransforms();

        // ---------- LOCK / SEAL POLICY ----------
        // Always seal the OLD door immediately (prevents double-trigger & backtrack in this frame)
        SealDoor(fromDoor, permanent: true);

        // Only seal the NEW room's entry if that room has multiple usable doors
        if (nextDoor)
        {
            int doorCount = CountUsableDoors(nextGO);
            Debug.Log($"[Flow] next room doors = {doorCount}");

            if (doorCount > 1)
            {
                // Multi-door room → permanently seal entry to prevent backtracking
                SealDoor(nextDoor, permanent: true);
            }
            else
            {
                // Single-door room → keep the entry usable.
                // Make sure it’s enabled (we might have disabled to avoid immediate retrigger).
                var col = nextDoor.GetComponent<Collider>();
                if (col) col.enabled = true;
                nextDoor.enabled = true;
            }
        }

        // ---------- CLEANUP ----------
        CullOldRooms();
    }

    RoomTemplate ChooseNextTemplate(string socketId)
    {
        var candidates = pool.FindAll(t => t && t.roomPrefab && HasSocket(t, socketId));
        if (candidates.Count == 0) return null;

        int sum = 0; foreach (var c in candidates) sum += Mathf.Max(1, c.weight);
        int pick = Random.Range(0, sum);
        foreach (var c in candidates)
        {
            pick -= Mathf.Max(1, c.weight);
            if (pick < 0) return c;
        }
        return candidates[0];
    }

    bool HasSocket(RoomTemplate t, string id)
    {
        if (t.sockets == null) return false;
        foreach (var s in t.sockets) if (s == id) return true;
        return false;
    }

    DoorTrigger FindMatchingDoor(GameObject roomGO, string socketId)
    {
        var doors = roomGO.GetComponentsInChildren<DoorTrigger>(true);

        // Helpful log so you can see exactly which sockets the new room exposes
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        for (int i = 0; i < doors.Length; i++)
        {
            var sid = string.IsNullOrEmpty(doors[i].SocketId) ? "<empty>" : doors[i].SocketId;
            if (i > 0) sb.Append(", ");
            sb.Append(doors[i].name).Append(":").Append(sid);
        }
        Debug.Log($"[Flow] Spawned '{roomGO.name}' with doors [{sb}]");

        foreach (var d in doors)
            if (d.SocketId == socketId)
                return d;

        Debug.LogError($"[Flow] No door with socket '{socketId}' found in '{roomGO.name}'.");
        return null;
    }

    void CullOldRooms()
    {
        if (_spawned.Count <= keepRooms) return;
        var toDestroy = _spawned[0];
        _spawned.RemoveAt(0);
        if (toDestroy) Destroy(toDestroy);
    }
}