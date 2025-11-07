using UnityEngine;
using System.Collections;

/// <summary>
/// Adds a glowing/highlight effect to a player sprite. Can be controlled externally.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class PlayerGlowEffect : MonoBehaviour
{
    [Header("Glow Settings")]
    [Tooltip("Auto-detect player role and set glow values accordingly")]
    [SerializeField] private bool autoDetectRole = true;

    [Tooltip("Color of the glow effect (auto-set based on role if autoDetectRole is enabled)")]
    [SerializeField] private Color glowColor = Color.yellow;

    [Tooltip("Speed of the pulsing glow effect")]
    [SerializeField] private float glowSpeed = 1.5f;

    [Tooltip("Minimum glow intensity (0 = no glow, 1 = full glow)")]
    [SerializeField] private float minGlowIntensity = 0.2f;

    [Tooltip("Maximum glow intensity (0 = no glow, 1 = full glow)")]
    [SerializeField] private float maxGlowIntensity = 0.8f;

    [Tooltip("Size multiplier for the glow effect (makes player slightly bigger when glowing)")]
    [SerializeField] private float glowSizeMultiplier = 1.1f;

    private SpriteRenderer _spriteRenderer;
    private Color _originalColor;
    private Vector3 _originalScale;
    private Coroutine _glowCoroutine;
    private bool _isGlowing = false;

    private void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        if (_spriteRenderer == null)
        {
            Debug.LogError("PlayerGlowEffect: SpriteRenderer component not found!", this);
            return;
        }

        _originalColor = _spriteRenderer.color;
        _originalScale = transform.localScale;

        // Auto-detect player role and set glow values
        if (autoDetectRole)
        {
            SetupGlowByRole();
        }
    }

    private void SetupGlowByRole()
    {
        CoopPlayerController playerController = GetComponent<CoopPlayerController>();
        if (playerController == null)
        {
            // Try parent if not on this GameObject
            playerController = GetComponentInParent<CoopPlayerController>();
        }

        if (playerController != null)
        {
            if (playerController.Role == PlayerRole.Fireboy)
            {
                // Fireboy: #FFEB79 (255, 235, 121)
                glowColor = new Color(1f, 0.922f, 0.475f, 1f);
                glowSpeed = 1.5f;
                minGlowIntensity = 0.2f;
            }
            else if (playerController.Role == PlayerRole.Watergirl)
            {
                // Watergirl: #41FF2C (65, 255, 44)
                glowColor = new Color(0.255f, 1f, 0.173f, 1f);
                glowSpeed = 1.5f;
                minGlowIntensity = 0.2f;
            }
        }
    }

    /// <summary>
    /// Start the glow effect
    /// </summary>
    public void StartGlow()
    {
        if (_isGlowing || _spriteRenderer == null) return;

        _isGlowing = true;
        if (_glowCoroutine != null)
        {
            StopCoroutine(_glowCoroutine);
        }
        _glowCoroutine = StartCoroutine(GlowEffect());
    }

    /// <summary>
    /// Stop the glow effect
    /// </summary>
    public void StopGlow()
    {
        if (!_isGlowing || _spriteRenderer == null) return;

        _isGlowing = false;
        if (_glowCoroutine != null)
        {
            StopCoroutine(_glowCoroutine);
            _glowCoroutine = null;
        }

        // Reset to original appearance
        _spriteRenderer.color = _originalColor;
        transform.localScale = _originalScale;
    }

    private IEnumerator GlowEffect()
    {
        while (_isGlowing)
        {
            // Calculate pulse value using PingPong (oscillates between 0 and 1)
            float pulse = Mathf.PingPong(Time.time * glowSpeed, 1f);

            // Interpolate between min and max intensity
            float intensity = Mathf.Lerp(minGlowIntensity, maxGlowIntensity, pulse);

            // Blend the original color with the glow color based on intensity
            Color blendedColor = Color.Lerp(_originalColor, glowColor, intensity);
            _spriteRenderer.color = blendedColor;

            // Scale effect (subtle pulsing size)
            float sizePulse = 1f + (intensity * 0.1f * (glowSizeMultiplier - 1f));
            transform.localScale = _originalScale * sizePulse;

            yield return null;
        }
    }

    private void OnDestroy()
    {
        StopGlow();
    }
}

