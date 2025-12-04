using UnityEngine;

public class WallDestructionFX : MonoBehaviour
{
    // 在 Inspector 里拖你的冰/火 FX prefab 进来
    public GameObject destroyFXPrefab;

    public void SpawnFX()
    {
        if (destroyFXPrefab == null) return;

        // 在墙的位置生成
        Instantiate(destroyFXPrefab, transform.position, Quaternion.identity);
    }
}
