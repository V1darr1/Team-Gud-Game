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
        // auto-find doors if not assigned
        if (doors == null || doors.Length == 0)
            doors = GetComponentsInChildren<Door>(true);

        // cast MonoBehaviours -> IRoomObjective
        _objectives = objectiveBehaviours
            .OfType<IRoomObjective>()
            .ToArray();

        // lock all doors at start
        foreach (var d in doors) d.Lock();
    }

    void OnEnable()
    {
        foreach (var obj in _objectives)
            obj.OnCompleted += OnObjectiveCompleted;
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
            foreach (var d in doors) d.Unlock();
        }
    }
}