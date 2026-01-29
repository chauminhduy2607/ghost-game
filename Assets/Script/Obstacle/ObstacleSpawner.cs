using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// ⭐ SCRIPT SPAWN VẬT CẢN - CHỈ LO VIỆC TẠO VÀ SẮP XẾP
/// - Tự động tạo hoặc sử dụng vật cản có sẵn
/// - Sắp xếp vật cản theo khoảng cách
/// - Không quan tâm đến di chuyển hay loop
/// </summary>
public class ObstacleSpawner : MonoBehaviour
{
    [Header("=== PREFAB ===")]
    [Tooltip("Prefab vật cản (nếu muốn tự động spawn)")]
    [SerializeField] private GameObject obstaclePrefab;
    
    [Header("=== SPAWN SETTINGS ===")]
    [Tooltip("Số lượng vật cản muốn spawn")]
    [SerializeField] private int spawnCount = 10;
    
    [Tooltip("Khoảng cách giữa các vật cản (trục Y)")]
    [SerializeField] private float spacingY = 1.5f;
    
    [Tooltip("Vị trí Y bắt đầu spawn")]
    [SerializeField] private float startY = 0f;
    
    [Header("=== AUTO SETUP ===")]
    [Tooltip("Tự động thêm ObstacleController vào vật cản")]
    [SerializeField] private bool autoAddController = true;
    
    [Tooltip("Sử dụng vật cản có sẵn trong scene (con của object này)")]
    [SerializeField] private bool useExistingObstacles = true;
    
    [Header("=== REFERENCE ===")]
    [SerializeField] private Transform player;
    [SerializeField] private Camera mainCamera;
    
    // Private
    private List<GameObject> spawnedObstacles = new List<GameObject>();
    
    // Public - để các script khác lấy danh sách vật cản
    public List<GameObject> SpawnedObstacles => spawnedObstacles;
    public int ObstacleCount => spawnedObstacles.Count;
    
    void Start()
    {
        // Tự động tìm references
        if (mainCamera == null)
            mainCamera = Camera.main;
        
        if (player == null)
        {
            GhostController ghost = FindObjectOfType<GhostController>();
            if (ghost != null)
                player = ghost.transform;
        }
        
        // Tính vị trí bắt đầu nếu có player
        if (player != null && startY == 0f)
        {
            float screenHeight = mainCamera.orthographicSize * 2f;
            startY = player.position.y + screenHeight * 0.3f;
        }
        
        // Spawn hoặc sử dụng vật cản có sẵn
        if (useExistingObstacles)
        {
            UseExistingObstacles();
        }
        else
        {
            SpawnNewObstacles();
        }
        
        // Sắp xếp vật cản
        ArrangeObstacles();
        
        PrintSpawnInfo();
    }
    
    // ==================== SỬ DỤNG VẬT CẢN CÓ SẴN ====================
    void UseExistingObstacles()
    {
        spawnedObstacles.Clear();
        
        foreach (Transform child in transform)
        {
            if (child == null) continue;
            
            // Thêm ObstacleController nếu chưa có
            if (autoAddController && child.GetComponent<ObstacleController>() == null)
            {
                child.gameObject.AddComponent<ObstacleController>();
                Debug.Log($"➕ Đã thêm ObstacleController vào {child.name}");
            }
            
            spawnedObstacles.Add(child.gameObject);
        }
        
        Debug.Log($"✅ Sử dụng {spawnedObstacles.Count} vật cản có sẵn");
    }
    
    // ==================== SPAWN VẬT CẢN MỚI ====================
    void SpawnNewObstacles()
    {
        if (obstaclePrefab == null)
        {
            Debug.LogError("❌ Chưa gán Obstacle Prefab!");
            return;
        }
        
        spawnedObstacles.Clear();
        
        for (int i = 0; i < spawnCount; i++)
        {
            // Tạo vật cản mới
            GameObject obstacle = Instantiate(obstaclePrefab, transform);
            obstacle.name = $"Obstacle_{i}";
            
            // Thêm ObstacleController nếu chưa có
            if (autoAddController && obstacle.GetComponent<ObstacleController>() == null)
            {
                obstacle.AddComponent<ObstacleController>();
            }
            
            spawnedObstacles.Add(obstacle);
        }
        
        Debug.Log($"✅ Đã spawn {spawnedObstacles.Count} vật cản mới");
    }
    
    // ==================== SẮP XẾP VẬT CẢN ====================
    void ArrangeObstacles()
    {
        if (spawnedObstacles.Count == 0)
        {
            Debug.LogWarning("⚠️ Không có vật cản để sắp xếp!");
            return;
        }
        
        for (int i = 0; i < spawnedObstacles.Count; i++)
        {
            GameObject obstacle = spawnedObstacles[i];
            if (obstacle == null) continue;
            
            // Tính vị trí Y
            float yPos = startY + (i * spacingY);
            
            // Đặt vị trí
            Vector3 pos = obstacle.transform.position;
            pos.y = yPos;
            obstacle.transform.position = pos;
            
            Debug.Log($"📍 {obstacle.name}: Y = {yPos:F1}");
        }
    }
    
    // ==================== PUBLIC METHODS ====================
    
    /// <summary>
    /// Reset và sắp xếp lại tất cả vật cản
    /// </summary>
    public void ResetAndArrange()
    {
        ArrangeObstacles();
        Debug.Log("🔄 Đã reset và sắp xếp lại vật cản");
    }
    
    /// <summary>
    /// Thay đổi khoảng cách và sắp xếp lại
    /// </summary>
    public void SetSpacing(float spacing)
    {
        spacingY = Mathf.Max(0.5f, spacing);
        ArrangeObstacles();
    }
    
    /// <summary>
    /// Spawn thêm vật cản
    /// </summary>
    public void SpawnMore(int count)
    {
        if (obstaclePrefab == null || useExistingObstacles)
        {
            Debug.LogWarning("⚠️ Không thể spawn thêm trong chế độ này");
            return;
        }
        
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
        Debug.Log($"➕ Đã spawn thêm {count} vật cản");
    }
    
    /// <summary>
    /// Xóa tất cả vật cản đã spawn
    /// </summary>
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
        Debug.Log("🗑️ Đã xóa tất cả vật cản");
    }
    
    /// <summary>
    /// Lấy vật cản theo index
    /// </summary>
    public GameObject GetObstacle(int index)
    {
        if (index >= 0 && index < spawnedObstacles.Count)
        {
            return spawnedObstacles[index];
        }
        return null;
    }
    
    // ==================== DEBUG ====================
    void PrintSpawnInfo()
    {
        Debug.Log("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        Debug.Log("🎯 OBSTACLE SPAWNER - THÔNG TIN");
        Debug.Log("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        Debug.Log($"📊 Tổng số vật cản: {spawnedObstacles.Count}");
        Debug.Log($"📏 Khoảng cách Y: {spacingY}");
        Debug.Log($"📍 Vị trí bắt đầu: {startY:F1}");
        Debug.Log($"🔧 Mode: {(useExistingObstacles ? "Sử dụng có sẵn" : "Spawn mới")}");
        Debug.Log("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
    }
    
    // ==================== GIZMOS ====================
    void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;
        
        // Vẽ line kết nối các vật cản
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