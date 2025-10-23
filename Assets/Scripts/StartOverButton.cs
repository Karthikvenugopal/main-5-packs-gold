using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class StartOverButton : MonoBehaviour
{
    [Header("Button Settings")]
    [SerializeField] private Button startOverButton;
    
    [Header("Optional: Custom Restart Logic")]
    [SerializeField] private bool useCustomRestartLogic = false;
    [SerializeField] private string customSceneName = "";
    
    private GameManager gameManager;
    private GameManagerTutorial tutorialManager;
    
    private void Awake()
    {
        if (startOverButton == null)
        {
            startOverButton = GetComponent<Button>();
        }
        
        gameManager = FindFirstObjectByType<GameManager>();
        tutorialManager = FindFirstObjectByType<GameManagerTutorial>();
    }
    
    private void Start()
    {
        if (startOverButton != null)
        {
            startOverButton.onClick.AddListener(OnStartOverClicked);
        }
        else
        {
            Debug.LogWarning("StartOverButton: No button component found!");
        }
    }
    
    private void OnDestroy()
    {
        if (startOverButton != null)
        {
            startOverButton.onClick.RemoveListener(OnStartOverClicked);
        }
    }
    

    private void OnStartOverClicked()
    {
        Time.timeScale = 1f;
        
        if (useCustomRestartLogic && !string.IsNullOrEmpty(customSceneName))
        {
            SceneManager.LoadScene(customSceneName);
        }
        else if (tutorialManager != null)
        {
            RestartTutorial();
        }
        else if (gameManager != null)
        {
            gameManager.RestartGame();
        }
        else
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }
    
    private void RestartTutorial()
    {
        // Reset tutorial state
        if (tutorialManager != null)
        {
            // Reset tutorial to intro step
            tutorialManager.ResetTutorial();
        }
        else
        {
            // Fallback: reload tutorial scene
            SceneManager.LoadScene("TutorialScene");
        }
    }
    
    public void SetButtonEnabled(bool enabled)
    {
        if (startOverButton != null)
        {
            startOverButton.interactable = enabled;
        }
    }

    public void SetButtonVisible(bool visible)
    {
        if (startOverButton != null)
        {
            startOverButton.gameObject.SetActive(visible);
        }
    }
}
