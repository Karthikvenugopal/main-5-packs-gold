using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[System.Serializable]
public class InstructionsItem2
{
    public RectTransform rect;      
    public CanvasGroup canvasGroup; 
}

public class InstructionsUI2 : MonoBehaviour
{
    [Header("Assign Title + Rows here")]
    public InstructionsItem2[] fallingItems;

    [Header("Settings")]
    public float gravity = -1200f;      
    public float fadeDuration = 0.8f;
    public float delayBeforeLoad = 1.4f;
    public string nextSceneName = "SampleScene"; 

    private bool started = false;

    public void OnReadyClicked()
    {
        Debug.Log("InstructionsUI2 OnReadyClicked called");
        if (started) return;
        started = true;
        StartCoroutine(FallAndLoad());
    }

    private IEnumerator FallAndLoad()
    {
        foreach (var item in fallingItems)
        {
            if (item == null || item.rect == null || item.canvasGroup == null) continue;
            StartCoroutine(FallAndFade(item));
        }

        yield return new WaitForSeconds(delayBeforeLoad);
        Debug.Log("InstructionsUI2 navigating to SampleScene");
        SceneManager.LoadScene("SampleScene");
    }

    private IEnumerator FallAndFade(InstructionsItem2 item)
    {
        Vector2 pos = item.rect.anchoredPosition;
        Vector2 velocity = Vector2.zero;
        float time = 0f;

        while (time < fadeDuration + 1f)
        {
            float dt = Time.unscaledDeltaTime;
            time += dt;
            velocity.y += gravity * dt;
            pos += velocity * dt;
            item.rect.anchoredPosition = pos;

            float t = Mathf.Clamp01(time / fadeDuration);
            item.canvasGroup.alpha = Mathf.Lerp(1f, 0f, t);
            yield return null;
        }

        item.canvasGroup.alpha = 0f;
    }
}
