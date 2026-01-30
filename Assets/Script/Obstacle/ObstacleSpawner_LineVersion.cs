using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// ⭐ OBSTACLE SPAWNER - LINE VERSION
/// - Fire&Line spawn theo 3 line: 2, 4, 6 (tránh line giữa)
/// - Mỗi obstacle trong container sẽ được gán vào 1 line ngẫu nhiên
/// - Obstacle di chuyển xuống theo line (không đổi X)
/// </summary>
public class ObstacleSpawner_LineVersion : MonoBehaviour
{
    [Header("=== ⭐ LINE SETTINGS ===")]
    [Tooltip("Danh sách line cho phép (Fire&Line: 2, 4, 6)")]
    [SerializeField] private int[] allowedLines = new int[] { 2, 4, 6 };
    
    [Tooltip("Mỗi obstacle spawn ở line khác nhau?")]
    [SerializeField] private bool distributeLinesEvenly = true;
    
    [Tooltip("Random line mỗi lần loop?")]
    [SerializeField] private bool randomizeLineOnLoop = true;
    
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
    private Dictionary<GameObject, int> obstacleLines = new Dictionary<GameObject, int>();
    
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
        
        // ⭐ Sắp xếp vật cản THEO LINE
        ArrangeObstaclesWithLines();
        
