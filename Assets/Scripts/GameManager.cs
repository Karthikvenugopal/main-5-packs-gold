using System.Collections;          
using System.Collections.Generic;  
using UnityEngine;
using UnityEngine.SceneManagement;


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
    }

    public void StartLevel(int totalIngredientCount)
    {
        isGameActive = true;
        currentTime = timeLimit;

        uiCanvas.timerText.gameObject.SetActive(true);
        uiCanvas.gameWonPanel.SetActive(false);
        uiCanvas.gameOverPanel.SetActive(false);

        collectedIngredients.Clear();
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
        isGameActive = false;
        Time.timeScale = 0f;
        uiCanvas.gameWonPanel.SetActive(true);

        float timeTaken = timeLimit - currentTime;
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
