using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawns collectible tokens at the positions defined by TokenAnchor components.
/// This keeps the scene editable while guaranteeing tokens exist every time the maze is generated.
/// </summary>
[DisallowMultipleComponent]
public class TokenPlacementManager : MonoBehaviour
{
    [SerializeField] private GameObject fireTokenPrefab;
    [SerializeField] private GameObject waterTokenPrefab;

    private readonly List<GameObject> _spawnedTokens = new();

    /// <summary>
    /// Spawns tokens at every anchor beneath this manager. Call after the maze has been generated.
    /// </summary>
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

    /// <summary>
    /// Ensures previously spawned tokens are removed before re-spawning.
    /// </summary>
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