        PrintSpawnInfo();
    }
    
    // ==================== SỬ DỤNG VẬT CẢN CÓ SẴN ====================
    void UseExistingObstacles()
    {
        spawnedObstacles.Clear();
        obstacleLines.Clear();
        
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
        obstacleLines.Clear();
        
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
    
    // ==================== ⭐ SẮP XẾP VẬT CẢN THEO LINE ====================
    void ArrangeObstaclesWithLines()
    {
        if (spawnedObstacles.Count == 0)
        {
            Debug.LogWarning("⚠️ Không có vật cản để sắp xếp!");
            return;
        }
        
        if (LineSystem.Instance == null)
        {
            Debug.LogError("❌ LineSystem không tồn tại! Vui lòng thêm LineSystem vào scene.");
            // Fallback: sắp xếp thông thường không theo line
            ArrangeObstaclesNormal();
            return;
        }
        
        Debug.Log("\n━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        Debug.Log("📍 FIRE&LINE - SẮP XẾP THEO LINE");
        Debug.Log("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        
        for (int i = 0; i < spawnedObstacles.Count; i++)
        {
            GameObject obstacle = spawnedObstacles[i];
            if (obstacle == null) continue;
            
            // ⭐ Chọn line cho obstacle này
            int lineIndex;
            
            if (distributeLinesEvenly)
            {
                // Phân bổ đều: obstacle 0 → line[0], obstacle 1 → line[1], ...
                lineIndex = allowedLines[i % allowedLines.Length];
            }
            else
            {
                // Random
                lineIndex = allowedLines[Random.Range(0, allowedLines.Length)];
            }
            
            // Lưu line của obstacle
            obstacleLines[obstacle] = lineIndex;
            
            // Lấy vị trí X của line
            float lineX = LineSystem.Instance.GetLineX(lineIndex);
            
            // Tính vị trí Y
            float yPos = startY + (i * spacingY);
            
            // Đặt vị trí
            Vector3 pos = obstacle.transform.position;
            pos.x = lineX;
            pos.y = yPos;
            obstacle.transform.position = pos;
            
            // ⭐ Lock X position của obstacle (freeze X)
            Rigidbody2D rb = obstacle.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.constraints = RigidbodyConstraints2D.FreezePositionX | RigidbodyConstraints2D.FreezeRotation;
            }
            
            Debug.Log($"📍 {obstacle.name}: Line {lineIndex} | X = {lineX:F2} | Y = {yPos:F2}");
        }
        
        Debug.Log("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n");
    }
    
    // Fallback nếu không có LineSystem
    void ArrangeObstaclesNormal()
    {
        for (int i = 0; i < spawnedObstacles.Count; i++)
        {
            GameObject obstacle = spawnedObstacles[i];
            if (obstacle == null) continue;
            
            float yPos = startY + (i * spacingY);
            
            Vector3 pos = obstacle.transform.position;
            pos.y = yPos;
            obstacle.transform.position = pos;
            
            Debug.Log($"📍 {obstacle.name}: Y = {yPos:F1} (No Line)");
        }
    }
    
    // ==================== PUBLIC METHODS ====================
    
    /// <summary>
    /// Reset và sắp xếp lại tất cả vật cản
    /// </summary>
    public void ResetAndArrange()
    {
        ArrangeObstaclesWithLines();
        Debug.Log("🔄 Đã reset và sắp xếp lại vật cản");
    }
    
    /// <summary>
    /// ⭐ Randomize line cho tất cả obstacles (dùng khi loop)
    /// </summary>
    public void RandomizeAllLines()
    {
        if (!randomizeLineOnLoop || LineSystem.Instance == null) return;
        
        foreach (GameObject obstacle in spawnedObstacles)
        {
            if (obstacle == null) continue;
            
            // Random line mới
            int newLine = allowedLines[Random.Range(0, allowedLines.Length)];
            obstacleLines[obstacle] = newLine;
            
            // Update vị trí X
            float lineX = LineSystem.Instance.GetLineX(newLine);
            Vector3 pos = obstacle.transform.position;
            pos.x = lineX;
            obstacle.transform.position = pos;
            
            Debug.Log($"🔄 {obstacle.name} → Line {newLine}");
        }
    }
    
    /// <summary>
    /// ⭐ Đặt obstacle vào line cụ thể
    /// </summary>
    public void SetObstacleLine(GameObject obstacle, int lineIndex)
    {
        if (LineSystem.Instance == null) return;
        if (!spawnedObstacles.Contains(obstacle)) return;
        
        obstacleLines[obstacle] = lineIndex;
        
        float lineX = LineSystem.Instance.GetLineX(lineIndex);
        Vector3 pos = obstacle.transform.position;
        pos.x = lineX;
        obstacle.transform.position = pos;
        
        Debug.Log($"📍 {obstacle.name} → Line {lineIndex} (X = {lineX:F2})");
    }
    
    /// <summary>
    /// Lấy line hiện tại của obstacle
    /// </summary>
    public int GetObstacleLine(GameObject obstacle)
    {
        if (obstacleLines.ContainsKey(obstacle))
            return obstacleLines[obstacle];
        return -1;
    }
    
    /// <summary>
    /// Thay đổi khoảng cách và sắp xếp lại
    /// </summary>
    public void SetSpacing(float spacing)
    {
        spacingY = Mathf.Max(0.5f, spacing);
        ArrangeObstaclesWithLines();
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
        
        ArrangeObstaclesWithLines();
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
        obstacleLines.Clear();
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
        Debug.Log("🎯 OBSTACLE SPAWNER (LINE VERSION)");
        Debug.Log("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        Debug.Log($"📊 Tổng số vật cản: {spawnedObstacles.Count}");
        Debug.Log($"📏 Khoảng cách Y: {spacingY}");
        Debug.Log($"📍 Vị trí bắt đầu: {startY:F1}");
        Debug.Log($"🔧 Mode: {(useExistingObstacles ? "Sử dụng có sẵn" : "Spawn mới")}");
        Debug.Log($"⭐ Lines cho phép: {string.Join(", ", allowedLines)}");
        Debug.Log($"⭐ Phân bổ đều: {(distributeLinesEvenly ? "YES" : "NO")}");
        Debug.Log("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
    }
    
    // ==================== GIZMOS ====================
    void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;
        
        // Vẽ line kết nối các vật cản cùng line
        if (LineSystem.Instance != null && obstacleLines.Count > 0)
        {
            // Group obstacles by line
            Dictionary<int, List<GameObject>> lineGroups = new Dictionary<int, List<GameObject>>();
            
            foreach (var kvp in obstacleLines)
            {
                int line = kvp.Value;
                if (!lineGroups.ContainsKey(line))
                    lineGroups[line] = new List<GameObject>();
                
                lineGroups[line].Add(kvp.Key);
            }
            
            // Draw connections for each line group
            foreach (var group in lineGroups.Values)
            {
                Gizmos.color = Color.yellow;
                for (int i = 0; i < group.Count - 1; i++)
                {
                    if (group[i] == null || group[i + 1] == null)
                        continue;
                    
                    Gizmos.DrawLine(
                        group[i].transform.position,
                        group[i + 1].transform.position
                    );
                }
            }
        }
    }
}