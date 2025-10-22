using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class IceWall : MonoBehaviour
{
    [SerializeField] private ParticleSystem meltEffect;

    private bool melted;
    private GameManagerTutorial tutorialManager;
    private void Awake()
    {
        BoxCollider2D collider = GetComponent<BoxCollider2D>();
        collider.isTrigger = false;
        gameObject.layer = LayerMask.NameToLayer("Wall");
    }

    private void Start()
    {
        tutorialManager = FindAnyObjectByType<GameManagerTutorial>();
    }

    public bool TryMelt(PlayerAbilityController abilityController)
    {
        if (melted || abilityController == null) return melted;
        if (!abilityController.ConsumeAbility(IngredientType.Chili)) return false;

        MeltInternal();
        return true;
    }

    private void MeltInternal()
    {
        // ADD THIS LINE
        Debug.Log("An Ice Wall was melted at position: " + transform.position + " at game time: " + Time.time);
        melted = true;
        if (meltEffect != null)
        {
            Instantiate(meltEffect, transform.position, Quaternion.identity);
        }

        GameManagerTutorial tutorialManager = Object.FindFirstObjectByType<GameManagerTutorial>();
        if (tutorialManager != null)
        {
            tutorialManager.OnIceWallMelted();
        }
        Destroy(gameObject);
    }

}
