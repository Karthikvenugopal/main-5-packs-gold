using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

// Runtime HUD to show a bottom-right Retry button on level scenes.
// Clicking reloads the current scene. LevelTimer already logs a failure
// on destroy, so analytics remain correct.
public class RetryHud : MonoBehaviour
{
    // Use a low sorting order so the instruction overlay (black popup)
    // renders above this HUD and masks the button until gameplay starts.
    private const int SortingOrder = -10;
    private static readonly Color ButtonColor = new Color(0.2f, 0.45f, 0.9f, 1f);

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
        if (!IsGameplayLevel(name)) return;
        if (FindObjectOfType<RetryHud>() != null) return;
        var host = new GameObject("__RetryHUD");
        Object.DontDestroyOnLoad(host);
        host.AddComponent<RetryHud>().CreateUI();
    }

    private static bool IsGameplayLevel(string name)
    {
        if (string.IsNullOrEmpty(name)) return false;
        name = name.ToLowerInvariant();
        return name == "level1scene" || name == "level2scene" || name == "level3scene" || name == "level1" || name == "level2" || name == "level3";
    }

    private Canvas _canvas;
    private Button _button;

    private void CreateUI()
    {
        var canvasGO = new GameObject("RetryCanvas");
        canvasGO.transform.SetParent(transform, false);
        _canvas = canvasGO.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = SortingOrder;
        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();

        var btnGO = new GameObject("RetryButton");
        btnGO.transform.SetParent(canvasGO.transform, false);
        var rect = btnGO.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(1f, 0f);
        rect.anchoredPosition = new Vector2(-20f, 20f);
        rect.sizeDelta = new Vector2(160f, 60f);

        var img = btnGO.AddComponent<Image>();
        img.color = ButtonColor;
        _button = btnGO.AddComponent<Button>();
        _button.onClick.AddListener(RestartScene);

        var labelGO = new GameObject("Label");
        labelGO.transform.SetParent(btnGO.transform, false);
        var labelRect = labelGO.AddComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        var tmp = labelGO.AddComponent<TextMeshProUGUI>();
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontSize = 28f;
        tmp.color = Color.white;
        tmp.raycastTarget = false;
        tmp.text = "Restart";
    }

    private void RestartScene()
    {
        var current = SceneManager.GetActiveScene();
        SceneManager.LoadScene(current.name);
    }
}
