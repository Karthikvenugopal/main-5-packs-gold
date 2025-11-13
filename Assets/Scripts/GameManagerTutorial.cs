using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Tutorial-specific GameManager. Similar to GameManager but designed for tutorial customization.
/// Can be extended with dynamic tutorial instructions and tutorial-specific behavior.
/// </summary>
public class GameManagerTutorial : MonoBehaviour
{
    // Event fired when any player enters the exit
    public static event System.Action<CoopPlayerController> OnPlayerEnteredExitEvent;
    [SerializeField] private Color messageBackground = new Color(0f, 0f, 0f, 0.55f);
    [Header("UI Messages")]
    [SerializeField] private string tutorialStartMessage = "Welcome! Follow Instructions on the screen to complete the tutorial.";
    [SerializeField] private string levelVictoryMessage = "Tutorial Complete! Press R to play again or continue to Level 1.";
    [SerializeField] private string waitForPartnerMessage = "{0} made it. Wait for your partner!";
    [SerializeField] private string exitReminderMessage = "Both heroes must stand in the exit to finish.";
    [Header("Player Hearts")]
    [SerializeField] private int startingHearts = 3;
    [Header("UI Sprites")]
    [Tooltip("Sprite for Ember's full heart (Red)")]
    [SerializeField] private Sprite emberHeartFullSprite;
    [Tooltip("Sprite for Ember's empty heart (Red)")]
    [SerializeField] private Sprite emberHeartEmptySprite;
    [Tooltip("Sprite for Aqua's full heart (Blue)")]
    [SerializeField] private Sprite aquaHeartFullSprite;
    [Tooltip("Sprite for Aqua's empty heart (Blue)")]
    [SerializeField] private Sprite aquaHeartEmptySprite;
    [Header("UI Icon Sizes")]
    [Tooltip("The size (Width, Height) for the heart icons.")]
    [SerializeField] private Vector2 heartIconSize = new Vector2(50f, 50f);
    [Header("UI Layout")]
    [Tooltip("The height (in reference pixels) of the top UI bar")]
    [SerializeField] private float topUiBarHeight = 160f;
    [Tooltip("The background color of the top UI bar")]
    [SerializeField] private Color topUiBarColor = new Color(0.1f, 0.1f, 0.1f, 0.8f);
    [Header("Progression")]
    [SerializeField] private string nextSceneName = "Level1Scene";
    [SerializeField] private float nextSceneDelaySeconds = 2f;
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    [Header("Victory Panel")]
    [SerializeField] private bool useVictoryPanel = true;
    [SerializeField] private string victoryTitleText = "Tutorial Complete";
    [SerializeField] private string victoryBodyText = "Ready for the real challenge?";
    [SerializeField] private string defeatTitleText = "Out of Hearts";
    [SerializeField] private string defeatBodyText = "Don't worry! Try again.";
    [SerializeField] private string nextLevelButtonText = "Continue to Level 1";
    [SerializeField] private string restartButtonText = "Restart Tutorial";
    [SerializeField] private string mainMenuButtonText = "Main Menu";
    [SerializeField] private string levelDefeatMessage = "Out of hearts! Try again?";

    // Keeps a visible record of how many fire tokens the team has picked up.
    public int fireTokensCollected = 0;

    // Keeps a visible record of how many water tokens the team has picked up.
    public int waterTokensCollected = 0;

    private static int s_totalFireTokensCollected;
    private static int s_totalWaterTokensCollected;

    private Canvas _hudCanvas;
    private RectTransform _topUiBar;
    private TextMeshProUGUI _tokensLabel;
    private List<Image> _emberHeartImages = new List<Image>();
    private List<Image> _aquaHeartImages = new List<Image>();
    private int _fireHearts;
    private int _waterHearts;
    private bool _reloadingScene;
    private int _totalFireTokens;
    private int _totalWaterTokens;
    private GameObject _victoryPanel;
    private TextMeshProUGUI _victoryTitleLabel;
    private TextMeshProUGUI _victoryBodyLabel;
    private TextMeshProUGUI _fireVictoryLabel;
    private TextMeshProUGUI _waterVictoryLabel;
    private GameObject _fireSummaryRoot;
    private GameObject _waterSummaryRoot;
    private Button _victoryRestartButton;
    private Button _victoryMainMenuButton;
    private Button _victoryNextLevelButton;

    private readonly List<CoopPlayerController> _players = new();
    private readonly HashSet<CoopPlayerController> _playersAtExit = new();

    // analytics code
    [Header("Analytics")]
    [SerializeField] private Analytics.LevelTimer levelTimer;

    private TextMeshProUGUI _statusLabel;
    private GameObject _statusBackground;
    private bool _levelReady;
    private bool _gameActive;
    private bool _gameFinished;
    private Coroutine _loadNextSceneRoutine;

