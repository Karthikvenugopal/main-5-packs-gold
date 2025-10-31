using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class IceWall : MonoBehaviour
{
    [SerializeField] private ParticleSystem meltEffect;

    private bool _melted;

    private void Awake()
    {
        Collider2D collider = GetComponent<Collider2D>();
        collider.isTrigger = false;
        gameObject.layer = LayerMask.NameToLayer("Wall");
    }

    public bool TryMelt(PlayerRole role)
    {
        if (_melted || role != PlayerRole.Fireboy) return false;
        Melt();
        return true;
    }

    private void Melt()
    {
        _melted = true;

        if (meltEffect != null)
        {
            Instantiate(meltEffect, transform.position, Quaternion.identity);
        }

        Destroy(gameObject);
    }
}
