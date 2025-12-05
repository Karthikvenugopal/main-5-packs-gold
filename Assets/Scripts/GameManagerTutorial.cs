using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;





public class GameManagerTutorial : MonoBehaviour
{
    public static GameManagerTutorial Instance { get; private set; }
    
    public static event System.Action<CoopPlayerController> OnPlayerEnteredExitEvent;
    [SerializeField] private Color messageBackground = new Color(0f, 0f, 0f, 0.55f);
    [Header("UI Messages")]
    [SerializeField] private string tutorialStartMessage = "Welcome! Follow Instructions on the screen to complete the tutorial.";
    [SerializeField] private string levelVictoryMessage = "";
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

    [Header("Theme Settings")]
    [SerializeField] private TMP_FontAsset themeFont;
    [SerializeField] private Sprite themeButtonSprite;
    [SerializeField] private Color themeButtonNormalColor = Color.white;
    [SerializeField] private Color themeButtonHighlightedColor = new Color(0.953f, 0.859f, 0.526f, 1f);
    [SerializeField] private Color themeButtonPressedColor = new Color(0.784f, 0.784f, 0.784f, 1f);
    [SerializeField] private Color themeButtonSelectedColor = new Color(0.961f, 0.961f, 0.961f, 1f);
    [SerializeField] private Color themeButtonTextColor = new Color(0.2f, 0.2f, 0.2f, 1f); 

    
    public Sprite ThemeButtonSprite => themeButtonSprite;
    public Color ThemeButtonNormalColor => themeButtonNormalColor;
    public Color ThemeButtonHighlightedColor => themeButtonHighlightedColor;
    public Color ThemeButtonPressedColor => themeButtonPressedColor;
    public Color ThemeButtonSelectedColor => themeButtonSelectedColor;
    public Color ThemeButtonTextColor => themeButtonTextColor;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (themeFont == null)
        {
            themeFont = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Fonts/UncialAntiqua-Regular SDF.asset");
        }
        if (themeButtonSprite == null)
        {
            themeButtonSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Images/button.png");
        }
    }
