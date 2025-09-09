using UnityEngine;

public class buttonFunctions : MonoBehaviour
{
    public void startGame()
    {
        gameManager.instance.UnpauseGame();
    }

    public void resume()
    {
        gameManager.instance.UnpauseGame();
    }

    public void restartGame()
    {
        gameManager.instance.RestartGame();
    }

    public void returnToMainMenu()
    {
        gameManager.instance.ReturnToMainMenu();
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