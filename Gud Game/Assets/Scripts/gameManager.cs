using UnityEngine;
using UnityEngine.UI;

public class gameManager : MonoBehaviour
{
    public static gameManager instance;

    [SerializeField] GameObject menuActive;
    [SerializeField] GameObject menuPause;
    [SerializeField] GameObject menuWin;
    [SerializeField] GameObject menuLose;
    [SerializeField] GameObject menuMain;

    public Image playerHPBar;
    public Image playerMPBar;
    public GameObject playerDamageFlash;

    public GameObject player;

    public bool isPaused;

    int gameGoalCount;

    float timeScaleOrig;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    private void Start()
    {

        timeScaleOrig = Time.timeScale;


        if (menuMain != null) menuMain.SetActive(true);
        if (menuPause != null) menuPause.SetActive(false);
        if (menuWin != null) menuWin.SetActive(false);
        if (menuLose != null) menuLose.SetActive(false);

        menuActive = menuMain;
        isPaused = true;
        Time.timeScale = 0f;


        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }


    // Update is called once per frame
    void Update()
    {
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
            // Hides the active menu
            menuActive.SetActive(false);
        }
        menuActive = null;
        Time.timeScale = timeScaleOrig;
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }
    public Transform playerSpawnPoint;
    public void RestartGame()
    {

        UnpauseGame();


        if (player != null)
        {

            player.transform.position = new Vector3(0, 1, 0);
            player.transform.rotation = Quaternion.identity;


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
