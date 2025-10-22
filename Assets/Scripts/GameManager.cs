using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    [Header("Prefab to Spawn")]
    public GameObject uiPrefab;

    [Header("UI Sprites")]
    public Sprite starFullSprite;
    public Sprite starEmptySprite;

    private UICanvas uiCanvas;

    private float timeLimit = 120f;
    private float currentTime;
    private bool isGameActive = true;
    private HashSet<IngredientType> collectedIngredients = new HashSet<IngredientType>();

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
            Debug.LogError("UI Prefab is not assigned in the GameManager inspector!");
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
        collectedIngredients.Add(type);
        Debug.Log($"Collected: {type}");
    }

    public void OnExitReached()
    {
        if (!isGameActive) return;

        bool hasChili = collectedIngredients.Contains(IngredientType.Chili);
        bool hasButter = collectedIngredients.Contains(IngredientType.Butter);
        bool hasBread = collectedIngredients.Contains(IngredientType.Bread);

        if (hasChili && hasButter && hasBread)
        {
            WinGame();
        }
        else
        {
            List<string> missing = new List<string>();
            if (!hasChili) missing.Add("Chili");
            if (!hasButter) missing.Add("Butter");
            if (!hasBread) missing.Add("Bread");

            string reason = "Oops! You missed " + string.Join(", ", missing) + "!";

            Debug.Log(reason);
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
        isGameActive = false;
        Time.timeScale = 0f;
        uiCanvas.gameOverPanel.SetActive(true);

        if (uiCanvas.loseReasonText != null)
        {
            uiCanvas.loseReasonText.text = reason;
        }
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

    private int CalculateStars(float time)
    {
        if (time <= 24f) return 5;
        if (time <= 48f) return 4;
        if (time <= 72f) return 3;
        if (time <= 96f) return 2;
        return 1;
    }
}
