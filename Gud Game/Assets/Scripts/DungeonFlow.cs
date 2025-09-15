using UnityEngine;
using System.Collections.Generic;

public class DungeonFlow : MonoBehaviour
{
    [Header("Rooms")]
    [SerializeField] private RoomTemplate startRoom;
    [SerializeField] private List<RoomTemplate> pool;

    [Header("Transition")]
    [SerializeField] private float doorOffset = 0.1f;
    [SerializeField] private int keepRooms = 3;

    private readonly List<GameObject> _spawned = new();
    private Transform _player;

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
        if (next == null) return;

        var nextGO = Instantiate(next.roomPrefab);
        _spawned.Add(nextGO);

        var nextDoor = FindMatchingDoor(nextGO, fromDoor.SocketId);

        if (nextDoor)
        {
            float angle = Vector3.SignedAngle(
                nextDoor.transform.forward,
                -fromDoor.transform.forward,
                Vector3.up
            );

            nextGO.transform.RotateAround(nextDoor.transform.position, Vector3.up, angle);

            Vector3 deltaPos = fromDoor.transform.position - nextDoor.transform.position;
            nextGO.transform.position += deltaPos;

            nextGO.transform.position += fromDoor.transform.forward * doorOffset;
        }
        else
        {
            nextGO.transform.position = fromDoor.transform.position + fromDoor.transform.forward * 6f;
        }

        if (nextDoor && nextDoor.SpawnPoint)
        {
            player.position = nextDoor.SpawnPoint.position;
            player.rotation = nextDoor.SpawnPoint.rotation;
        }
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
        foreach (var d in doors) if (d.SocketId == socketId) return d;
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