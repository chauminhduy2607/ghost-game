using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Tạo và sắp xếp vật cản theo khoảng cách
/// </summary>
public class ObstacleSpawner : MonoBehaviour
{
    [Header("=== PREFAB ===")]
    [SerializeField] private GameObject obstaclePrefab;
    
    [Header("=== SPAWN SETTINGS ===")]
    [SerializeField] private int spawnCount = 10;
    [SerializeField] private float spacingY = 1.5f;
    [SerializeField] private float startY = 0f;
    
    [Header("=== AUTO SETUP ===")]
    [SerializeField] private bool autoAddController = true;
    [SerializeField] private bool useExistingObstacles = true;
    
    [Header("=== REFERENCE ===")]
    [SerializeField] private Transform player;
    [SerializeField] private Camera mainCamera;
    
    private List<GameObject> spawnedObstacles = new List<GameObject>();
    
    public List<GameObject> SpawnedObstacles => spawnedObstacles;
    public int ObstacleCount => spawnedObstacles.Count;
    
    void Start()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;
        
        if (player == null)
        {
            GhostController ghost = FindObjectOfType<GhostController>();
            if (ghost != null)
                player = ghost.transform;
        }
        
        if (player != null && startY == 0f)
        {
            float screenHeight = mainCamera.orthographicSize * 2f;
            startY = player.position.y + screenHeight * 0.3f;
        }
        
        if (useExistingObstacles)
        {
            UseExistingObstacles();
        }
        else
        {
            SpawnNewObstacles();
        }
        
        ArrangeObstacles();
    }
    
    void UseExistingObstacles()
    {
        spawnedObstacles.Clear();
        
        foreach (Transform child in transform)
        {
            if (child == null) continue;
            
            if (autoAddController && child.GetComponent<ObstacleController>() == null)
            {
                child.gameObject.AddComponent<ObstacleController>();
            }
            
            spawnedObstacles.Add(child.gameObject);
        }
    }
    
    void SpawnNewObstacles()
    {
        if (obstaclePrefab == null) return;
        
        spawnedObstacles.Clear();
        
        for (int i = 0; i < spawnCount; i++)
        {
            GameObject obstacle = Instantiate(obstaclePrefab, transform);
            obstacle.name = $"Obstacle_{i}";
            
            if (autoAddController && obstacle.GetComponent<ObstacleController>() == null)
            {
                obstacle.AddComponent<ObstacleController>();
            }
            
            spawnedObstacles.Add(obstacle);
        }
    }
    
    void ArrangeObstacles()
    {
        if (spawnedObstacles.Count == 0) return;
        
        for (int i = 0; i < spawnedObstacles.Count; i++)
        {
            GameObject obstacle = spawnedObstacles[i];
            if (obstacle == null) continue;
            
            float yPos = startY + (i * spacingY);
            
            Vector3 pos = obstacle.transform.position;
            pos.y = yPos;
            obstacle.transform.position = pos;
        }
    }
    
    public void ResetAndArrange() => ArrangeObstacles();
    public void SetSpacing(float spacing)
    {
        spacingY = Mathf.Max(0.5f, spacing);
        ArrangeObstacles();
    }
    
    public void SpawnMore(int count)
    {
        if (obstaclePrefab == null || useExistingObstacles) return;
        
        int oldCount = spawnedObstacles.Count;
        
        for (int i = 0; i < count; i++)
        {
            GameObject obstacle = Instantiate(obstaclePrefab, transform);
            obstacle.name = $"Obstacle_{oldCount + i}";
            
            if (autoAddController && obstacle.GetComponent<ObstacleController>() == null)
            {
                obstacle.AddComponent<ObstacleController>();
            }
            
            spawnedObstacles.Add(obstacle);
        }
        
        ArrangeObstacles();
    }
    
    public void ClearAll()
    {
        foreach (GameObject obstacle in spawnedObstacles)
        {
            if (obstacle != null)
            {
                Destroy(obstacle);
            }
        }
        
        spawnedObstacles.Clear();
    }
    
    public GameObject GetObstacle(int index)
    {
        if (index >= 0 && index < spawnedObstacles.Count)
        {
            return spawnedObstacles[index];
        }
        return null;
    }
    
    void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;
        
        Gizmos.color = Color.yellow;
        for (int i = 0; i < spawnedObstacles.Count - 1; i++)
        {
            if (spawnedObstacles[i] == null || spawnedObstacles[i + 1] == null)
                continue;
            
            Gizmos.DrawLine(
                spawnedObstacles[i].transform.position,
                spawnedObstacles[i + 1].transform.position
            );
        }
    }
}