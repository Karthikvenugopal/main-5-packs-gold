using System.Collections;
using System.Collections.Generic;
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
    [SerializeField] private string levelStartMessage = "Work together. Fireboy melts ice; Watergirl extinguishes fire.";
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

    // Keeps a visible record of how many fire tokens the team has picked up.
    public int fireTokensCollected = 0;

    // Keeps a visible record of how many water tokens the team has picked up.
    public int waterTokensCollected = 0;

    private Canvas _hudCanvas;
    private TextMeshProUGUI _heartsLabel;
    private TextMeshProUGUI _tokensLabel;
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

    private void Awake()
    {
        EnsureHudCanvas();
        CreateHeartsUI();
        CreateTokensUI();
        CreateVictoryPanel();

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
    }

    private void Update()
    {
        if (_gameFinished && Input.GetKeyDown(KeyCode.R))
        {
            CancelNextSceneLoad();
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
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
        if (_gameActive || _players.Count < 2) return;

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

        if (_playersAtExit.Count == _players.Count)
        {
            HandleVictory();
        }
        else
        {
            UpdateStatus(string.Format(waitForPartnerMessage, player.Role));
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
        if (_totalFireTokens < fireTokensCollected)
        {
            _totalFireTokens = fireTokensCollected;
        }

        UpdateTokensUI();
    }

    public void OnWaterTokenCollected()
    {
        waterTokensCollected++;
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
        if (!useVictoryPanel || _victoryPanel == null) return;

        if (_fireVictoryLabel != null)
        {
            _fireVictoryLabel.text = $"Fire Tokens Collected: {fireTokensCollected}";
        }

        if (_waterVictoryLabel != null)
        {
            _waterVictoryLabel.text = $"Water Tokens Collected: {waterTokensCollected}";
        }

        _victoryPanel.SetActive(true);
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
        _heartsLabel.text = $"Fire Hearts: {_fireHearts};  Water Hearts: {_waterHearts}";
    }

    private void UpdateTokensUI()
    {
        if (_tokensLabel == null) return;

        int fireTotal = Mathf.Max(_totalFireTokens, fireTokensCollected);
        int waterTotal = Mathf.Max(_totalWaterTokens, waterTokensCollected);
        _tokensLabel.text = $"Fire Tokens: {fireTokensCollected}/{fireTotal};  Water Tokens: {waterTokensCollected}/{waterTotal}";
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

        if (!useVictoryPanel && !string.IsNullOrEmpty(nextSceneName))
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
        if (_victoryRestartButton != null)
        {
            _victoryRestartButton.onClick.RemoveListener(OnVictoryRestartClicked);
        }
    }
}
