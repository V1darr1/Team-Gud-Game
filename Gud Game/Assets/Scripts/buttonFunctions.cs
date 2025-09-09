using UnityEngine;

public class buttonFunctions : MonoBehaviour
{
    public void startGame()
    {
        gameManager.instance.UnpauseGame();
    }

    public void UnpauseGame()
    {
        gameManager.instance.UnpauseGame();
    }



    public void returnToMainMenu()
    {
        gameManager.instance.ReturnToMainMenu();
    }
    public void restartGame()
    {
        gameManager.instance.RestartGame();
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
