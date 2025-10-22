using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[System.Serializable]
public class TutorialItem
{
    public RectTransform rect;      
    public CanvasGroup canvasGroup; 
}

public class TutorialUI : MonoBehaviour
{
    [Header("Assign Title + Rows here")]
    public TutorialItem[] fallingItems;

    [Header("Settings")]
    public float gravity = -1200f;      
    public float fadeDuration = 0.8f;
    public float delayBeforeLoad = 1.4f;
    public string nextSceneName = "DemoTutorialScene"; 


    private bool started = false;

    public void OnProceedClicked()
    {
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
        SceneManager.LoadScene(nextSceneName);
    }

    private IEnumerator FallAndFade(TutorialItem item)
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
