using UnityEngine;
using TMPro;
using System;

/// <summary>
/// Displays a world-space instruction text in the tutorial maze.
/// Shows the text when the scene starts and can optionally hide it after a delay or when triggered.
/// </summary>
public class TutorialInstructionText : MonoBehaviour
{
    // Event fired when instruction is hidden
    public event Action OnInstructionHidden;

    [Header("Text Settings")]
    [Tooltip("The instruction text to display")]
    [TextArea(2, 4)]
    [SerializeField] private string instructionText = "Press W,A,S,D to move";

    [Tooltip("Color of the text")]
    [SerializeField] private Color textColor = Color.white;

    [Tooltip("Font size")]
    [SerializeField] private float fontSize = 4f;

    [Tooltip("Text alignment")]
    [SerializeField] private TextAlignmentOptions alignment = TextAlignmentOptions.Center;

    [Tooltip("Enable word wrapping")]
    [SerializeField] private bool enableWordWrapping = false;

    [Tooltip("Text wrapping mode")]
    [SerializeField] private TextWrappingModes textWrappingMode = TextWrappingModes.Normal;

    [Tooltip("Text area width (0 = auto-size)")]
    [SerializeField] private float textWidth = 0f;

    [Tooltip("Text area height (0 = auto-size)")]
    [SerializeField] private float textHeight = 0f;

    [Header("Display Settings")]
    [Tooltip("Show the text immediately when the scene starts")]
    [SerializeField] private bool showOnStart = true;

    [Tooltip("Hide the text after this many seconds (0 = never hide)")]
    [SerializeField] private float hideAfterSeconds = 0f;

    [Tooltip("Fade out duration when hiding (in seconds)")]
    [SerializeField] private float fadeOutDuration = 1f;

    [Header("Auto-Hide on Input")]
    [Tooltip("Automatically hide when WASD keys are pressed")]
    [SerializeField] private bool hideOnWASD = false;

    [Tooltip("Automatically hide when Arrow keys are pressed")]
    [SerializeField] private bool hideOnArrowKeys = false;

    [Tooltip("Delay before hiding when input is detected (in seconds)")]
    [SerializeField] private float hideDelayAfterInput = 1f;

    [Header("Text Glow Effect")]
    [Tooltip("Enable pulsing glow effect on the text while it's visible")]
    [SerializeField] private bool enableTextGlow = false;

    [Tooltip("Color of the glow effect (will blend with original text color)")]
    [SerializeField] private Color glowColor = Color.yellow;

    [Tooltip("Speed of the pulsing glow effect")]
    [SerializeField] private float glowSpeed = 2f;

    [Tooltip("Minimum glow intensity (0 = no glow, 1 = full glow)")]
    [SerializeField] private float minGlowIntensity = 0.3f;

    [Tooltip("Maximum glow intensity (0 = no glow, 1 = full glow)")]
    [SerializeField] private float maxGlowIntensity = 0.8f;

    [Header("Player Glow Effect")]
    [Tooltip("Optional: Player GameObject to apply glow effect to when this instruction is visible. If not set, will auto-find Fireboy for WASD instructions or Watergirl for Arrow instructions.")]
    [SerializeField] private GameObject targetPlayer;

    [Tooltip("Auto-detect player by role (Fireboy for WASD, Watergirl for Arrows)")]
    [SerializeField] private bool autoDetectPlayer = true;

    private TextMeshPro _textMesh;
    private Color _originalColor;
    private bool _isVisible = true;
    private bool _isHiding = false;
    private bool _hasFiredEvent = false;
    private Coroutine _fadeCoroutine;
    private Coroutine _textGlowCoroutine;
    private PlayerGlowEffect _playerGlow;

    private void Start()
    {
        SetupText();
        
        // Delay player glow setup to ensure players have spawned
        if (autoDetectPlayer && targetPlayer == null)
        {
            StartCoroutine(SetupPlayerGlowDelayed());
        }
        else
        {
            SetupPlayerGlow();
        }

        if (showOnStart)
        {
            Show();
        }
        else
        {
            Hide();
        }

        if (hideAfterSeconds > 0f)
        {
            Invoke(nameof(Hide), hideAfterSeconds);
        }
    }

