using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class JamPatch : MonoBehaviour
{
    [SerializeField] private ParticleSystem scoopEffect;

    private bool cleared;
    private GameManagerTutorial tutorialManager;

    private void Awake()
    {
        BoxCollider2D col = GetComponent<BoxCollider2D>();
        col.isTrigger = false;
        gameObject.layer = LayerMask.NameToLayer("Wall");
    }

    private void Start()
    {
        tutorialManager = FindAnyObjectByType<GameManagerTutorial>();
    }

    public bool TryScoop(PlayerAbilityController abilityController)
    {
        if (cleared || abilityController == null) return cleared;
        if (!abilityController.ConsumeAbility(IngredientType.Bread)) return false;

        ClearPatch();
        return true;
    }

    private void ClearPatch()
    {
        cleared = true;

        if (scoopEffect != null)
        {
            Instantiate(scoopEffect, transform.position, Quaternion.identity);
        }

        if (tutorialManager != null)
        {
            tutorialManager.OnJamCleared();
        }

        Destroy(gameObject);
    }
}
