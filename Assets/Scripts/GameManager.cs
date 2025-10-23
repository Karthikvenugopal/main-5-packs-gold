using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
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
    private bool infoPanelVisible;
    private bool infoPauseActive;
    private float cachedTimeScale = 1f;
    private GameObject infoPageInstance;
    private bool isLoadingInfoPage;
    private string targetInfoSceneName;
    private string loadedInfoSceneName;
    [SerializeField] private float lowTimeHighlightThreshold = 20f;
    private bool lowTimeHighlightTriggered;

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

        EnsureLevelInfoUI();

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

        if (uiCanvas.infoButton != null)
        {
            uiCanvas.infoButton.onClick.RemoveAllListeners();
            uiCanvas.infoButton.onClick.AddListener(OnInfoButtonClicked);
        }

        if (uiCanvas.infoCloseButton != null)
        {
            uiCanvas.infoCloseButton.onClick.RemoveAllListeners();
            uiCanvas.infoCloseButton.onClick.AddListener(() => ToggleInfoPanelInternal(false));
        }

        HideInfoPanelImmediate();
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

        if (!lowTimeHighlightTriggered &&
            ShouldHighlightIngredients() &&
            currentTime <= lowTimeHighlightThreshold)
        {
            lowTimeHighlightTriggered = true;
            HighlightRemainingIngredients();
        }

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

    private void EnsureLevelInfoUI()
    {
        if (uiCanvas == null) return;
        if (!(uiCanvas.transform is RectTransform canvasRect)) return;

        if (uiCanvas.infoButton == null)
        {
            uiCanvas.infoButton = CreateInfoButton(canvasRect);
        }

        if (uiCanvas.infoButton != null)
        {
            RectTransform infoRect = uiCanvas.infoButton.GetComponent<RectTransform>();
            if (infoRect != null)
            {
                infoRect.anchorMin = new Vector2(1f, 0f);
                infoRect.anchorMax = new Vector2(1f, 0f);
                infoRect.pivot = new Vector2(1f, 0f);
                infoRect.sizeDelta = new Vector2(160f, 48f);
                infoRect.anchoredPosition = new Vector2(-270f, 30f);
            }

            if (uiCanvas.infoButton.TryGetComponent(out Image infoImage))
            {
                infoImage.sprite = GetDefaultSprite();
                infoImage.type = Image.Type.Sliced;
                infoImage.color = new Color(0.16f, 0.18f, 0.22f, 0.95f);
            }

            TextMeshProUGUI label = uiCanvas.infoButton.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
            {
                label.text = "Info";
                label.fontSize = 28f;
                label.alignment = TextAlignmentOptions.Center;
                label.color = Color.white;
                label.enableWordWrapping = false;
            }
        }
    }

    private UnityEngine.UI.Button CreateInfoButton(RectTransform parent)
    {
        GameObject buttonGO = new GameObject("InfoButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(UnityEngine.UI.Button));
        buttonGO.transform.SetParent(parent, false);

        RectTransform rect = buttonGO.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(1f, 0f);
        rect.sizeDelta = new Vector2(160f, 48f);
        rect.anchoredPosition = new Vector2(-270f, 30f);

        Image background = buttonGO.GetComponent<Image>();
        background.sprite = GetDefaultSprite();
        background.type = Image.Type.Sliced;
        background.color = new Color(0.16f, 0.18f, 0.22f, 0.95f);

        UnityEngine.UI.Button button = buttonGO.GetComponent<UnityEngine.UI.Button>();
        button.targetGraphic = background;

        GameObject labelGO = new GameObject("Label", typeof(RectTransform));
        labelGO.transform.SetParent(rect, false);
        RectTransform labelRect = labelGO.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        TextMeshProUGUI label = labelGO.AddComponent<TextMeshProUGUI>();
        label.text = "Info";
        label.fontSize = 28f;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;
        label.enableWordWrapping = false;
        ApplyDefaultFont(label);

        return button;
    }

    private IEnumerator ShowInfoPageRoutine()
    {
        yield return EnsureInfoPageLoaded();
        if (infoPageInstance == null) yield break;
        ToggleInfoPanelInternal(true);
    }

    private IEnumerator EnsureInfoPageLoaded()
    {
        if (infoPageInstance != null) yield break;
        if (isLoadingInfoPage)
        {
            while (isLoadingInfoPage) { yield return null; }
            yield break;
        }

        if (string.IsNullOrEmpty(targetInfoSceneName))
        {
            targetInfoSceneName = ResolveInfoSceneName();
        }

        if (string.IsNullOrEmpty(targetInfoSceneName))
        {
            Debug.LogWarning("No info scene mapped for this level.");
            yield break;
        }

        isLoadingInfoPage = true;

        if (!Application.CanStreamedLevelBeLoaded(targetInfoSceneName))
        {
            Debug.LogWarning($"Info scene '{targetInfoSceneName}' not found; cannot display level info overlay.");
            isLoadingInfoPage = false;
            yield break;
        }

        AsyncOperation loadOp = SceneManager.LoadSceneAsync(targetInfoSceneName, LoadSceneMode.Additive);
        if (loadOp == null)
        {
            isLoadingInfoPage = false;
            yield break;
        }

        while (!loadOp.isDone)
        {
            yield return null;
        }

        Scene infoScene = SceneManager.GetSceneByName(targetInfoSceneName);
        if (!infoScene.IsValid())
        {
            isLoadingInfoPage = false;
            yield break;
        }

        GameObject[] roots = infoScene.GetRootGameObjects();

        foreach (GameObject root in roots)
        {
            if (root.TryGetComponent<EventSystem>(out _))
            {
                Destroy(root);
            }
        }

        GameObject canvasRoot = roots.FirstOrDefault(go => go.name == "Canvas");

        if (canvasRoot != null)
        {
            infoPageInstance = Instantiate(canvasRoot, uiCanvas.transform);
            infoPageInstance.name = "LevelInfoOverlay";
            ConfigureInfoOverlay(infoPageInstance);
            infoPageInstance.SetActive(false);
            uiCanvas.levelInfoPanel = infoPageInstance;
            loadedInfoSceneName = targetInfoSceneName;
        }
        else
        {
            Debug.LogWarning($"Canvas not found in info scene '{targetInfoSceneName}' while preparing level info overlay.");
        }

        yield return SceneManager.UnloadSceneAsync(infoScene);

        isLoadingInfoPage = false;
    }

    private void ConfigureInfoOverlay(GameObject overlay)
    {
        if (overlay == null) return;

        overlay.SetActive(true);
        overlay.transform.SetAsLastSibling();

        if (overlay.TryGetComponent(out RectTransform rectTransform))
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.localScale = Vector3.one * 0.78f;
        }

        if (overlay.TryGetComponent(out Canvas overlayCanvas))
        {
            overlayCanvas.overrideSorting = true;
            overlayCanvas.sortingOrder = 50;
        }

        foreach (RectTransform childRect in overlay.GetComponentsInChildren<RectTransform>(true))
        {
            childRect.localScale = Vector3.one;
        }

        foreach (CanvasGroup group in overlay.GetComponentsInChildren<CanvasGroup>(true))
        {
            group.alpha = 1f;
            group.interactable = true;
            group.blocksRaycasts = true;
        }

        foreach (var component in overlay.GetComponentsInChildren<InstructionsUI>(true))
        {
            Destroy(component);
        }

        foreach (var component in overlay.GetComponentsInChildren<InstructionsUI2>(true))
        {
            Destroy(component);
        }

        Button resumeButton = null;

        foreach (Button button in overlay.GetComponentsInChildren<Button>(true))
        {
            button.onClick = new Button.ButtonClickedEvent();
            TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>();
            if (label == null) continue;

            string text = label.text ?? string.Empty;
            if (text.IndexOf("ready", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                label.text = "Resume";
                resumeButton = button;
            }
            else if (text.IndexOf("next", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                     text.IndexOf("continue", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                label.text = "Resume";
                resumeButton = button;
            }
            else if (text.IndexOf("back", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                label.text = "Close";
                resumeButton ??= button;
            }
        }

        if (resumeButton == null)
        {
            resumeButton = overlay.GetComponentsInChildren<Button>(true).FirstOrDefault();
        }

        if (resumeButton != null)
        {
            resumeButton.onClick.AddListener(() => ToggleInfoPanelInternal(false));
            uiCanvas.infoCloseButton = resumeButton;
        }
    }

    private static Sprite cachedDefaultSprite;

    private static Sprite GetDefaultSprite()
    {
        if (cachedDefaultSprite == null)
        {
            cachedDefaultSprite = Resources.GetBuiltinResource<Sprite>("UISprite.psd");
        }

        return cachedDefaultSprite;
    }

    private static void ApplyDefaultFont(TextMeshProUGUI text)
    {
        if (text == null) return;
        if (TMP_Settings.defaultFontAsset != null)
        {
            text.font = TMP_Settings.defaultFontAsset;
        }
    }

    private void OnInfoButtonClicked()
    {
        if (infoPanelVisible)
        {
            ToggleInfoPanelInternal(false);
        }
        else
        {
            StartCoroutine(ShowInfoPageRoutine());
        }
    }

    private void ToggleInfoPanelInternal(bool show)
    {
        infoPanelVisible = show;

        if (infoPageInstance != null)
        {
            infoPageInstance.SetActive(show);
        }

        uiCanvas?.ShowLevelInfo(show);

        if (show)
        {
            cachedTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            infoPauseActive = true;
        }
        else if (infoPauseActive)
        {
            infoPauseActive = false;
            if (isGameActive)
            {
                Time.timeScale = cachedTimeScale > 0f ? cachedTimeScale : 1f;
            }
        }
    }

    private void HideInfoPanelImmediate()
    {
        if (infoPauseActive)
        {
            infoPauseActive = false;
            if (isGameActive)
            {
                Time.timeScale = cachedTimeScale > 0f ? cachedTimeScale : 1f;
            }
        }

        infoPanelVisible = false;
        if (infoPageInstance != null)
        {
            infoPageInstance.SetActive(false);
        }
        uiCanvas?.ShowLevelInfo(false);
    }

    private void HighlightRemainingIngredients()
    {
        IngredientPickup[] pickups = FindObjectsOfType<IngredientPickup>();
        foreach (var pickup in pickups)
        {
            if (pickup != null && pickup.gameObject.activeInHierarchy)
            {
                pickup.EnableHighlight();
            }
        }
    }

    private bool ShouldHighlightIngredients()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        if (string.IsNullOrEmpty(sceneName)) return false;

        if (sceneName.IndexOf("Instruction", System.StringComparison.OrdinalIgnoreCase) >= 0) return false;
        if (sceneName.IndexOf("Tutorial", System.StringComparison.OrdinalIgnoreCase) >= 0) return false;
        if (sceneName.IndexOf("Demo", System.StringComparison.OrdinalIgnoreCase) >= 0) return false;

        return true;
    }

    private string ResolveInfoSceneName()
    {
        string levelScene = SceneManager.GetActiveScene().name;
        if (string.IsNullOrEmpty(levelScene))
        {
            return "InstructionScene";
        }

        if (levelScene.IndexOf("SampleScene", System.StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return "InstructionsScene2";
        }

        if (levelScene.IndexOf("Level1", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
            levelScene.IndexOf("Tutorial", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
            levelScene.IndexOf("Demo", System.StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return "InstructionScene";
        }

        return "InstructionScene";
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
