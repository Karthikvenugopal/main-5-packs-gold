using UnityEngine;

public class UIFloat : MonoBehaviour
{
    public float amplitude = 10f;   // 上下浮动的幅度（像素）
    public float frequency = 1f;    // 速度（每秒几次往返）

    private RectTransform rectTransform;
    private Vector2 startPos;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        startPos = rectTransform.anchoredPosition;
    }

    void Update()
    {
        float offsetY = Mathf.Sin(Time.time * frequency) * amplitude;
        rectTransform.anchoredPosition = new Vector2(startPos.x, startPos.y + offsetY);
    }
}
