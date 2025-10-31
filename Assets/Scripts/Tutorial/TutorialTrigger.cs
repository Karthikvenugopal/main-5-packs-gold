using UnityEngine;
using TMPro;                
using System.Collections;   

[RequireComponent(typeof(BoxCollider2D))]
public class TutorialTrigger : MonoBehaviour
{
    [Tooltip("Drag the UI Text object from the Canvas here")]
    [SerializeField] private GameObject textElementToShow;

    [Tooltip("Check this if only ONE specific player role can trigger this")]
    [SerializeField] private bool requiresSpecificRole = false;

    [Tooltip("If the box above is checked, specify the role")]
    [SerializeField] private PlayerRole dedicatedRole;

    [Header("Fade Effect")]
    [Tooltip("Duration of the text fade in/out (in seconds)")]
    [SerializeField] private float fadeDuration = 0.5f;

    // --- MODIFIED: Replaced old glow with Outline Glow ---
    [Header("Glow Effect (Outline)")]
    [Tooltip("Enable the pulsing halo effect")]
    [SerializeField] private bool enableGlowEffect = true;

    [Tooltip("The color of the pulsing halo")]
    [SerializeField] private Color outlineGlowColor = Color.yellow;
    
    [Tooltip("The speed of the pulsing effect")]
    [SerializeField] private float outlineGlowSpeed = 1f;

    [Tooltip("The minimum thickness of the outline")]
    [SerializeField] private float minOutlineWidth = 0f;

    [Tooltip("The maximum thickness of the outline (e.g., 0.2)")]
    [SerializeField] private float maxOutlineWidth = 0.2f;
    // --- END OF MODIFICATION ---

    private int playersInZone = 0;
    private TextMeshProUGUI _textMesh;   
    private Coroutine _glowCoroutine;    

    private void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;

        if (textElementToShow == null)
        {
            Debug.LogError("TutorialTrigger: 'Text Element To Show' has not been assigned!", this);
            return;
        }
        
        _textMesh = textElementToShow.GetComponent<TextMeshProUGUI>();

        if (_textMesh == null)
        {
            Debug.LogError("TutorialTrigger: The assigned 'Text Element To Show' object does not have a TextMeshProUGUI component!", this);
            return;
        }

        // --- MODIFIED: Setup for Outline ---
        // Set the outline color from the inspector
        _textMesh.outlineColor = (Color32)outlineGlowColor;
        // Set the outline width to its minimum
        _textMesh.outlineWidth = minOutlineWidth;
        
        // Start with the text fully transparent
        _textMesh.canvasRenderer.SetAlpha(0.0f);
        
        // Ensure the GameObject itself is active (so it can run coroutines)
        textElementToShow.SetActive(true); 
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_textMesh == null) return; 
        if (!other.TryGetComponent(out CoopPlayerController player)) return;
        if (requiresSpecificRole && player.Role != dedicatedRole) return;

        playersInZone++;

        // 1. Trigger fade in (this just fades the alpha)
        _textMesh.CrossFadeAlpha(1.0f, fadeDuration, false);

        // 2. Trigger glow (if enabled)
        if (enableGlowEffect)
        {
            if (_glowCoroutine != null) StopCoroutine(_glowCoroutine);
            _glowCoroutine = StartCoroutine(GlowEffect());
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (_textMesh == null) return; 
        if (!other.TryGetComponent(out CoopPlayerController player)) return;
        if (requiresSpecificRole && player.Role != dedicatedRole) return;

        playersInZone--;
        
        if (playersInZone <= 0)
        {
            // 1. Trigger fade out
            _textMesh.CrossFadeAlpha(0.0f, fadeDuration, false);

            // 2. Stop the glow
            if (_glowCoroutine != null)
            {
                StopCoroutine(_glowCoroutine);
                _glowCoroutine = null;
                // --- MODIFIED: Reset the outline width ---
                _textMesh.outlineWidth = minOutlineWidth;
            }
            playersInZone = 0;
        }
    }

    // --- MODIFIED: This coroutine now animates the Outline Width ---
    private IEnumerator GlowEffect()
    {
        while (true)
        {
            // Use Mathf.PingPong to make a value oscillate between min and max
            float outlineValue = Mathf.PingPong(Time.time * outlineGlowSpeed, maxOutlineWidth - minOutlineWidth) + minOutlineWidth;

            // Animate the outline thickness
            _textMesh.outlineWidth = outlineValue;
            
            yield return null; // Wait for the next frame
        }
    }

    // Exposed API used by MazeBuilder_Tutorial
    public void SetText(string message)
    {
        if (_textMesh == null && textElementToShow != null)
        {
            _textMesh = textElementToShow.GetComponent<TextMeshProUGUI>();
        }

        if (_textMesh == null)
        {
            Debug.LogError("TutorialTrigger: No TextMeshProUGUI component available to set text.", this);
            return;
        }

        _textMesh.text = message ?? string.Empty;
    }
}