    private System.Collections.IEnumerator SetupPlayerGlowDelayed()
    {
        // Wait a frame to ensure players have spawned
        yield return null;
        
        // Try to find player, wait a bit more if not found
        int attempts = 0;
        while (targetPlayer == null && attempts < 10)
        {
            FindTargetPlayer();
            if (targetPlayer == null)
            {
                yield return new WaitForSeconds(0.1f);
                attempts++;
            }
        }

        SetupPlayerGlow();
        
        // If we found the player and instruction should be visible, start glow
        if (_playerGlow != null && _isVisible)
        {
            _playerGlow.StartGlow();
        }
    }

    private void SetupPlayerGlow()
    {
        // Auto-detect player if enabled and target not set
        if (autoDetectPlayer && targetPlayer == null)
        {
            FindTargetPlayer();
        }

        if (targetPlayer == null) return;

        // Try to get PlayerGlowEffect component from the target player
        _playerGlow = targetPlayer.GetComponent<PlayerGlowEffect>();
        if (_playerGlow == null)
        {
            // Auto-add it if it doesn't exist
            _playerGlow = targetPlayer.AddComponent<PlayerGlowEffect>();
        }
    }

    private void FindTargetPlayer()
    {
        // Find players by role based on which keys this instruction uses
        PlayerRole? targetRole = null; // Don't default - be explicit

        if (hideOnArrowKeys && !hideOnWASD)
        {
            targetRole = PlayerRole.Watergirl;
        }
        else if (hideOnWASD && !hideOnArrowKeys)
        {
            targetRole = PlayerRole.Fireboy;
        }
        // If both are enabled or neither, don't auto-detect (return null)
        // This prevents instructions without input detection from incorrectly targeting a player

        if (!targetRole.HasValue)
        {
            // Can't determine target player from input flags, so don't set targetPlayer
            return;
        }

        // Search for player with matching role
        CoopPlayerController[] players = FindObjectsByType<CoopPlayerController>(FindObjectsSortMode.None);
        foreach (CoopPlayerController player in players)
        {
            if (player.Role == targetRole.Value)
            {
                targetPlayer = player.gameObject;
                break;
            }
        }
    }

    private void StopPlayerGlow()
    {
        // First try to use the cached reference
        if (_playerGlow != null)
        {
            _playerGlow.StopGlow();
            return;
        }

        // If cached reference is null, try to find the player and glow component
        // This handles cases where the glow setup coroutine hasn't completed yet
        if (targetPlayer != null)
        {
            PlayerGlowEffect glow = targetPlayer.GetComponent<PlayerGlowEffect>();
            if (glow != null)
            {
                glow.StopGlow();
            }
        }
        else if (autoDetectPlayer)
        {
            // Auto-detect player and stop glow
            PlayerRole? targetRole = null; // Don't default - be explicit

            if (hideOnArrowKeys && !hideOnWASD)
            {
                targetRole = PlayerRole.Watergirl;
            }
            else if (hideOnWASD && !hideOnArrowKeys)
            {
                targetRole = PlayerRole.Fireboy;
            }

            // Only stop glow if we can determine the target role
            if (targetRole.HasValue)
            {
                // Find player with matching role and stop glow
                CoopPlayerController[] players = FindObjectsByType<CoopPlayerController>(FindObjectsSortMode.None);
                foreach (CoopPlayerController player in players)
                {
                    if (player.Role == targetRole.Value)
                    {
                        PlayerGlowEffect glow = player.GetComponent<PlayerGlowEffect>();
                        if (glow != null)
                        {
                            glow.StopGlow();
                        }
                        break;
                    }
                }
            }
        }
    }

