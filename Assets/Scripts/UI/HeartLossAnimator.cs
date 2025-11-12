using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HeartLossAnimator : MonoBehaviour
{
    [Header("Hierarchy References")]
    [SerializeField] private Transform heartsMasterContainer;
    [SerializeField] private Transform emberHeartsContainer;
    [SerializeField] private Transform aquaHeartsContainer;
    [SerializeField] private string heartsMasterName = "HeartsMasterContainer";
    [SerializeField] private string emberContainerName = "EmberHeartsContainer";
    [SerializeField] private string aquaContainerName = "AquaHeartsContainer";

    [Header("Animation Settings")]
    [SerializeField] private float popScaleMultiplier = 1.6f;
    [SerializeField] private float popDuration = 0.12f;
    [SerializeField] private float highlightDuration = 0.18f;
    [SerializeField] private float shatterDuration = 0.16f;
    [SerializeField, Range(0f, 1f)] private float highlightBlend = 0.7f;
    [SerializeField] private Color highlightColor = new Color(1f, 0.85f, 0.35f, 1f);
    [SerializeField] private ParticleSystem shatterEffectPrefab;

    private readonly List<GameObject> emberHearts = new();
    private readonly List<GameObject> aquaHearts = new();
    private readonly Dictionary<GameObject, HeartVisual> heartLookup = new();

    public event Action HeartAnimationFinished;

    public IReadOnlyList<GameObject> EmberHearts => emberHearts;
    public IReadOnlyList<GameObject> AquaHearts => aquaHearts;

    public bool IsAnimatingHeart(GameObject heart)
    {
        if (heart == null) return false;
        return heartLookup.TryGetValue(heart, out var visual) && visual.IsAnimating;
    }

    private void Awake()
    {
        AutoAssignContainers();
        CacheHearts();
    }

    private void Reset()
    {
        AutoAssignContainers();
        CacheHearts();
    }

    public void LoseHeart(bool isEmber, int heartIndex)
    {
        var list = isEmber ? emberHearts : aquaHearts;
        if (list.Count == 0)
        {
            Debug.LogWarning("HeartLossAnimator: No hearts cached for " + (isEmber ? "ember" : "aqua") + " container.");
            return;
        }
        if (heartIndex < 0 || heartIndex >= list.Count)
        {
            Debug.LogWarning($"HeartLossAnimator: heartIndex {heartIndex} is out of range for {(isEmber ? "ember" : "aqua")} hearts.");
            return;
        }

        var heartGO = list[heartIndex];
        if (!heartGO.activeInHierarchy)
        {
            return;
        }

        if (!heartLookup.TryGetValue(heartGO, out var heartVisual))
        {
            Debug.LogWarning("HeartLossAnimator: Could not resolve heart visuals for " + heartGO.name);
            return;
        }

        if (!gameObject.activeInHierarchy)
        {
            return;
        }

        if (!heartVisual.IsAnimating)
        {
            heartVisual.ShowRenderer();
            StartCoroutine(PlayHeartLoss(heartVisual));
        }
    }

    private IEnumerator PlayHeartLoss(HeartVisual heart)
    {
        heart.IsAnimating = true;
        var defaultScale = heart.DefaultScale;

        yield return AnimateScale(heart.Transform, defaultScale, defaultScale * popScaleMultiplier, popDuration);

        if (heart.SupportsColor && highlightDuration > 0f)
        {
            var baseColor = heart.DefaultColor;
            var targetColor = Color.Lerp(baseColor, highlightColor, highlightBlend);
            yield return AnimateColor(heart, baseColor, targetColor, highlightDuration * 0.5f);
            yield return AnimateColor(heart, targetColor, baseColor, highlightDuration * 0.5f);
        }

        if (shatterEffectPrefab != null)
        {
            var fx = Instantiate(shatterEffectPrefab, heart.Transform.position, Quaternion.identity, heart.Transform.parent);
            Destroy(fx.gameObject, fx.main.duration + fx.main.startLifetime.constantMax);
        }

        yield return AnimateScale(heart.Transform, heart.Transform.localScale, Vector3.zero, shatterDuration);

        if (heart.HasRenderer)
        {
            heart.HideRenderer();
        }
        else
        {
            heart.Root.SetActive(false);
        }

        heart.Transform.localScale = defaultScale;
        heart.ResetColor();
        heart.IsAnimating = false;
        HeartAnimationFinished?.Invoke();
    }

    private IEnumerator AnimateScale(Transform target, Vector3 from, Vector3 to, float duration)
    {
        if (duration <= 0f)
        {
            target.localScale = to;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            float t = Mathf.Clamp01(elapsed / duration);
            target.localScale = Vector3.LerpUnclamped(from, to, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        target.localScale = to;
    }

    private IEnumerator AnimateColor(HeartVisual heart, Color from, Color to, float duration)
    {
        if (duration <= 0f)
        {
            heart.SetColor(to);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            float t = Mathf.Clamp01(elapsed / duration);
            heart.SetColor(Color.LerpUnclamped(from, to, t));
            elapsed += Time.deltaTime;
            yield return null;
        }

        heart.SetColor(to);
    }

    private void CacheHearts()
    {
        emberHearts.Clear();
        aquaHearts.Clear();
        heartLookup.Clear();
        PopulateHeartList(emberHeartsContainer, emberHearts);
        PopulateHeartList(aquaHeartsContainer, aquaHearts);
    }

    private void PopulateHeartList(Transform container, List<GameObject> list)
    {
        if (container == null)
        {
            return;
        }

        var buffer = new List<Transform>();
        foreach (Transform child in container)
        {
            if (child.name.StartsWith("Heart", StringComparison.OrdinalIgnoreCase))
            {
                buffer.Add(child);
            }
        }

        buffer.Sort((a, b) => ExtractHeartIndex(a.name).CompareTo(ExtractHeartIndex(b.name)));

        foreach (var child in buffer)
        {
            var go = child.gameObject;
            list.Add(go);
            heartLookup[go] = new HeartVisual(go);
        }
    }

    private static int ExtractHeartIndex(string name)
    {
        var parts = name.Split('_');
        if (parts.Length > 1 && int.TryParse(parts[parts.Length - 1], out var index))
        {
            return index;
        }

        return int.MaxValue;
    }

    private void AutoAssignContainers()
    {
        if (heartsMasterContainer == null)
        {
            heartsMasterContainer = FindChildRecursive(transform, heartsMasterName) ?? transform;
        }

        if (emberHeartsContainer == null && heartsMasterContainer != null)
        {
            emberHeartsContainer = FindChildRecursive(heartsMasterContainer, emberContainerName);
        }

        if (aquaHeartsContainer == null && heartsMasterContainer != null)
        {
            aquaHeartsContainer = FindChildRecursive(heartsMasterContainer, aquaContainerName);
        }
    }

    private Transform FindChildRecursive(Transform parent, string childName)
    {
        if (parent == null || string.IsNullOrEmpty(childName))
        {
            return null;
        }

        foreach (Transform child in parent)
        {
            if (child.name == childName)
            {
                return child;
            }

            var nested = FindChildRecursive(child, childName);
            if (nested != null)
            {
                return nested;
            }
        }

        return null;
    }

    [System.Serializable]
    private sealed class HeartVisual
    {
        public GameObject Root { get; }
        public Transform Transform { get; }
        public Vector3 DefaultScale { get; }
        public Color DefaultColor { get; }
        public bool SupportsColor => _image != null || _spriteRenderer != null;
        public bool IsAnimating { get; set; }

        private readonly Image _image;
        private readonly SpriteRenderer _spriteRenderer;

        public HeartVisual(GameObject root)
        {
            Root = root;
            Transform = root.transform;
            _image = root.GetComponent<Image>();
            _spriteRenderer = root.GetComponent<SpriteRenderer>();
            DefaultScale = Transform.localScale;
            if (_image != null)
            {
                DefaultColor = _image.color;
            }
            else if (_spriteRenderer != null)
            {
                DefaultColor = _spriteRenderer.color;
            }
            else
            {
                DefaultColor = Color.white;
            }
        }

        public void SetColor(Color color)
        {
            if (_image != null)
            {
                _image.color = color;
            }
            else if (_spriteRenderer != null)
            {
                _spriteRenderer.color = color;
            }
        }

        public bool HasRenderer => _image != null || _spriteRenderer != null;

        public void ResetColor()
        {
            SetColor(DefaultColor);
        }

        public void HideRenderer()
        {
            if (_image != null)
            {
                _image.enabled = false;
            }
            else if (_spriteRenderer != null)
            {
                _spriteRenderer.enabled = false;
            }
        }

        public void ShowRenderer()
        {
            if (_image != null)
            {
                _image.enabled = true;
            }
            else if (_spriteRenderer != null)
            {
                _spriteRenderer.enabled = true;
            }
        }
    }
}
