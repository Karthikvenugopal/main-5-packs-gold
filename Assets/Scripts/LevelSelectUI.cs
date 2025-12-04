using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

public class LevelSelectUI : MonoBehaviour
{
    [Header("Scene Names")]
    [SerializeField] private string level1Scene = "Level1Scene";
    [SerializeField] private string level2Scene = "Level2Scene";
    [SerializeField] private string level3Scene = "Level3Scene";
    [SerializeField] private string level5Scene = "Level5Scene";
    [SerializeField] private string level4Scene = "Level4Scene";

    [Header("Button References")]
    [SerializeField] private Button[] levelButtons;

    [Header("Style (matches Main Menu)")]
    [SerializeField] private TMP_FontAsset buttonFont;
    [SerializeField] private Sprite buttonSprite; // Added to match GameManager theme
    [SerializeField] private Color buttonTextColor = new Color(0.2f, 0.2f, 0.2f, 1f);
    [SerializeField] private int buttonFontSize = 32;
    [SerializeField] private FontStyles buttonFontStyle = FontStyles.Bold;
    [SerializeField] private Color buttonImageColor = Color.white;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color highlightedColor = new Color(0.953f, 0.859f, 0.526f, 1f);
    [SerializeField] private Color pressedColor = new Color(0.784f, 0.784f, 0.784f, 1f);
    [SerializeField] private Color selectedColor = new Color(0.961f, 0.961f, 0.961f, 1f);
    [SerializeField] private Color disabledColor = new Color(0.784f, 0.784f, 0.784f, 0.5f);
    [SerializeField] private float colorMultiplier = 1f;
    [SerializeField] private float fadeDuration = 0.1f;

    public void LoadLevel1() => LoadScene(level1Scene);
    public void LoadLevel2() => LoadScene(level2Scene);
    public void LoadLevel3() => LoadScene(level3Scene);
    public void LoadLevel4() => LoadScene(level4Scene);
    public void LoadLevel5() => LoadScene(level5Scene); // Temporarily commented out

    private void Awake()
    {
        CaptureButtonsFromChildren();
        ApplyButtonStyle();
        WireButtonCallbacks();
        CreateBackButton();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (buttonFont == null)
        {
            buttonFont = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Fonts/UncialAntiqua-Regular SDF.asset");
        }
        if (buttonSprite == null)
        {
            buttonSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Images/button.png");
        }

        CaptureButtonsFromChildren();
        ApplyButtonStyle();
        WireButtonCallbacks();
    }
#endif

    private void CaptureButtonsFromChildren()
    {
        // Always refresh to pick up new buttons (e.g., Level5) without relying on serialized order.
        levelButtons = GetComponentsInChildren<Button>(true);
    }

    private void ApplyButtonStyle()
    {
        if (levelButtons == null) return;

        foreach (var button in levelButtons)
        {
            ApplyStyleToButton(button);
        }
    }

    private void ApplyStyleToButton(Button button)
    {
        if (button == null) return;

        var colors = button.colors;
        colors.normalColor = normalColor;
        colors.highlightedColor = highlightedColor;
        colors.pressedColor = pressedColor;
        colors.selectedColor = selectedColor;
        colors.disabledColor = disabledColor;
        colors.colorMultiplier = colorMultiplier;
        colors.fadeDuration = fadeDuration;
        button.colors = colors;

        if (button.targetGraphic is Image image)
        {
            if (buttonSprite != null)
            {
                image.sprite = buttonSprite;
                image.type = Image.Type.Sliced;
                image.pixelsPerUnitMultiplier = 1f;
                image.color = Color.white; // Reset color to white to show sprite
            }
            else
            {
                image.color = buttonImageColor;
            }
        }

        var text = button.GetComponentInChildren<TMP_Text>(true);
        if (text != null)
        {
            if (buttonFont != null)
            {
                text.font = buttonFont;
            }
            else if (GameManager.Instance != null)
            {
                text.font = GameManager.Instance.GetUpperUiFont();
            }

            text.color = buttonTextColor;
            text.fontSize = buttonFontSize;
            text.fontStyle = buttonFontStyle;
        }
    }