    private void Awake()
    {
        NormalizeDisplayStrings();
        if (!useVictoryPanel)
        {
            Debug.LogWarning("Victory panel was disabled; enabling it so end-of-level choices appear.", this);
            useVictoryPanel = true;
        }
        EnsureHudCanvas();
        CreateHeartsUI();
        // CreateTokensUI(); // Removed: Tutorial scene doesn't have tokens
        CreateVictoryPanel();
        CreateStatusUI();
        ResetTokenTracking();
        ResetHearts();
        EnsureLevelTimer();
    }

    private void Update()
    {
        if (_gameFinished && Input.GetKeyDown(KeyCode.R))
        {
            CancelNextSceneLoad();
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        if (_gameActive && !_gameFinished && AreAllPlayersAtExit())
        {
            HandleVictory();
        }
    }

    public void RegisterPlayer(CoopPlayerController player)
    {
        if (player == null || _players.Contains(player)) return;

        _players.Add(player);
        player.SetMovementEnabled(false);

        if (_levelReady && _players.Count >= 2)
        {
            TryStartLevel();
        }
    }

    public void OnLevelReady()
    {
        _levelReady = true;
        ResetTokenTracking();
        RecountTokensInScene();
        if (_players.Count >= 2)
        {
            TryStartLevel();
        }
    }

    private void TryStartLevel()
    {
        if (_gameActive || _players.Count < 2) return;

        _gameActive = true;
        _gameFinished = false;
        _playersAtExit.Clear();

        foreach (var player in _players)
        {
            player.SetMovementEnabled(true);
        }

        CancelNextSceneLoad();
        UpdateStatus(tutorialStartMessage);
    }

    public void OnPlayersTouched(CoopPlayerController playerA, CoopPlayerController playerB)
    {
        if (!_gameActive || _gameFinished) return;

        bool playerAAtExit = playerA != null && _playersAtExit.Contains(playerA);
        bool playerBAtExit = playerB != null && _playersAtExit.Contains(playerB);
        if (playerAAtExit || playerBAtExit)
        {
            return;
        }

        DamageBothPlayers(playerA, playerB);
        if (_gameFinished) return;
        UpdateStatus("Careful! Keep your distance.");
    }

    public void OnPlayerEnteredExit(CoopPlayerController player)
    {
        if (player == null) return;

        _playersAtExit.Add(player);

        // Fire event for other scripts to listen to
        OnPlayerEnteredExitEvent?.Invoke(player);

        if (!_gameActive || _gameFinished) return;

        if (AreAllPlayersAtExit())
        {
            HandleVictory();
        }
        else
        {
            UpdateStatus(string.Format(waitForPartnerMessage, GetDisplayName(player.Role)));
        }
    }

    public void OnPlayerExitedExit(CoopPlayerController player)
    {
        if (player == null) return;

        if (_playersAtExit.Remove(player) && _gameActive && !_gameFinished)
        {
            UpdateStatus(exitReminderMessage);
        }
    }

    public void OnPlayerHitByEnemy(CoopPlayerController player)
    {
        if (player == null) return;
        DamagePlayer(player.Role, 1);
    }

    public void OnFireTokenCollected()
    {
        fireTokensCollected++;
        s_totalFireTokensCollected++;
        if (_totalFireTokens < fireTokensCollected)
        {
            _totalFireTokens = fireTokensCollected;
        }

        UpdateTokensUI();
    }

    public void OnWaterTokenCollected()
    {
        waterTokensCollected++;
        s_totalWaterTokensCollected++;
        if (_totalWaterTokens < waterTokensCollected)
        {
            _totalWaterTokens = waterTokensCollected;
        }

        UpdateTokensUI();
    }

    public void OnExitReached()
    {
        HandleVictory();
    }

    private void FreezePlayers()
    {
        foreach (var player in _players)
        {
            player.SetMovementEnabled(false);
        }
    }

    /// <summary>
    /// Updates the status message. Can be overridden or extended for tutorial-specific instructions.
    /// </summary>
    protected virtual void UpdateStatus(string message)
    {
        if (_statusLabel != null)
        {
            _statusLabel.text = message;
        }

        Debug.Log(message);
    }

    private void CreateStatusUI()
    {
        if (_hudCanvas == null) return;

        GameObject background = new GameObject("MessageBackground");
        // Parent to the Top UI Bar instead of canvas
        background.transform.SetParent(_topUiBar, false);

        RectTransform bgRect = background.AddComponent<RectTransform>();
        // Anchor to the top-center of the bar
        bgRect.anchorMin = new Vector2(0.5f, 1f);
        bgRect.anchorMax = new Vector2(0.5f, 1f);
        bgRect.pivot = new Vector2(0.5f, 1f);
        bgRect.sizeDelta = new Vector2(680f, 120f);
        // Position it 40 pixels down from the top-center of the bar
        bgRect.anchoredPosition = new Vector2(0f, -40f);

        Image image = background.AddComponent<Image>();
        image.color = messageBackground;

        // Store reference to background for hiding later
        _statusBackground = background;

        GameObject textGO = new GameObject("StatusLabel");
        textGO.transform.SetParent(background.transform, false);

        RectTransform textRect = textGO.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(20f, 20f);
        textRect.offsetMax = new Vector2(-20f, -20f);

        _statusLabel = textGO.AddComponent<TextMeshProUGUI>();
        _statusLabel.alignment = TextAlignmentOptions.Center;
        _statusLabel.fontSize = 32f;
        _statusLabel.text = string.Empty;
    }

    private void EnsureHudCanvas()
    {
        if (_hudCanvas != null) return;

        GameObject canvasGO = new GameObject("MazeUI");
        canvasGO.transform.SetParent(transform, false);

        _hudCanvas = canvasGO.AddComponent<Canvas>();
        _hudCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _hudCanvas.sortingOrder = 200;

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();
        
        // Ensure EventSystem exists for UI interactions
        EnsureEventSystem();
        
        // Create the Top UI Bar
        GameObject topBarGO = new GameObject("TopUI_Bar_Background");
        topBarGO.transform.SetParent(canvasGO.transform, false);
        Image topBarImg = topBarGO.AddComponent<Image>();
        topBarImg.color = topUiBarColor;

        _topUiBar = topBarGO.GetComponent<RectTransform>();
        
        // Anchor it to the top edge and stretch 100% wide
        _topUiBar.anchorMin = new Vector2(0f, 1f); 
        _topUiBar.anchorMax = new Vector2(1f, 1f); 
        _topUiBar.pivot = new Vector2(0.5f, 1f);
        
        // Set its height using the variable
        _topUiBar.sizeDelta = new Vector2(0f, topUiBarHeight);
        _topUiBar.anchoredPosition = Vector2.zero;
    }

    private void EnsureEventSystem()
    {
        // Check if EventSystem already exists in the scene
        if (FindFirstObjectByType<EventSystem>() != null) return;

        // Create EventSystem if it doesn't exist
        GameObject eventSystemGO = new GameObject("EventSystem");
        eventSystemGO.AddComponent<EventSystem>();
        eventSystemGO.AddComponent<StandaloneInputModule>();
    }

    private void CreateHeartsUI()
    {
        if (_hudCanvas == null) return;
        
        // Create the Master Container for all "Life" UI
        GameObject heartsMasterContainer = new GameObject("HeartsMasterContainer");
        
        // Parent to the Top UI Bar
        heartsMasterContainer.transform.SetParent(_topUiBar, false);

        VerticalLayoutGroup masterLayout = heartsMasterContainer.AddComponent<VerticalLayoutGroup>();
        masterLayout.spacing = 10;
        masterLayout.childAlignment = TextAnchor.UpperRight;
        masterLayout.childControlWidth = false;
        masterLayout.childControlHeight = false;
        
        ContentSizeFitter masterFitter = heartsMasterContainer.AddComponent<ContentSizeFitter>();
        masterFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        RectTransform masterRect = heartsMasterContainer.GetComponent<RectTransform>();
        
        // Anchor to the top-right of the bar
        masterRect.anchorMin = new Vector2(1f, 1f); 
        masterRect.anchorMax = new Vector2(1f, 1f);
        masterRect.pivot = new Vector2(1f, 1f);
        // Position it 40px in from the top-right corner of the bar
        masterRect.anchoredPosition = new Vector2(-40f, -40f); 
        masterRect.sizeDelta = new Vector2(360f, 200f); 

        // Create the "Life" Title
        GameObject titleLabelGO = new GameObject("TitleLabel");
        titleLabelGO.transform.SetParent(heartsMasterContainer.transform, false);
        TextMeshProUGUI titleLabel = titleLabelGO.AddComponent<TextMeshProUGUI>();
        titleLabel.text = "Life";
        titleLabel.fontSize = 42f;
        titleLabel.fontStyle = FontStyles.Bold;
        titleLabel.color = Color.white;
        titleLabel.alignment = TextAlignmentOptions.Right;

        LayoutElement titleLayout = titleLabelGO.AddComponent<LayoutElement>();
        titleLayout.preferredWidth = 360f; 
        
        // Create Hearts Container for Ember
        GameObject emberHeartsGO = new GameObject("EmberHeartsContainer");
        emberHeartsGO.transform.SetParent(heartsMasterContainer.transform, false); 
        
        Image emberBg = emberHeartsGO.AddComponent<Image>();
        emberBg.color = new Color(0f, 0f, 0f, 0.35f); 
        
        HorizontalLayoutGroup emberLayout = emberHeartsGO.AddComponent<HorizontalLayoutGroup>();
        emberLayout.spacing = 10; 
        emberLayout.childAlignment = TextAnchor.MiddleRight;
        emberLayout.childControlWidth = false;
        emberLayout.childControlHeight = false;
        emberLayout.padding = new RectOffset(10, 10, 5, 5); 

        RectTransform emberRect = emberHeartsGO.GetComponent<RectTransform>();
        float heartContainerHeight = heartIconSize.y + emberLayout.padding.top + emberLayout.padding.bottom;
        emberRect.sizeDelta = new Vector2(360f, heartContainerHeight);

        GameObject emberLabelGO = new GameObject("Label");
        emberLabelGO.transform.SetParent(emberHeartsGO.transform, false); 
        TextMeshProUGUI emberLabel = emberLabelGO.AddComponent<TextMeshProUGUI>();
        emberLabel.text = "Ember:";
        emberLabel.fontSize = 40f;
        emberLabel.color = Color.white;
        emberLabel.alignment = TextAlignmentOptions.Right;
        LayoutElement emberLabelLayout = emberLabelGO.AddComponent<LayoutElement>();
        emberLabelLayout.preferredWidth = 160f; 

        _emberHeartImages.Clear();
        for (int i = 0; i < startingHearts; i++)
        {
            GameObject heartImgGO = new GameObject($"Heart_{i}");
            heartImgGO.transform.SetParent(emberHeartsGO.transform, false);
            Image heartImg = heartImgGO.AddComponent<Image>();
            heartImg.sprite = emberHeartFullSprite; 
            
            RectTransform heartRect = heartImgGO.GetComponent<RectTransform>();
            heartRect.sizeDelta = heartIconSize; 
            heartImgGO.AddComponent<LayoutElement>().preferredWidth = heartIconSize.x;
            
            _emberHeartImages.Add(heartImg);
        }

        // Create Hearts Container for Aqua
        GameObject aquaHeartsGO = new GameObject("AquaHeartsContainer");
        aquaHeartsGO.transform.SetParent(heartsMasterContainer.transform, false); 

        Image aquaBg = aquaHeartsGO.AddComponent<Image>();
        aquaBg.color = new Color(0f, 0f, 0f, 0.35f); 
        
        HorizontalLayoutGroup aquaLayout = aquaHeartsGO.AddComponent<HorizontalLayoutGroup>();
        aquaLayout.spacing = 10;
        aquaLayout.childAlignment = TextAnchor.MiddleRight;
        aquaLayout.childControlWidth = false;
        aquaLayout.childControlHeight = false;
        aquaLayout.padding = new RectOffset(10, 10, 5, 5); 

        RectTransform aquaRect = aquaHeartsGO.GetComponent<RectTransform>();
        aquaRect.sizeDelta = new Vector2(360f, heartContainerHeight); 

        GameObject aquaLabelGO = new GameObject("Label");
        aquaLabelGO.transform.SetParent(aquaHeartsGO.transform, false); 
        TextMeshProUGUI aquaLabel = aquaLabelGO.AddComponent<TextMeshProUGUI>();
        aquaLabel.text = "Aqua:";
        aquaLabel.fontSize = 40f;
        aquaLabel.color = Color.white;
        aquaLabel.alignment = TextAlignmentOptions.Right;
        LayoutElement aquaLabelLayout = aquaLabelGO.AddComponent<LayoutElement>();
        aquaLabelLayout.preferredWidth = 160f; 

        _aquaHeartImages.Clear();
        for (int i = 0; i < startingHearts; i++)
        {
            GameObject heartImgGO = new GameObject($"Heart_{i}");
            heartImgGO.transform.SetParent(aquaHeartsGO.transform, false);
            Image heartImg = heartImgGO.AddComponent<Image>();
            heartImg.sprite = aquaHeartFullSprite; 
            
            RectTransform heartRect = heartImgGO.GetComponent<RectTransform>();
            heartRect.sizeDelta = heartIconSize; 
            heartImgGO.AddComponent<LayoutElement>().preferredWidth = heartIconSize.x;
            
            _aquaHeartImages.Add(heartImg);
        }
        
        UpdateHeartsUI();
    }

    private void CreateTokensUI()
    {
        if (_hudCanvas == null || _tokensLabel != null) return;

        GameObject tokensGO = new GameObject("TokensLabel");
        tokensGO.transform.SetParent(_hudCanvas.transform, false);

        RectTransform rect = tokensGO.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.sizeDelta = new Vector2(520f, 60f);
        rect.anchoredPosition = new Vector2(40f, -40f);

        _tokensLabel = tokensGO.AddComponent<TextMeshProUGUI>();
        _tokensLabel.alignment = TextAlignmentOptions.Left;
        _tokensLabel.fontSize = 32f;
        _tokensLabel.raycastTarget = false;
        UpdateTokensUI();
    }

    private void CreateVictoryPanel()
    {
        if (!useVictoryPanel || _hudCanvas == null || _victoryPanel != null) return;

        _victoryPanel = new GameObject("EndOfLevelPanel");
        _victoryPanel.transform.SetParent(_hudCanvas.transform, false);

        RectTransform rect = _victoryPanel.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(620f, 360f);
        rect.anchoredPosition = Vector2.zero;

        Image background = _victoryPanel.AddComponent<Image>();
        background.color = new Color(0f, 0f, 0f, 0.78f);

        GameObject titleGO = new GameObject("Title");
        titleGO.transform.SetParent(_victoryPanel.transform, false);
        RectTransform titleRect = titleGO.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 1f);
        titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.sizeDelta = new Vector2(560f, 70f);
        titleRect.anchoredPosition = new Vector2(0f, -28f);

        _victoryTitleLabel = titleGO.AddComponent<TextMeshProUGUI>();
        _victoryTitleLabel.alignment = TextAlignmentOptions.Center;
        _victoryTitleLabel.fontSize = 42f;
        _victoryTitleLabel.fontStyle = FontStyles.Bold;
        _victoryTitleLabel.text = victoryTitleText;

        GameObject bodyGO = new GameObject("Body");
        bodyGO.transform.SetParent(_victoryPanel.transform, false);
        RectTransform bodyRect = bodyGO.AddComponent<RectTransform>();
        bodyRect.anchorMin = new Vector2(0.5f, 1f);
        bodyRect.anchorMax = new Vector2(0.5f, 1f);
        bodyRect.pivot = new Vector2(0.5f, 1f);
        bodyRect.sizeDelta = new Vector2(560f, 60f);
        bodyRect.anchoredPosition = new Vector2(0f, -110f);

        _victoryBodyLabel = bodyGO.AddComponent<TextMeshProUGUI>();
        _victoryBodyLabel.alignment = TextAlignmentOptions.Center;
        _victoryBodyLabel.fontSize = 28f;
        _victoryBodyLabel.text = victoryBodyText;

        _fireSummaryRoot = new GameObject("FireSummary");
        _fireSummaryRoot.transform.SetParent(_victoryPanel.transform, false);
        RectTransform fireRect = _fireSummaryRoot.AddComponent<RectTransform>();
        fireRect.anchorMin = new Vector2(0.5f, 0.5f);
        fireRect.anchorMax = new Vector2(0.5f, 0.5f);
        fireRect.pivot = new Vector2(0.5f, 0.5f);
        fireRect.sizeDelta = new Vector2(560f, 50f);
        fireRect.anchoredPosition = new Vector2(0f, 48f);

        _fireVictoryLabel = _fireSummaryRoot.AddComponent<TextMeshProUGUI>();
        _fireVictoryLabel.alignment = TextAlignmentOptions.Center;
        _fireVictoryLabel.fontSize = 30f;
        _fireVictoryLabel.text = string.Empty;

        _waterSummaryRoot = new GameObject("WaterSummary");
        _waterSummaryRoot.transform.SetParent(_victoryPanel.transform, false);
        RectTransform waterRect = _waterSummaryRoot.AddComponent<RectTransform>();
        waterRect.anchorMin = new Vector2(0.5f, 0.5f);
        waterRect.anchorMax = new Vector2(0.5f, 0.5f);
        waterRect.pivot = new Vector2(0.5f, 0.5f);
        waterRect.sizeDelta = new Vector2(560f, 50f);
        waterRect.anchoredPosition = new Vector2(0f, 0f);

        _waterVictoryLabel = _waterSummaryRoot.AddComponent<TextMeshProUGUI>();
        _waterVictoryLabel.alignment = TextAlignmentOptions.Center;
        _waterVictoryLabel.fontSize = 30f;
        _waterVictoryLabel.text = string.Empty;

        GameObject buttonRow = new GameObject("Buttons");
        buttonRow.transform.SetParent(_victoryPanel.transform, false);
        RectTransform buttonRowRect = buttonRow.AddComponent<RectTransform>();
        buttonRowRect.anchorMin = new Vector2(0.5f, 0f);
        buttonRowRect.anchorMax = new Vector2(0.5f, 0f);
        buttonRowRect.pivot = new Vector2(0.5f, 0f);
        buttonRowRect.sizeDelta = new Vector2(560f, 90f);
        buttonRowRect.anchoredPosition = new Vector2(0f, 32f);

        HorizontalLayoutGroup layoutGroup = buttonRow.AddComponent<HorizontalLayoutGroup>();
        layoutGroup.childAlignment = TextAnchor.MiddleCenter;
        layoutGroup.spacing = 24f;
        layoutGroup.padding = new RectOffset(10, 10, 10, 10);
        layoutGroup.childForceExpandHeight = false;
        layoutGroup.childForceExpandWidth = false;

        _victoryNextLevelButton = CreateEndPanelButton("NextLevelButton", buttonRow.transform, nextLevelButtonText);
        if (_victoryNextLevelButton != null)
        {
            _victoryNextLevelButton.onClick.AddListener(OnVictoryNextLevelClicked);
        }

        _victoryRestartButton = CreateEndPanelButton("RestartButton", buttonRow.transform, restartButtonText);
        if (_victoryRestartButton != null)
        {
            _victoryRestartButton.onClick.AddListener(OnVictoryRestartClicked);
        }

        _victoryMainMenuButton = CreateEndPanelButton("MainMenuButton", buttonRow.transform, mainMenuButtonText);
        if (_victoryMainMenuButton != null)
        {
            _victoryMainMenuButton.onClick.AddListener(OnVictoryMainMenuClicked);
        }

        _victoryPanel.SetActive(false);
    }

