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
    [SerializeField] private Sprite buttonSprite; 
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
    public void LoadLevel5() => LoadScene(level5Scene); 

    private void Awake()
    {
        CaptureButtonsFromChildren();
        ApplyButtonStyle();
        WireButtonCallbacks();
        EnforceButtonSpacing();
        CreateBackButton();
    }

    private void EnforceButtonSpacing()
    {
        
        Button btn1 = FindButtonByName("Level1Button");
        Button btn2 = FindButtonByName("Level2Button");
        Button btn3 = FindButtonByName("Level3Button");
        Button btn4 = FindButtonByName("Level4Button");
        Button btn5 = FindButtonByName("Level5Button");

        
        if (btn1 != null && btn2 != null)
        {
            
            
            RectTransform rt1 = btn1.GetComponent<RectTransform>();
            RectTransform rt2 = btn2.GetComponent<RectTransform>();
            
            if (rt1 != null && rt2 != null)
            {
                Vector2 startPos = rt1.anchoredPosition;
                Vector2 spacing = rt2.anchoredPosition - rt1.anchoredPosition;

                
                ApplySpacingToButton(btn3, rt2.anchoredPosition + spacing);
                ApplySpacingToButton(btn4, rt2.anchoredPosition + (spacing * 2));
                ApplySpacingToButton(btn5, rt2.anchoredPosition + (spacing * 3));
            }
        }
    }

    private void ApplySpacingToButton(Button btn, Vector2 newPos)
    {
        if (btn != null)
        {
            RectTransform rt = btn.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchoredPosition = newPos;
            }
        }
    }

    private Button FindButtonByName(string name)
    {
        if (levelButtons == null) return null;
        foreach (var btn in levelButtons)
        {
            if (btn == null) continue;
            if (btn.name.Equals(name, StringComparison.OrdinalIgnoreCase)) return btn;
        }
        return null;
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
                image.color = Color.white; 
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
        
        if (GameObject.Find(btnName) != null) return;

        GameObject buttonGO = new GameObject(btnName);
        
        
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            canvas = UnityEngine.Object.FindObjectOfType<Canvas>();
        }

        if (canvas != null)
        {
            buttonGO.transform.SetParent(canvas.transform, false);
            buttonGO.transform.SetAsLastSibling(); 
        }
        else
        {
            buttonGO.transform.SetParent(transform, false);
            Debug.LogWarning("LevelSelectUI: Could not find a Canvas. Back button might not be visible.");
        }

        
        Image image = buttonGO.AddComponent<Image>();
        
        Button button = buttonGO.AddComponent<Button>();
        
        
        RectTransform rect = buttonGO.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(1f, 0f);
        rect.sizeDelta = new Vector2(280f, 80f);
        
        rect.anchoredPosition = new Vector2(-50f, 50f); 

        
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

        
        if (GameManager.Instance != null)
        {
            ApplyGameManagerTheme(button, text, image);
        }
        else
        {
            
            ApplyStyleToButton(button);
        }

        
        button.onClick.AddListener(LoadMainMenu);
        
        Debug.Log($"Back button created on Canvas: {(canvas != null ? canvas.name : "null")}");
    }

    private void ApplyGameManagerTheme(Button button, TextMeshProUGUI text, Image image)
    {
        if (GameManager.Instance == null) return;

        
        var font = GameManager.Instance.GetUpperUiFont();
        if (font != null) text.font = font;

        
        text.color = GameManager.Instance.ThemeButtonTextColor;

        
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
