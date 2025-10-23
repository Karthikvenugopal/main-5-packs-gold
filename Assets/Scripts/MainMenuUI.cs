using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuUI : MonoBehaviour
{
    public void PlayGame()
    {
        SceneManager.LoadScene("InstructionScene"); 
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