    private Button CreateEndPanelButton(string name, Transform parent, string labelText)
    {
        GameObject buttonGO = new GameObject(name);
        buttonGO.transform.SetParent(parent, false);

        RectTransform rect = buttonGO.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(220f, 60f);

        Image background = buttonGO.AddComponent<Image>();
        background.color = new Color(0.25f, 0.45f, 0.9f, 1f);

        Button button = buttonGO.AddComponent<Button>();

        LayoutElement layout = buttonGO.AddComponent<LayoutElement>();
        layout.preferredWidth = 220f;
        layout.preferredHeight = 60f;
        layout.flexibleWidth = 0f;
        layout.flexibleHeight = 0f;

        GameObject labelGO = new GameObject("Label");
        labelGO.transform.SetParent(buttonGO.transform, false);
        RectTransform labelRect = labelGO.AddComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        TextMeshProUGUI label = labelGO.AddComponent<TextMeshProUGUI>();
        label.alignment = TextAlignmentOptions.Center;
        label.fontSize = 28f;
        label.text = labelText;
        label.raycastTarget = false; // Prevent text from blocking button clicks

        return button;
    }

    private void SetButtonLabel(Button button, string text)
    {
        if (button == null) return;

        TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>();
        if (label == null) return;

        label.text = text;
    }

