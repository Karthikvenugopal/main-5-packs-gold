using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuUI : MonoBehaviour
{
    public void PlayGame()
    {
        SceneManager.LoadScene("Level1Scene");
    }

    public void OpenTutorial()
    {
        SceneManager.LoadScene("Level2Scene");
    }

    public void QuitGame()
    {
        Application.Quit();
        Debug.Log("Game Closed"); 
    }
}
