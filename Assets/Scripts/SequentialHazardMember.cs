using UnityEngine;

[DisallowMultipleComponent]
public class SequentialHazardMember : MonoBehaviour
{
    private Level3Manager _manager;
    private int _sequenceId;
    private bool _initialized;
    private bool _notified;

    public void Initialize(Level3Manager manager, int sequenceId)
    {
        _manager = manager;
        _sequenceId = sequenceId;
        _initialized = true;
    }

    private void OnDestroy()
    {
        if (!_initialized || _notified)
        {
            return;
        }

        _notified = true;
        _manager?.NotifySequenceHazardCleared(_sequenceId);
    }
}
