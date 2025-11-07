using System.Collections;
using System.Collections.Generic;
using TMPro; // Still needed for other UI elements
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    // ... (你所有的 [Header] 和 [SerializeField] 变量都保持不变) ...
    // ... (isTutorialMode, messageBackground, levelIntroMessage, etc.) ...
    [Header("Tutorial Settings")]
    [SerializeField] private bool isTutorialMode = false;

    [SerializeField] private Color messageBackground = new Color(0f, 0f, 0f, 0.55f);
    [Header("UI Messages")]
    [SerializeField] private string levelIntroMessage = "Work together: melt the ice, douse the fire, and reach the exit.";
    [SerializeField] private string levelStartMessage = "Work together. Ember melts ice; Aqua extinguishes fire.";
    [SerializeField] private string levelVictoryMessage = "Victory! Both heroes reached safety. Press R to play again.";
    [SerializeField] private string waitForPartnerMessage = "{0} made it. Wait for your partner!";
    [SerializeField] private string exitReminderMessage = "Both heroes must stand in the exit to finish.";
    [Header("Player Hearts")]
    [SerializeField] private int startingHearts = 3;
    [Header("Progression")]
    [SerializeField] private string nextSceneName;
    [SerializeField] private float nextSceneDelaySeconds = 2f;
    [Header("Victory Panel")]
    [SerializeField] private bool useVictoryPanel = false;
    [SerializeField] private string victoryRestartSceneName = "Level1Scene";
    [Header("Session Tracking")]
    [SerializeField] private bool resetGlobalTokenTotalsOnLoad = false;
    [Header("Level Intro Instructions")]
    [SerializeField] private bool showInstructionPanel = true;
    [SerializeField] private string instructionPanelSceneName = "Level1Scene";
    [SerializeField] private string[] instructionLines = new[]
    {
        "<b>Heads up</b>",
        "Melt ice.",
        "Cool fire.",
        "Don't touch each other & lose a heart.",
        "Touch wrong obstacle & lose a heart."
    };
    [SerializeField] private string instructionContinuePrompt = "Press Space to start";
    [SerializeField] private string level2InstructionSceneName = "Level2Scene";
    [SerializeField] private string[] level2InstructionLines = new[]
    {
        "<b>Stay sharp</b>",
        "Opposites protect.",
        "Stand in front of danger for your partner."
    };
    [SerializeField] private string level2InstructionContinuePrompt = "Press Space to start";
    
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
    [Tooltip("The background color of the top UI bar")]
    [SerializeField] private Color topUiBarColor = new Color(0.1f, 0.1f, 0.1f, 0.8f);
    // --- MODIFICATION END ---


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
    // --- MODIFICATION END ---
    
    private List<Image> _emberHeartImages = new List<Image>();
    private List<Image> _aquaHeartImages = new List<Image>();
    private List<Image> _emberTokenImages = new List<Image>();
    private List<Image> _aquaTokenImages = new List<Image>();
    
    private int _fireHearts;
    private int _waterHearts;
    private bool _reloadingScene;
    private int _totalFireTokens;
    private int _totalWaterTokens;
    private GameObject _victoryPanel;
    private TextMeshProUGUI _fireVictoryLabel;
    private TextMeshProUGUI _waterVictoryLabel;
    private Button _victoryRestartButton;

    private readonly List<CoopPlayerController> _players = new();
    private readonly HashSet<CoopPlayerController> _playersAtExit = new();

    // analytics code
    [Header("Analytics")]
    [SerializeField] private Analytics.LevelTimer levelTimer;

    private TextMeshProUGUI _statusLabel;
    private bool _levelReady;
    private bool _gameActive;
    private bool _gameFinished;
    private Coroutine _loadNextSceneRoutine;
    private GameObject _instructionPanel;
    private bool _waitingForInstructionAck;
    private bool _instructionPausedTime;
    private float _previousTimeScale = 1f;

    private void Awake()
    {
        NormalizeDisplayStrings();
        EnsureHudCanvas(); // This function is now modified
        CreateHeartsUI(); // This function is now modified
        CreateTokensUI(); // This function is now modified
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
        
        CreateStatusUI(); // This function is now modified
        
        EnsureLevelTimer();
        CreateInstructionPanelIfNeeded();
    }

    // ... (Update, RegisterPlayer, OnLevelReady, TryStartLevel, etc. are UNCHANGED) ...
    // ... (Scroll down to EnsureHudCanvas) ...
    
    private void Update()
    {
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

    private void UpdateStatus(string message)
    {
        if (_statusLabel != null)
        {
            _statusLabel.text = message;
        }

        Debug.Log(message);
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

        GameObject textGO = new GameObject("StatusLabel");
        textGO.transform.SetParent(background.transform, false);

        RectTransform textRect = textGO.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(20f, 20f);
        textRect.offsetMax = new Vector2(-20f, -20f);

        _statusLabel = textGO.AddComponent<TextMeshProUGUI>();
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
        panelImage.color = new Color(0f, 0f, 0f, 0.78f);

        GameObject instructionsGO = new GameObject("InstructionLines");
        instructionsGO.transform.SetParent(_instructionPanel.transform, false);

        RectTransform instructionsRect = instructionsGO.AddComponent<RectTransform>();
        instructionsRect.anchorMin = new Vector2(0.5f, 0.5f);
        instructionsRect.anchorMax = new Vector2(0.5f, 0.5f);
        instructionsRect.pivot = new Vector2(0.5f, 0.5f);
        instructionsRect.sizeDelta = new Vector2(900f, 500f);

        TextMeshProUGUI instructionsLabel = instructionsGO.AddComponent<TextMeshProUGUI>();
        instructionsLabel.alignment = TextAlignmentOptions.Center;
        instructionsLabel.fontSize = 72f;
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
        
        // --- NEW: Create the Top UI Bar ---
        GameObject topBarGO = new GameObject("TopUI_Bar_Background");
        topBarGO.transform.SetParent(canvasGO.transform, false); // Parent to the Canvas
        Image topBarImg = topBarGO.AddComponent<Image>();
        topBarImg.color = topUiBarColor; // Use the new color variable

        _topUiBar = topBarGO.GetComponent<RectTransform>();
        
        // Anchor it to the top edge and stretch 100% wide
        _topUiBar.anchorMin = new Vector2(0f, 1f); 
        _topUiBar.anchorMax = new Vector2(1f, 1f); 
        _topUiBar.pivot = new Vector2(0.5f, 1f);
        
        // Set its height using the new variable
        _topUiBar.sizeDelta = new Vector2(0f, topUiBarHeight); // 0f for width = 100% stretch
        _topUiBar.anchoredPosition = Vector2.zero; // Position at the top
        // --- END NEW ---
    }
    // --- MODIFICATION END ---

    // --- MODIFICATION START ---
    // This function is modified to parent the HeartsMasterContainer to the _topUiBar
    // and anchor it to the top-right *of the bar*.
    private void CreateHeartsUI()
    {
        if (_hudCanvas == null) return;
        
        // --- 1. Create the Master Container for all "Life" UI ---
        GameObject heartsMasterContainer = new GameObject("HeartsMasterContainer");
        
        // --- MODIFICATION: Parent to the Top UI Bar ---
        heartsMasterContainer.transform.SetParent(_topUiBar, false);

        VerticalLayoutGroup masterLayout = heartsMasterContainer.AddComponent<VerticalLayoutGroup>();
        masterLayout.spacing = 10;
        masterLayout.childAlignment = TextAnchor.UpperRight;
        masterLayout.childControlWidth = false;
        masterLayout.childControlHeight = false;
        
        ContentSizeFitter masterFitter = heartsMasterContainer.AddComponent<ContentSizeFitter>();
        masterFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        RectTransform masterRect = heartsMasterContainer.GetComponent<RectTransform>();
        
        // --- MODIFICATION: Anchor to the top-right *of the bar* ---
        masterRect.anchorMin = new Vector2(1f, 1f); 
        masterRect.anchorMax = new Vector2(1f, 1f);
        masterRect.pivot = new Vector2(1f, 1f);
        // Position it 40px in from the top-right corner of the bar
        masterRect.anchoredPosition = new Vector2(-40f, -40f); 
        masterRect.sizeDelta = new Vector2(360f, 200f); 

        // --- 2. Create the "Life" Title ---
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
        
        // --- 3. Create Hearts Container for Ember ---
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

        // --- 4. Create Hearts Container for Aqua ---
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
    // --- MODIFICATION END ---
    
    // --- MODIFICATION START ---
    // This function is modified to parent the TokensMasterContainer to the _topUiBar
    // and anchor it to the top-left *of the bar*.
    private void CreateTokensUI()
    {
        if (_hudCanvas == null) return;
        
        // --- 1. Create the Master Container for all "Collect" UI ---
        GameObject tokensMasterContainer = new GameObject("TokensMasterContainer");
        
        // --- MODIFICATION: Parent to the Top UI Bar ---
        tokensMasterContainer.transform.SetParent(_topUiBar, false);

        VerticalLayoutGroup masterLayout = tokensMasterContainer.AddComponent<VerticalLayoutGroup>();
        masterLayout.spacing = 10;
        masterLayout.childAlignment = TextAnchor.UpperLeft;
        masterLayout.childControlWidth = false;
        masterLayout.childControlHeight = false;
        
        ContentSizeFitter masterFitter = tokensMasterContainer.AddComponent<ContentSizeFitter>();
        masterFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        masterFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize; // Let it wrap width

        RectTransform masterRect = tokensMasterContainer.GetComponent<RectTransform>();
        
        // --- MODIFICATION: Anchor to the top-left *of the bar* ---
        masterRect.anchorMin = new Vector2(0f, 1f); 
        masterRect.anchorMax = new Vector2(0f, 1f);
        masterRect.pivot = new Vector2(0f, 1f);
        // Position it 40px in from the top-left corner of the bar
        masterRect.anchoredPosition = new Vector2(40f, -40f);
        masterRect.sizeDelta = new Vector2(520f, 200f); 

        // --- 2. Create the "Collect" Title ---
        GameObject titleLabelGO = new GameObject("TitleLabel");
        titleLabelGO.transform.SetParent(tokensMasterContainer.transform, false); 
        TextMeshProUGUI titleLabel = titleLabelGO.AddComponent<TextMeshProUGUI>();
        titleLabel.text = "Collect";
        titleLabel.fontSize = 42f;
        titleLabel.fontStyle = FontStyles.Bold;
        titleLabel.color = Color.white;
        titleLabel.alignment = TextAlignmentOptions.Left;

        LayoutElement titleLayout = titleLabelGO.AddComponent<LayoutElement>();
        titleLayout.preferredWidth = 520f;
        
        // --- 3. Create Token Container for Ember ---
        GameObject emberTokensGO = new GameObject("EmberTokensContainer");
        emberTokensGO.transform.SetParent(tokensMasterContainer.transform, false); 
        
        Image emberBg = emberTokensGO.AddComponent<Image>();
        emberBg.color = new Color(0f, 0f, 0f, 0.35f); 
        
        HorizontalLayoutGroup emberLayout = emberTokensGO.AddComponent<HorizontalLayoutGroup>();
        emberLayout.spacing = 10; 
        emberLayout.childAlignment = TextAnchor.MiddleLeft; 
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
        aquaLayout.childAlignment = TextAnchor.MiddleLeft; 
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

        _victoryPanel = new GameObject("VictoryPanel");
        _victoryPanel.transform.SetParent(_hudCanvas.transform, false);

        RectTransform rect = _victoryPanel.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(560f, 280f);
        rect.anchoredPosition = Vector2.zero;

        Image background = _victoryPanel.AddComponent<Image>();
        background.color = new Color(0f, 0f, 0f, 0.78f);

        GameObject titleGO = new GameObject("Title");
        titleGO.transform.SetParent(_victoryPanel.transform, false);
        RectTransform titleRect = titleGO.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 1f);
        titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.sizeDelta = new Vector2(520f, 60f);
        titleRect.anchoredPosition = new Vector2(0f, -24f);

        TextMeshProUGUI titleLabel = titleGO.AddComponent<TextMeshProUGUI>();
        titleLabel.alignment = TextAlignmentOptions.Center;
        titleLabel.fontSize = 38f;
        titleLabel.fontStyle = FontStyles.Bold;
        titleLabel.text = "Level Complete";

        GameObject fireGO = new GameObject("FireSummary");
        fireGO.transform.SetParent(_victoryPanel.transform, false);
        RectTransform fireRect = fireGO.AddComponent<RectTransform>();
        fireRect.anchorMin = new Vector2(0.5f, 0.5f);
        fireRect.anchorMax = new Vector2(0.5f, 0.5f);
        fireRect.pivot = new Vector2(0.5f, 0.5f);
        fireRect.sizeDelta = new Vector2(520f, 50f);
        fireRect.anchoredPosition = new Vector2(0f, 36f);

        _fireVictoryLabel = fireGO.AddComponent<TextMeshProUGUI>();
        _fireVictoryLabel.alignment = TextAlignmentOptions.Center;
        _fireVictoryLabel.fontSize = 30f;
        _fireVictoryLabel.text = string.Empty;

        GameObject waterGO = new GameObject("WaterSummary");
        waterGO.transform.SetParent(_victoryPanel.transform, false);
        RectTransform waterRect = waterGO.AddComponent<RectTransform>();
        waterRect.anchorMin = new Vector2(0.5f, 0.5f);
        waterRect.anchorMax = new Vector2(0.5f, 0.5f);
        waterRect.pivot = new Vector2(0.5f, 0.5f);
        waterRect.sizeDelta = new Vector2(520f, 50f);
        waterRect.anchoredPosition = new Vector2(0f, -14f);

        _waterVictoryLabel = waterGO.AddComponent<TextMeshProUGUI>();
        _waterVictoryLabel.alignment = TextAlignmentOptions.Center;
        _waterVictoryLabel.fontSize = 30f;
        _waterVictoryLabel.text = string.Empty;

        GameObject buttonGO = new GameObject("RestartButton");
        buttonGO.transform.SetParent(_victoryPanel.transform, false);
        RectTransform buttonRect = buttonGO.AddComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0f);
        buttonRect.anchorMax = new Vector2(0.5f, 0f);
        buttonRect.pivot = new Vector2(0.5f, 0f);
        buttonRect.sizeDelta = new Vector2(260f, 70f);
        buttonRect.anchoredPosition = new Vector2(0f, 24f);

        Image buttonBackground = buttonGO.AddComponent<Image>();
        buttonBackground.color = new Color(0.25f, 0.45f, 0.9f, 1f);

        _victoryRestartButton = buttonGO.AddComponent<Button>();
        _victoryRestartButton.onClick.AddListener(OnVictoryRestartClicked);

        GameObject buttonTextGO = new GameObject("Label");
        buttonTextGO.transform.SetParent(buttonGO.transform, false);
        RectTransform buttonTextRect = buttonTextGO.AddComponent<RectTransform>();
        buttonTextRect.anchorMin = Vector2.zero;
        buttonTextRect.anchorMax = Vector2.one;
        buttonTextRect.offsetMin = Vector2.zero;
        buttonTextRect.offsetMax = Vector2.zero;

        TextMeshProUGUI buttonLabel = buttonTextGO.AddComponent<TextMeshProUGUI>();
        buttonLabel.alignment = TextAlignmentOptions.Center;
        buttonLabel.fontSize = 28f;
        buttonLabel.text = "Restart";

        _victoryPanel.SetActive(false);
    }

    private void ShowVictoryPanel()
    {
        // ... (This function is UNCHANGED) ...
        if (!useVictoryPanel || _victoryPanel == null) return;

        if (_fireVictoryLabel != null)
        {
            _fireVictoryLabel.text = $"Total Ember Tokens: {s_totalFireTokensCollected}";
        }

        if (_waterVictoryLabel != null)
        {
            _waterVictoryLabel.text = $"Total Aqua Tokens: {s_totalWaterTokensCollected}";
        }

        _victoryPanel.SetActive(true);
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
            if (i < _fireHearts)
            {
                // This index is less than the current health, show Ember's "full" heart
                _emberHeartImages[i].sprite = emberHeartFullSprite;
                _emberHeartImages[i].gameObject.SetActive(true);
            }
            else
            {
                // This index is equal or greater, show Ember's "empty" heart
                if (emberHeartEmptySprite != null)
                {
                    // If an "empty" sprite is provided, show it
                    _emberHeartImages[i].sprite = emberHeartEmptySprite;
                }
                else
                {
                    // Otherwise, just hide this heart image
                    _emberHeartImages[i].gameObject.SetActive(false);
                }
            }
        }

        // Loop through all of Aqua's heart images
        for (int i = 0; i < _aquaHeartImages.Count; i++)
        {
            if (i < _waterHearts)
            {
                // This index is less than the current health, show Aqua's "full" heart
                _aquaHeartImages[i].sprite = aquaHeartFullSprite;
                _aquaHeartImages[i].gameObject.SetActive(true);
            }
            else
            {
                // This index is equal or greater, show Aqua's "empty" heart
                if (aquaHeartEmptySprite != null)
                {
                    // If an "empty" sprite is provided, show it
                    _aquaHeartImages[i].sprite = aquaHeartEmptySprite;
                }
                else
                {
                    // Otherwise, just hide this heart image
                    _aquaHeartImages[i].gameObject.SetActive(false);
                }
            }
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
        Transform searchRoot = _topUiBar != null ? _topUiBar : _hudCanvas.transform;
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
            Destroy(img.gameObject);
        }
        _emberTokenImages.Clear();

        foreach (Image img in _aquaTokenImages)
        {
            Destroy(img.gameObject);
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
                
                // --- BUG FIX ---
                // This line is CRITICAL and has been re-added.
                // It tells the HorizontalLayoutGroup how wide the icon is.
                tokenImgGO.AddComponent<LayoutElement>().preferredWidth = tokenIconSize.x;
                // --- END BUG FIX ---
                
                _emberTokenImages.Add(tokenImg);
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

                // --- BUG FIX ---
                // This line is CRITICAL and has been re-added.
                tokenImgGO.AddComponent<LayoutElement>().preferredWidth = tokenIconSize.x;
                // --- END BUG FIX ---
                
                _aquaTokenImages.Add(tokenImg);
            }
        }

        UpdateTokensUI();
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
            case PlayerRole.Watergirl: // This is the line I fixed for you before
                _waterHearts = Mathf.Max(0, _waterHearts - amount);
                break;
            default:
                 Debug.LogWarning($"ApplyDamage called with unhandled role: {role}");
                 break;
        }

        UpdateHeartsUI(); // This will now update the images
        TriggerHurtEffect(role);

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
        if (_reloadingScene) return;
        _reloadingScene = true;

        _gameFinished = true;
        _gameActive = false;
        FreezePlayers();
        CancelNextSceneLoad();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void OnVictoryRestartClicked()
    {
        if (_reloadingScene) return;

        _reloadingScene = true;
        CancelNextSceneLoad();

        string sceneToLoad = string.IsNullOrEmpty(victoryRestartSceneName)
            ? SceneManager.GetActiveScene().name
            : victoryRestartSceneName;

        SceneManager.LoadScene(sceneToLoad);
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

        ShowVictoryPanel();

        bool shouldAutoAdvance = (!useVictoryPanel || isTutorialMode) && !string.IsNullOrEmpty(nextSceneName);

        if (shouldAutoAdvance)
        {
            CancelNextSceneLoad();
            _loadNextSceneRoutine = StartCoroutine(LoadNextSceneAfterDelay());
        }
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

    private IEnumerator LoadNextSceneAfterDelay()
    {
        if (nextSceneDelaySeconds > 0f)
        {
            yield return new WaitForSeconds(nextSceneDelaySeconds);
        }

        _loadNextSceneRoutine = null;
        SceneManager.LoadScene(nextSceneName);
    }

    private void CancelNextSceneLoad()
    {
        if (_loadNextSceneRoutine == null) return;

        StopCoroutine(_loadNextSceneRoutine);
        _loadNextSceneRoutine = null;
    }

    private void OnDestroy()
    {
        RestoreTimeScaleIfNeeded();

        if (_victoryRestartButton != null)
        {
            _victoryRestartButton.onClick.RemoveListener(OnVictoryRestartClicked);
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
        waitForPartnerMessage = ReplaceLegacyNames(waitForPartnerMessage);
        exitReminderMessage = ReplaceLegacyNames(exitReminderMessage);
    }

    private static string ReplaceLegacyNames(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;
        return text.Replace("Fireboy", "Ember").Replace("Watergirl", "Aqua");
    }
}
