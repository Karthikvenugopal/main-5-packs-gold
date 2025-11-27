using UnityEngine;

[RequireComponent(typeof(CoopPlayerController))]
[RequireComponent(typeof(SpriteRenderer))]
public class PlayerSteamState : MonoBehaviour
{
    [Tooltip("蒸汽模式默认持续时间（秒）")]
    [SerializeField] private float defaultSteamDuration = 10f;

    private float _remainingTime;

    // 角色 & 渲染
    private CoopPlayerController _player;
    private SpriteRenderer _renderer;

    // Fireboy: #ED642E
    private static readonly Color FireNormalColor = new Color(
        237f / 255f,   // ED
        100f / 255f,   // 64
        46f  / 255f,   // 2E
        1f
    );

    // Watergirl: #3272F2
    private static readonly Color WaterNormalColor = new Color(
        50f  / 255f,   // 32
        114f / 255f,   // 72
        242f / 255f,   // F2
        1f
    );

    /// <summary>是否处于蒸汽模式中。</summary>
    public bool IsInSteamMode => _remainingTime > 0f;

    private void Awake()
    {
        _player = GetComponent<CoopPlayerController>();
        _renderer = GetComponent<SpriteRenderer>();
    }

    /// <summary>
    /// 开启蒸汽模式。如果 duration 小于等于 0，则使用默认持续时间。
    /// </summary>
    public void EnterSteamMode(float duration)
    {
        if (duration <= 0f)
            duration = defaultSteamDuration;

        _remainingTime = duration;

        Debug.Log($"[PlayerSteamState] {name} ENTER steam mode for {_remainingTime:F1} seconds.");

        ApplySteamColor(true);
    }

    private void Update()
    {
        if (_remainingTime > 0f)
        {
            _remainingTime -= Time.deltaTime;

            if (_remainingTime <= 0f)
            {
                _remainingTime = 0f;
                Debug.Log($"[PlayerSteamState] {name} steam mode ENDED.");
                ApplySteamColor(false); // 蒸汽结束，恢复颜色
            }
        }
    }

    /// <summary>
    /// 根据当前角色，切换为蒸汽颜色或恢复原色。
    /// </summary>
    private void ApplySteamColor(bool steamOn)
    {
        if (_renderer == null || _player == null) return;

        float currentAlpha = _renderer.color.a;

        if (steamOn)
        {
            Color steamColor;

            if (_player.Role == PlayerRole.Fireboy)
            {
                // Fireboy 蒸汽色：#FF84E4
                steamColor = new Color(
                    1.0f,
                    0.5176470588235295f,
                    0.8941176470588236f,
                    currentAlpha
                );
            }
            else
            {
                // Watergirl 蒸汽色：#18D4FE
                steamColor = new Color(
                    0.09411764705882353f,
                    0.8313725490196079f,
                    0.996078431372549f,
                    currentAlpha
                );
            }

            _renderer.color = steamColor;
        }
        else
        {
            // 恢复正常颜色
            Color normal = _player.Role == PlayerRole.Fireboy ? FireNormalColor : WaterNormalColor;
            normal.a = currentAlpha;
            _renderer.color = normal;
        }
    }
}
