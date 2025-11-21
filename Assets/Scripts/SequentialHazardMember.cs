using UnityEngine;

public interface ISequentialHazardManager
{
    void NotifySequenceHazardCleared(int sequenceId);
}

[DisallowMultipleComponent]
public class SequentialHazardMember : MonoBehaviour
{
    private ISequentialHazardManager _manager;
    private int _sequenceId;
    private bool _initialized;
    private bool _notified;

    public void Initialize(ISequentialHazardManager manager, int sequenceId)
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
