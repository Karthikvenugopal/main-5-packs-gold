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
    [SerializeField] private Color buttonTextColor = Color.white;
    [SerializeField] private int buttonFontSize = 32;
    [SerializeField] private FontStyles buttonFontStyle = FontStyles.Bold;
    [SerializeField] private Color buttonImageColor = Color.white;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color highlightedColor = new Color(0.8392157f, 0.7607843f, 1f, 1f);
    [SerializeField] private Color pressedColor = new Color(0.78431374f, 0.78431374f, 0.78431374f, 1f);
    [SerializeField] private Color selectedColor = new Color(0.9607843f, 0.9607843f, 0.9607843f, 1f);
    [SerializeField] private Color disabledColor = new Color(0.78431374f, 0.78431374f, 0.78431374f, 0.5019608f);
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
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        CaptureButtonsFromChildren();
        ApplyButtonStyle();
        WireButtonCallbacks();
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

    private void WireButtonCallbacks()
    {
        if (levelButtons == null) return;

        AssignButtonHandler(0, LoadLevel1);
        AssignButtonHandler(1, LoadLevel2);
        AssignButtonHandler(2, LoadLevel3);
        AssignButtonHandler(3, LoadLevel4);
    }

    private void AssignButtonHandler(int index, UnityAction handler)
    {
        if (index < 0 || index >= levelButtons.Length) return;
        var button = levelButtons[index];
        if (button == null) return;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(handler);
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