    private void Update()
    {
        if (!_isVisible || _isHiding) return;

        bool shouldHide = false;

        // Check for WASD input
        if (hideOnWASD)
        {
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.A) || 
                Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.D))
            {
                shouldHide = true;
            }
        }

        // Check for Arrow key input
        if (hideOnArrowKeys)
        {
            if (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.DownArrow) || 
                Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.RightArrow))
            {
                shouldHide = true;
            }
        }

        if (shouldHide && !_isHiding)
        {
            _isHiding = true;
            Invoke(nameof(Hide), hideDelayAfterInput);
        }
    }

    private void SetupText()
    {
        // Get or create TextMeshPro component
        _textMesh = GetComponent<TextMeshPro>();
        if (_textMesh == null)
        {
            _textMesh = gameObject.AddComponent<TextMeshPro>();
        }

        // Configure text
        _textMesh.text = instructionText;
        _textMesh.color = textColor;
        _textMesh.fontSize = fontSize;
        _textMesh.alignment = alignment;
        _textMesh.enableWordWrapping = enableWordWrapping;
        _textMesh.textWrappingMode = textWrappingMode;
        _textMesh.fontStyle = FontStyles.Bold;

        // Set text area size if specified (for world-space TextMeshPro)
        if (textWidth > 0f || textHeight > 0f)
        {
            Rect rect = _textMesh.rectTransform.rect;
            float width = textWidth > 0f ? textWidth : rect.width;
            float height = textHeight > 0f ? textHeight : rect.height;
            _textMesh.rectTransform.sizeDelta = new Vector2(width, height);
        }

        // Store original color for fade effect
        _originalColor = textColor;
    }

    /// <summary>
    /// Show the instruction text
    /// </summary>
    public void Show()
    {
        if (_textMesh == null) SetupText();
        
        _isVisible = true;
        _isHiding = false;
        _hasFiredEvent = false;
        _textMesh.color = _originalColor;
        _textMesh.gameObject.SetActive(true);

        // Start text glow effect
        if (enableTextGlow)
        {
            StartTextGlow();
        }

        // Start player glow effect
        if (_playerGlow != null)
        {
            _playerGlow.StartGlow();
        }
    }

    private void FireHiddenEvent()
    {
        if (!_hasFiredEvent)
        {
            _hasFiredEvent = true;
            OnInstructionHidden?.Invoke();
        }
    }

    /// <summary>
    /// Hide the instruction text (with optional fade out)
    /// </summary>
    public void Hide()
    {
        if (_textMesh == null || !_isVisible) return;

        _isVisible = false;
        _isHiding = false;

        // Stop text glow effect
        StopTextGlow();

        // Stop player glow effect immediately
        StopPlayerGlow();

        // Cancel any pending hide invokes
        CancelInvoke(nameof(Hide));
        CancelInvoke(nameof(StopPlayerGlow));

        // Stop any running fade coroutine
        if (_fadeCoroutine != null)
        {
            StopCoroutine(_fadeCoroutine);
            _fadeCoroutine = null;
        }

        if (fadeOutDuration > 0f)
        {
            _fadeCoroutine = StartCoroutine(FadeOut());
        }
        else
        {
            _textMesh.gameObject.SetActive(false);
            // Fire event when hidden (immediate)
            FireHiddenEvent();
        }
    }

    /// <summary>
    /// Update the instruction text programmatically
    /// </summary>
    public void SetText(string newText)
    {
        instructionText = newText;
        if (_textMesh != null)
        {
            _textMesh.text = instructionText;
        }
    }

    private void StartTextGlow()
    {
        if (_textGlowCoroutine != null)
        {
            StopCoroutine(_textGlowCoroutine);
        }
        _textGlowCoroutine = StartCoroutine(TextGlowEffect());
    }

    private void StopTextGlow()
    {
        if (_textGlowCoroutine != null)
        {
            StopCoroutine(_textGlowCoroutine);
            _textGlowCoroutine = null;
        }
        
        // Restore original color
        if (_textMesh != null)
        {
            _textMesh.color = _originalColor;
        }
    }

    private System.Collections.IEnumerator TextGlowEffect()
    {
        while (_isVisible && !_isHiding)
        {
            // Calculate pulse value using PingPong (oscillates between 0 and 1)
            float pulse = Mathf.PingPong(Time.time * glowSpeed, 1f);

            // Interpolate between min and max intensity
            float intensity = Mathf.Lerp(minGlowIntensity, maxGlowIntensity, pulse);

            // Blend the original color with the glow color based on intensity
            Color blendedColor = Color.Lerp(_originalColor, glowColor, intensity);
            _textMesh.color = blendedColor;

            yield return null;
        }
    }

    private System.Collections.IEnumerator FadeOut()
    {
        float timer = 0f;
        Color startColor = _textMesh.color;
        Color endColor = new Color(startColor.r, startColor.g, startColor.b, 0f);

        while (timer < fadeOutDuration)
        {
            timer += Time.deltaTime;
            _textMesh.color = Color.Lerp(startColor, endColor, timer / fadeOutDuration);
            yield return null;
        }

        _textMesh.gameObject.SetActive(false);
        _fadeCoroutine = null;

        // Fire event when fade completes
        FireHiddenEvent();
    }

    private void OnValidate()
    {
        // Update text in editor when values change
        if (_textMesh != null && Application.isPlaying)
        {
            SetupText();
        }
    }
}

