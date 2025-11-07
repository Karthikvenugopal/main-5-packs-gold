using System;
using System.Collections;
using UnityEngine;

public class ChainWallPair : MonoBehaviour
{
    private Vector2 _icePosition;
    private Vector2 _firePosition;
    private Func<Vector2, GameObject> _spawnIce;
    private Func<Vector2, GameObject> _spawnFire;

    private GameObject _activeIce;
    private GameObject _activeFire;
    private bool _tearingDown;

    public void Initialize(
        Vector2 icePosition,
        Vector2 firePosition,
        Func<Vector2, GameObject> spawnIce,
        Func<Vector2, GameObject> spawnFire)
    {
        _icePosition = icePosition;
        _firePosition = firePosition;
        _spawnIce = spawnIce;
        _spawnFire = spawnFire;

        SpawnIceImmediate();
    }

    private void SpawnIceImmediate()
    {
        if (_spawnIce == null) return;

        _activeIce = _spawnIce(_icePosition);
        AttachMember(_activeIce, ChainWallMemberType.Ice);
    }

    private void SpawnFireImmediate()
    {
        if (_spawnFire == null) return;

        _activeFire = _spawnFire(_firePosition);
        AttachMember(_activeFire, ChainWallMemberType.Fire);
    }

    private void AttachMember(GameObject wallObject, ChainWallMemberType memberType)
    {
        if (wallObject == null) return;

        ChainWallMember member = wallObject.GetComponent<ChainWallMember>();
        if (member == null)
        {
            member = wallObject.AddComponent<ChainWallMember>();
        }

        member.Assign(this, memberType);
    }

    internal void NotifyMemberDestroyed(ChainWallMemberType type)
    {
        if (_tearingDown) return;

        if (type == ChainWallMemberType.Ice)
        {
            _activeIce = null;
            if (_activeFire == null)
            {
                StartCoroutine(SpawnFireNextFrame());
            }
        }
        else
        {
            _activeFire = null;
            if (_activeIce == null)
            {
                StartCoroutine(SpawnIceNextFrame());
            }
        }
    }

    private IEnumerator SpawnFireNextFrame()
    {
        yield return null;
        if (_tearingDown || _activeFire != null) yield break;
        SpawnFireImmediate();
    }

    private IEnumerator SpawnIceNextFrame()
    {
        yield return null;
        if (_tearingDown || _activeIce != null) yield break;
        SpawnIceImmediate();
    }

    internal bool IsTearingDown => _tearingDown;

    private void OnDisable()
    {
        _tearingDown = true;
    }

    private void OnDestroy()
    {
        _tearingDown = true;
    }

    internal static void SetActiveComponents(GameObject target, bool enabled)
    {
        if (target == null) return;

        if (target.TryGetComponent(out Collider2D collider))
        {
            collider.enabled = enabled;
        }

        if (target.TryGetComponent(out SpriteRenderer renderer))
        {
            renderer.enabled = enabled;
        }
    }
}

public enum ChainWallMemberType
{
    Ice,
    Fire
}

[DisallowMultipleComponent]
public class ChainWallMember : MonoBehaviour
{
    private ChainWallPair _pair;
    private ChainWallMemberType _memberType;
    private bool _assigned;

    public void Assign(ChainWallPair pair, ChainWallMemberType memberType)
    {
        _pair = pair;
        _memberType = memberType;
        _assigned = true;
    }

    private void OnDisable()
    {
        if (!_assigned || _pair == null || _pair.IsTearingDown) return;
        ChainWallPair.SetActiveComponents(gameObject, false);
    }

    private void OnDestroy()
    {
        if (!_assigned || _pair == null || _pair.IsTearingDown) return;
        ChainWallPair.SetActiveComponents(gameObject, false);
        _pair.NotifyMemberDestroyed(_memberType);
    }
}
