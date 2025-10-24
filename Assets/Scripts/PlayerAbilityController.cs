using System.Collections;
using UnityEngine;

public enum IngredientType
{
    None = 0,
    Chili,
    Butter,
    Bread,
    Garlic,
    Chocolate
}

public class PlayerAbilityController : MonoBehaviour
{
    [Header("Movement & Effects")]
    [SerializeField] private float baseSpeed = 4f;
    [SerializeField] private float butterSpeedMultiplier = 1.15f;   
    [SerializeField] private float stickySlowMultiplier  = 0.60f;   

    [Header("UI / FX Broadcast")]
    [Tooltip("If true, will SendMessage(\"OnAbilityChanged\", IngredientType) when ability changes.")]
    [SerializeField] private bool broadcastAbilityChanged = true;

    public IngredientType CurrentAbility { get; private set; } = IngredientType.None;

    
    private bool _inStickyZone;

    
    private Coroutine _expireRoutine;

    private void Awake()
    {
        
        ApplyVisualsFor(CurrentAbility);
    }

    public void GrantAbility(IngredientType newAbility, float durationSeconds)
    {
        SetCurrentAbility(newAbility);

        if (Application.isPlaying && durationSeconds > 0f)
        {
            if (_expireRoutine != null) StopCoroutine(_expireRoutine);
            _expireRoutine = StartCoroutine(ExpireAfter(durationSeconds, newAbility));
        }
    }

    private IEnumerator ExpireAfter(float seconds, IngredientType abilityGranted)
    {
        yield return new WaitForSeconds(seconds);
        
        if (CurrentAbility == abilityGranted)
        {
            ClearCurrentAbilityEffects();
            CurrentAbility = IngredientType.None;
            ApplyVisualsFor(CurrentAbility);
        }
        _expireRoutine = null;
    }

    
    
    
    public void SetCurrentAbility(IngredientType newAbility)
    {
        if (newAbility == CurrentAbility) return;

        if (_expireRoutine != null) { StopCoroutine(_expireRoutine); _expireRoutine = null; }

        
        ClearCurrentAbilityEffects();
        CurrentAbility = newAbility;
        ApplyCurrentAbilityEffects();
        ApplyVisualsFor(CurrentAbility);
    }

    public bool HasAbility(IngredientType t) => CurrentAbility == t;

    public bool ConsumeAbility(IngredientType t)
    {
        if (CurrentAbility != t) return false;

        if (_expireRoutine != null) { StopCoroutine(_expireRoutine); _expireRoutine = null; }
        ClearCurrentAbilityEffects();
        CurrentAbility = IngredientType.None;
        ApplyVisualsFor(CurrentAbility);
        return true;
    }

    public bool TryConsumeAnyAbility(out IngredientType consumed)
    {
        if (CurrentAbility == IngredientType.None)
        {
            consumed = IngredientType.None;
            return false;
        }

        consumed = CurrentAbility;
        if (_expireRoutine != null) { StopCoroutine(_expireRoutine); _expireRoutine = null; }
        ClearCurrentAbilityEffects();
        CurrentAbility = IngredientType.None;
        ApplyVisualsFor(CurrentAbility);
        return true;
    }

    
    public void EnterStickyZone() => _inStickyZone = true;
    public void ExitStickyZone()  => _inStickyZone = false;

    
    
    
    public float CurrentSpeedMultiplier
    {
        get
        {
            if (_inStickyZone && CurrentAbility != IngredientType.Butter)
                return stickySlowMultiplier;

            if (CurrentAbility == IngredientType.Butter)
                return butterSpeedMultiplier;

            return 1f;
        }
    }

    
    public float GetMoveSpeedMultiplier() => CurrentSpeedMultiplier;

    public float GetMoveSpeed(float externalMultiplier = 1f)
        => baseSpeed * CurrentSpeedMultiplier * Mathf.Max(0.0001f, externalMultiplier);

    private void ApplyCurrentAbilityEffects()
    {
        
    }

    private void ClearCurrentAbilityEffects()
    {
        
    }

    
    
    
    private void ApplyVisualsFor(IngredientType ability)
    {
        if (!broadcastAbilityChanged) return;

        
        
        SendMessage("OnAbilityChanged", ability, SendMessageOptions.DontRequireReceiver);
    }
}
