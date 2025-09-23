using UnityEngine;

public class RewardEveryNRooms : MonoBehaviour
{
    [SerializeField] int roomsPerChoice = 3;      // set 1 for testing; 3 for real
    [SerializeField] RewardUI rewardUI;           // assign in Inspector
    [SerializeField] RewardItem[] pool;           // your 3 assets

    void Awake()
    {
        
    }

    void OnEnable() { TrySubscribe(); }
    void Start() { TrySubscribe(); }
    void OnDisable()
    {
        if (gameManager.instance != null)
            gameManager.instance.OnRoomCleared -= HandleRoomCleared;
    }

    void TrySubscribe()
    {
        if (gameManager.instance != null)
        {
            gameManager.instance.OnRoomCleared -= HandleRoomCleared;
            gameManager.instance.OnRoomCleared += HandleRoomCleared;
            Debug.Log("[Rewards] Subscribed to OnRoomCleared");
        }
        else
        {
            Debug.LogWarning("[Rewards] gameManager.instance is null; will try again in Start");
        }
    }

    void HandleRoomCleared()
    {
        var gm = gameManager.instance;
        if (!gm) return;

        Debug.Log($"[Rewards] HandleRoomCleared roomsCleared={gm.roomsClearedThisRun}");

        if (roomsPerChoice <= 0) roomsPerChoice = 1;
        if (gm.roomsClearedThisRun % roomsPerChoice != 0)
        {
            Debug.Log("[Rewards] Not a reward room yet");
            return;
        }

        if (!rewardUI) { Debug.LogError("[Rewards] rewardUI not assigned"); return; }
        if (pool == null || pool.Length < 1) { Debug.LogError("[Rewards] Pool empty"); return; }

        var choices = GetChoices(3);
        Debug.Log($"[Rewards] Opening panel with {choices.Length} choices");

        // ---- OPEN: pause game + show cursor + disable player input ----
        Time.timeScale = 0f;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        gameManager.instance.player?.GetComponent<PlayerController>()?.SetInputEnabled(false);

        rewardUI.Setup(choices, this);
    }

    RewardItem[] GetChoices(int count)
    {
        int n = Mathf.Clamp(count, 1, Mathf.Min(3, pool.Length));
        var arr = new RewardItem[n];
        for (int i = 0; i < n; i++) arr[i] = pool[Random.Range(0, pool.Length)];
        return arr;
    }

    public void Pick(RewardItem r)
    {
        Apply(r);
        rewardUI.Hide();

        // ---- CLOSE: resume game + hide cursor + re-enable input ----
        Time.timeScale = 1f;
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        gameManager.instance.player?.GetComponent<PlayerController>()?.SetInputEnabled(true);
    }

    void Apply(RewardItem r)
    {
        var player = gameManager.instance?.player;
        if (!player) { Debug.LogWarning("[Rewards] No player found to apply reward"); return; }

        var upgrades = player.GetComponent<PlayerUpgrades>() ?? player.AddComponent<PlayerUpgrades>();
        var effects = player.GetComponent<PlayerEffects>();

        switch (r.rewardType)
        {
            case RewardItem.RewardType.TripleBurst:
                upgrades.EnableTripleBurst();                     
                Debug.Log("[Rewards] Applied TripleBurst/Cone");
                break;

            case RewardItem.RewardType.AOEOnHit:
                if (!effects) effects = player.AddComponent<PlayerEffects>();
                effects.aoeOnHit = true;
                effects.aoeDamage = Mathf.Max(1, r.value);
                Debug.Log("[Rewards] Applied AOEOnHit");
                break;

            case RewardItem.RewardType.SpeedPercent:
                upgrades.AddSpeedPercent(r.value);
                Debug.Log($"[Rewards] Applied Speed +{r.value}%");
                break;
        }
    }

    // Force-open for testing 
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F10))
        {
            if (!rewardUI) { Debug.LogError("[Rewards] No rewardUI assigned"); return; }

            Time.timeScale = 0f;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            gameManager.instance.player?.GetComponent<PlayerController>()?.SetInputEnabled(false);

            rewardUI.Setup(GetChoices(3), this);
            Debug.Log("[Rewards] Forced open via F10");
        }
    }
}
