using System.Collections.Generic;
using UnityEngine;

public class SandboxSceneSetup : MonoBehaviour
{
    [Header("Core Prefabs")]
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private GameObject floorPrefab;
    [SerializeField] private GameObject wallPrefab;
    [SerializeField] private GameObject stickyZonePrefab;
    [SerializeField] private GameObject waterPatchPrefab;
    [SerializeField] private GameObject iceWallPrefab;

    [Header("Ingredient Prefabs")]
    [SerializeField] private GameObject chiliPrefab;
    [SerializeField] private GameObject butterPrefab;
    [SerializeField] private GameObject breadPrefab;
    [SerializeField] private GameObject garlicPrefab;
    [SerializeField] private GameObject chocolatePrefab;

    [Header("Enemy Prefabs")]
    [SerializeField] private GameObject rollingPinPrefab;
    [SerializeField] private GameObject knifePrefab;

    [Header("Arena Settings")]
    [SerializeField] private Vector2Int arenaSize = new Vector2Int(14, 12);
    [SerializeField] private Vector2 arenaCenter = Vector2.zero;

    private readonly List<GameObject> spawnedIngredients = new();

    private void Awake()
    {
        SetupCamera();
        BuildArena();
        SpawnPlayer();
        SpawnPickups();
        SpawnObstacles();
        SpawnEnemies();
    }

    private void SetupCamera()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null) return;

        mainCamera.orthographic = true;
        mainCamera.transform.position = new Vector3(arenaCenter.x, arenaCenter.y, -10f);

        float verticalSize = arenaSize.y * 0.6f;
        float horizontalSize = (arenaSize.x * 0.6f) / mainCamera.aspect;
        mainCamera.orthographicSize = Mathf.Max(verticalSize, horizontalSize);
    }

    private void BuildArena()
    {
        if (floorPrefab == null || wallPrefab == null) return;

        Vector2 start = arenaCenter - new Vector2((arenaSize.x - 1) * 0.5f, (arenaSize.y - 1) * 0.5f);

        for (int x = 0; x < arenaSize.x; x++)
        {
            for (int y = 0; y < arenaSize.y; y++)
            {
                Vector2 cellPos = start + new Vector2(x, y);
                Instantiate(floorPrefab, cellPos, Quaternion.identity, transform);
            }
        }

        float minX = start.x - 1f;
        float maxX = start.x + arenaSize.x;
        float minY = start.y - 1f;
        float maxY = start.y + arenaSize.y;

        for (int x = -1; x <= arenaSize.x; x++)
        {
            Vector2 bottomPos = start + new Vector2(x, -1);
            Vector2 topPos = start + new Vector2(x, arenaSize.y);
            Instantiate(wallPrefab, bottomPos, Quaternion.identity, transform);
            Instantiate(wallPrefab, topPos, Quaternion.identity, transform);
        }

        for (int y = 0; y < arenaSize.y; y++)
        {
            Vector2 leftPos = new Vector2(minX, start.y + y);
            Vector2 rightPos = new Vector2(maxX, start.y + y);
            Instantiate(wallPrefab, leftPos, Quaternion.identity, transform);
            Instantiate(wallPrefab, rightPos, Quaternion.identity, transform);
        }
    }

    private void SpawnPlayer()
    {
        if (playerPrefab == null) return;
        Instantiate(playerPrefab, arenaCenter, Quaternion.identity);
    }

    private void SpawnPickups()
    {
        SpawnIngredient(chiliPrefab, arenaCenter + new Vector2(-4f, 0f));
        SpawnIngredient(butterPrefab, arenaCenter + new Vector2(4f, 0f));
        SpawnIngredient(breadPrefab, arenaCenter + new Vector2(0f, 3.5f));
        SpawnIngredient(garlicPrefab, arenaCenter + new Vector2(0f, -3.5f));
        SpawnIngredient(chocolatePrefab, arenaCenter + new Vector2(-4f, 3.5f));
    }

    private void SpawnObstacles()
    {
        if (stickyZonePrefab != null)
        {
            Instantiate(stickyZonePrefab, arenaCenter + new Vector2(2.5f, -2.5f), Quaternion.identity, transform);
        }

        if (waterPatchPrefab != null)
        {
            Instantiate(waterPatchPrefab, arenaCenter + new Vector2(-2.5f, -2.5f), Quaternion.identity, transform);
        }

        if (iceWallPrefab != null)
        {
            Instantiate(iceWallPrefab, arenaCenter + new Vector2(0f, 5f), Quaternion.identity, transform);
        }
    }

    private void SpawnEnemies()
    {
        if (rollingPinPrefab != null)
        {
            Instantiate(rollingPinPrefab, arenaCenter + new Vector2(-5f, 1.5f), Quaternion.identity, transform);
        }

        if (knifePrefab != null)
        {
            Instantiate(knifePrefab, arenaCenter + new Vector2(5f, 1.5f), Quaternion.identity, transform);
        }
    }

    private void SpawnIngredient(GameObject prefab, Vector2 position)
    {
        if (prefab == null) return;

        GameObject instance = Instantiate(prefab, position, Quaternion.identity, transform);
        instance.tag = "Ingredient";
        spawnedIngredients.Add(instance);
    }
}
