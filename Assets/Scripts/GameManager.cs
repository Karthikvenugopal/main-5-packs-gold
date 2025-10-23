using System.Collections;          
using System.Collections.Generic;  
using UnityEngine;
using UnityEngine.SceneManagement;
using System;



public class GameManager : MonoBehaviour
{
    [Header("Prefab to Spawn")]
    public GameObject uiPrefab;

    [Header("UI Sprites")]
    public Sprite starFullSprite;
    public Sprite starEmptySprite;

    private UICanvas uiCanvas;

    private float timeLimit = 90f;
    private float currentTime;
    private bool isGameActive = true;
    private Dictionary<IngredientType, int> collectedIngredients = new Dictionary<IngredientType, int>();

    //analytics
    private string levelId;
    private DateTime startedUtc;



    void Awake()
    {
        Time.timeScale = 1f;
        if (uiPrefab != null)
        {
            GameObject uiInstance = Instantiate(uiPrefab);
            uiCanvas = uiInstance.GetComponent<UICanvas>();
        }
        else
        {
            return;
        }

        foreach (var button in uiCanvas.restartButtons)
        {
            button.onClick.AddListener(RestartGame);
        }

        // Set up NextLevel button
        if (uiCanvas.nextLevelButton != null)
        {
            Debug.Log("NextLevel button found, adding listener");
            uiCanvas.nextLevelButton.onClick.AddListener(LoadNextLevel);
        }
        else
        {
            Debug.Log("NextLevel button is NULL - not assigned in UICanvas!");
        }
    }

    public void StartLevel(int totalIngredientCount)
    {
        isGameActive = true;
        currentTime = timeLimit;

        uiCanvas.timerText.gameObject.SetActive(true);
        uiCanvas.gameWonPanel.SetActive(false);
        uiCanvas.gameOverPanel.SetActive(false);

        collectedIngredients.Clear();

        //analytics 
        levelId = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name; // e.g., "Level1Scene"
        startedUtc = DateTime.UtcNow;
    }

    void Update()
    {
        if (!isGameActive) return;

        currentTime -= Time.deltaTime;
        currentTime = Mathf.Max(0, currentTime);
        UpdateTimerUI();

        if (currentTime <= 0)
        {
            LoseGame();
        }
    }

    public void OnIngredientEaten(IngredientType type)
    {
        if (!isGameActive) return;

        if (!collectedIngredients.ContainsKey(type))
            collectedIngredients[type] = 0;

        collectedIngredients[type] += 1;

        Debug.Log($"Collected: {type}, total count = {collectedIngredients[type]}");
    }


    public void OnExitReached()
    {
        if (!isGameActive) return;

        Dictionary<IngredientType, int> requiredIngredients = new();

        string currentScene = SceneManager.GetActiveScene().name;

        if (currentScene.Contains("SampleScene")) 
        {
            requiredIngredients[IngredientType.Bread] = 2;
            requiredIngredients[IngredientType.Butter] = 2;
            requiredIngredients[IngredientType.Chili] = 2;
        }
        else 
        {
            requiredIngredients[IngredientType.Bread] = 2;
            requiredIngredients[IngredientType.Butter] = 2;
        }

        List<string> missingList = new List<string>();
        bool hasAll = true;

        foreach (var kvp in requiredIngredients)
        {
            IngredientType type = kvp.Key;
            int required = kvp.Value;
            int collected = collectedIngredients.ContainsKey(type) ? collectedIngredients[type] : 0;

            if (collected < required)
            {
                hasAll = false;
                missingList.Add($"{type} ({collected}/{required})");
            }
        }

        if (hasAll)
        {
            WinGame();
        }
        else
        {
            string reason = "Oops! You missed out on:\n" + string.Join(", ", missingList);
            LoseGame(reason);
        }
    }





    private void WinGame()
    {
        Debug.Log("WinGame called - showing game won panel");
        isGameActive = false;
        Time.timeScale = 0f;
        uiCanvas.gameWonPanel.SetActive(true);

        float timeTaken = timeLimit - currentTime;

        //Analytics
        string levelId = SceneManager.GetActiveScene().name;
        AnalyticsManager.I?.LogRow(levelId, success: true, timeSpentS: timeTaken);
        var sender  = FindObjectOfType<AnalyticsSender>();
        if (sender) sender.SendLevelResult(levelId, true, timeTaken);



        int starsEarned = CalculateStars(timeTaken);

        for (int i = 0; i < uiCanvas.stars.Length; i++)
        {
            uiCanvas.stars[i].sprite = (i < starsEarned)
                ? starFullSprite
                : starEmptySprite;
        }

    }

    private void LoseGame(string reason = "")
    {
        if (!isGameActive) return; 

        isGameActive = false;
        Time.timeScale = 0f;

        if (uiCanvas != null)
            uiCanvas.gameOverPanel.SetActive(true);

        if (uiCanvas != null && uiCanvas.loseReasonText != null)
            uiCanvas.loseReasonText.text = reason;

        Debug.Log("LoseGame() called: " + reason);

        //analytics 
        float timeSpent = timeLimit - currentTime;
        string levelId = SceneManager.GetActiveScene().name;
        AnalyticsManager.I?.LogRow(levelId, success: false, timeSpentS: timeSpent);
        var sender  = FindObjectOfType<AnalyticsSender>();
        if (sender) sender.SendLevelResult(levelId, false, timeSpent);


    }



    public void OnPlayerHitByEnemy()
    {
        if (!isGameActive) return;

        isGameActive = false;
        Time.timeScale = 0f;

        if (uiCanvas != null && uiCanvas.gameOverPanel != null)
            uiCanvas.gameOverPanel.SetActive(true);

        if (uiCanvas != null && uiCanvas.loseReasonText != null)
            uiCanvas.loseReasonText.text = "Oops! You got hit!";
    }

    private void UpdateTimerUI()
    {
        int minutes = Mathf.FloorToInt(currentTime / 60);
        int seconds = Mathf.FloorToInt(currentTime % 60);
        int milliseconds = Mathf.FloorToInt((currentTime * 100) % 100);
        uiCanvas.timerText.text = $"Time left: {minutes:00}:{seconds:00}:{milliseconds:00}";
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void LoadNextLevel()
    {
        Debug.Log("LoadNextLevel called - navigating to InstructionsScene2");
        Time.timeScale = 1f;
        
        // Test if the scene exists
        if (Application.CanStreamedLevelBeLoaded("InstructionsScene2"))
        {
            Debug.Log("InstructionsScene2 exists, loading...");
            SceneManager.LoadScene("InstructionsScene2");
        }
        else
        {
            Debug.LogError("InstructionsScene2 scene not found! Available scenes:");
            for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            {
                string scenePath = SceneManager.GetSceneByBuildIndex(i).path;
                Debug.Log($"Scene {i}: {scenePath}");
            }
        }
    }

    public void ShowRestartPanel()
    {
        Time.timeScale = 0f;

        if (uiCanvas != null)
        {
            uiCanvas.gameOverPanel.SetActive(true);     // show the lose panel
            if (uiCanvas.loseReasonText != null)
                uiCanvas.loseReasonText.text = "";      // blank out the reason
        }
    }


    private int CalculateStars(float time)
    {
        if (time <= 18f) return 5;
        if (time <= 36f) return 4;
        if (time <= 54f) return 3;
        if (time <= 72f) return 2;
        return 1;
    }

}