    private bool TryGetNextSceneName(out string sceneName)
    {
        if (!string.IsNullOrEmpty(nextSceneName))
        {
            sceneName = nextSceneName;
            return true;
        }

        Scene activeScene = SceneManager.GetActiveScene();
        int nextIndex = activeScene.buildIndex + 1;

        if (nextIndex >= SceneManager.sceneCountInBuildSettings)
        {
            sceneName = null;
            return false;
        }

        string path = SceneUtility.GetScenePathByBuildIndex(nextIndex);
        if (string.IsNullOrEmpty(path))
        {
            sceneName = null;
            return false;
        }

        sceneName = Path.GetFileNameWithoutExtension(path);
        return !string.IsNullOrEmpty(sceneName);
    }

    private void ShowEndPanel(EndGameState state)
    {
        if (!useVictoryPanel || _victoryPanel == null) return;

        bool isVictory = state == EndGameState.Victory;

        if (_victoryTitleLabel != null)
        {
            _victoryTitleLabel.text = isVictory ? victoryTitleText : defeatTitleText;
        }

        if (_victoryBodyLabel != null)
        {
            _victoryBodyLabel.text = isVictory ? victoryBodyText : defeatBodyText;
        }

        // Hide token summary displays
        if (_fireSummaryRoot != null)
        {
            _fireSummaryRoot.SetActive(false);
        }

        if (_waterSummaryRoot != null)
        {
            _waterSummaryRoot.SetActive(false);
        }

        SetButtonLabel(_victoryNextLevelButton, nextLevelButtonText);
        SetButtonLabel(_victoryRestartButton, restartButtonText);
        SetButtonLabel(_victoryMainMenuButton, mainMenuButtonText);

        // For tutorial, always allow advancing to Level 1 on victory
        bool canAdvance = isVictory;

        if (_victoryNextLevelButton != null)
        {
            _victoryNextLevelButton.gameObject.SetActive(canAdvance);
        }

        if (_victoryRestartButton != null)
        {
            bool showRestart = !isVictory || !canAdvance;
            _victoryRestartButton.gameObject.SetActive(showRestart);
        }

        if (_victoryMainMenuButton != null)
        {
            bool showMainMenu = !string.IsNullOrEmpty(mainMenuSceneName);
            _victoryMainMenuButton.gameObject.SetActive(showMainMenu);
        }

        _victoryPanel.SetActive(true);
    }

