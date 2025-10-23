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

        int requiredCount = 2;

        int GetCount(IngredientType type)
        {
            return collectedIngredients.ContainsKey(type) ? collectedIngredients[type] : 0;
        }
        int chiliCount = GetCount(IngredientType.Chili);
        int butterCount = GetCount(IngredientType.Butter);
        int breadCount = GetCount(IngredientType.Bread);

        bool hasEnoughChili = chiliCount >= requiredCount;
        bool hasEnoughButter = butterCount >= requiredCount;
        bool hasEnoughBread = breadCount >= requiredCount;

        if (hasEnoughChili && hasEnoughButter && hasEnoughBread)
        {
            WinGame();
        }
        else
        {
            List<string> missing = new List<string>();

            if (!hasEnoughChili)
                missing.Add($"Chili ({chiliCount}/{requiredCount})");

            if (!hasEnoughButter)
                missing.Add($"Butter ({butterCount}/{requiredCount})");

            if (!hasEnoughBread)
                missing.Add($"Bread ({breadCount}/{requiredCount})");

            string reason = "Oops! You only collected:\n" + string.Join(", ", missing);
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
