using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    [SerializeField] private Color messageBackground = new Color(0f, 0f, 0f, 0.55f);

    private readonly List<CoopPlayerController> _players = new();
    private readonly HashSet<CoopPlayerController> _playersAtExit = new();

    private TextMeshProUGUI _statusLabel;
    private bool _levelReady;
    private bool _gameActive;
    private bool _gameFinished;

    private void Awake()
    {
        CreateStatusUI();
    }

    private void Update()
    {
        if (_gameFinished && Input.GetKeyDown(KeyCode.R))
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }

    public void StartLevel(int totalIngredientCount)
    {
        OnLevelReady();
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
        UpdateStatus("Level 1 – The Frozen Gate: melt the ice, douse the fire, and meet at the exit.");
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

        UpdateStatus("Work together. Fireboy melts ice; Watergirl extinguishes fire.");
    }

    public void OnPlayersTouched()
    {
        if (!_gameActive || _gameFinished) return;

        _gameFinished = true;
        _gameActive = false;
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
            _gameFinished = true;
            _gameActive = false;
            UpdateStatus("Victory! Both heroes reached safety. Press R to play again.");
            FreezePlayers();
        }
        else
        {
            UpdateStatus($"{player.Role} made it. Wait for your partner!");
        }
    }

    public void OnPlayerExitedExit(CoopPlayerController player)
    {
        if (player == null) return;

        if (_playersAtExit.Remove(player) && _gameActive && !_gameFinished)
        {
            UpdateStatus("Both heroes must stand in the exit to finish.");
        }
    }

    public void OnIngredientEaten(IngredientType type)
    {
        Debug.Log($"Legacy ingredient collected: {type}");
    }

    public void OnPlayerHitByEnemy()
    {
        if (!_gameActive || _gameFinished) return;

        _gameFinished = true;
        _gameActive = false;
        UpdateStatus("An enemy caught you! Press R to restart.");
        FreezePlayers();
    }

    public void OnExitReached()
    {
        if (_gameFinished) return;

        _gameFinished = true;
        _gameActive = false;
        UpdateStatus("Exit reached. Press R to restart.");
        FreezePlayers();
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
}
