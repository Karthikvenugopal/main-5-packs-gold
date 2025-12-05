using System.Collections.Generic;
using UnityEngine;





[DisallowMultipleComponent]
public class TokenPlacementManager : MonoBehaviour
{
    [SerializeField] private GameObject fireTokenPrefab;
    [SerializeField] private GameObject waterTokenPrefab;

    private readonly List<GameObject> _spawnedTokens = new();

    
    
    
    public void SpawnTokens()
    {
        ClearSpawnedTokens();

        TokenAnchor[] anchors = GetComponentsInChildren<TokenAnchor>(includeInactive: true);
        foreach (TokenAnchor anchor in anchors)
        {
            GameObject prefab = GetPrefabForAnchor(anchor);
            if (prefab == null)
            {
                Debug.LogWarning($"TokenPlacementManager: Missing prefab for {anchor.TokenType} anchor '{anchor.name}'.");
                continue;
            }

            GameObject token = Instantiate(prefab, anchor.transform.position, Quaternion.identity, transform);
            _spawnedTokens.Add(token);
        }
    }

    
    
    
    public void ClearSpawnedTokens()
    {
        for (int i = _spawnedTokens.Count - 1; i >= 0; i--)
        {
            if (_spawnedTokens[i] != null)
            {
                Destroy(_spawnedTokens[i]);
            }
        }
        _spawnedTokens.Clear();
    }

    private GameObject GetPrefabForAnchor(TokenAnchor anchor)
    {
        return anchor.TokenType == TokenSpriteConfigurator.TokenType.Fire ? fireTokenPrefab : waterTokenPrefab;
    }
}
