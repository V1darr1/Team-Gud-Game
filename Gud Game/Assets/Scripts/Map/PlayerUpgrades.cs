using UnityEngine;

public class PlayerUpgrades : MonoBehaviour
{
    [Header("Speed Bonus")]
    [Tooltip("Additive multiplier: 0.15 = +15%.")]
    public float speedPercentBonus = 0f;

    [Tooltip("Hard cap for total percent bonus. 0.5 = +50% max.")]
    public float speedPercentCap = 0.50f;

    [Header("Fire Modifiers")]
    public bool tripleBurstEnabled = false;
    public int burstCount = 3;
    public float burstInterval = 0.08f;

    [Header("Cone Shot")]
    public float coneTotalAngle = 15f;

    public void AddSpeedPercent(int percent)
    {
        speedPercentBonus += Mathf.Max(0, percent) / 100f;
        speedPercentBonus = Mathf.Clamp(speedPercentBonus, -0.90f, speedPercentCap);
    }

    public void EnableTripleBurst()
    {
        tripleBurstEnabled = true;
        burstCount = Mathf.Max(3, burstCount);
    }
}
