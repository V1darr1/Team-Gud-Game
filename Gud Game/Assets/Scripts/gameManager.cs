using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class gameManager : MonoBehaviour
{
    public static gameManager instance;
    public static bool gameHasBooted = false; // The static flag

    // References to your menu UI panels
    public GameObject menuPause;
    [SerializeField] GameObject menuWin;
    [SerializeField] GameObject menuLose;
    [SerializeField] GameObject settingsMenu;
    [SerializeField] GameObject menuMain; // Add this back for consistency
    [SerializeField] Camera mainCam; // Add this back for consistency

    [SerializeField] TMP_Text enemiesRemainingText;
    [SerializeField] TMP_Text roomsCompletedText;

    [HideInInspector] public GameObject menuActive;

    public Image playerHPBar;
    public Image playerMPBar;
    public GameObject playerDamageFlash;

    public GameObject player;
    public PlayerController playerController;
    public DamageableHealth playerDamageableHealth;
    public Transform playerSpawnPoint;

    public bool isPaused;
    public bool yInvertON;
    public bool yInvertOFF;

    public System.Action OnRoomCleared;
    public int roomsClearedThisRun;

    int gameGoalCount;
    float timeScaleOrig;

    private int enemiesRemaining;
    private int roomsCompleted;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    private void Start()
    {
        // This check runs only the very first time the game is loaded from the editor.
        if (!gameHasBooted)
        {
            gameHasBooted = true;
            SceneManager.LoadScene("Main Menu");
            return;
        }

        // The rest of the Start() code will only run after a player clicks "New Game"
        // in the Main Menu scene.
        timeScaleOrig = Time.timeScale;
        player = GameObject.FindWithTag("Player");
        if (player)
        {
            playerController = player.GetComponent<PlayerController>();
            playerDamageableHealth = player.GetComponent<DamageableHealth>();
        }

        if (playerHPBar) playerHPBar.fillAmount = 1f;
        if (playerMPBar) playerMPBar.fillAmount = 1f;

        isPaused = false;
        Time.timeScale = 1f;
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        RefreshAllUI();
        SetRoomsCompleted(0);

        // This line plays the in-game music when the game scene loads.
        MusicManager.Instance.PlayMusic("Play Music");
    }

    void Update()
    {
        if (Input.GetButtonDown("Cancel"))
        {
            if (menuActive == null) PauseGame(menuPause);
            else if (menuActive == menuPause) UnpauseGame();
        }
        HealthAndMana();
    }

    public void PauseGame(GameObject menu)
    {
        isPaused = true;
        if (menuActive) menuActive.SetActive(false);
        menuActive = menu;
        if (menuActive) menuActive.SetActive(true);
        Time.timeScale = 0;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void UnpauseGame()
    {
        isPaused = false;
        if (menuActive) menuActive.SetActive(false);
        menuActive = null;
        Time.timeScale = timeScaleOrig;
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    public void ReturnToMainMenu()
    {
        // This is called from the pause menu. It loads the main menu.
        gameHasBooted = false; // Reset the flag
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        SceneManager.LoadScene("Main Menu");
        MusicManager.Instance.PlayMusic("MainMenu");
    }

    public void OnNewGame()
    {
        var scene = SceneManager.GetActiveScene().buildIndex;
        SceneManager.LoadScene(scene);
        
    }

    public void ReturnToPauseMenu(GameObject menu)
    {
        PauseGame(menu);

        var cam = mainCam != null ? mainCam : Camera.main;
        if (cam != null) cam.ResetProjectionMatrix();
    }

    public void updateGameGoal(int amount)
    {

    }

    public void OpenWinMenu()
    {
        PauseGame(menuWin);
    }

    public void OpenLoseMenu()
    {
        PauseGame(menuLose);
    }

    public void OpenSettingsMenu()
    {
        PauseGame(settingsMenu);
    }

    void HealthAndMana()
    {
        if (!player)
            player = GameObject.FindWithTag("Player");

        if (!playerController && player)
            playerController = player.GetComponent<PlayerController>();

        if (!playerDamageableHealth && player)
            playerDamageableHealth = player.GetComponent<DamageableHealth>();

        if (playerDamageableHealth && playerHPBar)
            playerHPBar.fillAmount = playerDamageableHealth.CurrentHealth / Mathf.Max(1f, playerDamageableHealth.MaxHealth);

        if (playerController && playerMPBar)
            playerMPBar.fillAmount = playerController.CurrentMana / Mathf.Max(1f, playerController.MaxMana);
    }

    public void NotifyRoomCleared()
    {
        roomsClearedThisRun++;
        SetRoomsCompleted(roomsClearedThisRun);
        OnRoomCleared?.Invoke();
    }

    public void SetEnemiesRemaining(int value)
    {
        enemiesRemaining = Mathf.Max(0, value);
        if (enemiesRemainingText) enemiesRemainingText.text = $"{enemiesRemaining}";
    }

    public void SetRoomsCompleted(int value)
    {
        roomsCompleted = Mathf.Max(0, value);
        if (roomsCompletedText) roomsCompletedText.text = $"{roomsCompleted}";
    }

    public void IncrementRoomsCompleted()
    {
        SetRoomsCompleted(roomsCompleted + 1);
    }

    public int CurrentLevel => Mathf.Max(1, roomsClearedThisRun + 1);
    public void DecrementEnemyCount() => SetEnemiesRemaining(enemiesRemaining - 1);
    public void IncrementEnemyCount() => SetEnemiesRemaining(enemiesRemaining + 1);

    private void RefreshAllUI()
    {
        SetEnemiesRemaining(enemiesRemaining);
        SetRoomsCompleted(roomsCompleted);
    }
    public void updateGameGoal(int amount)
    {

    }
}