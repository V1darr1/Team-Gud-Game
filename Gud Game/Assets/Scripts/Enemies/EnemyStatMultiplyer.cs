using UnityEngine;


[CreateAssetMenu(menuName = "Enemy Stats Multiplyer")]
public class EnemyStatMultiplyer : ScriptableObject
{
    public GameObject enemyModel;

    [Range(0,1)] public float healthMultiplier;
    [Range(0, 5)] public float damageMultiplier;
    [Range(0, 5)] public float sightRange;
    [Range(0, 5)] public float attackRange;
}
