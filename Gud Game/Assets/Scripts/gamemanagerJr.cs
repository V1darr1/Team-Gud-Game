using UnityEngine;
using UnityEngine.UI;
public class gamemanager : MonoBehaviour
{

    public static gamemanager instance;

    [SerializeField] GameObject menuActive;
    [SerializeField] GameObject menuPause;
    [SerializeField] GameObject menuWin;
    [SerializeField] GameObject menuLose;

    public Image playerHPBar;
    public GameObject playerDamageFlash;

    public GameObject player;
    public PlayerController playerScript;

    public bool isPaused;

    nint gameGoalCount;

    float timeScaleOrig;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        instance = this;
        timeScaleOrig = Time.timeScale;

        player = GameObject.FindWithTag("Player");
        playerScript = player.GetComponent<PlayerController>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetButtonDown("Cancel"))
        {
            if (menuActive == null)
            {
                statePause();
                menuActive = menuPause;
                menuActive.SetActive(true);
            }
            else if (menuActive == menuPause)
            {
                stateUnpause();
            }
        }
    }

    public void statePause()
    {
        isPaused = !isPaused;
        Time.timeScale = 0;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

    }

    public void stateUnpause()
    {
        isPaused = !isPaused;
        Time.timeScale = timeScaleOrig;
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        menuActive.SetActive(false);
        menuWin = null;
    }

    public void updateGameGoal(int amount)
    {
        gameGoalCount += amount;

        if (gameGoalCount <= 0)
        {
            //you won!!
            statePause();
            menuActive = menuWin;
            menuActive.SetActive(true);

        }
    }

    public void youLose()
    {
        statePause();
        menuActive = menuLose;
        menuActive.SetActive(true);
    }

}
