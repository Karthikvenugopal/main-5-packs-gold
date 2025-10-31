using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class FireWall : MonoBehaviour
{
    [SerializeField] private ParticleSystem extinguishEffect;

    private bool _extinguished;

    private void Awake()
    {
        Collider2D collider = GetComponent<Collider2D>();
        collider.isTrigger = false;
        gameObject.layer = LayerMask.NameToLayer("Wall");
    }

    public bool TryExtinguish(PlayerRole role)
    {
        if (_extinguished || role != PlayerRole.Watergirl) return false;
        Extinguish();
        return true;
    }

    private void Extinguish()
    {
        _extinguished = true;

        if (extinguishEffect != null)
        {
            Instantiate(extinguishEffect, transform.position, Quaternion.identity);
        }

        Destroy(gameObject);
    }
}
