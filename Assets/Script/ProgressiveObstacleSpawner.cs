using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// ⭐ PROGRESSIVE SPAWNER - SPAWN NHANH THEO GHOST
/// - Vị trí spawn như cũ (không spawn xa)
/// - Tốc độ spawn tăng khi Ghost bay nhanh
/// - Pattern: 6 Fire&Line → 2 FlyingCircle
/// - Tăng tốc độ vật cản dần dần
/// </summary>
public class ProgressiveObstacleSpawner : MonoBehaviour
{
    [Header("=== PREFABS ===")]
    [SerializeField] private GameObject fireLinePrefab;
    [SerializeField] private GameObject flyingCirclePrefab;
    
    [Header("=== PATTERN ===")]
    [Tooltip("Số Fire&Line mỗi lần spawn")]
    [SerializeField] private int fireLinesPerWave = 6;
    
    [Tooltip("Số FlyingCircle mỗi lần spawn")]
    [SerializeField] private int circlesPerWave = 2;
    
    [Header("=== SPAWN POSITION ===")]
    [Tooltip("Khoảng cách tối thiểu từ Ghost đến vật cản tiếp theo")]
    [SerializeField] private float spawnDistanceFromGhost = 8f;
    
    [Header("=== SPACING ===")]
    [SerializeField] private float spacingY = 1.5f;
    [SerializeField] private float startY = 5f;
    
    [Header("=== SPEED PROGRESSION ===")]
    [Tooltip("Tốc độ ban đầu của vật cản")]
    [SerializeField] private float initialSpeed = 1.0f;
    
    [Tooltip("Tăng tốc độ mỗi wave (thêm vào)")]
    [SerializeField] private float speedIncrement = 0.2f;
    
    [Tooltip("Tốc độ tối đa")]
    [SerializeField] private float maxSpeed = 5.0f;
    
    [Header("=== ROTATION SPEED ===")]
    [Tooltip("Tốc độ quay ban đầu của FlyingCircle")]
    [SerializeField] private float initialRotationSpeed = 120f;
    
    [Tooltip("Tăng tốc độ quay mỗi wave")]
    [SerializeField] private float rotationSpeedIncrement = 20f;
    
    [Tooltip("Tốc độ quay tối đa")]
    [SerializeField] private float maxRotationSpeed = 360f;
    
    [Header("=== REFERENCES ===")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Transform player;
    
    [Header("=== USE EXISTING ===")]
    [SerializeField] private bool useExistingObstacles = true;
    
    [Header("=== LOOP ===")]
    [SerializeField] private float loopThreshold = 0.5f;
    [SerializeField] private bool randomizeOnLoop = true;
    
    [Header("=== DEBUG ===")]
    [SerializeField] private bool showDebugInfo = true;
    
    // Private
    private List<GameObject> allObstacles = new List<GameObject>();
    private float screenHeight;
    private int totalLooped = 0;
    private int currentWave = 0;
    private float currentSpeed;
    private float currentRotationSpeed;
    private int spawnCounter = 0;
    
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
        
        // Tính start Y
        if (player != null && startY == 0f)
        {
            startY = player.position.y + screenHeight * 0.3f;
        }
        
        // Reset speed
        currentSpeed = initialSpeed;
        currentRotationSpeed = initialRotationSpeed;
        
        // Spawn hoặc sử dụng có sẵn
        if (useExistingObstacles)
        {
            UseExistingObstacles();
        }
        else
        {
            SpawnInitialPattern();
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
            
            if (child.name.Contains("Fire") || child.name.Contains("Obstacle"))
            {
                SetupFireLine(child.gameObject, currentSpeed);
            }
            else if (child.name.Contains("Circle") || child.name.Contains("Flying"))
            {
                SetupFlyingCircle(child.gameObject, currentRotationSpeed);
            }
            
            allObstacles.Add(child.gameObject);
        }
        
        Debug.Log($"✅ Sử dụng {allObstacles.Count} vật cản có sẵn");
    }
    
    // ==================== SPAWN PATTERN BAN ĐẦU ====================
    void SpawnInitialPattern()
    {
        if (fireLinePrefab == null || flyingCirclePrefab == null)
        {
            Debug.LogError("❌ Chưa gán Prefab!");
            return;
        }
        
        allObstacles.Clear();
        
        // Spawn 3 waves ban đầu
        for (int wave = 0; wave < 3; wave++)
        {
            // Spawn Fire&Lines
            for (int i = 0; i < fireLinesPerWave; i++)
            {
                GameObject fireLine = Instantiate(fireLinePrefab, transform);
                fireLine.name = $"Fire&Line_{spawnCounter++}";
                SetupFireLine(fireLine, currentSpeed);
                allObstacles.Add(fireLine);
            }
            
            // Spawn FlyingCircles
            for (int i = 0; i < circlesPerWave; i++)
            {
                GameObject circle = Instantiate(flyingCirclePrefab, transform);
                circle.name = $"FlyingCircle_{spawnCounter++}";
                SetupFlyingCircle(circle, currentRotationSpeed);
                allObstacles.Add(circle);
            }
            
            // Tăng độ khó cho wave tiếp theo
            IncreaseWaveDifficulty();
        }
        
        Debug.Log($"✅ Đã spawn {allObstacles.Count} vật cản");
    }
    
