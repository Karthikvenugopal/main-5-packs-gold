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
    [Header("Progression")]
    [SerializeField] private string nextSceneName;
    [SerializeField] private float nextSceneDelaySeconds = 2f;

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
        // --- MODIFICATION START ---
        if (!isTutorialMode)
        {
            CreateStatusUI();
        }
        // --- MODIFICATION END ---
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

    public void OnPlayersTouched()
    {
        if (!_gameActive || _gameFinished) return;

        _gameFinished = true;
        _gameActive = false;
        // analytics code
        EnsureLevelTimer();
        (levelTimer ?? FindAnyObjectByType<Analytics.LevelTimer>())?.MarkFailure();
        UpdateStatus("They touched! Press R to restart.");
        FreezePlayers();
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

    public void OnPlayerHitByEnemy()
    {
        if (!_gameActive || _gameFinished) return;

        _gameFinished = true;
        _gameActive = false;
        // analytics code
        EnsureLevelTimer();
        (levelTimer ?? FindAnyObjectByType<Analytics.LevelTimer>())?.MarkFailure();
        UpdateStatus("An enemy caught you! Press R to restart.");
        FreezePlayers();
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
        GameObject canvasGO = new GameObject("MazeUI");
        canvasGO.transform.SetParent(transform, false);

        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();

        GameObject background = new GameObject("MessageBackground");
        background.transform.SetParent(canvas.transform, false);

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

        if (!string.IsNullOrEmpty(nextSceneName))
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
}
