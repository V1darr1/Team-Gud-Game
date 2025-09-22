using UnityEngine;

public class PlayerUpgrades : MonoBehaviour
{
    [Header("Speed Bonus")]
    [Tooltip("Additive +% applied to your base speed (e.g., 0.15 = +15%).")]
    public float speedPercentBonus = 0f;

    [Header("Fire Modifiers")]
    public bool tripleBurstEnabled = false;
    [Tooltip("How many projectiles in burst when enabled.")]
    public int burstCount = 3;
    [Tooltip("Delay between burst shots (seconds).")]
    public float burstInterval = 0.08f;
    public float coneTotalAngle = 15f; 

    public void AddSpeedPercent(int percent)
    {
        speedPercentBonus += Mathf.Max(0, percent) / 100f;
    }

    public void EnableTripleBurst()
    {
        tripleBurstEnabled = true;
        burstCount = Mathf.Max(3, burstCount);
    }
}

