using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class buttonFunctions : MonoBehaviour
{
    [SerializeField] private CameraController cameraController;

    public void startGame()
    {
        gameManager.bootToMainMenu = false;


        SceneManager.LoadScene("Levels/Presentation Map");
    }

    public void resume()
    {
        gameManager.instance.UnpauseGame();
    }

    public void returnToMainMenu()
    {
        Debug.Log("Button Clicked! Attempting To Return To MAinMenu");
        gameManager.instance.ReturnToMainMenu();

        SceneManager.LoadScene("Levels/Main Menu");
    }

    public void openSettingsMenu()
    {
        gameManager.instance.OpenSettingsMenu();
    }

    public void InvertYOn()
    {
        if (cameraController != null) cameraController.SetInvertY(true);
    }

    public void InvertYOff()
    {
        if (cameraController != null) cameraController.SetInvertY(false);
    }

    public void newGame()
    {
        StartCoroutine(NewGameRoutine());
    }

    private IEnumerator NewGameRoutine()
    {
        gameManager.instance.OnNewGame();

        // wait a second or two so rooms reset & player gets placed
        yield return new WaitForSeconds(1.5f);

        SceneManager.LoadScene("Levels/Presentation Map");

        gameManager.instance.UnpauseGame();

    }

    public void quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
    public void LoadMainMenuAdditively()
    {
        // This pauses the game and loads the main menu on top
        gameManager.instance.PauseGame(gameManager.instance.menuPause);
        SceneManager.LoadScene("Levels/Main Menu", LoadSceneMode.Additive);
    }

    public void UnloadMainMenu()
    {
        // This unloads the main menu and unpauses the game
        SceneManager.UnloadSceneAsync("Levels/MainMenu");
        gameManager.instance.UnpauseGame();
    }

}
