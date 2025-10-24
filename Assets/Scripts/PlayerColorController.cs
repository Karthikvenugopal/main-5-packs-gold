

using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class PlayerColorController : MonoBehaviour
{
    [Header("Colors per ability")]
    [SerializeField] private Color noneColor      = new Color(0.063f, 0.714f, 0.173f, 1f);      
    [SerializeField] private Color chiliColor     = new Color(0.95f, 0.25f, 0.15f); 
    [SerializeField] private Color butterColor    = new Color(0.98f, 0.92f, 0.50f); 
    [SerializeField] private Color breadColor     = new Color(0.85f, 0.70f, 0.50f); 
    [SerializeField] private Color garlicColor    = new Color(0.95f, 0.93f, 0.86f); 
    [SerializeField] private Color chocolateColor = new Color(0.45f, 0.30f, 0.18f); 

    [Header("Transition")]
    [SerializeField] private float lerpTime = 0.08f;

    private SpriteRenderer _sr;
    private Color _target;

    private void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
        _target = _sr.color;
    }

    private void Update()
    {
        if (_sr != null && _sr.color != _target)
            _sr.color = Color.Lerp(_sr.color, _target, 1f - Mathf.Exp(-Time.deltaTime / Mathf.Max(0.0001f, lerpTime)));
    }

    
    public void OnAbilityChanged(IngredientType ability)
    {
        _target = ability switch
        {
            IngredientType.Chili     => chiliColor,
            IngredientType.Butter    => butterColor,
            IngredientType.Bread     => breadColor,
            IngredientType.Garlic    => garlicColor,
            IngredientType.Chocolate => chocolateColor,
            _                        => noneColor
        };
    }
}

