using UnityEngine;

public class PlayerSpawner : MonoBehaviour
{
    public GameObject playerPrefab;
    void Start()
    {
      GameObject spawnMarker = GameObject.Find("PlayerSpawn");
        if (spawnMarker != null && playerPrefab != null)
        {
            GameObject playerObject = Instantiate(playerPrefab, spawnMarker.transform.position, Quaternion.identity);
            if(playerObject.TryGetComponent(out SpriteRenderer sr)) sr.sortingOrder = 3; 
        }
        else
        {
            Debug.LogWarning("PlayerSpawn not found or PlayerPrefab not assigned!");
        }
    }


    void Update()
    {
        
    }
}
