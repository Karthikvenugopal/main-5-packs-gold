using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class LevelSelectUI : MonoBehaviour
{
    [Header("Scene Names")]
    [SerializeField] private string level1Scene = "Level1Scene";
    [SerializeField] private string level2Scene = "Level2Scene";
    [SerializeField] private string level3Scene = "Level3Scene";
    [SerializeField] private string level5Scene = "Level5Scene";

    [Header("Button References")]
    [SerializeField] private Button[] levelButtons;

    [Header("Style (matches Main Menu)")]
    [SerializeField] private TMP_FontAsset buttonFont;
    [SerializeField] private Color buttonTextColor = Color.white;
    [SerializeField] private int buttonFontSize = 32;
    [SerializeField] private FontStyles buttonFontStyle = FontStyles.Bold;
    [SerializeField] private Color buttonImageColor = new Color(0.8156863f, 0.1254902f, 0.5647059f, 1f);
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color highlightedColor = new Color(0.9528302f, 0.85927933f, 0.5258544f, 1f);
    [SerializeField] private Color pressedColor = new Color(0.78431374f, 0.78431374f, 0.78431374f, 1f);
    [SerializeField] private Color selectedColor = new Color(0.9607843f, 0.9607843f, 0.9607843f, 1f);
    [SerializeField] private Color disabledColor = new Color(0.78431374f, 0.78431374f, 0.78431374f, 0.5019608f);
    [SerializeField] private float colorMultiplier = 1f;
    [SerializeField] private float fadeDuration = 0.1f;

    public void LoadLevel1() => LoadScene(level1Scene);
    public void LoadLevel2() => LoadScene(level2Scene);
    public void LoadLevel3() => LoadScene(level3Scene);
    public void LoadLevel5() => LoadScene(level5Scene);

    private void Awake()
    {
        CaptureButtonsFromChildren();
        ApplyButtonStyle();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        CaptureButtonsFromChildren();
        ApplyButtonStyle();
    }
#endif

    private void CaptureButtonsFromChildren()
    {
        if (levelButtons != null && levelButtons.Length > 0) return;
        levelButtons = GetComponentsInChildren<Button>(true);
    }

    private void ApplyButtonStyle()
    {
        if (levelButtons == null) return;

        foreach (var button in levelButtons)
        {
            if (button == null) continue;

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
                image.color = buttonImageColor;
            }

            var text = button.GetComponentInChildren<TMP_Text>(true);
            if (text != null)
            {
                if (buttonFont != null)
                {
                    text.font = buttonFont;
                }

                text.color = buttonTextColor;
                text.fontSize = buttonFontSize;
                text.fontStyle = buttonFontStyle;
            }
        }
    }

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

