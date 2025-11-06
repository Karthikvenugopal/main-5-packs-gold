using UnityEngine;
using TMPro;
using System.Collections;

/// <summary>
/// Triggers a dialogue box when a player enters a specific zone.
/// The dialogue appears near the player and auto-hides after a specified duration.
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class DialogueTriggerZone : MonoBehaviour
{
    [Header("Dialogue Settings")]
    [TextArea(2, 4)]
    [SerializeField] private string dialogueText = "Tread ahead carefully. Shield each other from danger";
    
    [SerializeField] private Color textColor = Color.white;
    [SerializeField] private Color backgroundColor = new Color(0f, 0f, 0f, 0.8f);
    [SerializeField] private float fontSize = 3.5f;
    [SerializeField] private float displayDuration = 5f;
    [SerializeField] private Vector2 offsetFromPlayer = new Vector2(5f, 0.8f);
    [SerializeField] private float padding = 0.3f;
    
    [Header("Pulsating Effect")]
    [SerializeField] private bool enablePulsatingEffect = true;
    [SerializeField] private Color glowColor = Color.yellow;
    [SerializeField] private float pulseSpeed = 2f;
    [SerializeField] private float minGlowIntensity = 0.3f;
    [SerializeField] private float maxGlowIntensity = 0.8f;
    
    private BoxCollider2D _trigger;
    private bool _hasTriggered = false;
    private GameObject _currentDialogueBox;
    private Coroutine _pulseCoroutine;
    private TextMeshPro _currentTextMesh;
    private Color _originalTextColor;

    private void Awake()
    {
        _trigger = GetComponent<BoxCollider2D>();
        _trigger.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_hasTriggered) return;
        if (!other.TryGetComponent(out CoopPlayerController player)) return;

        _hasTriggered = true;
        ShowDialogueNearPlayer(player);
    }

    private void ShowDialogueNearPlayer(CoopPlayerController player)
    {
        // Don't create multiple dialogue boxes
        if (_currentDialogueBox != null)
        {
            Destroy(_currentDialogueBox);
        }

        // Create dialogue box GameObject
        GameObject dialogueBox = new GameObject("DialogueBox");
        dialogueBox.transform.SetParent(transform);
        
        // Position it near the player
        Vector3 playerPosition = player.transform.position;
        dialogueBox.transform.position = playerPosition + (Vector3)offsetFromPlayer;

        // Create background
        GameObject background = new GameObject("Background");
        background.transform.SetParent(dialogueBox.transform);
        background.transform.localPosition = Vector3.zero;

        SpriteRenderer bgRenderer = background.AddComponent<SpriteRenderer>();
        bgRenderer.color = backgroundColor;
        bgRenderer.sortingOrder = 10;

        // Create a simple white sprite for the background
        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 100f);
        bgRenderer.sprite = sprite;

        // Create text
        GameObject textObject = new GameObject("Text");
        textObject.transform.SetParent(dialogueBox.transform);
        textObject.transform.localPosition = Vector3.zero;

        TextMeshPro textMesh = textObject.AddComponent<TextMeshPro>();
        textMesh.text = dialogueText;
        textMesh.color = textColor;
        textMesh.fontSize = fontSize;
        textMesh.alignment = TextAlignmentOptions.Center;
        textMesh.fontStyle = FontStyles.Bold;
        textMesh.enableWordWrapping = true;
        textMesh.rectTransform.sizeDelta = new Vector2(8f, 0f); // Auto-height

        // Force text to update bounds
        textMesh.ForceMeshUpdate();

        // Store text reference and original color for pulsating effect
        _currentTextMesh = textMesh;
        _originalTextColor = textColor;

        // Size background to fit text with padding
        Bounds textBounds = textMesh.bounds;
        float bgWidth = textBounds.size.x + padding * 2f;
        float bgHeight = textBounds.size.y + padding * 2f;
        
        background.transform.localScale = new Vector3(bgWidth, bgHeight, 1f);
        background.transform.localPosition = new Vector3(0f, 0f, 0.01f); // Slightly behind text

        // Center the text on the background
        textObject.transform.localPosition = new Vector3(0f, 0f, -0.01f);

        _currentDialogueBox = dialogueBox;

        // Start pulsating effect if enabled
        if (enablePulsatingEffect)
        {
            StartPulsatingEffect();
        }

        // Auto-hide after duration
        StartCoroutine(HideDialogueAfterDelay(dialogueBox, displayDuration));
    }

    private void StartPulsatingEffect()
    {
        if (_pulseCoroutine != null)
        {
            StopCoroutine(_pulseCoroutine);
        }
        _pulseCoroutine = StartCoroutine(PulsatingEffect());
    }

    private void StopPulsatingEffect()
    {
        if (_pulseCoroutine != null)
        {
            StopCoroutine(_pulseCoroutine);
            _pulseCoroutine = null;
        }

        // Restore original color
        if (_currentTextMesh != null)
        {
            _currentTextMesh.color = _originalTextColor;
        }
    }

    private IEnumerator PulsatingEffect()
    {
        while (_currentDialogueBox != null && _currentTextMesh != null)
        {
            // Calculate pulse value using PingPong (oscillates between 0 and 1)
            float pulse = Mathf.PingPong(Time.time * pulseSpeed, 1f);

            // Interpolate between min and max intensity
            float intensity = Mathf.Lerp(minGlowIntensity, maxGlowIntensity, pulse);

            // Blend the original color with the glow color based on intensity
            Color blendedColor = Color.Lerp(_originalTextColor, glowColor, intensity);
            _currentTextMesh.color = blendedColor;

            yield return null;
        }
    }

    private IEnumerator HideDialogueAfterDelay(GameObject dialogueBox, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (dialogueBox != null)
        {
            // Stop pulsating effect
            StopPulsatingEffect();

            // Fade out effect
            SpriteRenderer bg = dialogueBox.transform.Find("Background")?.GetComponent<SpriteRenderer>();
            TextMeshPro text = dialogueBox.transform.Find("Text")?.GetComponent<TextMeshPro>();

            if (bg != null && text != null)
            {
                float fadeDuration = 0.5f;
                float timer = 0f;
                Color bgStartColor = bg.color;
                Color textStartColor = text.color;

                while (timer < fadeDuration && dialogueBox != null)
                {
                    timer += Time.deltaTime;
                    float alpha = 1f - (timer / fadeDuration);
                    
                    Color bgColor = bgStartColor;
                    bgColor.a = bgStartColor.a * alpha;
                    bg.color = bgColor;

                    Color textColor = textStartColor;
                    textColor.a = textStartColor.a * alpha;
                    text.color = textColor;

                    yield return null;
                }
            }

            if (dialogueBox != null)
            {
                Destroy(dialogueBox);
            }
        }

        _currentDialogueBox = null;
        _currentTextMesh = null;
    }

    /// <summary>
    /// Reset the trigger so it can be triggered again (useful for testing or respawning)
    /// </summary>
    public void ResetTrigger()
    {
        _hasTriggered = false;
        StopPulsatingEffect();
        if (_currentDialogueBox != null)
        {
            Destroy(_currentDialogueBox);
            _currentDialogueBox = null;
        }
        _currentTextMesh = null;
    }
}

