using UnityEngine;
using UnityEngine.UI;

public class gameManager : MonoBehaviour
{
    public static gameManager instance;

    [SerializeField] GameObject menuActive;
    [SerializeField] GameObject menuPause;
    [SerializeField] GameObject menuWin;
    [SerializeField] GameObject menuLose;

    public Image playerHPBar;
    public GameObject playerDamageFlash;
    
    public GameObject player;

    public bool isPaused;

    int gameGoalCount;

    float timeScaleOrig;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        instance = this;
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown("Cancel"))
        {
            if(menuActive == null)
            {
                // State Pause logic
            }
        }
    }
}
