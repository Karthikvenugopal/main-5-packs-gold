using UnityEngine;
using System;

[RequireComponent(typeof(Collider2D))]
public class WispActivationZone : MonoBehaviour
{
    
    public static event Action<Transform> ZoneTriggered;

    
    private bool hasBeenTriggered = false; 

    
    private void Reset()
    {
        var collider = GetComponent<Collider2D>();
        if (collider != null)
        {
            collider.isTrigger = true;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasBeenTriggered) return; 

        if (!IsPlayerTag(other)) return;
        
        hasBeenTriggered = true; 
        ZoneTriggered?.Invoke(other.transform);
        
    }

    private static bool IsPlayerTag(Component component)
    {
        return component.CompareTag("Player") ||
               component.CompareTag("FirePlayer") ||
               component.CompareTag("WaterPlayer");
    }
}