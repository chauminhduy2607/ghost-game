using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// ⭐ UNIVERSAL SPAWNER - SPAWN TẤT CẢ VẬT CẢN
/// - Pattern: 6 Fire&Line → 1 FlyingCircle → lặp lại
/// - Tự động sắp xếp và loop
/// - GẮN VÀO 1 EMPTY OBJECT (VD: ObstacleManager)
/// </summary>
public class UniversalObstacleSpawner : MonoBehaviour
{
    [Header("=== PREFABS ===")]
    [SerializeField] private GameObject fireLinePrefab;
    [SerializeField] private GameObject flyingCirclePrefab;
    
    [Header("=== SPAWN PATTERN ===")]
    [Tooltip("Số Fire&Line trước khi spawn FlyingCircle")]
    [SerializeField] private int fireLinesBeforeCircle = 6;
    
    [Tooltip("Tổng số vật cản spawn")]
    [SerializeField] private int totalObstacleCount = 20;
    
    [Header("=== SPACING ===")]
    [SerializeField] private float spacingY = 1.5f;
    [SerializeField] private float startY = 5f;
    
    [Header("=== REFERENCES ===")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Transform player;
    
    [Header("=== USE EXISTING ===")]
    [Tooltip("Sử dụng vật cản có sẵn trong scene (con của object này)")]
    [SerializeField] private bool useExistingObstacles = true;
    
    [Header("=== LOOP ===")]
    [SerializeField] private float loopThreshold = 0.5f;
    [SerializeField] private bool randomizeOnLoop = true;
    
    [Header("=== DEBUG ===")]
    [SerializeField] private bool showDebugInfo = true;
    
    // Private
    private List<GameObject> allObstacles = new List<GameObject>();
    private int spawnCounter = 0;
    private float screenHeight;
    private int totalLooped = 0;
    
    void Start()
    {
        // Auto references
        if (mainCamera == null)
            mainCamera = Camera.main;
        
        if (player == null)
        {
            GhostController ghost = FindObjectOfType<GhostController>();
            if (ghost != null)
                player = ghost.transform;
        }
        
        screenHeight = mainCamera.orthographicSize * 2f;
        
        // Tính start Y nếu có player
        if (player != null && startY == 0f)
        {
            startY = player.position.y + screenHeight * 0.3f;
        }
        
        // Spawn hoặc sử dụng có sẵn
        if (useExistingObstacles)
        {
            UseExistingObstacles();
        }
        else
        {
            SpawnAllObstacles();
        }
        
        ArrangeAllObstacles();
        
        PrintInfo();
    }
    
    void Update()
    {
        CheckAndLoopObstacles();
    }
    
    // ==================== SỬ DỤNG VẬT CẢN CÓ SẴN ====================
    void UseExistingObstacles()
    {
        allObstacles.Clear();
        
        foreach (Transform child in transform)
        {
            if (child == null) continue;
            
            // Setup dựa vào loại
            if (child.name.Contains("Fire") || child.name.Contains("Obstacle"))
            {
                SetupFireLine(child.gameObject);
            }
            else if (child.name.Contains("Circle") || child.name.Contains("Flying"))
            {
                SetupFlyingCircle(child.gameObject);
            }
            
            allObstacles.Add(child.gameObject);
        }
        
        Debug.Log($"✅ Sử dụng {allObstacles.Count} vật cản có sẵn");
    }
    
    // ==================== SPAWN VẬT CẢN MỚI ====================
    void SpawnAllObstacles()
    {
        if (fireLinePrefab == null || flyingCirclePrefab == null)
        {
            Debug.LogError("❌ Chưa gán Prefab!");
            return;
        }
        
        allObstacles.Clear();
        spawnCounter = 0;
        
        for (int i = 0; i < totalObstacleCount; i++)
        {
            GameObject obstacle = SpawnNextObstacle();
            if (obstacle != null)
            {
                obstacle.name = $"{obstacle.name}_{i}";
                allObstacles.Add(obstacle);
            }
        }
        
        Debug.Log($"✅ Đã spawn {allObstacles.Count} vật cản");
        Debug.Log($"   - Fire&Line: {CountType("Fire")}");
        Debug.Log($"   - FlyingCircle: {CountType("Circle")}");
    }
    
    GameObject SpawnNextObstacle()
    {
        GameObject obstacle;
        
        // Pattern: 6 Fire&Line → 1 FlyingCircle
        if (spawnCounter % (fireLinesBeforeCircle + 1) < fireLinesBeforeCircle)
        {
            // Spawn Fire&Line
            obstacle = Instantiate(fireLinePrefab, transform);
            SetupFireLine(obstacle);
        }
        else
        {
            // Spawn FlyingCircle
            obstacle = Instantiate(flyingCirclePrefab, transform);
            SetupFlyingCircle(obstacle);
        }
        
        spawnCounter++;
        return obstacle;
    }
    
    // ==================== SETUP FIRE&LINE ====================
    void SetupFireLine(GameObject obj)
    {
        // Thêm ObstacleSpawner nếu chưa có
        ObstacleSpawner spawner = obj.GetComponent<ObstacleSpawner>();
        if (spawner == null)
        {
            spawner = obj.AddComponent<ObstacleSpawner>();
        }
        
        // Thêm ObstacleLooper nếu chưa có
        ObstacleLooper looper = obj.GetComponent<ObstacleLooper>();
        if (looper == null)
        {
            looper = obj.AddComponent<ObstacleLooper>();
        }
    }
    
    // ==================== SETUP FLYING CIRCLE ====================
    void SetupFlyingCircle(GameObject obj)
    {
        // Thêm FlyingCircleController nếu chưa có
        FlyingCircleController controller = obj.GetComponent<FlyingCircleController>();
        if (controller == null)
        {
            controller = obj.AddComponent<FlyingCircleController>();
        }
    }
    
    // ==================== SẮP XẾP TẤT CẢ ====================
    void ArrangeAllObstacles()
    {
        if (allObstacles.Count == 0) return;
        
        for (int i = 0; i < allObstacles.Count; i++)
        {
            GameObject obstacle = allObstacles[i];
            if (obstacle == null) continue;
            
            float yPos = startY + (i * spacingY);
            
            Vector3 pos = obstacle.transform.position;
            pos.y = yPos;
            obstacle.transform.position = pos;
            
            // Gọi SetPositionY nếu có
            var fireLine = obstacle.GetComponent<ObstacleSpawner>();
            if (fireLine != null)
            {
                // Fire&Line sẽ tự arrange các con của nó
            }
            
            var circle = obstacle.GetComponent<FlyingCircleController>();
            if (circle != null)
            {
                circle.SetPositionY(yPos);
            }
        }
    }
    
    // ==================== LOOP VẬT CẢN ====================
    void CheckAndLoopObstacles()
    {
        if (allObstacles.Count == 0) return;
        
        float cameraBottomEdge = mainCamera.transform.position.y - mainCamera.orthographicSize;
        float loopLine = cameraBottomEdge - (screenHeight * loopThreshold);
        
        GameObject lowestObstacle = null;
        float lowestY = float.MaxValue;
        float highestY = float.MinValue;
        
        // Tìm thấp nhất và cao nhất
        foreach (GameObject obstacle in allObstacles)
        {
            if (obstacle == null) continue;
            
            float y = obstacle.transform.position.y;
            
            if (y < lowestY)
            {
                lowestY = y;
                lowestObstacle = obstacle;
            }
            
            if (y > highestY)
            {
                highestY = y;
            }
        }
        
        // Loop
        if (lowestObstacle != null && lowestY < loopLine)
        {
            float newY = highestY + spacingY;
            
            Vector3 pos = lowestObstacle.transform.position;
            pos.y = newY;
            lowestObstacle.transform.position = pos;
            
            // Randomize nếu cần
            if (randomizeOnLoop)
            {
                RandomizeObstacle(lowestObstacle);
            }
            
            totalLooped++;
            
            if (totalLooped % 5 == 0)
                Debug.Log($"♻️ Đã loop {totalLooped} lần");
        }
    }
    
    void RandomizeObstacle(GameObject obstacle)
    {
        // Fire&Line
        var spawner = obstacle.GetComponent<ObstacleSpawner>();
        if (spawner != null)
        {
            // Spawner sẽ tự randomize các obstacle con của nó
            foreach (var obs in spawner.SpawnedObstacles)
            {
                var controller = obs.GetComponent<ObstacleController>();
                if (controller != null)
                {
                    controller.RandomizeOnly();
                }
            }
        }
        
        // FlyingCircle
        var circle = obstacle.GetComponent<FlyingCircleController>();
        if (circle != null)
        {
            circle.RandomizeAngles();
        }
    }
    
    // ==================== HELPER ====================
    int CountType(string keyword)
    {
        int count = 0;
        foreach (var obs in allObstacles)
        {
            if (obs != null && obs.name.Contains(keyword))
                count++;
        }
        return count;
    }
    
    void PrintInfo()
    {
        Debug.Log("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        Debug.Log("🌍 UNIVERSAL OBSTACLE SPAWNER");
        Debug.Log("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        Debug.Log($"📊 Tổng vật cản: {allObstacles.Count}");
        Debug.Log($"🔥 Fire&Line: {CountType("Fire")}");
        Debug.Log($"⭕ FlyingCircle: {CountType("Circle")}");
        Debug.Log($"📏 Pattern: {fireLinesBeforeCircle} Fire&Line → 1 Circle");
        Debug.Log($"📍 Spacing Y: {spacingY}");
        Debug.Log("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
    }
    
    // ==================== DEBUG UI ====================
    void OnGUI()
    {
        if (!showDebugInfo || !Application.isPlaying) return;
        
        GUIStyle style = new GUIStyle();
        style.fontSize = 16;
        style.normal.textColor = Color.yellow;
        style.alignment = TextAnchor.LowerLeft;
        style.fontStyle = FontStyle.Bold;
        
        int onScreen = 0;
        float cameraBottom = mainCamera.transform.position.y - mainCamera.orthographicSize;
        float cameraTop = mainCamera.transform.position.y + mainCamera.orthographicSize;
        
        foreach (GameObject obstacle in allObstacles)
        {
            if (obstacle == null) continue;
            float y = obstacle.transform.position.y;
            if (y >= cameraBottom && y <= cameraTop)
                onScreen++;
        }
        
        string info = "🌍 UNIVERSAL SPAWNER\n";
        info += "━━━━━━━━━━━━━━━\n";
        info += $"Total: {allObstacles.Count}\n";
        info += $"Fire: {CountType("Fire")}\n";
        info += $"Circle: {CountType("Circle")}\n";
        info += $"On Screen: {onScreen}\n";
        info += $"Looped: {totalLooped}";
        
        GUI.Label(new Rect(10, Screen.height - 150, 250, 150), info, style);
    }
    
    // ==================== PUBLIC METHODS ====================
    
    public void ResetAll()
    {
        ArrangeAllObstacles();
        totalLooped = 0;
        Debug.Log("🔄 Reset tất cả vật cản!");
    }
    
    public void SetSpacing(float spacing)
    {
        spacingY = Mathf.Max(0.5f, spacing);
        ArrangeAllObstacles();
    }
}