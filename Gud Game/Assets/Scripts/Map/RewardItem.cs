using UnityEngine;

[CreateAssetMenu(menuName = "Rogue/Reward Item", fileName = "RewardItem")]
public class RewardItem : ScriptableObject
{
    public string itemName;
    [TextArea] public string description;
    public Sprite icon;

    public enum RewardType { TripleBurst, AOEOnHit, SpeedPercent, AddMaxHP, AddMaxMana }
    public RewardType rewardType;

   
    //  - TripleBurst: ignored (we set burst=3 internally)
    //  - AOEOnHit:    value = damage
    //  - SpeedPercent:value = percent (e.g., 15 => +15%)
    public int value = 10;
}