#endif
    [Tooltip("Sprite for the trophy icon shown on victory (between title and score)")]
    [SerializeField] private Sprite trophySprite;
    [Tooltip("Size of the trophy icon (Width, Height)")]
    [SerializeField] private Vector2 trophyIconSize = new Vector2(100f, 100f);
    [Header("Progression")]
    [SerializeField] private string nextSceneName = "Level1Scene";
    [SerializeField] private float nextSceneDelaySeconds = 2f;
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    [Header("Victory Panel")]
    [SerializeField] private bool useVictoryPanel = true;
    [SerializeField] private string victoryTitleText = "Tutorial Complete";
    [SerializeField] private string victoryBodyText = "Ready for the real challenge?";
    
    [SerializeField] private string[] victorySlogans = new[]
    {
        "No Cap, That Was Epic!",
        "Main Character Energy!",
        "Slayed It!",
        "Built Different!",
        "GOAted Behavior!",
        "Vibe Check: Passed!",
        "Sheeeeeesh! You Did That!"
    };
    
    [SerializeField] private string defeatTitleText = "Out of Hearts";
    [SerializeField] private string defeatBodyText = "Don't worry! Try again.";
    [SerializeField] private string nextLevelButtonText = "Continue to Level 1";
    [SerializeField] private string restartButtonText = "Restart Tutorial";
    [SerializeField] private string mainMenuButtonText = "Main Menu";
    [SerializeField] private string levelDefeatMessage = "Out of hearts! Try again?";

    
    public int fireTokensCollected = 0;

    
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
    private Image _victoryTrophyImage;
    private VerticalLayoutGroup _victoryContentLayout;

    private readonly List<CoopPlayerController> _players = new();
    private readonly HashSet<CoopPlayerController> _playersAtExit = new();

    
    [Header("Analytics")]
    [SerializeField] private Analytics.LevelTimer levelTimer;

    private TextMeshProUGUI _statusLabel;
    private GameObject _statusBackground;
    private bool _levelReady;
    private bool _gameActive;
    private bool _gameFinished;
    private Coroutine _loadNextSceneRoutine;
    private TMP_FontAsset _cachedEndPanelFont;

    private void Awake()
    {
        Instance = this;
        NormalizeDisplayStrings();
        if (!useVictoryPanel)
        {
            Debug.LogWarning("Victory panel was disabled; enabling it so end-of-level choices appear.", this);
            useVictoryPanel = true;
        }
        EnsureHudCanvas();
        CreateHeartsUI();
        
        CreateVictoryPanel();
        CreateStatusUI();
        ResetTokenTracking();
        ResetHearts();
        EnsureLevelTimer();
    }

    private void Update()
    {


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

    
    
    
    protected virtual void UpdateStatus(string message)
    {
        if (_statusLabel != null)
        {
            _statusLabel.text = message;
        }

        Debug.Log(message);
    }

    public TMP_FontAsset GetUpperUiFont()
    {
        
        if (themeFont != null)
        {
            return themeFont;
        }
        return TMP_Settings.defaultFontAsset;
    }

    private TMP_FontAsset GetEndPanelFont()
    {
        if (_cachedEndPanelFont != null)
        {
            return _cachedEndPanelFont;
        }

        if (TMP_Settings.defaultFontAsset != null)
        {
            _cachedEndPanelFont = TMP_Settings.defaultFontAsset;
        }
        else if (themeFont != null)
        {
            _cachedEndPanelFont = themeFont;
        }

        return _cachedEndPanelFont;
    }

    private void ApplyEndPanelFont(TextMeshProUGUI label)
    {
        if (label == null) return;

        TMP_FontAsset fontAsset = GetEndPanelFont();
        if (fontAsset != null)
        {
            label.font = fontAsset;
        }
    }

    private void CreateStatusUI()
    {
        if (_hudCanvas == null) return;

        GameObject background = new GameObject("MessageBackground");
        
        background.transform.SetParent(_topUiBar, false);

        RectTransform bgRect = background.AddComponent<RectTransform>();
        
        bgRect.anchorMin = new Vector2(0.5f, 1f);
        bgRect.anchorMax = new Vector2(0.5f, 1f);
        bgRect.pivot = new Vector2(0.5f, 1f);
        bgRect.sizeDelta = new Vector2(680f, 120f);
        
        bgRect.anchoredPosition = new Vector2(0f, -100f);

        Image image = background.AddComponent<Image>();
        image.color = messageBackground;

        
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
        
        
        EnsureEventSystem();
        
        
        GameObject topBarGO = new GameObject("TopUI_Bar_Background");
        topBarGO.transform.SetParent(canvasGO.transform, false);
        Image topBarImg = topBarGO.AddComponent<Image>();
        topBarImg.color = topUiBarColor;

        _topUiBar = topBarGO.GetComponent<RectTransform>();
        
        
        _topUiBar.anchorMin = new Vector2(0f, 1f); 
        _topUiBar.anchorMax = new Vector2(1f, 1f); 
        _topUiBar.pivot = new Vector2(0.5f, 1f);
        
        
        _topUiBar.sizeDelta = new Vector2(0f, topUiBarHeight);
        _topUiBar.anchoredPosition = Vector2.zero;
    }

    private void EnsureEventSystem()
    {
        
        if (FindFirstObjectByType<EventSystem>() != null) return;

        
        GameObject eventSystemGO = new GameObject("EventSystem");
        eventSystemGO.AddComponent<EventSystem>();
        eventSystemGO.AddComponent<StandaloneInputModule>();
    }

    private void CreateHeartsUI()
    {
        if (_hudCanvas == null) return;
        
        
        GameObject heartsMasterContainer = new GameObject("HeartsMasterContainer");
        
        
        heartsMasterContainer.transform.SetParent(_topUiBar, false);

        VerticalLayoutGroup masterLayout = heartsMasterContainer.AddComponent<VerticalLayoutGroup>();
        masterLayout.spacing = 10;
        masterLayout.childAlignment = TextAnchor.UpperRight;
        masterLayout.childControlWidth = false;
        masterLayout.childControlHeight = false;
        
        ContentSizeFitter masterFitter = heartsMasterContainer.AddComponent<ContentSizeFitter>();
        masterFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        RectTransform masterRect = heartsMasterContainer.GetComponent<RectTransform>();
        
        
        masterRect.anchorMin = new Vector2(1f, 1f); 
        masterRect.anchorMax = new Vector2(1f, 1f);
        masterRect.pivot = new Vector2(1f, 1f);
        
        masterRect.anchoredPosition = new Vector2(-40f, -20f); 
        masterRect.sizeDelta = new Vector2(360f, 200f); 

        
        GameObject titleLabelGO = new GameObject("TitleLabel");
        titleLabelGO.transform.SetParent(heartsMasterContainer.transform, false);
        TextMeshProUGUI titleLabel = titleLabelGO.AddComponent<TextMeshProUGUI>();
        titleLabel.text = "Lives";
        titleLabel.fontSize = 42f;
        titleLabel.fontStyle = FontStyles.Bold;
        titleLabel.color = Color.white;
        titleLabel.alignment = TextAlignmentOptions.Left;

        LayoutElement titleLayout = titleLabelGO.AddComponent<LayoutElement>();
        titleLayout.preferredWidth = 120f; 
        
        
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
        ConfigureVictoryPanelRect(rect, new Vector2(900f, 650f));

        Image background = _victoryPanel.AddComponent<Image>();
        background.color = new Color(0f, 0f, 0f, 0.78f);

        GameObject content = new GameObject("Content");
        content.transform.SetParent(_victoryPanel.transform, false);
        RectTransform contentRect = content.AddComponent<RectTransform>();
        contentRect.anchorMin = Vector2.zero;
        contentRect.anchorMax = Vector2.one;
        contentRect.offsetMin = new Vector2(48f, 32f);
        contentRect.offsetMax = new Vector2(-48f, -48f);

        _victoryContentLayout = content.AddComponent<VerticalLayoutGroup>();
        _victoryContentLayout.childAlignment = TextAnchor.UpperCenter;
        _victoryContentLayout.spacing = 10f;
        _victoryContentLayout.padding = new RectOffset(0, 0, 5, 10);
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
        ApplyEndPanelFont(_victoryTitleLabel);

        
        GameObject trophyContainer = new GameObject("TrophyContainer");
        trophyContainer.transform.SetParent(content.transform, false);
        RectTransform trophyContainerRect = trophyContainer.AddComponent<RectTransform>();
        trophyContainerRect.anchorMin = new Vector2(0f, 0.5f);
        trophyContainerRect.anchorMax = new Vector2(1f, 0.5f);
        trophyContainerRect.sizeDelta = new Vector2(0f, trophyIconSize.y + 30f);
        LayoutElement trophyContainerLayout = trophyContainer.AddComponent<LayoutElement>();
        trophyContainerLayout.preferredHeight = trophyIconSize.y + 30f;
        trophyContainerLayout.flexibleWidth = 0f;
        trophyContainerLayout.flexibleHeight = 0f;

        
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
        trophyContainer.SetActive(false);

        GameObject bodyGO = new GameObject("Body");
        bodyGO.transform.SetParent(content.transform, false);
        RectTransform bodyRect = bodyGO.AddComponent<RectTransform>();
        bodyRect.anchorMin = new Vector2(0f, 0.5f);
        bodyRect.anchorMax = new Vector2(1f, 0.5f);
        bodyRect.sizeDelta = new Vector2(0f, 150f);
        LayoutElement bodyLayout = bodyGO.AddComponent<LayoutElement>();
        bodyLayout.preferredHeight = 150f;

        _victoryBodyLabel = bodyGO.AddComponent<TextMeshProUGUI>();
        _victoryBodyLabel.alignment = TextAlignmentOptions.Center;
        _victoryBodyLabel.fontSize = 40f;
        _victoryBodyLabel.enableWordWrapping = true;
        _victoryBodyLabel.text = victoryBodyText;
        ApplyEndPanelFont(_victoryBodyLabel);

        GameObject summaryGroup = new GameObject("TokenSummary");
        summaryGroup.transform.SetParent(content.transform, false);
        RectTransform summaryRect = summaryGroup.AddComponent<RectTransform>();
        summaryRect.anchorMin = new Vector2(0f, 0.5f);
        summaryRect.anchorMax = new Vector2(1f, 0.5f);
        summaryRect.sizeDelta = new Vector2(0f, 0f);
        LayoutElement summaryLayoutElement = summaryGroup.AddComponent<LayoutElement>();
        summaryLayoutElement.preferredHeight = 0f;

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
        ApplyEndPanelFont(_fireVictoryLabel);

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
        ApplyEndPanelFont(_waterVictoryLabel);

        GameObject buttonRow = new GameObject("Buttons");
        buttonRow.transform.SetParent(content.transform, false);
        RectTransform buttonRowRect = buttonRow.AddComponent<RectTransform>();
        buttonRowRect.anchorMin = new Vector2(0f, 0.5f);
        buttonRowRect.anchorMax = new Vector2(1f, 0.5f);
        buttonRowRect.sizeDelta = new Vector2(0f, 100f);
        LayoutElement buttonRowLayout = buttonRow.AddComponent<LayoutElement>();
        buttonRowLayout.preferredHeight = 100f;

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
        label.fontSize = 32f;
        label.color = Color.white;
        label.text = labelText;
        label.fontStyle = FontStyles.Bold;
        ApplyEndPanelFont(label);

        
        if (themeButtonSprite != null)
        {
            bg.sprite = themeButtonSprite;
            bg.type = Image.Type.Sliced;
            bg.pixelsPerUnitMultiplier = 1f;
        }
        
        
        bg.color = Color.white; 

        ColorBlock colors = button.colors;
        colors.normalColor = themeButtonNormalColor;
        colors.highlightedColor = themeButtonHighlightedColor;
        colors.pressedColor = themeButtonPressedColor;
        colors.selectedColor = themeButtonSelectedColor;
        colors.colorMultiplier = 1f;
        button.colors = colors;

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
            if (isVictory && victorySlogans != null && victorySlogans.Length > 0)
            {
                _victoryTitleLabel.text = victorySlogans[UnityEngine.Random.Range(0, victorySlogans.Length)];
            }
            else
            {
                _victoryTitleLabel.text = isVictory ? victoryTitleText : defeatTitleText;
            }
        }

        
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
            _victoryBodyLabel.text = isVictory ? victoryBodyText : defeatBodyText;
        }

        
        if (_fireSummaryRoot != null)
        {
            _fireSummaryRoot.SetActive(false);
        }

        if (_waterSummaryRoot != null)
        {
            _waterSummaryRoot.SetActive(false);
        }

        if (_victoryContentLayout != null)
        {
            _victoryContentLayout.childAlignment = isVictory ? TextAnchor.UpperCenter : TextAnchor.MiddleCenter;
        }

        SetButtonLabel(_victoryNextLevelButton, nextLevelButtonText);
        SetButtonLabel(_victoryRestartButton, restartButtonText);
        SetButtonLabel(_victoryMainMenuButton, mainMenuButtonText);

        
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
        UpdateStatus("");
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
        
        EnsureLevelTimer();
        (levelTimer ?? FindAnyObjectByType<Analytics.LevelTimer>())?.MarkSuccess();
        
        
        if (_statusBackground != null)
        {
            _statusBackground.SetActive(false);
        }
        
        FreezePlayers();
        CancelNextSceneLoad();
        ShowEndPanel(EndGameState.Victory);
    }

    
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
