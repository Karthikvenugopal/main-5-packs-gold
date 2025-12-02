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
        rect.anchoredPosition = new Vector2(-30f, 30f);
        bool isLevel1Button = IsLevel1Scene(SceneManager.GetActiveScene().name);
        rect.sizeDelta = isLevel1Button ? new Vector2(150f, 55f) : new Vector2(110f, 40f);

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
        tmp.fontSize = 26f;
        tmp.enableAutoSizing = true;
        tmp.fontSizeMin = 20f;
        tmp.fontSizeMax = 28f;
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
        rect.sizeDelta = new Vector2(520f, 320f);
        rect.anchoredPosition = Vector2.zero;

        var bg = _modalPanel.AddComponent<Image>();
        bg.color = PanelColor;

        var layout = _modalPanel.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(30, 30, 30, 30);
        layout.spacing = 18f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlHeight = true;
        layout.childForceExpandHeight = true;
        layout.childForceExpandWidth = true;

        var titleGO = new GameObject("Title");
        titleGO.transform.SetParent(_modalPanel.transform, false);
        var titleText = titleGO.AddComponent<TextMeshProUGUI>();
        titleText.text = "Options";
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.fontSize = 44f;
        titleText.color = Color.white;

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
        layout.preferredHeight = 80f;

        var labelGO = new GameObject("Label");
        labelGO.transform.SetParent(btnGO.transform, false);
        var labelText = labelGO.AddComponent<TextMeshProUGUI>();
        labelText.text = label;
        labelText.alignment = TextAlignmentOptions.Center;
        labelText.fontSize = 32f;
        labelText.color = Color.white;
        labelText.raycastTarget = false;

        return button;
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
