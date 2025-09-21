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

    public void PauseGame(GameObject menu)
    {
        isPaused = true;

        if(menuActive) menuActive.SetActive(false);

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
       
        
           playerHPBar.fillAmount = (playerDamageableHealth.CurrentHealth / playerDamageableHealth.MaxHealth);
          playerMPBar.fillAmount = (playerController.CurrentMana / playerController.MaxMana);
    }
}