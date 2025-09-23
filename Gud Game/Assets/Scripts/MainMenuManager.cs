using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    public void StartNewGame()
    {

        SceneManager.LoadScene("Levels/Playable Map");
    }

    public void QuitGame()
    {

        Application.Quit();


#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
