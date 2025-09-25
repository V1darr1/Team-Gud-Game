using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [SerializeField] MusicManager musicManager;
    private void Awake()
    {
        // This is the most reliable place to call a singleton's method.
        musicManager.PlayMusic("MainMenu");
    }

    public void StartNewGame()
    {
        SceneManager.LoadScene("Presentation Map");
    }

    public void QuitGame()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
