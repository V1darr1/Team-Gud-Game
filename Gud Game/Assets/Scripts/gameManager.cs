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
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        if (menuPause != null) menuPause.SetActive(false);
        if (menuWin != null) menuWin.SetActive(false);
        if (menuLose != null) menuLose.SetActive(false);


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
        menuActive = menu;
        menuActive.SetActive(true);
        Time.timeScale = 0;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }
    public void UnpauseGame()
    {
        isPaused = false;
        menuActive.SetActive(false);
        menuActive = null;
        Time.timeScale = timeScaleOrig;
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    public void OpenWinMenu()
    {
        PauseGame(menuWin);
    }
    public void OpenLoseMenu()
    {
        PauseGame(menuLose);
    }
    public void OpenMain()
    {
        PauseGame(menuMain);
    }
}
