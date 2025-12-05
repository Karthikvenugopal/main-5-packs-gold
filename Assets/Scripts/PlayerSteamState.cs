using UnityEngine;

[RequireComponent(typeof(CoopPlayerController))]
[RequireComponent(typeof(SpriteRenderer))]
public class PlayerSteamState : MonoBehaviour
{
    [Tooltip("蒸汽模式默认持续时间（秒）")]
    [SerializeField] private float defaultSteamDuration = 10f;

    private float _remainingTime;

    
    private CoopPlayerController _player;
    private SpriteRenderer _renderer;

    
    private static readonly Color FireNormalColor = new Color(
        243f / 255f,   
        229f / 255f,   
        223f / 255f,   
        1f
    );

    
    private static readonly Color WaterNormalColor = new Color(
        208f / 255f,   
        219f / 255f,   
        241f / 255f,   
        1f
    );

    
    public bool IsInSteamMode => _remainingTime > 0f;

    private void Awake()
    {
        _player = GetComponent<CoopPlayerController>();
        _renderer = GetComponent<SpriteRenderer>();
    }

    
    
    
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
                ApplySteamColor(false); 
            }
        }
    }

    
    
    
    private void ApplySteamColor(bool steamOn)
    {
        if (_renderer == null || _player == null) return;

        float currentAlpha = _renderer.color.a;

        if (steamOn)
        {
            _renderer.color = new Color(1f, 1f, 1f, currentAlpha);
        }
        else
        {
            Color normal = _player.Role == PlayerRole.Fireboy ? FireNormalColor : WaterNormalColor;
            normal.a = currentAlpha;
            _renderer.color = normal;
        }
    }
}
