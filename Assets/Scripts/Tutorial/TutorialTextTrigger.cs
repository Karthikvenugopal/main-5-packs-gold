using UnityEngine;
using TMPro;
using System.Collections;

// This script controls a single tutorial text element.
// It is designed to be activated by a manager.

[RequireComponent(typeof(BoxCollider2D))]
public class TutorialTextTrigger : MonoBehaviour
{
    [Tooltip("The TextMeshPro object to fade in/out")]
    [SerializeField] private TextMeshPro textMesh;

    [Tooltip("How fast the text fades in and out")]
    [SerializeField] private float fadeDuration = 0.5f;

    private BoxCollider2D triggerCollider;
    private Color originalColor;
    private Coroutine fadeCoroutine;

    private void Awake()
    {
        // Get the collider component
        triggerCollider = GetComponent<BoxCollider2D>();
        triggerCollider.isTrigger = true; // Ensure it's a trigger

        if (textMesh == null)
        {
            Debug.LogError("TutorialTextTrigger: TextMeshPro component is not assigned!", this);
            return;
        }

        // 1. Store the original color
        originalColor = textMesh.color;

        // 2. Start completely transparent
        textMesh.color = new Color(originalColor.r, originalColor.g, originalColor.b, 0f);
        
        // 3. Start disabled. The TutorialSceneManager will activate this.
        triggerCollider.enabled = false;
        textMesh.gameObject.SetActive(false);
    }

    // This is called by TutorialSceneManager to turn this trigger on
    public void Activate()
    {
        if (textMesh == null) return;
        
        textMesh.gameObject.SetActive(true);
        triggerCollider.enabled = true;
    }

    // This can be called to permanently disable the trigger
    public void Complete()
    {
        triggerCollider.enabled = false;
        StartFade(0f); // Fade out
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Check if it's a player
        if (other.GetComponent<CoopPlayerController>() != null)
        {
            // Start fading IN
            StartFade(1f); // 1f = target alpha (fully visible)
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.GetComponent<CoopPlayerController>() != null)
        {
            // Start fading OUT
            StartFade(0f); // 0f = target alpha (fully transparent)
        }
    }

    private void StartFade(float targetAlpha)
    {
        // If a fade is already running, stop it
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }
        // Start the new fade coroutine
        fadeCoroutine = StartCoroutine(FadeText(targetAlpha));
    }

    // The Coroutine that handles the smooth fade effect
    private IEnumerator FadeText(float targetAlpha)
    {
        float timer = 0f;
        Color startColor = textMesh.color;
        Color endColor = new Color(originalColor.r, originalColor.g, originalColor.b, targetAlpha);

        while (timer < fadeDuration)
        {
            // Smoothly interpolate the color's alpha value
            textMesh.color = Color.Lerp(startColor, endColor, timer / fadeDuration);
            timer += Time.deltaTime;
            yield return null; // Wait for the next frame
        }
        
        textMesh.color = endColor; // Ensure it reaches the exact target value
        fadeCoroutine = null;
    }
}