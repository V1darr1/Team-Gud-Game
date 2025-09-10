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

    public void quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}