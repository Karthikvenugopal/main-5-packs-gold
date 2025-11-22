using UnityEngine;

public enum PairedHazardSlot
{
    First,
    Second
}

public enum PairedHazardType
{
    Fire,
    Ice
}

public interface IPairedHazardManager
{
    void NotifyPairedHazardCleared(int pairIndex, PairedHazardSlot slot);
}

[DisallowMultipleComponent]
public class PairedHazardMember : MonoBehaviour
{
    private IPairedHazardManager _manager;
    private int _pairIndex;
    private PairedHazardSlot _slot;
    private bool _initialized;
    private bool _notified;

    public void Initialize(IPairedHazardManager manager, int pairIndex, PairedHazardSlot slot)
    {
        _manager = manager;
        _pairIndex = pairIndex;
        _slot = slot;
        _initialized = true;
        _notified = false;
    }

    private void OnDestroy()
    {
        if (!_initialized || _notified)
        {
            return;
        }

        _notified = true;
        _manager?.NotifyPairedHazardCleared(_pairIndex, _slot);
    }
}
