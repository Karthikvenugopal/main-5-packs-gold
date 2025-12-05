using UnityEngine;
using System.Collections;




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
            
            playerController = GetComponentInParent<CoopPlayerController>();
        }

        if (playerController != null)
        {
            if (playerController.Role == PlayerRole.Fireboy)
            {
                
                glowColor = new Color(1f, 0.922f, 0.475f, 1f);
                glowSpeed = 1.5f;
                minGlowIntensity = 0.2f;
            }
            else if (playerController.Role == PlayerRole.Watergirl)
            {
                
                glowColor = new Color(0.255f, 1f, 0.173f, 1f);
                glowSpeed = 1.5f;
                minGlowIntensity = 0.2f;
            }
        }
    }

    
    
    
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

    
    
    
    public void StopGlow()
    {
        if (!_isGlowing || _spriteRenderer == null) return;

        _isGlowing = false;
        if (_glowCoroutine != null)
        {
            StopCoroutine(_glowCoroutine);
            _glowCoroutine = null;
        }

        
        _spriteRenderer.color = _originalColor;
        transform.localScale = _originalScale;
    }

    private IEnumerator GlowEffect()
    {
        while (_isGlowing)
        {
            
            float pulse = Mathf.PingPong(Time.time * glowSpeed, 1f);

            
            float intensity = Mathf.Lerp(minGlowIntensity, maxGlowIntensity, pulse);

            
            Color blendedColor = Color.Lerp(_originalColor, glowColor, intensity);
            _spriteRenderer.color = blendedColor;

            
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

