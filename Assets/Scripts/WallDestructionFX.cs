using UnityEngine;

public class WallDestructionFX : MonoBehaviour
{
    
    public GameObject destroyFXPrefab;

    public void SpawnFX()
    {
        if (destroyFXPrefab == null) return;

        
        Instantiate(destroyFXPrefab, transform.position, Quaternion.identity);
    }
}
