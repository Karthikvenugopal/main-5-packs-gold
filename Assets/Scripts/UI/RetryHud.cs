using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

// Runtime HUD to show an Options button with a simple pause menu on level scenes.
// Provides Continue, Restart Level, and Main Menu.
public class RetryHud : MonoBehaviour
{
    // Use a low sorting order so the instruction overlay (black popup)
    // renders above this HUD and masks the button until gameplay starts.
    private const int SortingOrder = -10;
    private static readonly Color ButtonColor = new Color(0.2f, 0.45f, 0.9f, 1f);
    private static readonly Color PanelColor = new Color(0f, 0f, 0f, 0.85f);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        // Handle the currently loaded scene when entering Play Mode
        OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        var name = scene.name;
        var existing = Object.FindObjectOfType<RetryHud>();

        if (!IsGameplayLevel(name))
        {
            if (existing != null)
            {
                existing.DestroySelf();
            }
            return;
        }

        if (existing != null) return;

        var host = new GameObject("__RetryHUD");
        Object.DontDestroyOnLoad(host);
        host.AddComponent<RetryHud>().CreateUI();
    }

    private static bool IsGameplayLevel(string name)
    {
        if (string.IsNullOrEmpty(name)) return false;
        name = name.ToLowerInvariant();
        return name == "level1scene" || name == "level2scene" || name == "level3scene" || name == "level4scene" || name == "level5scene" ||
               name == "level1" || name == "level2" || name == "level3" || name == "level4" || name == "level5" ||
               name == "tutorial" || name == "tutorialscene";
    }

    private static bool IsLevel1Scene(string name)
    {
        if (string.IsNullOrEmpty(name)) return false;
        name = name.ToLowerInvariant();
        return name == "level1scene" || name == "level1";
    }

    private Canvas _canvas;
    private Button _optionsButton;
    private GameObject _modalPanel;
    private Button _continueButton;
    private Button _restartButton;
    private Button _menuButton;
    private bool _paused;

    private void CreateUI()
    {
        var canvasGO = new GameObject("RetryCanvas");
        canvasGO.transform.SetParent(transform, false);
        _canvas = canvasGO.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = SortingOrder;
        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();

        var btnGO = new GameObject("OptionsButton");
        btnGO.transform.SetParent(canvasGO.transform, false);
        var rect = btnGO.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(1f, 0f);
        rect.anchoredPosition = new Vector2(-15f, 15f);
        rect.sizeDelta = new Vector2(220f, 75f); // Midway size

        var img = btnGO.AddComponent<Image>();
        img.color = ButtonColor;
        _optionsButton = btnGO.AddComponent<Button>();
        _optionsButton.onClick.AddListener(ToggleOptionsPanel);

        var labelGO = new GameObject("Label");
        labelGO.transform.SetParent(btnGO.transform, false);
        var labelRect = labelGO.AddComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        var tmp = labelGO.AddComponent<TextMeshProUGUI>();
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontSize = 30f;
        tmp.enableAutoSizing = false;
        tmp.fontSizeMin = 24f;
        tmp.fontSizeMax = 34f;
        tmp.fontStyle = FontStyles.Bold;
        
        // Theme Application
        ApplyThemeToButton(img, _optionsButton, tmp);
        tmp.color = Color.white;

        tmp.raycastTarget = false;
        tmp.text = "Options";

        CreateModal(canvasGO.transform);
    }

    private void CreateModal(Transform parent)
    {
        _modalPanel = new GameObject("OptionsModal");
        _modalPanel.transform.SetParent(parent, false);
        var rect = _modalPanel.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(800f, 600f); // Significantly increased panel size
        rect.anchoredPosition = Vector2.zero;

        var bg = _modalPanel.AddComponent<Image>();
        bg.color = PanelColor;

        var layout = _modalPanel.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(60, 60, 60, 60); // Significantly increased padding
        layout.spacing = 50f; // Significantly increased spacing
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlHeight = true;
        layout.childForceExpandHeight = true;
        layout.childForceExpandWidth = true;

        var titleGO = new GameObject("Title");
        titleGO.transform.SetParent(_modalPanel.transform, false);
        var titleText = titleGO.AddComponent<TextMeshProUGUI>();
        titleText.text = "Options";
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.fontSize = 64f; // Increased title font
        titleText.color = Color.white;
        
        var font = GetThemeFont();
        if (font != null)
        {
            titleText.font = font;
        }

        _continueButton = CreateModalButton("Continue", ContinueGame);
        _restartButton = CreateModalButton("Restart Level", RestartScene);
        _menuButton = CreateModalButton("Main Menu", ReturnToMainMenu);

        _modalPanel.SetActive(false);
    }

    private Button CreateModalButton(string label, UnityEngine.Events.UnityAction handler)
    {
        var btnGO = new GameObject(label.Replace(" ", "") + "Button");
        btnGO.transform.SetParent(_modalPanel.transform, false);
        var rect = btnGO.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(0f, 80f);

        var img = btnGO.AddComponent<Image>();
        img.color = ButtonColor;

        var button = btnGO.AddComponent<Button>();
        button.onClick.AddListener(handler);

        var layout = btnGO.AddComponent<LayoutElement>();
        layout.preferredHeight = 120f; // Significantly increased button height

        var labelGO = new GameObject("Label");
        labelGO.transform.SetParent(btnGO.transform, false);
        
        // Fix: Ensure label stretches to fill the button
        var labelRect = labelGO.AddComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        var labelText = labelGO.AddComponent<TextMeshProUGUI>();
        labelText.text = label;
        labelText.alignment = TextAlignmentOptions.Center;
        labelText.fontSize = 48f; // Significantly increased font size
        labelText.enableWordWrapping = false; // Prevent wrapping
        
        // Theme Application for Modal Buttons
        ApplyThemeToButton(img, button, labelText);
        
        labelText.raycastTarget = false;

        return button;
    }

    private void ApplyThemeToButton(Image img, Button button, TextMeshProUGUI labelText)
    {
        var font = GetThemeFont();
        if (font != null) labelText.font = font;

        var textColor = GetThemeButtonTextColor();
        if (textColor.HasValue) labelText.color = textColor.Value;
        else labelText.color = Color.white;

        var sprite = GetThemeButtonSprite();
        if (sprite != null)
        {
            img.sprite = sprite;
            img.type = Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = 1f;
            img.color = Color.white;
        }

        var normal = GetThemeButtonNormalColor();
        var highlighted = GetThemeButtonHighlightedColor();
        var pressed = GetThemeButtonPressedColor();
        var selected = GetThemeButtonSelectedColor();

        if (normal.HasValue)
        {
            var colors = button.colors;
            colors.normalColor = normal.Value;
            if (highlighted.HasValue) colors.highlightedColor = highlighted.Value;
            if (pressed.HasValue) colors.pressedColor = pressed.Value;
            if (selected.HasValue) colors.selectedColor = selected.Value;
            button.colors = colors;
        }
    }

    // --- Helper Methods to abstract Theme Provider ---

    private TMP_FontAsset GetThemeFont()
    {
        if (GameManager.Instance != null) return GameManager.Instance.GetUpperUiFont();
        if (GameManagerTutorial.Instance != null) return GameManagerTutorial.Instance.GetUpperUiFont();
        return null;
    }

    private Sprite GetThemeButtonSprite()
    {
        if (GameManager.Instance != null) return GameManager.Instance.ThemeButtonSprite;
        if (GameManagerTutorial.Instance != null) return GameManagerTutorial.Instance.ThemeButtonSprite;
        return null;
    }

    private Color? GetThemeButtonTextColor()
    {
        if (GameManager.Instance != null) return GameManager.Instance.ThemeButtonTextColor;
        if (GameManagerTutorial.Instance != null) return GameManagerTutorial.Instance.ThemeButtonTextColor;
        return null;
    }

    private Color? GetThemeButtonNormalColor()
    {
        if (GameManager.Instance != null) return GameManager.Instance.ThemeButtonNormalColor;
        if (GameManagerTutorial.Instance != null) return GameManagerTutorial.Instance.ThemeButtonNormalColor;
        return null;
    }

    private Color? GetThemeButtonHighlightedColor()
    {
        if (GameManager.Instance != null) return GameManager.Instance.ThemeButtonHighlightedColor;
        if (GameManagerTutorial.Instance != null) return GameManagerTutorial.Instance.ThemeButtonHighlightedColor;
        return null;
    }

    private Color? GetThemeButtonPressedColor()
    {
        if (GameManager.Instance != null) return GameManager.Instance.ThemeButtonPressedColor;
        if (GameManagerTutorial.Instance != null) return GameManagerTutorial.Instance.ThemeButtonPressedColor;
        return null;
    }

    private Color? GetThemeButtonSelectedColor()
    {
        if (GameManager.Instance != null) return GameManager.Instance.ThemeButtonSelectedColor;
        if (GameManagerTutorial.Instance != null) return GameManagerTutorial.Instance.ThemeButtonSelectedColor;
        return null;
    }

    private void ToggleOptionsPanel()
    {
        if (_modalPanel == null) return;
        bool show = !_modalPanel.activeSelf;
        _modalPanel.SetActive(show);
        _paused = show;
        Time.timeScale = show ? 0f : 1f;
    }

    private void ContinueGame()
    {
        ToggleOptionsPanel();
    }

    private void RestartScene()
    {
        if (_paused)
        {
            Time.timeScale = 1f;
            _paused = false;
            if (_modalPanel != null) _modalPanel.SetActive(false);
        }
        var current = SceneManager.GetActiveScene();
        SceneManager.LoadScene(current.name);
    }

    private void ReturnToMainMenu()
    {
        if (_paused)
        {
            Time.timeScale = 1f;
            _paused = false;
        }

        SceneManager.LoadScene("MainMenu");
    }

    private void DestroySelf()
    {
        if (_paused)
        {
            Time.timeScale = 1f;
            _paused = false;
        }

        if (_modalPanel != null)
        {
            _modalPanel.SetActive(false);
        }

        Destroy(gameObject);
    }
}