    private enum EndGameState
    {
        Victory,
        Defeat
    }

    private void ResetHearts()
    {
        int clampedHearts = Mathf.Max(0, startingHearts);
        _fireHearts = clampedHearts;
        _waterHearts = clampedHearts;
        UpdateHeartsUI();
    }

    private void ResetTokenTracking()
    {
        fireTokensCollected = 0;
        waterTokensCollected = 0;
        _totalFireTokens = 0;
        _totalWaterTokens = 0;
        UpdateTokensUI();
    }

    private void UpdateHeartsUI()
    {
        // Loop through all of Ember's heart images
        for (int i = 0; i < _emberHeartImages.Count; i++)
        {
            var heartImage = _emberHeartImages[i];
            if (heartImage == null)
            {
                continue;
            }

            heartImage.gameObject.SetActive(true);

            if (i < _fireHearts)
            {
                heartImage.sprite = emberHeartFullSprite;
                heartImage.enabled = true;
            }
            else if (emberHeartEmptySprite != null)
            {
                heartImage.sprite = emberHeartEmptySprite;
                heartImage.enabled = true;
            }
            else
            {
                heartImage.enabled = false;
            }
        }

        // Loop through all of Aqua's heart images
        for (int i = 0; i < _aquaHeartImages.Count; i++)
        {
            var heartImage = _aquaHeartImages[i];
            if (heartImage == null)
            {
                continue;
            }

            heartImage.gameObject.SetActive(true);

            if (i < _waterHearts)
            {
                heartImage.sprite = aquaHeartFullSprite;
                heartImage.enabled = true;
            }
            else if (aquaHeartEmptySprite != null)
            {
                heartImage.sprite = aquaHeartEmptySprite;
                heartImage.enabled = true;
            }
            else
            {
                heartImage.enabled = false;
            }
        }
    }

