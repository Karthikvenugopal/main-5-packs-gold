using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Button))]
public class ButtonHoverScaler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField, Min(1f)] private float hoverScaleMultiplier = 1.08f;
    [SerializeField, Min(0f)] private float transitionDuration = 0.15f;
    [SerializeField] private AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private bool useUnscaledTime = true;

    private Vector3 _baseScale = Vector3.one;
    private Coroutine _animationRoutine;
    private bool _baseScaleInitialized;

    private void Awake()
    {
        CacheBaseScale();
    }

    private void OnEnable()
    {
        CacheBaseScale();
        ResetScale();
    }

    private void OnDisable()
    {
        ResetScale();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        AnimateTo(_baseScale * hoverScaleMultiplier);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        AnimateTo(_baseScale);
    }

    private void CacheBaseScale()
    {
        if (_baseScaleInitialized)
        {
            return;
        }

        var current = transform.localScale;
        _baseScale = current == Vector3.zero ? Vector3.one : current;
        _baseScaleInitialized = true;
    }

    private void ResetScale()
    {
        if (!_baseScaleInitialized)
        {
            return;
        }

        if (_animationRoutine != null)
        {
            StopCoroutine(_animationRoutine);
            _animationRoutine = null;
        }

        transform.localScale = _baseScale;
    }

    private void AnimateTo(Vector3 targetScale)
    {
        if (!gameObject.activeInHierarchy)
        {
            transform.localScale = targetScale;
            return;
        }

        if (_animationRoutine != null)
        {
            StopCoroutine(_animationRoutine);
        }

        _animationRoutine = StartCoroutine(AnimateScale(targetScale));
    }

    private IEnumerator AnimateScale(Vector3 targetScale)
    {
        float duration = Mathf.Max(0.01f, transitionDuration);
        Vector3 initialScale = transform.localScale;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float normalizedTime = Mathf.Clamp01(elapsed / duration);
            float easedT = transitionCurve != null ? transitionCurve.Evaluate(normalizedTime) : normalizedTime;
            transform.localScale = Vector3.LerpUnclamped(initialScale, targetScale, easedT);
            elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            yield return null;
        }

        transform.localScale = targetScale;
        _animationRoutine = null;
    }
}
