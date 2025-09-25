using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class gameManager : MonoBehaviour
{
    public static gameManager instance;

    public static bool bootToMainMenu = true;

    // References to your menu UI panels
    [SerializeField] GameObject menuMain;
    public GameObject menuPause;
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

    private int enemiesRemaining;
    private int roomsCompleted;

    int gameGoalCount;
    float timeScaleOrig;


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

        if (bootToMainMenu)
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
        Debug.Log("Game is unpausing.");
        isPaused = false;

        if (menuActive) menuActive.SetActive(false);
        menuActive = null;

        Time.timeScale = timeScaleOrig;
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }
    public void ReturnToMainMenu()
    {
        SceneManager.LoadScene("Levels/Main Menu");
    }

    public void OnNewGame()
    {
        SceneManager.LoadScene("Levels/Playable Map");
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


        playerHPBar.fillAmount = (playerDamageableHealth.CurrentHealth / playerDamageableHealth.MaxHealth);
        playerMPBar.fillAmount = (playerController.CurrentMana / playerController.MaxMana);
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

    public void DecrementEnemyCount() => SetEnemiesRemaining(enemiesRemaining - 1);
    public void IncrementEnemyCount() => SetEnemiesRemaining(enemiesRemaining + 1);

    private void RefreshAllUI()
    {
        SetEnemiesRemaining(enemiesRemaining);
        SetRoomsCompleted(roomsCompleted);
    }
}