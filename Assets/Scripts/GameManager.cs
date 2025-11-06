using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    // --- MODIFICATION START ---
    [Header("Tutorial Settings")]
    [SerializeField] private bool isTutorialMode = false;
    // --- MODIFICATION END ---

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
    [Header("Session Tracking")]
    [SerializeField] private bool resetGlobalTokenTotalsOnLoad = false;
    [Header("Level Intro Instructions")]
    [SerializeField] private bool showInstructionPanel = true;
    [SerializeField] private string instructionPanelSceneName = "Level1Scene";
    [SerializeField] private string[] instructionLines = new[]
    {
        "<b>Level 1</b>",
        "",
        "Collect maximum number of tokens and exit",
        "",
        "Remember: Ember melts ice; Aqua extinguishes fire.",
        "Caution: Touch each other -> lose a heart.",
        "Caution: Touch wrong obstacle -> lose a heart.",
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

    // Keeps a visible record of how many fire tokens the team has picked up.
    public int fireTokensCollected = 0;

    // Keeps a visible record of how many water tokens the team has picked up.
    public int waterTokensCollected = 0;

    private static int s_totalFireTokensCollected;
    private static int s_totalWaterTokensCollected;

    private Canvas _hudCanvas;
    private TextMeshProUGUI _heartsLabel;
    private TextMeshProUGUI _tokensLabel;
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
        if (!useVictoryPanel)
        {
            Debug.LogWarning("Victory panel was disabled; enabling it so end-of-level choices appear.", this);
            useVictoryPanel = true;
        }
        EnsureHudCanvas();
        CreateHeartsUI();
        CreateTokensUI();
        CreateVictoryPanel();

        if (resetGlobalTokenTotalsOnLoad)
        {
            s_totalFireTokensCollected = 0;
            s_totalWaterTokensCollected = 0;
        }

        // --- MODIFICATION START ---
        if (!isTutorialMode)
        {
            CreateStatusUI();
        }
        // --- MODIFICATION END ---
        ResetTokenTracking();
        ResetHearts();
        CreateStatusUI();
        // analytics code: ensure a LevelTimer exists in gameplay scenes
        EnsureLevelTimer();
        CreateInstructionPanelIfNeeded();
    }

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
        // --- MODIFICATION START ---
        if (!isTutorialMode)
        {
            UpdateStatus(levelIntroMessage);
        }
        // --- MODIFICATION END ---
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
        // --- MODIFICATION START ---
        if (!isTutorialMode)
        {
            UpdateStatus(levelStartMessage);
        }
        // --- MODIFICATION END ---
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

    private void CreateStatusUI()
    {
        if (_hudCanvas == null) return;

        GameObject background = new GameObject("MessageBackground");
        background.transform.SetParent(_hudCanvas.transform, false);

        RectTransform bgRect = background.AddComponent<RectTransform>();
        bgRect.anchorMin = new Vector2(0.5f, 1f);
        bgRect.anchorMax = new Vector2(0.5f, 1f);
        bgRect.pivot = new Vector2(0.5f, 1f);
        bgRect.sizeDelta = new Vector2(680f, 120f);
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

    private bool TryGetInstructionContentForScene(out string[] lines, out string prompt)
    {
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
        instructionsRect.sizeDelta = new Vector2(1900f, 500f);

        TextMeshProUGUI instructionsLabel = instructionsGO.AddComponent<TextMeshProUGUI>();
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
        if (_instructionPausedTime)
        {
            Time.timeScale = _previousTimeScale;
            _instructionPausedTime = false;
        }
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
    }

    private void CreateHeartsUI()
    {
        if (_hudCanvas == null || _heartsLabel != null) return;

        GameObject heartsGO = new GameObject("HeartsLabel");
        heartsGO.transform.SetParent(_hudCanvas.transform, false);

        RectTransform rect = heartsGO.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.sizeDelta = new Vector2(450f, 60f);
        rect.anchoredPosition = new Vector2(-40f, -40f);

        _heartsLabel = heartsGO.AddComponent<TextMeshProUGUI>();
        _heartsLabel.alignment = TextAlignmentOptions.Right;
        _heartsLabel.fontSize = 32f;
        _heartsLabel.raycastTarget = false;
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

        if (_fireSummaryRoot != null)
        {
            _fireSummaryRoot.SetActive(isVictory);
        }

        if (_waterSummaryRoot != null)
        {
            _waterSummaryRoot.SetActive(isVictory);
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
        if (_heartsLabel == null) return;
        _heartsLabel.text = $"Ember Hearts: {_fireHearts};  Aqua Hearts: {_waterHearts}";
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

        // analytics hotspot capture on damage
        try
        {
            EnsureLevelTimer();
            float elapsed = levelTimer != null ? levelTimer.ElapsedSeconds : 0f;
            Vector3 worldPos = Vector3.zero;
            for (int i = 0; i < _players.Count; i++)
            {
                var p = _players[i];
                if (p != null && p.Role == role)
                {
                    worldPos = p.transform.position;
                    break;
                }
            }
            Analytics.GoogleSheetsAnalytics.SendFailureHotspot(
                null,
                worldPos,
                elapsed,
                Mathf.Max(0, _fireHearts + _waterHearts),
                Mathf.Max(0, fireTokensCollected),
                Mathf.Max(0, waterTokensCollected),
                1f);
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
}
