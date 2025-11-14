using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuUI : MonoBehaviour
{
    [SerializeField] private string levelSelectScene = "LevelSelectScene";

    public void PlayGame()
    {
        SceneManager.LoadScene(levelSelectScene);
    }

    public void OpenTutorial()
    {
        SceneManager.LoadScene("TutorialScene");
    }

    public void QuitGame()
    {
        Application.Quit();
        Debug.Log("Game Closed"); 
    }
}
