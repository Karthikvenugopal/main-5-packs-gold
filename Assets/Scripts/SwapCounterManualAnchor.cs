using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public class SwapCounterManualAnchor : MonoBehaviour
{
    [SerializeField] private RectTransform swapCounterContainer;
    [SerializeField] private TextMeshProUGUI swapCounterLabel;

    public RectTransform Container
    {
        get
        {
            if (swapCounterContainer == null)
            {
                swapCounterContainer = GetComponent<RectTransform>();
            }

            return swapCounterContainer;
        }
    }

    public TextMeshProUGUI Label
    {
        get
        {
            if (swapCounterLabel == null)
            {
                swapCounterLabel = GetComponentInChildren<TextMeshProUGUI>(true);
            }

            return swapCounterLabel;
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (swapCounterContainer == null)
        {
            swapCounterContainer = GetComponent<RectTransform>();
        }

        if (swapCounterLabel == null)
        {
            swapCounterLabel = GetComponentInChildren<TextMeshProUGUI>(true);
        }
    }
#endif
}
