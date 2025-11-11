using System.Collections;
using UnityEngine;

public class HeartLossAnimatorExample : MonoBehaviour
{
    [SerializeField] private HeartLossAnimator heartAnimator;
    [SerializeField] private bool triggerOnStart = true;
    [SerializeField] private float delayBetweenHearts = 0.8f;

    private void Start()
    {
        if (triggerOnStart && heartAnimator != null)
        {
            StartCoroutine(DemoSequence());
        }
    }

    public void TriggerDemo()
    {
        if (heartAnimator != null)
        {
            StopAllCoroutines();
            StartCoroutine(DemoSequence());
        }
    }

    private IEnumerator DemoSequence()
    {
        for (int i = 0; i < 3; i++)
        {
            heartAnimator.LoseHeart(true, i);
            yield return new WaitForSeconds(delayBetweenHearts);
        }

        for (int i = 2; i >= 0; i--)
        {
            heartAnimator.LoseHeart(false, i);
            yield return new WaitForSeconds(delayBetweenHearts);
        }
    }
}
