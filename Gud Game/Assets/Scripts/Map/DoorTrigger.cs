using UnityEngine;

[RequireComponent (typeof(Collider))]
public class DoorTrigger : MonoBehaviour
{
    [SerializeField] private Door door;             // door this trigger belongs to
    [SerializeField] private Transform spawnPoint;  // where we place the player in the next room
    [SerializeField] private string socketId = "A"; // an ID so we align door to door

    Collider _col;
    bool _used;

    private void Awake()
    {
        _col = GetComponent<Collider>();
        if (_col) _col.isTrigger = true;

        if (!door)      door        = GetComponentInParent<Door>();
        if (!spawnPoint) spawnPoint = transform.Find("SpawnPoint");
    }

    void Reset()
    {
        var c = GetComponent<Collider>();
        if (c) c.isTrigger = true;
        if (!door)      door        = GetComponentInParent<Door>();
        if (!spawnPoint) spawnPoint = transform.Find("SpawnPoint");
    }

    void OnTriggerEnter(Collider other)
    {
        if (_used) return; // ← guard (prevents duplicate spawns)
        Debug.Log($"[DoorTrigger] ENTER by '{other.name}', tag='{other.tag}'", this);

        if (!other.CompareTag("Player")) { Debug.Log("[DoorTrigger] Not the player, ignoring."); return; }
        if (!door) { Debug.LogError("[DoorTrigger] No Door reference assigned."); return; }

        Debug.Log($"[DoorTrigger] Door.IsLocked = {door.IsLocked}");
        if (door.IsLocked) { Debug.Log("[DoorTrigger] Door is locked; no transition."); return; }

        var flow = FindFirstObjectByType<DungeonFlow>();
        if (!flow) { Debug.LogError("[DoorTrigger] No DungeonFlow found in scene."); return; }

        // mark used & disable the collider immediately
        _used = true;
        if (_col) _col.enabled = false;

        Debug.Log("[DoorTrigger] Requesting transition...");
        flow.RequestTransitionFromDoor(this, other.transform);
    }

    System.Collections.IEnumerator Reenable(float delay)
    {
        yield return new WaitForSeconds(delay);
        _used = false;
        if (_col) _col.enabled = true;
        if (door) door.Unlock(); // only if you want the door usable again
    }

    public string SocketId => socketId;
    public Transform SpawnPoint => spawnPoint;
    public Door Door => door;
}
