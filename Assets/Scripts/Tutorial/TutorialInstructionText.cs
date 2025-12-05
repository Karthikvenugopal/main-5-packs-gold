using UnityEngine;
using TMPro;
using System;





public class TutorialInstructionText : MonoBehaviour
{
    
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
        
        yield return null;
        
        
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
        
        
        if (_playerGlow != null && _isVisible)
        {
            _playerGlow.StartGlow();
        }
    }

    private System.Collections.IEnumerator CheckRendererDelayed()
    {
        yield return null; 
        
        if (_textMesh != null)
        {
            MeshRenderer renderer = _textMesh.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.sortingOrder = 10;
                renderer.enabled = true;
                Debug.Log($"[TutorialInstructionText] Renderer found after delay on {gameObject.name}, enabled: {renderer.enabled}");
            }
            else
            {
                Debug.LogError($"[TutorialInstructionText] Still no MeshRenderer found after delay on {gameObject.name}");
            }
        }
    }

    private void SetupPlayerGlow()
    {
        
        if (autoDetectPlayer && targetPlayer == null)
        {
            FindTargetPlayer();
        }

        if (targetPlayer == null) return;

        
        _playerGlow = targetPlayer.GetComponent<PlayerGlowEffect>();
        if (_playerGlow == null)
        {
            
            _playerGlow = targetPlayer.AddComponent<PlayerGlowEffect>();
        }
    }

    private void FindTargetPlayer()
    {
        
        PlayerRole? targetRole = null; 

        if (hideOnArrowKeys && !hideOnWASD)
        {
            targetRole = PlayerRole.Watergirl;
        }
        else if (hideOnWASD && !hideOnArrowKeys)
        {
            targetRole = PlayerRole.Fireboy;
        }
        
        

        if (!targetRole.HasValue)
        {
            
            return;
        }

        
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
        
        if (_playerGlow != null)
        {
            _playerGlow.StopGlow();
            return;
        }

        
        
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
            
            PlayerRole? targetRole = null; 

            if (hideOnArrowKeys && !hideOnWASD)
            {
                targetRole = PlayerRole.Watergirl;
            }
            else if (hideOnWASD && !hideOnArrowKeys)
            {
                targetRole = PlayerRole.Fireboy;
            }

            
            if (targetRole.HasValue)
            {
                
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

        
        if (hideOnWASD)
        {
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.A) || 
                Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.D))
            {
                shouldHide = true;
            }
        }

        
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
        
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }

        
        _textMesh = GetComponent<TextMeshPro>();
        if (_textMesh == null)
        {
            _textMesh = gameObject.AddComponent<TextMeshPro>();
            Debug.Log($"[TutorialInstructionText] Created TextMeshPro component on {gameObject.name}");
        }

        
        
        if (_textMesh.font == null)
        {
            
            if (TMP_Settings.defaultFontAsset != null)
            {
                _textMesh.font = TMP_Settings.defaultFontAsset;
                Debug.Log($"[TutorialInstructionText] Assigned default font to {gameObject.name}");
            }
            else
            {
                Debug.LogError($"[TutorialInstructionText] No default font asset found in TMP Settings for {gameObject.name}");
            }
        }
        
        
        
        if (_textMesh.font != null && _textMesh.fontSharedMaterial == null)
        {
            
            if (_textMesh.font.material != null)
            {
                _textMesh.fontSharedMaterial = _textMesh.font.material;
            }
        }

        
        _textMesh.text = instructionText;
        _textMesh.color = textColor;
        _textMesh.fontSize = fontSize;
        _textMesh.alignment = alignment;
        _textMesh.enableWordWrapping = enableWordWrapping;
        _textMesh.textWrappingMode = textWrappingMode;
        _textMesh.fontStyle = FontStyles.Bold;

        
        if (textWidth > 0f || textHeight > 0f)
        {
            Rect rect = _textMesh.rectTransform.rect;
            float width = textWidth > 0f ? textWidth : rect.width;
            float height = textHeight > 0f ? textHeight : rect.height;
            _textMesh.rectTransform.sizeDelta = new Vector2(width, height);
        }

        
        MeshRenderer renderer = _textMesh.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            renderer.sortingOrder = 10; 
            renderer.enabled = true; 
            Debug.Log($"[TutorialInstructionText] Renderer found on {gameObject.name}, sortingOrder: {renderer.sortingOrder}, enabled: {renderer.enabled}, material: {renderer.sharedMaterial != null}");
        }
        else
        {
            Debug.LogWarning($"[TutorialInstructionText] No MeshRenderer found on {gameObject.name} after creating TextMeshPro");
            
            StartCoroutine(CheckRendererDelayed());
        }

        
        _textMesh.ForceMeshUpdate();
        
        
        Debug.Log($"[TutorialInstructionText] SetupText complete for {gameObject.name}: text='{instructionText}', font={_textMesh.font != null}, position={transform.position}, active={gameObject.activeSelf}");

        
        _originalColor = textColor;
    }

    
    
    
    public void Show()
    {
        Debug.Log($"[TutorialInstructionText] Show() called for {gameObject.name}");
        
        
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
            Debug.Log($"[TutorialInstructionText] Activated GameObject {gameObject.name}");
        }

        if (_textMesh == null) SetupText();
        
        if (_textMesh == null)
        {
            Debug.LogError($"[TutorialInstructionText] TextMeshPro is null after SetupText for {gameObject.name}");
            return;
        }
        
        _isVisible = true;
        _isHiding = false;
        _hasFiredEvent = false;
        _textMesh.color = _originalColor;
        _textMesh.gameObject.SetActive(true);
        
        
        MeshRenderer renderer = _textMesh.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            renderer.enabled = true;
            Debug.Log($"[TutorialInstructionText] Enabled renderer on {gameObject.name}, sortingOrder: {renderer.sortingOrder}");
        }
        else
        {
            Debug.LogWarning($"[TutorialInstructionText] No MeshRenderer found when showing {gameObject.name}");
        }
        
        
        _textMesh.ForceMeshUpdate();
        
        
        Debug.Log($"[TutorialInstructionText] Show() complete for {gameObject.name}: text='{_textMesh.text}', font={_textMesh.font != null}, color={_textMesh.color}, position={transform.position}, bounds={_textMesh.bounds}");

        
        if (enableTextGlow)
        {
            StartTextGlow();
        }

        
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

    
    
    
    public void Hide()
    {
        if (_textMesh == null || !_isVisible) return;

        _isVisible = false;
        _isHiding = false;

        
        StopTextGlow();

        
        StopPlayerGlow();

        
        CancelInvoke(nameof(Hide));
        CancelInvoke(nameof(StopPlayerGlow));

        
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
            
            MeshRenderer renderer = _textMesh.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.enabled = false;
            }
            else
            {
                
                _textMesh.gameObject.SetActive(false);
            }
            
            FireHiddenEvent();
        }
    }

    
    
    
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
        
        
        if (_textMesh != null)
        {
            _textMesh.color = _originalColor;
        }
    }

    private System.Collections.IEnumerator TextGlowEffect()
    {
        while (_isVisible && !_isHiding)
        {
            
            float pulse = Mathf.PingPong(Time.time * glowSpeed, 1f);

            
            float intensity = Mathf.Lerp(minGlowIntensity, maxGlowIntensity, pulse);

            
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

        
        MeshRenderer renderer = _textMesh.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            renderer.enabled = false;
        }
        else
        {
            
            _textMesh.gameObject.SetActive(false);
        }
        _fadeCoroutine = null;

        
        FireHiddenEvent();
    }

    private void OnValidate()
    {
        
        if (_textMesh != null && Application.isPlaying)
        {
            SetupText();
        }
    }
}

