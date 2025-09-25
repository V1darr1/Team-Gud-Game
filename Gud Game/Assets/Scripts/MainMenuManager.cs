using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    private void Awake()
    {
        // This is the most reliable place to call a singleton's method.
        MusicManager.Instance.PlayMusic("MainMenu");
    }

    public void StartNewGame()
    {
        SceneManager.LoadScene("Presentation Map");
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}