    private void UpdateTokensUI()
    {
        if (_tokensLabel == null) return;

        int fireTotal = Mathf.Max(_totalFireTokens, fireTokensCollected);
        int waterTotal = Mathf.Max(_totalWaterTokens, waterTokensCollected);
        _tokensLabel.text = $"Ember Tokens: {fireTokensCollected}/{fireTotal};  Aqua Tokens: {waterTokensCollected}/{waterTotal}";
    }

    private void RecountTokensInScene()
    {
        int fireCount = 0;
        int waterCount = 0;

        TokenCollect[] tokens = FindObjectsOfType<TokenCollect>(includeInactive: true);
        foreach (var token in tokens)
        {
            if (token == null) continue;

            if (token.CompareTag("FireToken"))
            {
                fireCount++;
            }
            else if (token.CompareTag("WaterToken"))
            {
                waterCount++;
            }
        }

        _totalFireTokens = fireCount;
        _totalWaterTokens = waterCount;
        UpdateTokensUI();
    }

    private void DamageBothPlayers(CoopPlayerController playerA, CoopPlayerController playerB)
    {
        if (!_gameActive || _gameFinished) return;
        if (playerA == null && playerB == null) return;

        if (playerA != null)
        {
            ApplyDamage(playerA.Role, 1, suppressCheck: true);
        }

        if (playerB != null)
        {
            ApplyDamage(playerB.Role, 1, suppressCheck: true);
        }

        CheckForHeartDepletion();
    }

