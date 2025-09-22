using UnityEngine;
using System.Linq;

public class RoomController : MonoBehaviour
{
    [SerializeField] private Door[] doors;
    [SerializeField] private MonoBehaviour[] objectiveBehaviours; // scripts that implement IRoomObjective

    private IRoomObjective[] _objectives;
    private int _completedCount;


    void Awake()
    {
        // Auto-find doors if not assigned
        if (doors == null || doors.Length == 0)
            doors = GetComponentsInChildren<Door>(true);

        // If you didn't assign objectives in the inspector, auto-find any under this room
        if (objectiveBehaviours == null || objectiveBehaviours.Length == 0)
        {
            objectiveBehaviours = GetComponentsInChildren<MonoBehaviour>(true)
                                  .Where(mb => mb is IRoomObjective)
                                  .ToArray();
        }

        // Cast MonoBehaviours -> IRoomObjective (and de-dupe)
        _objectives = objectiveBehaviours
                      .OfType<IRoomObjective>()
                      .Distinct()
                      .ToArray();
    }

    void OnEnable()
    {
        foreach (var obj in _objectives)
            obj.OnCompleted += OnObjectiveCompleted;
    }

    void Start()
    {
        // IMPORTANT: lock in Start (after OnEnable) so we don't miss a fast completion
        if (_objectives == null || _objectives.Length == 0)
        {
            // No objectives → keep doors usable (do not trap 1-door rooms)
            foreach (var d in doors) d.Unlock();
        }
        else
        {
            foreach (var d in doors) d.Lock();
        }
    }

    void OnDisable()
    {
        foreach (var obj in _objectives)
            obj.OnCompleted -= OnObjectiveCompleted;
    }

    void OnObjectiveCompleted(IRoomObjective obj)
    {
        _completedCount++;
        if (_completedCount >= _objectives.Length)
        {
            // Unlock remaining doors. If your Door has a permanent lock guard,
            // Unlock() will naturally skip sealed doors.
            foreach (var d in doors) d.Unlock();
          
            gameManager.instance.roomsClearedThisRun++;
            gameManager.instance.OnRoomCleared?.Invoke();
        }
    }
}