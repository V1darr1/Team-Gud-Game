using UnityEngine;

[CreateAssetMenu(menuName = "Dungeons/Room Template")]
public class RoomTemplate : ScriptableObject
{
    public GameObject roomPrefab;   // singular, used by DungeonFlow
    public string[] sockets;        // door IDs this room has
    public int weight = 1;          // selection weight
}