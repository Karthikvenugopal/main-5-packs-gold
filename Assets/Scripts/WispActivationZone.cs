using UnityEngine;
using System;

[RequireComponent(typeof(Collider2D))]
public class WispActivationZone : MonoBehaviour
{
    // WispEnemy 订阅的就是这个静态事件
    public static event Action<Transform> ZoneTriggered;

    // --- ADDED: 标志位，确保事件只触发一次 ---
    private bool hasBeenTriggered = false; 

    // Reset 函数用于在编辑器中检查时自动设置 Is Trigger 属性
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