    public void DamagePlayer(PlayerRole role, int amount)
    {
        if (amount <= 0 || !_gameActive || _gameFinished) return;
        ApplyDamage(role, amount, suppressCheck: false);
    }

    private void ApplyDamage(PlayerRole role, int amount, bool suppressCheck)
    {
        if (amount <= 0) return;

        switch (role)
        {
            case PlayerRole.Fireboy:
                _fireHearts = Mathf.Max(0, _fireHearts - amount);
                break;
            case PlayerRole.Watergirl:
                _waterHearts = Mathf.Max(0, _waterHearts - amount);
                break;
        }

        UpdateHeartsUI();

        if (!suppressCheck)
        {
            CheckForHeartDepletion();
        }
    }

    private void CheckForHeartDepletion()
    {
        if (_fireHearts > 0 && _waterHearts > 0) return;
        HandleOutOfHearts();
    }

    private void HandleOutOfHearts()
    {
        if (_gameFinished) return;

        _gameFinished = true;
        _gameActive = false;
        FreezePlayers();
        CancelNextSceneLoad();
        UpdateStatus(levelDefeatMessage);
        ShowEndPanel(EndGameState.Defeat);
    }

    private void OnVictoryRestartClicked()
    {
        if (_reloadingScene) return;

        _reloadingScene = true;
        CancelNextSceneLoad();

        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void OnVictoryNextLevelClicked()
    {
        if (_reloadingScene) return;

        _reloadingScene = true;
        CancelNextSceneLoad();
        SceneManager.LoadScene("Level1Scene");
    }

    private void OnVictoryMainMenuClicked()
    {
        if (_reloadingScene || string.IsNullOrEmpty(mainMenuSceneName)) return;

        _reloadingScene = true;
        CancelNextSceneLoad();
        SceneManager.LoadScene(mainMenuSceneName);
    }

    private void HandleVictory()
    {
        if (_gameFinished) return;

        _gameFinished = true;
        _gameActive = false;
        // analytics code
        EnsureLevelTimer();
        (levelTimer ?? FindAnyObjectByType<Analytics.LevelTimer>())?.MarkSuccess();
        
        // Hide the status UI instead of showing victory message
        if (_statusBackground != null)
        {
            _statusBackground.SetActive(false);
        }
        
        FreezePlayers();
        CancelNextSceneLoad();
        ShowEndPanel(EndGameState.Victory);
    }

    // analytics code
    private void EnsureLevelTimer()
    {
        if (levelTimer != null) return;

        var active = SceneManager.GetActiveScene().name;
        if (string.Equals(active, "MainMenu", System.StringComparison.OrdinalIgnoreCase)) return;

        var existing = FindAnyObjectByType<Analytics.LevelTimer>();
        if (existing != null)
        {
            levelTimer = existing;
            levelTimer.autoSendFailureOnDestroy = true;
            return;
        }

        var go = new GameObject("LevelAnalytics");
        levelTimer = go.AddComponent<Analytics.LevelTimer>();
        levelTimer.autoSendFailureOnDestroy = true;
    }

    private IEnumerator LoadNextSceneAfterDelay()
    {
        if (!TryGetNextSceneName(out string sceneToLoad))
        {
            yield break;
        }

        if (nextSceneDelaySeconds > 0f)
        {
            yield return new WaitForSeconds(nextSceneDelaySeconds);
        }

        _loadNextSceneRoutine = null;
        SceneManager.LoadScene(sceneToLoad);
    }

    private void CancelNextSceneLoad()
    {
        if (_loadNextSceneRoutine == null) return;

        StopCoroutine(_loadNextSceneRoutine);
        _loadNextSceneRoutine = null;
    }

    private void OnDestroy()
    {
        if (_victoryRestartButton != null)
        {
            _victoryRestartButton.onClick.RemoveListener(OnVictoryRestartClicked);
        }

        if (_victoryNextLevelButton != null)
        {
            _victoryNextLevelButton.onClick.RemoveListener(OnVictoryNextLevelClicked);
        }

        if (_victoryMainMenuButton != null)
        {
            _victoryMainMenuButton.onClick.RemoveListener(OnVictoryMainMenuClicked);
        }
    }

    private bool AreAllPlayersAtExit()
    {
        if (_players.Count == 0 || _playersAtExit.Count == 0) return false;

        foreach (var player in _players)
        {
            if (player == null || !_playersAtExit.Contains(player))
            {
                return false;
            }
        }

        return true;
    }

    private static string GetDisplayName(PlayerRole role)
    {
        return role == PlayerRole.Fireboy ? "Ember" : "Aqua";
    }

    private void NormalizeDisplayStrings()
    {
        tutorialStartMessage = ReplaceLegacyNames(tutorialStartMessage);
        levelVictoryMessage = ReplaceLegacyNames(levelVictoryMessage);
        levelDefeatMessage = ReplaceLegacyNames(levelDefeatMessage);
        waitForPartnerMessage = ReplaceLegacyNames(waitForPartnerMessage);
        exitReminderMessage = ReplaceLegacyNames(exitReminderMessage);
        victoryTitleText = ReplaceLegacyNames(victoryTitleText);
        defeatTitleText = ReplaceLegacyNames(defeatTitleText);
        victoryBodyText = ReplaceLegacyNames(victoryBodyText);
        defeatBodyText = ReplaceLegacyNames(defeatBodyText);
        nextLevelButtonText = ReplaceLegacyNames(nextLevelButtonText);
        restartButtonText = ReplaceLegacyNames(restartButtonText);
        mainMenuButtonText = ReplaceLegacyNames(mainMenuButtonText);
    }

    private static string ReplaceLegacyNames(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;
        return text.Replace("Fireboy", "Ember").Replace("Watergirl", "Aqua");
    }
}