    private void WireButtonCallbacks()
    {
        if (levelButtons == null) return;

        // Prefer name-based wiring to avoid mis-ordered button arrays in the scene.
        AssignButtonByName("Level1Button", LoadLevel1);
        AssignButtonByName("Level2Button", LoadLevel2);
        AssignButtonByName("Level3Button", LoadLevel3);
        AssignButtonByName("Level4Button", LoadLevel4);
        AssignButtonByName("Level5Button", LoadLevel5);
    }

    private void AssignButtonHandler(int index, UnityAction handler)
    {
        if (index < 0 || index >= levelButtons.Length) return;
        var button = levelButtons[index];
        if (button == null) return;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(handler);
    }

    private void AssignButtonByName(string buttonName, UnityAction handler)
    {
        if (string.IsNullOrEmpty(buttonName) || levelButtons == null) return;
        foreach (var button in levelButtons)
        {
            if (button == null) continue;
            if (!button.name.Equals(buttonName, StringComparison.OrdinalIgnoreCase)) continue;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(handler);
            break;
        }
    }

    private void CreateBackButton()
    {
        string btnName = "BackToMainMenuButton";
        // Check globally if button exists to avoid duplicates if we reparented it out of this transform
        if (GameObject.Find(btnName) != null) return;

        GameObject buttonGO = new GameObject(btnName);
        
        // Find the root Canvas to ensure the button is on the screen and not clipped/hidden by parent rects
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            canvas = UnityEngine.Object.FindObjectOfType<Canvas>();
        }

        if (canvas != null)
        {
            buttonGO.transform.SetParent(canvas.transform, false);
            buttonGO.transform.SetAsLastSibling(); // Ensure it renders on top of everything
        }
        else
        {
            buttonGO.transform.SetParent(transform, false);
            Debug.LogWarning("LevelSelectUI: Could not find a Canvas. Back button might not be visible.");
        }

        // Add Image component first so Button can find it as target graphic
        Image image = buttonGO.AddComponent<Image>();
        
        Button button = buttonGO.AddComponent<Button>();
        
        // RectTransform setup - Bottom Right
        RectTransform rect = buttonGO.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(1f, 0f);
        rect.sizeDelta = new Vector2(280f, 80f);
        // Position with safe padding from the corner
        rect.anchoredPosition = new Vector2(-50f, 50f); 

        // Add Text
        GameObject textGO = new GameObject("Text");
        textGO.transform.SetParent(buttonGO.transform, false);
        RectTransform textRect = textGO.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        TextMeshProUGUI text = textGO.AddComponent<TextMeshProUGUI>();
        text.text = "Back";
        text.alignment = TextAlignmentOptions.Center;
        text.enableAutoSizing = true;
        text.fontSizeMin = 18f;
        text.fontSizeMax = buttonFontSize;

        // Apply Theme from GameManager to match Options button
        if (GameManager.Instance != null)
        {
            ApplyGameManagerTheme(button, text, image);
        }
        else
        {
            // Fallback to local style if GM is missing
            ApplyStyleToButton(button);
        }

        // Add Listener
        button.onClick.AddListener(LoadMainMenu);
        
        Debug.Log($"Back button created on Canvas: {(canvas != null ? canvas.name : "null")}");
    }

    private void ApplyGameManagerTheme(Button button, TextMeshProUGUI text, Image image)
    {
        if (GameManager.Instance == null) return;

        // Font
        var font = GameManager.Instance.GetUpperUiFont();
        if (font != null) text.font = font;

        // Text Color
        text.color = GameManager.Instance.ThemeButtonTextColor;

        // Sprite
        if (GameManager.Instance.ThemeButtonSprite != null)
        {
            image.sprite = GameManager.Instance.ThemeButtonSprite;
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 1f;
            image.color = Color.white;
        }
        else
        {
            image.color = buttonImageColor;
        }

        // Colors
        var colors = button.colors;
        colors.normalColor = GameManager.Instance.ThemeButtonNormalColor;
        colors.highlightedColor = GameManager.Instance.ThemeButtonHighlightedColor;
        colors.pressedColor = GameManager.Instance.ThemeButtonPressedColor;
        colors.selectedColor = GameManager.Instance.ThemeButtonSelectedColor;
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.1f;
        button.colors = colors;
    }

    public void LoadMainMenu() => LoadScene("MainMenu");

    private void LoadScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogWarning("LevelSelectUI: Scene name is empty.");
            return;
        }

        SceneManager.LoadScene(sceneName);
    }
}
