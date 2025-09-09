using UnityEngine;
using UnityEngine.UI;

public class gameManager : MonoBehaviour
{
    public static gameManager instance;

    // References to your menu UI panels
    [SerializeField] GameObject menuMain;
    [SerializeField] GameObject menuPause;
    [SerializeField] GameObject menuWin;
    [SerializeField] GameObject menuLose;

    [HideInInspector] public GameObject menuActive;

    public Image playerHPBar;
    public Image playerMPBar;
    public GameObject playerDamageFlash;

    public GameObject player;
    public Transform playerSpawnPoint; // A reference to the player's start position

    public bool isPaused;
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
        }
    }

    private void Start()
    {
        timeScaleOrig = Time.timeScale;

        // On game start, show the main menu and hide all others.
        if (menuMain != null) menuMain.SetActive(true);
        if (menuPause != null) menuPause.SetActive(false);
        if (menuWin != null) menuWin.SetActive(false);
        if (menuLose != null) menuLose.SetActive(false);

        // Set the main menu as the active one and pause the game
        menuActive = menuMain;
        isPaused = true;
        Time.timeScale = 0f;
    }

    void Update()
    {
        // Listen for the "Escape" key to toggle the pause menu
        if (Input.GetButtonDown("Cancel"))
        {
            if (menuActive == null)
            {
                PauseGame(menuPause);
            }
            else if (menuActive == menuPause)
            {
                UnpauseGame();
            }
        }
    }

    public void PauseGame(GameObject menu)
    {
        isPaused = true;

        if (menuActive != null)
        {
            menuActive.SetActive(false);
        }

        menuActive = menu;
        menuActive.SetActive(true);
        Time.timeScale = 0;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void UnpauseGame()
    {
        isPaused = false;

        if (menuActive != null)
        {
            menuActive.SetActive(false);
        }

        menuActive = null;
        Time.timeScale = timeScaleOrig;
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    // This function restarts the game by resetting the player
    public void RestartGame()
    {
        UnpauseGame();

        if (player != null && playerSpawnPoint != null)
        {
            player.transform.position = playerSpawnPoint.position;
            player.transform.rotation = playerSpawnPoint.rotation;

            if (playerHPBar != null) playerHPBar.fillAmount = 1f;
            if (playerMPBar != null) playerMPBar.fillAmount = 1f;
        }

        gameGoalCount = 0;
    }

    public void ReturnToMainMenu()
    {
        PauseGame(menuMain);
    }

    public void OpenWinMenu()
    {
        PauseGame(menuWin);
    }

    public void OpenLoseMenu()
    {
        PauseGame(menuLose);
    }
}