    // ==================== SETUP FIRE&LINE ====================
    void SetupFireLine(GameObject obj, float speed)
    {
        ObstacleSpawner spawner = obj.GetComponent<ObstacleSpawner>();
        if (spawner == null)
        {
            spawner = obj.AddComponent<ObstacleSpawner>();
        }
        
        ObstacleLooper looper = obj.GetComponent<ObstacleLooper>();
        if (looper == null)
        {
            looper = obj.AddComponent<ObstacleLooper>();
        }
        
        StartCoroutine(SetFireLineSpeed(obj, speed));
    }
    
    System.Collections.IEnumerator SetFireLineSpeed(GameObject fireLine, float speed)
    {
        yield return null;
        
        ObstacleSpawner spawner = fireLine.GetComponent<ObstacleSpawner>();
        if (spawner != null && spawner.SpawnedObstacles != null)
        {
            foreach (GameObject obstacle in spawner.SpawnedObstacles)
            {
                if (obstacle == null) continue;
                
                ObstacleMovement movement = obstacle.GetComponent<ObstacleMovement>();
                if (movement != null)
                {
                    movement.SetSpeed(speed);
                }
            }
        }
    }
    
    // ==================== SETUP FLYING CIRCLE ====================
    void SetupFlyingCircle(GameObject obj, float rotSpeed)
    {
        FlyingCircleController controller = obj.GetComponent<FlyingCircleController>();
        if (controller == null)
        {
            controller = obj.AddComponent<FlyingCircleController>();
        }
        
        controller.SetRotationSpeed(rotSpeed);
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
        }
    }
    
    // ==================== LOOP VẬT CẢN - THEO WAVE ====================
    void CheckAndLoopObstacles()
    {
        if (allObstacles.Count == 0 || player == null) return;
        
        float ghostY = player.position.y;
        float cameraBottomEdge = mainCamera.transform.position.y - mainCamera.orthographicSize;
        
        // ⭐ ĐIỀU CHỈNH LOOP THRESHOLD DỰA VÀO TỐC ĐỘ GHOST
        GhostController ghost = player.GetComponent<GhostController>();
        float ghostVelocityY = ghost != null ? ghost.Velocity.y : 0f;
        
        float dynamicLoopThreshold = loopThreshold;
        if (ghostVelocityY > 5f)
        {
            dynamicLoopThreshold = loopThreshold + 2f;
        }
        else if (ghostVelocityY > 3f)
        {
            dynamicLoopThreshold = loopThreshold + 1f;
        }
        
        float loopLine = cameraBottomEdge - (screenHeight * dynamicLoopThreshold);
        
        // ⭐ TÌM VẬT CẢN THẤP NHẤT VÀ CAO NHẤT
        float lowestY = float.MaxValue;
        float highestY = float.MinValue;
        
        foreach (GameObject obstacle in allObstacles)
        {
            if (obstacle == null) continue;
            
            float y = obstacle.transform.position.y;
            
            if (y < lowestY)
                lowestY = y;
            
            if (y > highestY)
                highestY = y;
        }
        
        // ⭐ KIỂM TRA XEM CẦN LOOP WAVE KHÔNG
        float requiredHighestY = ghostY + spawnDistanceFromGhost;
        
        if (lowestY < loopLine || highestY < requiredHighestY)
        {
            // ⭐⭐ LOOP CẢ WAVE (6 Fire + 2 Circle)
            LoopNextWave(loopLine, highestY);
        }
    }
    
    // ⭐⭐ LOOP MỘT WAVE HOÀN CHỈNH
    void LoopNextWave(float loopLine, float currentHighestY)
    {
        List<GameObject> toLoop = new List<GameObject>();
        
        // Tìm các vật cản cần loop (dưới loopLine)
        foreach (GameObject obstacle in allObstacles)
        {
            if (obstacle == null) continue;
            
            if (obstacle.transform.position.y < loopLine)
            {
                toLoop.Add(obstacle);
            }
        }
        
        if (toLoop.Count == 0) return;
        
        // ⭐⭐ SẮP XẾP THEO PATTERN: 6 Fire rồi 2 Circle
        List<GameObject> fireLines = new List<GameObject>();
        List<GameObject> circles = new List<GameObject>();
        
        foreach (GameObject obstacle in toLoop)
        {
            if (obstacle.name.Contains("Fire"))
            {
                fireLines.Add(obstacle);
            }
            else if (obstacle.name.Contains("Circle"))
            {
                circles.Add(obstacle);
            }
        }
        
        float newY = currentHighestY + spacingY;
        
        // Loop 6 Fire&Line trước
        int fireCount = Mathf.Min(fireLines.Count, fireLinesPerWave);
        for (int i = 0; i < fireCount; i++)
        {
            Vector3 pos = fireLines[i].transform.position;
            pos.y = newY;
            fireLines[i].transform.position = pos;
            
            if (randomizeOnLoop)
                RandomizeObstacle(fireLines[i]);
            
            ApplyCurrentSpeedToObstacle(fireLines[i]);
            
            newY += spacingY;
            totalLooped++;
        }
        
        // Loop 2 Circle sau
        int circleCount = Mathf.Min(circles.Count, circlesPerWave);
        for (int i = 0; i < circleCount; i++)
        {
            Vector3 pos = circles[i].transform.position;
            pos.y = newY;
            circles[i].transform.position = pos;
            
            if (randomizeOnLoop)
                RandomizeObstacle(circles[i]);
            
            ApplyCurrentSpeedToObstacle(circles[i]);
            
            newY += spacingY;
            totalLooped++;
        }
        
        // Tăng độ khó sau mỗi wave
        if (fireCount > 0 || circleCount > 0)
        {
            IncreaseWaveDifficulty();
            Debug.Log($"♻️ Loop Wave {currentWave}: {fireCount} Fire + {circleCount} Circle");
        }
    }
    
    void RandomizeObstacle(GameObject obstacle)
    {
        var spawner = obstacle.GetComponent<ObstacleSpawner>();
        if (spawner != null && spawner.SpawnedObstacles != null)
        {
            foreach (var obs in spawner.SpawnedObstacles)
            {
                var controller = obs.GetComponent<ObstacleController>();
                if (controller != null)
                {
                    controller.RandomizeOnly();
                }
            }
        }
        
        var circle = obstacle.GetComponent<FlyingCircleController>();
        if (circle != null)
        {
            circle.RandomizeAngles();
        }
    }
    
    void ApplyCurrentSpeedToObstacle(GameObject obstacle)
    {
        var spawner = obstacle.GetComponent<ObstacleSpawner>();
        if (spawner != null && spawner.SpawnedObstacles != null)
        {
            foreach (var obs in spawner.SpawnedObstacles)
            {
                var movement = obs.GetComponent<ObstacleMovement>();
                if (movement != null)
                {
                    movement.SetSpeed(currentSpeed);
                }
            }
        }
        
        var circle = obstacle.GetComponent<FlyingCircleController>();
        if (circle != null)
        {
            circle.SetRotationSpeed(currentRotationSpeed);
        }
    }
    
    // ==================== TĂNG ĐỘ KHÓ ====================
    void IncreaseWaveDifficulty()
    {
        currentWave++;
        
        currentSpeed = Mathf.Min(currentSpeed + speedIncrement, maxSpeed);
        currentRotationSpeed = Mathf.Min(currentRotationSpeed + rotationSpeedIncrement, maxRotationSpeed);
        
        Debug.Log($"📈 Wave {currentWave}: Speed = {currentSpeed:F1}, Rotation = {currentRotationSpeed:F0}°/s");
    }
    
    // ==================== DEBUG ====================
    void PrintInfo()
    {
        Debug.Log("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        Debug.Log("🎮 PROGRESSIVE SPAWNER V2");
        Debug.Log("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        Debug.Log($"📊 Pattern: {fireLinesPerWave} Fire → {circlesPerWave} Circle");
        Debug.Log($"📍 Spawn khi Ghost cần: {spawnDistanceFromGhost}");
        Debug.Log($"🏁 Tốc độ ban đầu: {initialSpeed:F1}");
        Debug.Log($"📈 Tăng tốc: +{speedIncrement:F1} mỗi wave");
        Debug.Log($"⚡ Loop động theo tốc độ Ghost");
        Debug.Log("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
    }
    
    void OnGUI()
    {
        if (!showDebugInfo || !Application.isPlaying || player == null) return;
        
        GUIStyle style = new GUIStyle();
        style.fontSize = 16;
        style.normal.textColor = Color.yellow;
        style.alignment = TextAnchor.LowerLeft;
        style.fontStyle = FontStyle.Bold;
        
        GhostController ghost = player.GetComponent<GhostController>();
        float ghostVelocity = ghost != null ? ghost.Velocity.y : 0f;
        
        // Tính vật cản cao nhất
        float highestY = float.MinValue;
        foreach (GameObject obstacle in allObstacles)
        {
            if (obstacle != null && obstacle.transform.position.y > highestY)
            {
                highestY = obstacle.transform.position.y;
            }
        }
        
        float distanceAhead = highestY - player.position.y;
        
        string info = "🎮 SPAWNER V2\n";
        info += "━━━━━━━━━━━━━━━━━\n";
        info += $"Wave: {currentWave}\n";
        info += $"Speed: {currentSpeed:F1}\n";
        info += $"Ghost Vel: {ghostVelocity:F1}\n";
        info += $"Distance Ahead: {distanceAhead:F1}\n";
        info += $"Looped: {totalLooped}";
        
        GUI.Label(new Rect(10, Screen.height - 140, 250, 140), info, style);
    }
    
    // ==================== PUBLIC METHODS ====================
    
    public void ResetDifficulty()
    {
        currentWave = 0;
        currentSpeed = initialSpeed;
        currentRotationSpeed = initialRotationSpeed;
        totalLooped = 0;
        
        Debug.Log("🔄 Reset độ khó!");
    }
}