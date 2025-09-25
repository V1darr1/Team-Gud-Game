using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class gameManager : MonoBehaviour
{
    public static gameManager instance;

    private static bool bootToMainMenu = true;

    // References to your menu UI panels
    [SerializeField] GameObject menuMain;
    [SerializeField] GameObject menuPause;
    [SerializeField] GameObject menuWin;
    [SerializeField] GameObject menuLose;
    [SerializeField] GameObject settingsMenu;
    [SerializeField] Camera mainCam;

    [SerializeField] TMP_Text enemiesRemainingText;
    [SerializeField] TMP_Text roomsCompletedText;

    [HideInInspector] public GameObject menuActive;
   
    public Image playerHPBar;
    public Image playerMPBar;
    public GameObject playerDamageFlash;

    public GameObject player;
    public PlayerController playerController;
    public DamageableHealth playerDamageableHealth;
    public Transform playerSpawnPoint; // A reference to the player's start position

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
            // Removed DontDestroyOnLoad as it's not needed for a single-scene setup.
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
        timeScaleOrig = Time.timeScale;
        player = GameObject.FindWithTag("Player");
        playerController = player.GetComponent<PlayerController>();
        playerDamageableHealth = player.GetComponent<DamageableHealth>();

        if (menuMain != null) menuMain.SetActive(true);
        if (menuPause != null) menuPause.SetActive(false);
        if (menuWin != null) menuWin.SetActive(false);
        if (menuLose != null) menuLose.SetActive(false);

        if(bootToMainMenu)
        {
            if (menuMain) menuMain.SetActive(true);
            menuActive = menuMain;
            isPaused = true;
            Time.timeScale = 0f;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
        else
        {
            menuActive = null;
            isPaused = false;
            Time.timeScale = 1f;
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;

            if (playerHPBar) playerHPBar.fillAmount = 1f;
            if (playerMPBar) playerMPBar.fillAmount = 1f;
        }
        if (!player) player = GameObject.FindWithTag("Player");
        if (!playerController && player) playerController = player.GetComponent<PlayerController>();
        if (!playerDamageableHealth && player) playerDamageableHealth = player.GetComponent<DamageableHealth>();

        RefreshAllUI();                 // show 0/0 at boot
        SetRoomsCompleted(0);           // start-of-run baseline

        MusicManager.Instance.PlayMusic("MainMenu");
    }

    void Update()
    {
        if(Input.GetButtonDown("Cancel"))
        {
            if (menuActive == null) PauseGame(menuPause);
            else if (menuActive == menuPause) UnpauseGame();
        }
        HealthAndMana();
    }


    public void Play()
    {
        MusicManager.Instance.PlayMusic("MainMenu");
        
    }
    public void PauseGame(GameObject menu)
    {
        isPaused = true;

        if(menuActive) menuActive.SetActive(false);

        menuActive = menu;
        if (menuActive) menuActive.SetActive(true);

        Time.timeScale = 0;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        MusicManager.Instance.PlayMusic("MainMenu");
    }

    public void UnpauseGame()
    {
        isPaused = false;

        if (menuActive) menuActive.SetActive(false);
        menuActive = null;

        Time.timeScale = timeScaleOrig;
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        MusicManager.Instance.PlayMusic("Play Music");
    }
   
    public void ReturnToMainMenu()
    {
        bootToMainMenu = true;
        Time.timeScale = 1f;
        isPaused = false;
        menuActive = null;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex, LoadSceneMode.Single);
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
        SetRoomsCompleted(roomsClearedThisRun);  // keep the HUD in sync

        // Fire event for systems like RewardEveryNRooms
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

}