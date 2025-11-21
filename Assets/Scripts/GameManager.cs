using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro; // Still needed for other UI elements
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public enum DamageCause
    {
        Unknown = 0,
        PlayerTouch = 1,
        FireWall = 2,
        IceWall = 3,
        ProjectileFire = 4,
        ProjectileIce = 5
    }
    // Assist metrics removed

    // --- MODIFICATION START ---
    // ... (isTutorialMode, messageBackground, levelIntroMessage, etc.) ...
    [Header("Tutorial Settings")]
    [SerializeField] private bool isTutorialMode = false;

    [SerializeField] private Color messageBackground = new Color(0f, 0f, 0f, 0.55f);
    [Header("UI Fonts")]
    [SerializeField] private TMP_FontAsset upperUiFont;
    [Header("UI Messages")]
    [SerializeField] private string levelIntroMessage = "";
    [SerializeField] private string levelStartMessage = "";
    [SerializeField] private string levelVictoryMessage = "Victory! Both heroes reached safety. Press R to play again.";
    [SerializeField] private string waitForPartnerMessage = "{0} made it. Wait for your partner!";
    [SerializeField] private string exitReminderMessage = "Both heroes must stand in the exit to finish.";
    [Header("Player Hearts")]
    [SerializeField] private int startingHearts = 3;
    [Header("Progression")]
    [SerializeField] private string nextSceneName;
    [SerializeField] private float nextSceneDelaySeconds = 2f;
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    [Header("Victory Panel")]
    [SerializeField] private bool useVictoryPanel = true;
    [SerializeField] private string victoryTitleText = "Level Complete";
    [SerializeField] private string victoryBodyText = "Choose where to go next.";
    [SerializeField] private string defeatTitleText = "Out of Hearts";
    [SerializeField] private string defeatBodyText = "You ran out of hearts. Try again?";
    [SerializeField] private string nextLevelButtonText = "Next Level";
    [SerializeField] private string restartButtonText = "Restart";
    [SerializeField] private string mainMenuButtonText = "Main Menu";
    [SerializeField] private string levelDefeatMessage = "Out of hearts! Choose an option.";
    [Tooltip("Sprite for the trophy icon shown on victory (between title and score)")]
    [SerializeField] private Sprite trophySprite;
    [Tooltip("Size of the trophy icon (Width, Height)")]
    [SerializeField] private Vector2 trophyIconSize = new Vector2(100f, 100f);
    [Header("Session Tracking")]
    [SerializeField] private bool resetGlobalTokenTotalsOnLoad = false;
    [Header("Scoring System")]
    [SerializeField] private bool enableScoring = true;
    [SerializeField] private int basePoints = 1000;
    [SerializeField] private int pointsPerToken = 100;
    [SerializeField] private float timeBonusMultiplier = 50f;
    [SerializeField] private float targetTimeSeconds = 60f; // Level 1: 1 minute
    [Tooltip("Target time for Level 2. Set to 0 to use targetTimeSeconds.")]
    [SerializeField] private float level2TargetTimeSeconds = 180f; // Level 2: 3 minutes
    [Tooltip("Target time for Level 3. Set to 0 to use targetTimeSeconds.")]
    [SerializeField] private float level3TargetTimeSeconds = 120f; // Level 3: 2 minutes
    [Header("Level Intro Instructions")]
    [SerializeField] private bool showInstructionPanel = true;
    [SerializeField] private string instructionPanelSceneName = "Level1Scene";
    [SerializeField] private string[] instructionLines = new[]
    {
        "<b>Level 1</b>",
        "",
        "Collect maximum number of tokens and exit",
        "",
        "Caution: Touch each other? lose a heart.",
        "Caution: Touch wrong obstacle? lose a heart.",
        "Work together but never collide!"
    };
    [SerializeField] private string instructionContinuePrompt = "Press Space to start";
    [SerializeField] private string level2InstructionSceneName = "Level2Scene";
    [SerializeField] private string[] level2InstructionLines = new[]
    {
        "<b>Level 2</b>",
        "",
        "Tip: Opposites protect. Shield your partner from danger."
    };
    [SerializeField] private string level2InstructionContinuePrompt = "Press Space to start";
    [SerializeField] private string level3InstructionSceneName = "Level3Scene";
    [SerializeField] private string[] level3InstructionLines = new[]
    {
        "<b>Level 3</b>",
        "",
        "Dynamic obstacles: Stay Sharp", 
        "Destroying one obstacle can lead to the creation of a new one",
        "",
        "Keep collecting tokens and avoid hazards!"
    };
    [SerializeField] private string level3InstructionContinuePrompt = "Press Space to start";
    
    [Header("UI Sprites")]
    [Tooltip("Sprite for Ember's full heart (Red)")]
    [SerializeField] private Sprite emberHeartFullSprite;
    [Tooltip("Sprite for Ember's empty heart (Red)")]
    [SerializeField] private Sprite emberHeartEmptySprite;
    [Tooltip("Sprite for Aqua's full heart (Blue)")]
    [SerializeField] private Sprite aquaHeartFullSprite;
    [Tooltip("Sprite for Aqua's empty heart (Blue)")]
    [SerializeField] private Sprite aquaHeartEmptySprite;
    
    [Tooltip("Sprite for a collected fire token")]
    [SerializeField] private Sprite fireTokenCollectedSprite;
    [Tooltip("Sprite for an empty fire token slot")]
    [SerializeField] private Sprite fireTokenEmptySprite;
    [Tooltip("Sprite for a collected water token")]
    [SerializeField] private Sprite waterTokenCollectedSprite;
    [Tooltip("Sprite for an empty water token slot")]
    [SerializeField] private Sprite waterTokenEmptySprite;
    
    [Header("UI Icon Sizes")]
    [Tooltip("The size (Width, Height) for the heart icons.")]
    [SerializeField] private Vector2 heartIconSize = new Vector2(50f, 50f);
    [Tooltip("The size (Width, Height) for the token icons.")]
    [SerializeField] private Vector2 tokenIconSize = new Vector2(45f, 45f);

    // --- MODIFICATION START ---
    // This new variable controls the height of the top UI bar.
    [Header("UI Layout")]
    [Tooltip("The height (in reference pixels) of the top UI bar")]
    [SerializeField] private float topUiBarHeight = 160f;
    [Tooltip("The maximum width (in reference pixels) of the top UI bar")]
    [SerializeField] private float topUiBarWidth = 1320f;
    [Tooltip("The background color of the top UI bar")]
    [SerializeField] private Color topUiBarColor = new Color(0f, 0f, 0f, 0f);
    // --- MODIFICATION END ---

    [Header("Audio")]
    [SerializeField] private AudioClip heartLossSfx;
    [SerializeField, Range(0f, 1f)] private float heartLossSfxVolume = 0.9f;


    // Keeps a visible record of how many fire tokens the team has picked up.
    public int fireTokensCollected = 0;
    // Keeps a visible record of how many water tokens the team has picked up.
    public int waterTokensCollected = 0;

    private static int s_totalFireTokensCollected;
    private static int s_totalWaterTokensCollected;

    private Canvas _hudCanvas;
    
    // --- MODIFICATION START ---
    // We add a reference for the top UI bar's RectTransform.
    private RectTransform _topUiBar;
    private RectTransform _topUiContentRoot;
    // --- MODIFICATION END ---
    private HeartLossAnimator _heartLossAnimator;
    
    private List<Image> _emberHeartImages = new List<Image>();
    private List<Image> _aquaHeartImages = new List<Image>();
    private List<Image> _emberTokenImages = new List<Image>();
    private List<Image> _aquaTokenImages = new List<Image>();
    private readonly Dictionary<Image, Vector3> _tokenIconBaseScales = new();
    private float _tokenBreathTimer;
    [Header("Token UI Breath")]
    [SerializeField] private bool enableTokenBreath = true;
    [SerializeField, Min(0.05f)] private float tokenBreathCycleSeconds = 1.6f;
    [SerializeField] private Vector2 tokenBreathScaleRange = new Vector2(0.85f, 1.1f);
    
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
    private VerticalLayoutGroup _victoryContentLayout;
    private Button _victoryRestartButton;
    private Button _victoryMainMenuButton;
    private Button _victoryNextLevelButton;
    private Image _victoryTrophyImage;

    private readonly List<CoopPlayerController> _players = new();
    private readonly HashSet<CoopPlayerController> _playersAtExit = new();

    // analytics code
    [Header("Analytics")]
    [SerializeField] private Analytics.LevelTimer levelTimer;

    private TextMeshProUGUI _statusLabel;
    private Image _statusBackgroundImage;
    private TMP_FontAsset _cachedUpperUiFont;
    private bool _levelReady;
    private bool _gameActive;
    private bool _gameFinished;
    private Coroutine _loadNextSceneRoutine;
    private GameObject _instructionPanel;
    private bool _waitingForInstructionAck;
    private bool _instructionPausedTime;
    private float _previousTimeScale = 1f;
    
    // Scoring system fields
    private bool _scoringTimerStarted;
    private float _scoringStartTime;
    
    // Audio fields
    private AudioClip _generatedHeartLossClip;

    public bool TryGetTokenCompletionSnapshot(out Analytics.TokenCompletionSnapshot snapshot)
    {
        int tokensAvailable = Mathf.Max(0, _totalFireTokens + _totalWaterTokens);
        int tokensCollected = Mathf.Max(0, fireTokensCollected + waterTokensCollected);
        if (tokensAvailable <= 0 && tokensCollected <= 0)
        {
            snapshot = default;
            return false;
        }

        snapshot = new Analytics.TokenCompletionSnapshot(tokensCollected, tokensAvailable);
        return true;
    }

    private void Awake()
    {
        NormalizeDisplayStrings();
        if (!useVictoryPanel)
        {
            Debug.LogWarning("Victory panel was disabled; enabling it so end-of-level choices appear.", this);
            useVictoryPanel = true;
        }
        EnsureHudCanvas();
        CreateTokensUI();
        CreateHeartsUI();
        CreateVictoryPanel();

        if (resetGlobalTokenTotalsOnLoad)
        {
            s_totalFireTokensCollected = 0;
            s_totalWaterTokensCollected = 0;
        }

        if (!isTutorialMode)
        {
            CreateStatusUI(); // This function is now modified
        }
        ResetTokenTracking();
        ResetHearts();
        
        EnsureLevelTimer();
        CreateInstructionPanelIfNeeded();
        
        // Initialize scoring timer (will start when level actually begins)
        _scoringTimerStarted = false;
        _scoringStartTime = 0f;
    }

    // ... (Update, RegisterPlayer, OnLevelReady, TryStartLevel, etc. are UNCHANGED) ...
    // ... (Scroll down to EnsureHudCanvas) ...
    
    private void Update()
    {
        UpdateTokenBreathing();
        if (_waitingForInstructionAck)
        {
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
            {
                DismissInstructionPanel();
            }

            return;
        }

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
            if (!isTutorialMode)
            {
                UpdateStatus(levelIntroMessage);
            }
            ResetTokenTracking();
            RecountTokensInScene();
            SyncTokenTracker(resetLevelState: true);
            if (_players.Count >= 2)
            {
                TryStartLevel();
            }
        }

    private void TryStartLevel()
    {
        if (_gameActive || _players.Count < 2 || _waitingForInstructionAck) return;

        _gameActive = true;
        _gameFinished = false;
        _playersAtExit.Clear();

        // Start the scoring timer when the level actually begins (separate from analytics timer)
        _scoringStartTime = Time.realtimeSinceStartup;
        _scoringTimerStarted = true;

        foreach (var player in _players)
        {
            player.SetMovementEnabled(true);
        }

        CancelNextSceneLoad();
        if (!isTutorialMode)
        {
            UpdateStatus(levelStartMessage);
        }
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

        if (!isTutorialMode)
        {
            UpdateStatus("Careful! Keep your distance.");
        }
    }

    public void OnPlayerEnteredExit(CoopPlayerController player)
    {
        if (player == null) return;

        _playersAtExit.Add(player);

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

    public void OnPlayerHitByEnemy(CoopPlayerController player, CannonVariant variant = CannonVariant.Fire)
    {
        if (player == null) return;
        var cause = variant == CannonVariant.Fire ? DamageCause.ProjectileFire : DamageCause.ProjectileIce;
        DamagePlayer(player.Role, 1, cause);
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
            SyncTokenTracker(resetLevelState: false);
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
            SyncTokenTracker(resetLevelState: false);
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

    private void UpdateStatus(string message)
    {
        if (_statusLabel != null)
        {
            _statusLabel.text = message;
        }

        if (_statusBackgroundImage != null)
        {
            bool hasMessage = !string.IsNullOrWhiteSpace(message);
            _statusBackgroundImage.gameObject.SetActive(hasMessage);
        }

        Debug.Log(message);
    }

    private TMP_FontAsset GetUpperUiFont()
    {
        if (_cachedUpperUiFont != null)
        {
            return _cachedUpperUiFont;
        }

        if (upperUiFont != null)
        {
            _cachedUpperUiFont = upperUiFont;
        }
        else if (TMP_Settings.defaultFontAsset != null)
        {
            _cachedUpperUiFont = TMP_Settings.defaultFontAsset;
        }

        return _cachedUpperUiFont;
    }

    private void ApplyUpperUiFont(TextMeshProUGUI label)
    {
        if (label == null)
        {
            return;
        }

        var fontAsset = GetUpperUiFont();
        if (fontAsset != null)
        {
            label.font = fontAsset;
        }
    }

    // --- MODIFICATION START ---
    // This function is modified to parent the Status UI to the _topUiBar
    // and anchor it to the top-center.
    private void CreateStatusUI()
    {
        if (_hudCanvas == null) return;
        
        // This logic to create a second one is still here per your request
        string backgroundName = "MessageBackground";
        if (_hudCanvas.transform.Find(backgroundName) != null)
        {
             backgroundName = "MessageBackground_2";
        }

        GameObject background = new GameObject(backgroundName); 
        
        // --- MODIFICATION: Parent to the Top UI Bar ---
        background.transform.SetParent(_topUiBar, false);

        RectTransform bgRect = background.AddComponent<RectTransform>();
        
        // --- MODIFICATION: Anchor to the Top-Center of the bar ---
        bgRect.anchorMin = new Vector2(0.5f, 1f); // Top-Center
        bgRect.anchorMax = new Vector2(0.5f, 1f); // Top-Center
        bgRect.pivot = new Vector2(0.5f, 1f);
        bgRect.sizeDelta = new Vector2(680f, 120f);
        // Position it 40 pixels down from the top-center of the bar
        bgRect.anchoredPosition = new Vector2(0f, -40f); 

        Image image = background.AddComponent<Image>();
        image.color = messageBackground;
        _statusBackgroundImage = image;
        image.gameObject.SetActive(false);

        GameObject textGO = new GameObject("StatusLabel");
        textGO.transform.SetParent(background.transform, false);

        RectTransform textRect = textGO.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(20f, 20f);
        textRect.offsetMax = new Vector2(-20f, -20f);

        _statusLabel = textGO.AddComponent<TextMeshProUGUI>();
        ApplyUpperUiFont(_statusLabel);
        _statusLabel.alignment = TextAlignmentOptions.Center;
        _statusLabel.fontSize = 40f;
        _statusLabel.text = string.Empty;
    }
    // --- MODIFICATION END ---


    private bool TryGetInstructionContentForScene(out string[] lines, out string prompt)
    {
        // ... (This function is UNCHANGED) ...
        lines = null;
        prompt = instructionContinuePrompt;

        if (!showInstructionPanel) return false;

        string currentScene = SceneManager.GetActiveScene().name;

        if (!string.IsNullOrEmpty(instructionPanelSceneName) &&
            currentScene == instructionPanelSceneName)
        {
            lines = instructionLines;
            prompt = instructionContinuePrompt;
        }
        else if (!string.IsNullOrEmpty(level2InstructionSceneName) &&
                 currentScene == level2InstructionSceneName)
        {
            lines = level2InstructionLines;
            prompt = string.IsNullOrEmpty(level2InstructionContinuePrompt)
                ? instructionContinuePrompt
                : level2InstructionContinuePrompt;
        }
        else if (!string.IsNullOrEmpty(level3InstructionSceneName) &&
                 currentScene == level3InstructionSceneName)
        {
            lines = level3InstructionLines;
            prompt = string.IsNullOrEmpty(level3InstructionContinuePrompt)
                ? instructionContinuePrompt
                : level3InstructionContinuePrompt;
        }
        else if (string.IsNullOrEmpty(instructionPanelSceneName))
        {
            lines = instructionLines;
        }

        return lines != null && lines.Length > 0;
    }

    private void CreateInstructionPanelIfNeeded()
    {
        // ... (This function is UNCHANGED) ...
        if (_hudCanvas == null || _instructionPanel != null) return;

        if (!TryGetInstructionContentForScene(out var lines, out var prompt)) return;

        _instructionPanel = new GameObject("InstructionPanel");
        _instructionPanel.transform.SetParent(_hudCanvas.transform, false);

        RectTransform panelRect = _instructionPanel.AddComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        Image panelImage = _instructionPanel.AddComponent<Image>();
        panelImage.color = new Color(0f, 0f, 0f, 252f / 255f);

        GameObject instructionsGO = new GameObject("InstructionLines");
        instructionsGO.transform.SetParent(_instructionPanel.transform, false);

        RectTransform instructionsRect = instructionsGO.AddComponent<RectTransform>();
        instructionsRect.anchorMin = new Vector2(0.5f, 0.5f);
        instructionsRect.anchorMax = new Vector2(0.5f, 0.5f);
        instructionsRect.pivot = new Vector2(0.5f, 0.5f);
        instructionsRect.sizeDelta = new Vector2(1900f, 50f);

        TextMeshProUGUI instructionsLabel = instructionsGO.AddComponent<TextMeshProUGUI>();
        ApplyUpperUiFont(instructionsLabel);
        instructionsLabel.alignment = TextAlignmentOptions.Center;
        instructionsLabel.fontSize = 60f;
        instructionsLabel.text = lines != null && lines.Length > 0
            ? string.Join("\n", lines)
            : string.Empty;
        instructionsLabel.raycastTarget = false;

        GameObject promptGO = new GameObject("InstructionPrompt");
        promptGO.transform.SetParent(_instructionPanel.transform, false);

        RectTransform promptRect = promptGO.AddComponent<RectTransform>();
        promptRect.anchorMin = new Vector2(0.5f, 0f);
        promptRect.anchorMax = new Vector2(0.5f, 0f);
        promptRect.pivot = new Vector2(0.5f, 0f);
        promptRect.anchoredPosition = new Vector2(0f, 60f);
        promptRect.sizeDelta = new Vector2(700f, 80f);

        TextMeshProUGUI promptLabel = promptGO.AddComponent<TextMeshProUGUI>();
        ApplyUpperUiFont(promptLabel);
        promptLabel.alignment = TextAlignmentOptions.Center;
        promptLabel.fontSize = 36f;
        promptLabel.text = string.IsNullOrEmpty(prompt)
            ? "Press Space to start"
            : prompt;
        promptLabel.raycastTarget = false;

        _instructionPanel.transform.SetAsLastSibling();

        _waitingForInstructionAck = true;

        if (!_instructionPausedTime)
        {
            _previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            _instructionPausedTime = true;
        }
    }

    private void DismissInstructionPanel()
    {
        // ... (This function is UNCHANGED) ...
        _waitingForInstructionAck = false;

        if (_instructionPanel != null)
        {
            _instructionPanel.SetActive(false);
        }

        RestoreTimeScaleIfNeeded();

        if (_levelReady)
        {
            TryStartLevel();
        }
    }

    private void RestoreTimeScaleIfNeeded()
    {
        // ... (This function is UNCHANGED) ...
        if (_instructionPausedTime)
        {
            Time.timeScale = _previousTimeScale;
            _instructionPausedTime = false;
        }
    }

    // --- MODIFICATION START ---
    // This function is modified to create the _topUiBar.
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

        EnsureEventSystemExists();
        
        // --- NEW: Create the Top UI Bar ---
        GameObject topBarGO = new GameObject("TopUI_Bar_Background");
        topBarGO.transform.SetParent(canvasGO.transform, false); // Parent to the Canvas
        Image topBarImg = topBarGO.AddComponent<Image>();
        topBarImg.color = topUiBarColor; // Use the new color variable

        _topUiBar = topBarGO.GetComponent<RectTransform>();
        
        // Anchor it to the top-center and clamp its width so it doesn't exceed the maze area.
        _topUiBar.anchorMin = new Vector2(0.5f, 1f); 
        _topUiBar.anchorMax = new Vector2(0.5f, 1f); 
        _topUiBar.pivot = new Vector2(0.5f, 1f);
        
        float clampedBarWidth = Mathf.Max(400f, topUiBarWidth);
        _topUiBar.sizeDelta = new Vector2(clampedBarWidth, topUiBarHeight);
        _topUiBar.anchoredPosition = Vector2.zero; // Position at the top

        // Create a centered content root that keeps hearts/tokens grouped together.
        GameObject topUiContent = new GameObject("TopUIContent");
        topUiContent.transform.SetParent(topBarGO.transform, false);
        _topUiContentRoot = topUiContent.AddComponent<RectTransform>();
        _topUiContentRoot.anchorMin = Vector2.zero;
        _topUiContentRoot.anchorMax = Vector2.one;
        _topUiContentRoot.offsetMin = new Vector2(40f, 20f);
        _topUiContentRoot.offsetMax = new Vector2(-40f, -20f);

        // --- END NEW ---
    }
    // --- MODIFICATION END ---

    private void EnsureEventSystemExists()
    {
        if (EventSystem.current != null) return;

        GameObject eventSystemGO = new GameObject("EventSystem");
        eventSystemGO.AddComponent<EventSystem>();
        eventSystemGO.AddComponent<StandaloneInputModule>();
        DontDestroyOnLoad(eventSystemGO);
    }

    // --- MODIFICATION START ---
    // This function now parents the Hearts UI into the centered Top UI stack.
    private void CreateHeartsUI()
    {
        if (_hudCanvas == null) return;
        
        // --- 1. Create the Master Container for all "Life" UI ---
        GameObject heartsMasterContainer = new GameObject("HeartsMasterContainer", typeof(RectTransform));
        
        // --- MODIFICATION: Parent to the centered Top UI content root ---
        var heartsParent = _topUiContentRoot != null ? _topUiContentRoot : _topUiBar;
        heartsMasterContainer.transform.SetParent(heartsParent, false);

        RectTransform masterRect = heartsMasterContainer.GetComponent<RectTransform>();
        masterRect.anchorMin = new Vector2(1f, 0.5f); 
        masterRect.anchorMax = new Vector2(1f, 0.5f);
        masterRect.pivot = new Vector2(1f, 0.5f);
        masterRect.anchoredPosition = new Vector2(-20f, 0f); 
        masterRect.sizeDelta = new Vector2(360f, 200f); 

        VerticalLayoutGroup masterLayout = heartsMasterContainer.AddComponent<VerticalLayoutGroup>();
        masterLayout.spacing = 10;
        masterLayout.childAlignment = TextAnchor.MiddleCenter;
        masterLayout.childControlWidth = false;
        masterLayout.childControlHeight = false;
        
        ContentSizeFitter masterFitter = heartsMasterContainer.AddComponent<ContentSizeFitter>();
        masterFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        
        // --- 3. Create Hearts Container for Ember ---
        GameObject emberHeartsGO = new GameObject("EmberHeartsContainer");
        emberHeartsGO.transform.SetParent(heartsMasterContainer.transform, false); 
        
        Image emberBg = emberHeartsGO.AddComponent<Image>();
        emberBg.color = new Color(0f, 0f, 0f, 0.35f); 
        
        HorizontalLayoutGroup emberLayout = emberHeartsGO.AddComponent<HorizontalLayoutGroup>();
        emberLayout.spacing = 10; 
        emberLayout.childAlignment = TextAnchor.MiddleCenter;
        emberLayout.childControlWidth = false;
        emberLayout.childControlHeight = false;
        emberLayout.padding = new RectOffset(10, 10, 5, 5); 

        RectTransform emberRect = emberHeartsGO.GetComponent<RectTransform>();
        float heartContainerHeight = heartIconSize.y + emberLayout.padding.top + emberLayout.padding.bottom;
        emberRect.sizeDelta = new Vector2(360f, heartContainerHeight);

        GameObject emberLabelGO = new GameObject("Label");
        emberLabelGO.transform.SetParent(emberHeartsGO.transform, false); 
        TextMeshProUGUI emberLabel = emberLabelGO.AddComponent<TextMeshProUGUI>();
        ApplyUpperUiFont(emberLabel);
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

        // --- 4. Create Hearts Container for Aqua ---
        GameObject aquaHeartsGO = new GameObject("AquaHeartsContainer");
        aquaHeartsGO.transform.SetParent(heartsMasterContainer.transform, false); 

        Image aquaBg = aquaHeartsGO.AddComponent<Image>();
        aquaBg.color = new Color(0f, 0f, 0f, 0.35f); 
        
        HorizontalLayoutGroup aquaLayout = aquaHeartsGO.AddComponent<HorizontalLayoutGroup>();
        aquaLayout.spacing = 10;
        aquaLayout.childAlignment = TextAnchor.MiddleCenter;
        aquaLayout.childControlWidth = false;
        aquaLayout.childControlHeight = false;
        aquaLayout.padding = new RectOffset(10, 10, 5, 5); 

        RectTransform aquaRect = aquaHeartsGO.GetComponent<RectTransform>();
        aquaRect.sizeDelta = new Vector2(360f, heartContainerHeight); 

        GameObject aquaLabelGO = new GameObject("Label");
        aquaLabelGO.transform.SetParent(aquaHeartsGO.transform, false); 
        TextMeshProUGUI aquaLabel = aquaLabelGO.AddComponent<TextMeshProUGUI>();
        ApplyUpperUiFont(aquaLabel);
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

        _heartLossAnimator = heartsMasterContainer.AddComponent<HeartLossAnimator>();

        var heartLossAudioSource = heartsMasterContainer.AddComponent<AudioSource>();
        heartLossAudioSource.playOnAwake = false;
        heartLossAudioSource.loop = false;
        heartLossAudioSource.spatialBlend = 0f;

        _heartLossAnimator.ConfigureAudio(GetHeartLossClip(), heartLossAudioSource, heartLossSfxVolume);
        _heartLossAnimator.HeartAnimationFinished += UpdateHeartsUI;
    }
    // --- MODIFICATION END ---
    
    // --- MODIFICATION START ---
    // This function now parents the Tokens UI into the centered Top UI stack.
    private void CreateTokensUI()
    {
        if (_hudCanvas == null) return;
        
        // --- 1. Create the Master Container for all "Collect" UI ---
        GameObject tokensMasterContainer = new GameObject("TokensMasterContainer", typeof(RectTransform));
        
        // --- MODIFICATION: Parent to the centered Top UI content root ---
        var tokensParent = _topUiContentRoot != null ? _topUiContentRoot : _topUiBar;
        tokensMasterContainer.transform.SetParent(tokensParent, false);

        RectTransform masterRect = tokensMasterContainer.GetComponent<RectTransform>();
        masterRect.anchorMin = new Vector2(0f, 0.5f); 
        masterRect.anchorMax = new Vector2(0f, 0.5f);
        masterRect.pivot = new Vector2(0f, 0.5f);
        masterRect.anchoredPosition = new Vector2(20f, 0f);
        masterRect.sizeDelta = new Vector2(520f, 200f); 

        VerticalLayoutGroup masterLayout = tokensMasterContainer.AddComponent<VerticalLayoutGroup>();
        masterLayout.spacing = 10;
        masterLayout.childAlignment = TextAnchor.MiddleCenter;
        masterLayout.childControlWidth = false;
        masterLayout.childControlHeight = false;
        
        ContentSizeFitter masterFitter = tokensMasterContainer.AddComponent<ContentSizeFitter>();
        masterFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        masterFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize; // Let it wrap width
        
        // --- 3. Create Token Container for Ember ---
        GameObject emberTokensGO = new GameObject("EmberTokensContainer");
        emberTokensGO.transform.SetParent(tokensMasterContainer.transform, false); 
        
        Image emberBg = emberTokensGO.AddComponent<Image>();
        emberBg.color = new Color(0f, 0f, 0f, 0.35f); 
        
        HorizontalLayoutGroup emberLayout = emberTokensGO.AddComponent<HorizontalLayoutGroup>();
        emberLayout.spacing = 10; 
        emberLayout.childAlignment = TextAnchor.MiddleCenter; 
        emberLayout.childControlWidth = false;
        emberLayout.childControlHeight = false;
        emberLayout.padding = new RectOffset(10, 10, 5, 5); 

        RectTransform emberRect = emberTokensGO.GetComponent<RectTransform>();
        float tokenContainerHeight = tokenIconSize.y + emberLayout.padding.top + emberLayout.padding.bottom;
        emberRect.sizeDelta = new Vector2(520f, tokenContainerHeight); 
        
        // --- MODIFICATION: Add Content Size Fitter ---
        ContentSizeFitter emberFitter = emberTokensGO.AddComponent<ContentSizeFitter>();
        emberFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize; // Make it wrap content

        GameObject emberLabelGO = new GameObject("Label");
        emberLabelGO.transform.SetParent(emberTokensGO.transform, false); 
        TextMeshProUGUI emberLabel = emberLabelGO.AddComponent<TextMeshProUGUI>();
        ApplyUpperUiFont(emberLabel);
        emberLabel.text = "Fire:";
        emberLabel.fontSize = 40f;
        emberLabel.color = Color.white;
        emberLabel.alignment = TextAlignmentOptions.Left;
        LayoutElement emberLabelLayout = emberLabelGO.AddComponent<LayoutElement>();
        emberLabelLayout.preferredWidth = 130f; 

        // --- 4. Create Token Container for Aqua ---
        GameObject aquaTokensGO = new GameObject("AquaTokensContainer");
        aquaTokensGO.transform.SetParent(tokensMasterContainer.transform, false); 
        
        Image aquaBg = aquaTokensGO.AddComponent<Image>();
        aquaBg.color = new Color(0f, 0f, 0f, 0.35f); 
        
        HorizontalLayoutGroup aquaLayout = aquaTokensGO.AddComponent<HorizontalLayoutGroup>();
        aquaLayout.spacing = 10;
        aquaLayout.childAlignment = TextAnchor.MiddleCenter; 
        aquaLayout.childControlWidth = false;
        aquaLayout.childControlHeight = false;
        aquaLayout.padding = new RectOffset(10, 10, 5, 5); 

        RectTransform aquaRect = aquaTokensGO.GetComponent<RectTransform>();
        aquaRect.sizeDelta = new Vector2(520f, tokenContainerHeight);
        
        // --- MODIFICATION: Add Content Size Fitter ---
        ContentSizeFitter aquaFitter = aquaTokensGO.AddComponent<ContentSizeFitter>();
        aquaFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize; // Make it wrap content

        GameObject aquaLabelGO = new GameObject("Label");
        aquaLabelGO.transform.SetParent(aquaTokensGO.transform, false); 
        TextMeshProUGUI aquaLabel = aquaLabelGO.AddComponent<TextMeshProUGUI>();
        ApplyUpperUiFont(aquaLabel);
        aquaLabel.text = "Water:";
        aquaLabel.fontSize = 40f;
        aquaLabel.color = Color.white;
        aquaLabel.alignment = TextAlignmentOptions.Left;
        LayoutElement aquaLabelLayout = aquaLabelGO.AddComponent<LayoutElement>();
        aquaLabelLayout.preferredWidth = 130f; 

        UpdateTokensUI();
    }
    // --- MODIFICATION END ---

    private void CreateVictoryPanel()
    {
        // ... (This function is UNCHANGED, it's an overlay so it's fine) ...
        if (!useVictoryPanel || _hudCanvas == null || _victoryPanel != null) return;

        _victoryPanel = new GameObject("EndOfLevelPanel");
        _victoryPanel.transform.SetParent(_hudCanvas.transform, false);

        RectTransform rect = _victoryPanel.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(640f, 550f); // Increased height to accommodate larger font
        rect.anchoredPosition = Vector2.zero;

        Image background = _victoryPanel.AddComponent<Image>();
        background.color = new Color(0f, 0f, 0f, 0.78f);

        GameObject content = new GameObject("Content");
        content.transform.SetParent(_victoryPanel.transform, false);
        RectTransform contentRect = content.AddComponent<RectTransform>();
        contentRect.anchorMin = Vector2.zero;
        contentRect.anchorMax = Vector2.one;
        contentRect.offsetMin = new Vector2(48f, 32f); // Reduced top offset to bring title closer to top
        contentRect.offsetMax = new Vector2(-48f, -48f);

        _victoryContentLayout = content.AddComponent<VerticalLayoutGroup>();
        _victoryContentLayout.childAlignment = TextAnchor.UpperCenter;
        _victoryContentLayout.spacing = 10f; // Reduced spacing to bring buttons closer to score text
        _victoryContentLayout.padding = new RectOffset(0, 0, 5, 10); // Reduced top padding to bring title closer to top
        _victoryContentLayout.childControlWidth = true;
        _victoryContentLayout.childForceExpandWidth = true;
        _victoryContentLayout.childControlHeight = false;
        _victoryContentLayout.childForceExpandHeight = false;

        GameObject titleGO = new GameObject("Title");
        titleGO.transform.SetParent(content.transform, false);
        RectTransform titleRect = titleGO.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 0.5f);
        titleRect.anchorMax = new Vector2(1f, 0.5f);
        titleRect.sizeDelta = new Vector2(0f, 90f);
        LayoutElement titleLayout = titleGO.AddComponent<LayoutElement>();
        titleLayout.preferredHeight = 90f;

        _victoryTitleLabel = titleGO.AddComponent<TextMeshProUGUI>();
        _victoryTitleLabel.alignment = TextAlignmentOptions.Center;
        _victoryTitleLabel.fontSize = 56f;
        _victoryTitleLabel.fontStyle = FontStyles.Bold;
        _victoryTitleLabel.text = victoryTitleText;

        // Create Trophy Image (between title and body/score text)
        GameObject trophyContainer = new GameObject("TrophyContainer");
        trophyContainer.transform.SetParent(content.transform, false);
        RectTransform trophyContainerRect = trophyContainer.AddComponent<RectTransform>();
        trophyContainerRect.anchorMin = new Vector2(0f, 0.5f);
        trophyContainerRect.anchorMax = new Vector2(1f, 0.5f);
        trophyContainerRect.sizeDelta = new Vector2(0f, trophyIconSize.y + 30f); // Add extra space below trophy
        LayoutElement trophyContainerLayout = trophyContainer.AddComponent<LayoutElement>();
        trophyContainerLayout.preferredHeight = trophyIconSize.y + 30f; // Extra space for spacing
        trophyContainerLayout.flexibleWidth = 0f;
        trophyContainerLayout.flexibleHeight = 0f;

        // Add horizontal layout to center the trophy
        HorizontalLayoutGroup trophyContainerLayoutGroup = trophyContainer.AddComponent<HorizontalLayoutGroup>();
        trophyContainerLayoutGroup.childAlignment = TextAnchor.MiddleCenter;
        trophyContainerLayoutGroup.childControlWidth = false;
        trophyContainerLayoutGroup.childControlHeight = false;
        trophyContainerLayoutGroup.childForceExpandWidth = false;
        trophyContainerLayoutGroup.childForceExpandHeight = false;

        GameObject trophyGO = new GameObject("Trophy");
        trophyGO.transform.SetParent(trophyContainer.transform, false);
        RectTransform trophyRect = trophyGO.AddComponent<RectTransform>();
        trophyRect.sizeDelta = trophyIconSize;

        _victoryTrophyImage = trophyGO.AddComponent<Image>();
        if (trophySprite != null)
        {
            _victoryTrophyImage.sprite = trophySprite;
        }
        _victoryTrophyImage.preserveAspect = true;
        trophyContainer.SetActive(false); // Hidden by default, shown only on victory

        GameObject bodyGO = new GameObject("Body");
        bodyGO.transform.SetParent(content.transform, false);
        RectTransform bodyRect = bodyGO.AddComponent<RectTransform>();
        bodyRect.anchorMin = new Vector2(0f, 0.5f);
        bodyRect.anchorMax = new Vector2(1f, 0.5f);
        bodyRect.sizeDelta = new Vector2(0f, 150f);
        LayoutElement bodyLayout = bodyGO.AddComponent<LayoutElement>();
        bodyLayout.preferredHeight = 150f; // Increased to accommodate multiple lines for score display

        _victoryBodyLabel = bodyGO.AddComponent<TextMeshProUGUI>();
        _victoryBodyLabel.alignment = TextAlignmentOptions.Center;
        _victoryBodyLabel.fontSize = 40f;
        _victoryBodyLabel.enableWordWrapping = true;
        _victoryBodyLabel.text = victoryBodyText;

        GameObject summaryGroup = new GameObject("TokenSummary");
        summaryGroup.transform.SetParent(content.transform, false);
        RectTransform summaryRect = summaryGroup.AddComponent<RectTransform>();
        summaryRect.anchorMin = new Vector2(0f, 0.5f);
        summaryRect.anchorMax = new Vector2(1f, 0.5f);
        summaryRect.sizeDelta = new Vector2(0f, 0f); // Reduced size since it's hidden
        LayoutElement summaryLayoutElement = summaryGroup.AddComponent<LayoutElement>();
        summaryLayoutElement.preferredHeight = 0f; // Reduced height since summary is hidden

        VerticalLayoutGroup summaryLayout = summaryGroup.AddComponent<VerticalLayoutGroup>();
        summaryLayout.childAlignment = TextAnchor.MiddleCenter;
        summaryLayout.spacing = 12f;
        summaryLayout.childControlWidth = true;
        summaryLayout.childForceExpandWidth = true;
        summaryLayout.childControlHeight = false;
        summaryLayout.childForceExpandHeight = false;

        _fireSummaryRoot = new GameObject("FireSummary");
        _fireSummaryRoot.transform.SetParent(summaryGroup.transform, false);
        RectTransform fireRect = _fireSummaryRoot.AddComponent<RectTransform>();
        fireRect.anchorMin = new Vector2(0f, 0.5f);
        fireRect.anchorMax = new Vector2(1f, 0.5f);
        fireRect.sizeDelta = new Vector2(0f, 50f);
        _fireSummaryRoot.AddComponent<LayoutElement>().preferredHeight = 50f;

        _fireVictoryLabel = _fireSummaryRoot.AddComponent<TextMeshProUGUI>();
        _fireVictoryLabel.alignment = TextAlignmentOptions.Center;
        _fireVictoryLabel.fontSize = 34f;
        _fireVictoryLabel.text = string.Empty;

        _waterSummaryRoot = new GameObject("WaterSummary");
        _waterSummaryRoot.transform.SetParent(summaryGroup.transform, false);
        RectTransform waterRect = _waterSummaryRoot.AddComponent<RectTransform>();
        waterRect.anchorMin = new Vector2(0f, 0.5f);
        waterRect.anchorMax = new Vector2(1f, 0.5f);
        waterRect.sizeDelta = new Vector2(0f, 50f);
        _waterSummaryRoot.AddComponent<LayoutElement>().preferredHeight = 50f;

        _waterVictoryLabel = _waterSummaryRoot.AddComponent<TextMeshProUGUI>();
        _waterVictoryLabel.alignment = TextAlignmentOptions.Center;
        _waterVictoryLabel.fontSize = 34f;
        _waterVictoryLabel.text = string.Empty;

        GameObject buttonRow = new GameObject("Buttons");
        buttonRow.transform.SetParent(content.transform, false);
        RectTransform buttonRowRect = buttonRow.AddComponent<RectTransform>();
        buttonRowRect.anchorMin = new Vector2(0f, 0.5f);
        buttonRowRect.anchorMax = new Vector2(1f, 0.5f);
        buttonRowRect.sizeDelta = new Vector2(0f, 70f); // Reduced height
        LayoutElement buttonRowLayout = buttonRow.AddComponent<LayoutElement>();
        buttonRowLayout.preferredHeight = 75f; // Reduced height to fit better

        HorizontalLayoutGroup layoutGroup = buttonRow.AddComponent<HorizontalLayoutGroup>();
        layoutGroup.childAlignment = TextAnchor.MiddleCenter;
        layoutGroup.spacing = 24f;
        layoutGroup.padding = new RectOffset(10, 10, 10, 10);
        layoutGroup.childForceExpandHeight = false;
        layoutGroup.childForceExpandWidth = true;
        layoutGroup.childControlHeight = true;
        layoutGroup.childControlWidth = true;

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

    private static void ConfigureVictoryPanelRect(RectTransform rect, Vector2 size)
    {
        if (rect == null) return;

        Vector2 center = new Vector2(0.5f, 0.5f);
        rect.anchorMin = center;
        rect.anchorMax = center;
        rect.pivot = center;
        rect.sizeDelta = size;
        rect.anchoredPosition = Vector2.zero;
        rect.localPosition = Vector3.zero;
    }

    private Button CreateEndPanelButton(string name, Transform parent, string labelText)
    {
        GameObject buttonGO = new GameObject(name);
        buttonGO.transform.SetParent(parent, false);

        RectTransform rect = buttonGO.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(160f, 60f);

        Image bg = buttonGO.AddComponent<Image>();
        bg.color = new Color(0.2f, 0.45f, 0.9f, 1f);

        Button button = buttonGO.AddComponent<Button>();
        LayoutElement layout = buttonGO.AddComponent<LayoutElement>();
        layout.preferredWidth = rect.sizeDelta.x;
        layout.preferredHeight = rect.sizeDelta.y;

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
        label.color = Color.white;
        label.raycastTarget = false;

        return button;
    }

    private void SetButtonLabel(Button button, string text)
    {
        if (button == null) return;

        TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>();
        if (label != null)
        {
            label.text = text;
        }
    }

    private void ShowEndPanel(EndGameState state)
    {
        // ... (This function is UNCHANGED) ...
        if (!useVictoryPanel || _victoryPanel == null) return;

        bool isVictory = state == EndGameState.Victory;

        if (_victoryTitleLabel != null)
        {
            _victoryTitleLabel.text = isVictory ? victoryTitleText : defeatTitleText;
        }

        // Show/hide trophy based on victory state
        if (_victoryTrophyImage != null)
        {
            GameObject trophyContainer = _victoryTrophyImage.transform.parent?.gameObject;
            if (trophyContainer != null)
            {
                trophyContainer.SetActive(isVictory && trophySprite != null);
            }
        }

        if (_victoryBodyLabel != null)
        {
            // Check if we're in a scored level and scoring is enabled
            if (isVictory && enableScoring && IsScoredLevel())
            {
                _victoryBodyLabel.text = BuildScoreDisplayText();
            }
            else
            {
                _victoryBodyLabel.text = isVictory ? victoryBodyText : defeatBodyText;
            }
        }

        if (_fireSummaryRoot != null)
        {
            // Hide token count display for all levels
            _fireSummaryRoot.SetActive(false);
        }

        if (_waterSummaryRoot != null)
        {
            // Hide token count display for all levels
            _waterSummaryRoot.SetActive(false);
        }

        if (_victoryContentLayout != null)
        {
            _victoryContentLayout.childAlignment = isVictory ? TextAnchor.UpperCenter : TextAnchor.MiddleCenter;
        }

        SetButtonLabel(_victoryNextLevelButton, nextLevelButtonText);
        SetButtonLabel(_victoryRestartButton, restartButtonText);
        SetButtonLabel(_victoryMainMenuButton, mainMenuButtonText);

        bool hasNextScene = TryGetNextSceneName(out _);
        if (isVictory)
        {
            if (_fireVictoryLabel != null)
            {
                _fireVictoryLabel.text = $"Total Ember Tokens: {s_totalFireTokensCollected}";
            }

            if (_waterVictoryLabel != null)
            {
                _waterVictoryLabel.text = $"Total Aqua Tokens: {s_totalWaterTokensCollected}";
            }
        }

        bool canAdvance = isVictory && hasNextScene;

        bool isFinalLevelVictory = isVictory &&
                                   !string.IsNullOrEmpty(level3InstructionSceneName) &&
                                   SceneManager.GetActiveScene().name == level3InstructionSceneName;

        if (_victoryNextLevelButton != null)
        {
            _victoryNextLevelButton.gameObject.SetActive(!isFinalLevelVictory && canAdvance);
        }

        if (_victoryRestartButton != null)
        {
            bool showRestart = !isFinalLevelVictory && (!isVictory || !canAdvance);
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
        // ... (This function is UNCHANGED) ...
        int clampedHearts = Mathf.Max(0, startingHearts);
        _fireHearts = clampedHearts;
        _waterHearts = clampedHearts;
        UpdateHeartsUI();
    }

    private void ResetTokenTracking()
    {
        // ... (This function is UNCHANGED) ...
        fireTokensCollected = 0;
        waterTokensCollected = 0;
        _totalFireTokens = 0;
        _totalWaterTokens = 0;
        UpdateTokensUI();
    }

    private void UpdateHeartsUI()
    {
        // ... (This function is UNCHANGED) ...
        // Loop through all of Ember's heart images
        for (int i = 0; i < _emberHeartImages.Count; i++)
        {
            var heartImage = _emberHeartImages[i];
            if (heartImage == null)
            {
                continue;
            }

            if (_heartLossAnimator != null && _heartLossAnimator.IsAnimatingHeart(heartImage.gameObject))
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

            if (_heartLossAnimator != null && _heartLossAnimator.IsAnimatingHeart(heartImage.gameObject))
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

    private void TriggerHeartLossAnimations(bool isEmber, int previousHeartCount, int heartsLost)
    {
        if (_heartLossAnimator == null || heartsLost <= 0 || previousHeartCount <= 0)
        {
            return;
        }

        for (int i = 0; i < heartsLost; i++)
        {
            int heartIndex = previousHeartCount - 1 - i;
            if (heartIndex < 0)
            {
                break;
            }

            _heartLossAnimator.LoseHeart(isEmber, heartIndex);
        }
    }

    private void UpdateTokensUI()
    {
        // ... (This function is UNCHANGED) ...
        // Loop through all of Ember's token images
        for (int i = 0; i < _emberTokenImages.Count; i++)
        {
            if (i < fireTokensCollected)
            {
                // This index is less than the collected count, show "collected" sprite
                _emberTokenImages[i].sprite = fireTokenCollectedSprite;
            }
            else
            {
                // This index is greater, show "empty" sprite
                _emberTokenImages[i].sprite = fireTokenEmptySprite;
            }
        }

        // Loop through all of Aqua's token images
        for (int i = 0; i < _aquaTokenImages.Count; i++)
        {
            if (i < waterTokensCollected)
            {
                _aquaTokenImages[i].sprite = waterTokenCollectedSprite;
            }
            else
            {
                _aquaTokenImages[i].sprite = waterTokenEmptySprite;
            }
        }

        RefreshTokenBreathVisuals();
    }
    
    private void UpdateTokenBreathing()
    {
        if (!enableTokenBreath) return;
        if (_emberTokenImages.Count == 0 && _aquaTokenImages.Count == 0) return;

        _tokenBreathTimer += Time.unscaledDeltaTime;
        ApplyTokenBreathToImages(CalculateTokenBreathMultiplier());
    }

    private AudioClip GetHeartLossClip()
    {
        if (heartLossSfx != null)
        {
            return heartLossSfx;
        }

        if (_generatedHeartLossClip == null)
        {
            _generatedHeartLossClip = BuildProceduralHeartLossClip();
        }

        return _generatedHeartLossClip;
    }

    private static AudioClip BuildProceduralHeartLossClip()
    {
        const int sampleRate = 44100;
        const float duration = 0.35f;
        int sampleCount = Mathf.CeilToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];

        double phasePrimary = 0d;
        double phaseSecondary = 0d;

        const float startFreq = 1100f;
        const float endFreq = 280f;
        const float secondaryOffset = 180f;

        for (int i = 0; i < sampleCount; i++)
        {
            float progress = i / (float)sampleCount;
            float freq = Mathf.Lerp(startFreq, endFreq, progress);
            float envelope = Mathf.SmoothStep(1f, 0f, progress) * Mathf.Clamp01(progress * 4f);

            phasePrimary += freq / sampleRate;
            phaseSecondary += (freq + secondaryOffset) / sampleRate;

            if (phasePrimary > 1d) phasePrimary -= 1d;
            if (phaseSecondary > 1d) phaseSecondary -= 1d;

            float sample = (Mathf.Sin((float)(phasePrimary * 2d * System.Math.PI)) * 0.8f +
                            Mathf.Sin((float)(phaseSecondary * 2d * System.Math.PI)) * 0.2f) * envelope;

            samples[i] = sample;
        }

        var clip = AudioClip.Create("ProceduralHeartLoss", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private void RefreshTokenBreathVisuals()
    {
        if (!enableTokenBreath) return;
        if (_emberTokenImages.Count == 0 && _aquaTokenImages.Count == 0) return;

        ApplyTokenBreathToImages(CalculateTokenBreathMultiplier());
    }

    private float CalculateTokenBreathMultiplier()
    {
        float duration = Mathf.Max(0.01f, tokenBreathCycleSeconds);
        float phase = Mathf.Repeat(_tokenBreathTimer / duration, 1f);
        float normalized = 0.5f + 0.5f * Mathf.Sin(phase * Mathf.PI * 2f);
        float minScale = Mathf.Min(tokenBreathScaleRange.x, tokenBreathScaleRange.y);
        float maxScale = Mathf.Max(tokenBreathScaleRange.x, tokenBreathScaleRange.y);
        return Mathf.Lerp(minScale, maxScale, normalized);
    }

    private void ApplyTokenBreathToImages(float activeMultiplier)
    {
        ApplyTokenBreathToCollection(_emberTokenImages, fireTokensCollected, activeMultiplier);
        ApplyTokenBreathToCollection(_aquaTokenImages, waterTokensCollected, activeMultiplier);
    }

    private void ApplyTokenBreathToCollection(List<Image> images, int collectedCount, float activeMultiplier)
    {
        for (int i = 0; i < images.Count; i++)
        {
            float multiplier = i < collectedCount ? activeMultiplier : 1f;
            ApplyTokenIconScale(images[i], multiplier);
        }
    }

    private void ApplyTokenIconScale(Image image, float multiplier)
    {
        if (image == null) return;

        RectTransform rect = image.rectTransform;
        if (rect == null) return;

        if (!_tokenIconBaseScales.TryGetValue(image, out Vector3 baseScale))
        {
            baseScale = rect.localScale;
            _tokenIconBaseScales[image] = baseScale;
        }

        rect.localScale = baseScale * multiplier;
    }

    // --- MODIFICATION START ---
    // This function is modified to FIX the bug where token icons were not appearing.
    // The line "AddComponent<LayoutElement>()" was accidentally removed in the previous
    // version and has been RESTORED. This is required for the ContentSizeFitter.
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
                // *** PLEASE CHECK THIS: ***
                // Make sure your blue droplet prefabs/objects in the scene
                // have their 'Tag' set to 'WaterToken' in the Inspector!
                waterCount++;
            }
        }

        _totalFireTokens = fireCount;
        _totalWaterTokens = waterCount;
        
        // 1. Find the master container, *then* the sub-containers
        Transform searchRoot = _topUiContentRoot != null ? _topUiContentRoot : (_topUiBar != null ? _topUiBar : _hudCanvas.transform);
        Transform tokensMasterContainer = searchRoot.Find("TokensMasterContainer");
        if (tokensMasterContainer == null)
        {
            Debug.LogError("RecountTokensInScene: Could not find TokensMasterContainer!");
            return;
        }
        
        Transform emberContainer = tokensMasterContainer.Find("EmberTokensContainer");
        Transform aquaContainer = tokensMasterContainer.Find("AquaTokensContainer");

        // 2. Clear any old token images (in case of scene restart)
        foreach (Image img in _emberTokenImages)
        {
            if (img != null)
            {
                _tokenIconBaseScales.Remove(img);
                Destroy(img.gameObject);
            }
        }
        _emberTokenImages.Clear();

        foreach (Image img in _aquaTokenImages)
        {
            if (img != null)
            {
                _tokenIconBaseScales.Remove(img);
                Destroy(img.gameObject);
            }
        }
        _aquaTokenImages.Clear();
        
        // 3. Create the "empty" token slots based on the level's total
        if (emberContainer != null)
        {
            for (int i = 0; i < _totalFireTokens; i++)
            {
                GameObject tokenImgGO = new GameObject($"Token_Fire_{i}");
                tokenImgGO.transform.SetParent(emberContainer, false);
                Image tokenImg = tokenImgGO.AddComponent<Image>();
                tokenImg.sprite = fireTokenEmptySprite; 
                
                RectTransform tokenRect = tokenImgGO.GetComponent<RectTransform>();
                tokenRect.sizeDelta = tokenIconSize; 
                tokenRect.localScale = Vector3.one;
                
                // --- BUG FIX ---
                // This line is CRITICAL and has been re-added.
                // It tells the HorizontalLayoutGroup how wide the icon is.
                tokenImgGO.AddComponent<LayoutElement>().preferredWidth = tokenIconSize.x;
                // --- END BUG FIX ---
                
                _emberTokenImages.Add(tokenImg);
                _tokenIconBaseScales[tokenImg] = tokenRect.localScale;
            }
        }
        
        if (aquaContainer != null)
        {
            for (int i = 0; i < _totalWaterTokens; i++)
            {
                GameObject tokenImgGO = new GameObject($"Token_Aqua_{i}");
                tokenImgGO.transform.SetParent(aquaContainer, false);
                Image tokenImg = tokenImgGO.AddComponent<Image>();
                tokenImg.sprite = waterTokenEmptySprite; 
                
                RectTransform tokenRect = tokenImgGO.GetComponent<RectTransform>();
                tokenRect.sizeDelta = tokenIconSize; 
                tokenRect.localScale = Vector3.one;

                // --- BUG FIX ---
                // This line is CRITICAL and has been re-added.
                tokenImgGO.AddComponent<LayoutElement>().preferredWidth = tokenIconSize.x;
                // --- END BUG FIX ---
                
                _aquaTokenImages.Add(tokenImg);
                _tokenIconBaseScales[tokenImg] = tokenRect.localScale;
            }
        }

        UpdateTokensUI();
        SyncTokenTracker(resetLevelState: false);
    }
    // --- MODIFICATION END ---

    // ... (DamageBothPlayers, DamagePlayer, ApplyDamage, etc. are UNCHANGED) ...
    // ... (Scroll down to the end) ...
    
    private void DamageBothPlayers(CoopPlayerController playerA, CoopPlayerController playerB)
    {
        if (!_gameActive || _gameFinished) return;
        if (playerA == null && playerB == null) return;

        if (playerA != null)
        {
            ApplyDamage(playerA.Role, 1, suppressCheck: true, cause: DamageCause.PlayerTouch);
        }

        if (playerB != null)
        {
            ApplyDamage(playerB.Role, 1, suppressCheck: true, cause: DamageCause.PlayerTouch);
        }

        CheckForHeartDepletion();
    }

    public void DamagePlayer(PlayerRole role, int amount, DamageCause cause = DamageCause.Unknown, Vector3? worldOverride = null)
    {
        if (amount <= 0 || !_gameActive || _gameFinished) return;
        ApplyDamage(role, amount, suppressCheck: false, cause: cause, worldOverride: worldOverride);
    }

    private void ApplyDamage(PlayerRole role, int amount, bool suppressCheck, DamageCause cause = DamageCause.Unknown, Vector3? worldOverride = null)
    {
        if (amount <= 0) return;
        int previousFireHearts = _fireHearts;
        int previousWaterHearts = _waterHearts;

        switch (role)
        {
            case PlayerRole.Fireboy:
                _fireHearts = Mathf.Max(0, _fireHearts - amount);
                TriggerHeartLossAnimations(true, previousFireHearts, previousFireHearts - _fireHearts);
                break;
            case PlayerRole.Watergirl: // This is the line I fixed for you before
                _waterHearts = Mathf.Max(0, _waterHearts - amount);
                TriggerHeartLossAnimations(false, previousWaterHearts, previousWaterHearts - _waterHearts);
                break;
            default:
                 Debug.LogWarning($"ApplyDamage called with unhandled role: {role}");
                 break;
        }

        UpdateHeartsUI(); // This will now update the images
        TriggerHurtEffect(role);

        // analytics: heart loss event
        try
        {
            EnsureLevelTimer();
            float elapsed = levelTimer != null ? levelTimer.ElapsedSeconds : 0f;
            Analytics.GoogleSheetsAnalytics.SendHeartLoss(
                null,
                role == PlayerRole.Fireboy ? "fire" : "water",
                cause.ToString(),
                elapsed);
        }
        catch { }

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
            EnsureLevelTimer();
            levelTimer?.MarkFailure();
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
        if (!TryGetNextSceneName(out string sceneToLoad)) return;

        _reloadingScene = true;
        CancelNextSceneLoad();
        SceneManager.LoadScene(sceneToLoad);
    }

    private void OnVictoryMainMenuClicked()
    {
        if (_reloadingScene || string.IsNullOrEmpty(mainMenuSceneName)) return;

        _reloadingScene = true;
        CancelNextSceneLoad();
        SceneManager.LoadScene(mainMenuSceneName);
    }

    private void TriggerHurtEffect(PlayerRole role)
    {
        foreach (var player in _players)
        {
            if (player == null || player.Role != role) continue;
            player.PlayHurtFlash();
            break;
        }
    }

        private void HandleVictory()
        {
            if (_gameFinished) return;

            _gameFinished = true;
            _gameActive = false;
            // analytics code
            EnsureLevelTimer();
            (levelTimer ?? FindAnyObjectByType<Analytics.LevelTimer>())?.MarkSuccess();
            UpdateStatus(levelVictoryMessage);
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
                // Ensures abandon/quit attempts are captured
                levelTimer.autoSendFailureOnDestroy = true;
                return;
            }

            var go = new GameObject("LevelAnalytics");
            levelTimer = go.AddComponent<Analytics.LevelTimer>();
            levelTimer.autoSendFailureOnDestroy = true;

        }

        private void SyncTokenTracker(bool resetLevelState)
        {
            var tracker = Analytics.TokenTracker.Instance;
            string levelId = GetAnalyticsLevelId();
            int tokensCollected = Mathf.Max(0, fireTokensCollected + waterTokensCollected);
            int tokensAvailable = Mathf.Max(0, _totalFireTokens + _totalWaterTokens);

            if (resetLevelState)
            {
                tracker.ResetForLevel(levelId, tokensAvailable, tokensCollected);
            }
            else
            {
                tracker.UpdateTotals(levelId, tokensCollected, tokensAvailable);
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[Analytics][TokenTracker] {(resetLevelState ? "Reset" : "Update")} level={levelId} collected={tokensCollected} available={tokensAvailable}");
#endif
        }

        private string GetAnalyticsLevelId()
        {
            if (levelTimer != null && !string.IsNullOrWhiteSpace(levelTimer.ResolvedLevelId))
            {
                return levelTimer.ResolvedLevelId;
            }

            return SceneManager.GetActiveScene().name;
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

    private bool TryGetNextSceneName(out string sceneName)
    {
        if (!string.IsNullOrEmpty(nextSceneName))
        {
            sceneName = nextSceneName;
            return true;
        }

        Scene activeScene = SceneManager.GetActiveScene();
        int activeIndex = activeScene.buildIndex;
        int totalScenes = SceneManager.sceneCountInBuildSettings;

        if (activeIndex >= 0 && totalScenes > 0)
        {
            for (int index = activeIndex + 1; index < totalScenes; index++)
            {
                string nextPath = SceneUtility.GetScenePathByBuildIndex(index);
                if (string.IsNullOrEmpty(nextPath)) continue;

                string candidate = Path.GetFileNameWithoutExtension(nextPath);
                if (!string.IsNullOrEmpty(candidate))
                {
                    sceneName = candidate;
                    return true;
                }
            }
        }

        sceneName = null;
        return false;
    }

    private void OnDestroy()
    {
        if (_heartLossAnimator != null)
        {
            _heartLossAnimator.HeartAnimationFinished -= UpdateHeartsUI;
        }

        RestoreTimeScaleIfNeeded();

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
        levelIntroMessage = ReplaceLegacyNames(levelIntroMessage);
        levelStartMessage = ReplaceLegacyNames(levelStartMessage);
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

    private bool IsScoredLevel()
    {
        string currentScene = SceneManager.GetActiveScene().name;
        // Check if we're in any of the main level scenes (Level1, Level2, or Level3)
        // Tutorial uses GameManagerTutorial, so it's automatically excluded
        return (!string.IsNullOrEmpty(instructionPanelSceneName) && currentScene == instructionPanelSceneName) ||
               (!string.IsNullOrEmpty(level2InstructionSceneName) && currentScene == level2InstructionSceneName) ||
               (!string.IsNullOrEmpty(level3InstructionSceneName) && currentScene == level3InstructionSceneName);
    }

    private float GetTargetTimeForCurrentLevel()
    {
        string currentScene = SceneManager.GetActiveScene().name;
        
        // Check for level-specific target times
        if (!string.IsNullOrEmpty(level2InstructionSceneName) && currentScene == level2InstructionSceneName)
        {
            return level2TargetTimeSeconds > 0f ? level2TargetTimeSeconds : targetTimeSeconds;
        }
        
        if (!string.IsNullOrEmpty(level3InstructionSceneName) && currentScene == level3InstructionSceneName)
        {
            return level3TargetTimeSeconds > 0f ? level3TargetTimeSeconds : targetTimeSeconds;
        }
        
        // Default to Level 1 target time (or general target time)
        return targetTimeSeconds;
    }

    private string BuildScoreDisplayText()
    {
        // Use the scoring timer (actual gameplay time) instead of analytics timer
        float elapsedSecondsRaw = _scoringTimerStarted 
            ? Mathf.Max(0f, Time.realtimeSinceStartup - _scoringStartTime) 
            : 0f;
        // Round to whole seconds to match the display format (MM:SS)
        float elapsedSeconds = Mathf.Floor(elapsedSecondsRaw);
        float levelTargetTime = GetTargetTimeForCurrentLevel();
        
        // Calculate score components
        int totalTokens = fireTokensCollected + waterTokensCollected;
        int tokenBonus = totalTokens * pointsPerToken;
        float timeBonus = Mathf.Max(0f, (levelTargetTime - elapsedSeconds) * timeBonusMultiplier);
        int timeBonusInt = Mathf.RoundToInt(timeBonus);
        int totalScore = basePoints + tokenBonus + timeBonusInt;
        
        // Format time as MM:SS
        string timeFormatted = FormatTime(elapsedSeconds);
        string timeBonusFormatted = FormatNumber(timeBonusInt);
        
        // Format token display (X/Y)
        int totalTokensInLevel = _totalFireTokens + _totalWaterTokens;
        string tokenBonusFormatted = FormatNumber(tokenBonus);
        
        // Format total score
        string totalScoreFormatted = FormatNumber(totalScore);
        
        // Build the display text
        return $"Time: {timeFormatted} (Bonus +{timeBonusFormatted})\n" +
               $"Tokens: {totalTokens}/{totalTokensInLevel} (+{tokenBonusFormatted})\n" +
               $"Total Score: {totalScoreFormatted} points";
    }

    private string FormatTime(float seconds)
    {
        int minutes = Mathf.FloorToInt(seconds / 60f);
        int secs = Mathf.FloorToInt(seconds % 60f);
        return $"{minutes:00}:{secs:00}";
    }

    private string FormatNumber(int number)
    {
        return number.ToString("N0");
    }
}
