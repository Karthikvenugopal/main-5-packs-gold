using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Ensures every Button in the scene receives the ButtonHoverScaler behaviour.
internal sealed class ButtonHoverInstaller : MonoBehaviour
{
    private const float RescanInterval = 0.5f;
    private float _scanTimer = RescanInterval;
    private static bool _bootstrapComplete;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (_bootstrapComplete) return;
        _bootstrapComplete = true;
        var host = new GameObject("__ButtonHoverInstaller");
        host.hideFlags = HideFlags.HideAndDontSave;
        DontDestroyOnLoad(host);
        host.AddComponent<ButtonHoverInstaller>();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        ApplyHoverEffect();
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Update()
    {
        _scanTimer -= Time.unscaledDeltaTime;
        if (_scanTimer <= 0f)
        {
            _scanTimer = RescanInterval;
            ApplyHoverEffect();
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplyHoverEffect();
    }

    private static void ApplyHoverEffect()
    {
        var buttons = Object.FindObjectsOfType<Button>(true);
        foreach (var button in buttons)
        {
            if (button == null) continue;
            var go = button.gameObject;
            if (!go.scene.IsValid()) continue;
            if (go.GetComponent<ButtonHoverScaler>() == null)
            {
                go.AddComponent<ButtonHoverScaler>();
            }
        }
    }
}
