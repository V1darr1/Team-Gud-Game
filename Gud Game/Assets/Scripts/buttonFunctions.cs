using System.Collections;
using UnityEngine;

public class buttonFunctions : MonoBehaviour
{
    [SerializeField] private CameraController cameraController;

    public void startGame()
    {
        gameManager.instance.UnpauseGame();
    }

    public void resume()
    {
        gameManager.instance.UnpauseGame();
    }

    public void returnToMainMenu()
    {
        gameManager.instance.ReturnToMainMenu();

        MusicManager.Instance.PlayMusic("MainMenu");
    }

    public void openSettingsMenu()
    {
        gameManager.instance.OpenSettingsMenu();
    }

    public void InvertYOn()
    {
        if(cameraController != null) cameraController.SetInvertY(true);
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
}