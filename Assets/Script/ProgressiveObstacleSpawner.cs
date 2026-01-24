using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// ⭐⭐⭐ PROGRESSIVE SPAWNER - COMPLETE GROUP LOOP
/// - Pattern: 4 Fire&Line → 1 FlyingCircle → 4 FlyingFire
/// - Loop ĐỒNG BỘ: Phải đủ CẢ GROUP mới loop
/// - Không bao giờ spawn loạn
/// - ✨ UPDATE: FlyingFire spawn SÁT sau Circle (bỏ groupSpacing)
/// </summary>
public class ProgressiveObstacleSpawner : MonoBehaviour
{
    [Header("=== PREFABS ===")]
    [SerializeField] private GameObject fireLinePrefab;
    [SerializeField] private GameObject flyingCirclePrefab;
    [SerializeField] private GameObject flyingFirePrefab;
    
    [Header("=== PATTERN ===")]
    [SerializeField] private int fireLinesPerGroup = 4;
    [SerializeField] private int circlesPerGroup = 1;
    [SerializeField] private int flyingFiresPerGroup = 4;
    
    [Header("=== SPAWN POSITION ===")]
    [SerializeField] private float baseSpawnDistance = 25f;
    [SerializeField] private float velocityMultiplier = 3f;
    [SerializeField] private float maxSpawnDistance = 60f;
    
    [Header("=== SPACING ===")]
    [SerializeField] private float spacingY = 5.0f;
    [SerializeField] private float groupSpacing = 3.0f;  // Khoảng cách giữa các nhóm
    [SerializeField] private float startY = 5f;
    
    [Header("=== SPEED PROGRESSION ===")]
    [SerializeField] private float initialSpeed = 1.0f;
    [SerializeField] private float speedIncrement = 0.2f;
    [SerializeField] private float maxSpeed = 5.0f;
    
    [Header("=== ROTATION SPEED ===")]
    [SerializeField] private float initialRotationSpeed = 120f;
    [SerializeField] private float rotationSpeedIncrement = 20f;
    [SerializeField] private float maxRotationSpeed = 360f;
    
    [Header("=== REFERENCES ===")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Transform player;
    
    [Header("=== USE EXISTING ===")]
    [SerializeField] private bool useExistingObstacles = true;
    
    [Header("=== LOOP ===")]
    [SerializeField] private float loopThreshold = 1.5f;
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
    
    // ⭐⭐⭐ QUAN TRỌNG: Lưu vị trí Y của từng loại để đồng bộ
    private class GroupTracker
    {
        public List<GameObject> fireLines = new List<GameObject>();
        public List<GameObject> circles = new List<GameObject>();
        public List<GameObject> flyingFires = new List<GameObject>();
        
        public void Clear()
        {
            fireLines.Clear();
            circles.Clear();
            flyingFires.Clear();
        }
        
        public int TotalCount()
        {
            return fireLines.Count + circles.Count + flyingFires.Count;
        }
    }
    
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
        
        screenHeight = mainCamera.orthographicSize * 2f;
        
        if (player != null && startY == 0f)
        {
            startY = player.position.y + screenHeight * 0.3f;
        }
        
        currentSpeed = initialSpeed;
        currentRotationSpeed = initialRotationSpeed;
        
        if (useExistingObstacles)
        {
            UseExistingObstacles();
        }
        else
        {
            SpawnInitialPattern();
        }
        
        DisableCircleRotations();
        ArrangeAllObstaclesInPattern();
        StartCoroutine(EnableCircleRotationsDelayed());
        Invoke("ForceEnableAllCircles", 0.5f);
        
        PrintInfo();
    }
    
    void ForceEnableAllCircles()
    {
        CircleRotation[] rotations = FindObjectsOfType<CircleRotation>(true);
        foreach (CircleRotation rotation in rotations)
        {
            rotation.enabled = true;
        }
        if (rotations.Length > 0)
            Debug.Log($"✅ FORCE ENABLE {rotations.Length} CircleRotation!");
    }
    
    void DisableCircleRotations()
    {
        CircleRotation[] rotations = FindObjectsOfType<CircleRotation>();
        foreach (CircleRotation rotation in rotations)
        {
            rotation.enabled = false;
        }
    }
    
    System.Collections.IEnumerator EnableCircleRotationsDelayed()
    {
        yield return new WaitForEndOfFrame();
        
        CircleRotation[] rotations = FindObjectsOfType<CircleRotation>();
        foreach (CircleRotation rotation in rotations)
        {
            rotation.enabled = true;
        }
        
        if (rotations.Length > 0)
            Debug.Log($"✅ Đã bật {rotations.Length} CircleRotation!");
    }
    
    void Update()
    {
        CheckAndLoopObstacles();
    }
    
    void UseExistingObstacles()
    {
        allObstacles.Clear();
        
        foreach (Transform child in transform)
        {
            if (child == null) continue;
            
            string name = child.name.ToLower();
            
            if (name.Contains("fire") && name.Contains("line"))
            {
                SetupFireLine(child.gameObject, currentSpeed);
                allObstacles.Add(child.gameObject);
            }
            else if (name.Contains("circle") && name.Contains("flying"))
            {
                SetupFlyingCircle(child.gameObject, currentRotationSpeed);
                allObstacles.Add(child.gameObject);
            }
            else if (name.Contains("flying") && name.Contains("fire"))
            {
                SetupFlyingFire(child.gameObject, currentSpeed);
                allObstacles.Add(child.gameObject);
            }
        }
        
        Debug.Log($"✅ Sử dụng {allObstacles.Count} container");
    }
    
    void SpawnInitialPattern()
    {
        if (fireLinePrefab == null || flyingCirclePrefab == null)
        {
            Debug.LogError("❌ Chưa gán Prefab!");
            return;
        }
        
        allObstacles.Clear();
        int spawnCounter = 0;
        
        for (int group = 0; group < 3; group++)
        {
            for (int i = 0; i < fireLinesPerGroup; i++)
            {
                GameObject fireLine = Instantiate(fireLinePrefab, transform);
                fireLine.name = $"Fire&Line_{spawnCounter++}";
                SetupFireLine(fireLine, currentSpeed);
                allObstacles.Add(fireLine);
            }
            
            for (int i = 0; i < circlesPerGroup; i++)
            {
                GameObject circle = Instantiate(flyingCirclePrefab, transform);
                circle.name = $"FlyingCircle_{spawnCounter++}";
                SetupFlyingCircle(circle, currentRotationSpeed);
                allObstacles.Add(circle);
            }
            
            if (flyingFirePrefab != null)
            {
                for (int i = 0; i < flyingFiresPerGroup; i++)
                {
                    GameObject flyingFire = Instantiate(flyingFirePrefab, transform);
                    flyingFire.name = $"FlyingFire_{spawnCounter++}";
                    SetupFlyingFire(flyingFire, currentSpeed);
                    allObstacles.Add(flyingFire);
                }
            }
        }
        
        Debug.Log($"✅ Đã spawn {allObstacles.Count} vật cản");
    }
    
    void SetupFireLine(GameObject obj, float speed)
    {
        ObstacleSpawner spawner = obj.GetComponent<ObstacleSpawner>();
        if (spawner == null)
            spawner = obj.AddComponent<ObstacleSpawner>();
        
        ObstacleLooper looper = obj.GetComponent<ObstacleLooper>();
        if (looper != null)
        {
            looper.enabled = false;
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
    
    void SetupFlyingCircle(GameObject obj, float rotSpeed)
    {
        FlyingCircleController controller = obj.GetComponent<FlyingCircleController>();
        if (controller == null)
            controller = obj.AddComponent<FlyingCircleController>();
        
        controller.SetRotationSpeed(rotSpeed);
    }
    
    void SetupFlyingFire(GameObject obj, float speed)
    {
        ObstacleSpawner spawner = obj.GetComponent<ObstacleSpawner>();
        if (spawner == null)
            spawner = obj.AddComponent<ObstacleSpawner>();
        
        ObstacleLooper looper = obj.GetComponent<ObstacleLooper>();
        if (looper != null)
        {
            looper.enabled = false;
        }
        
        StartCoroutine(SetFlyingFireSpeed(obj, speed));
    }
    
    System.Collections.IEnumerator SetFlyingFireSpeed(GameObject flyingFire, float speed)
    {
        yield return null;
        
        ObstacleSpawner spawner = flyingFire.GetComponent<ObstacleSpawner>();
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
    
    void ArrangeAllObstaclesInPattern()
    {
        if (allObstacles.Count == 0) return;
        
        List<GameObject> fireLineContainers = new List<GameObject>();
        List<GameObject> circleContainers = new List<GameObject>();
        List<GameObject> flyingFireContainers = new List<GameObject>();
        
        foreach (GameObject obstacle in allObstacles)
        {
            if (obstacle == null) continue;
            
            string name = obstacle.name.ToLower();
            
            if (name.Contains("flying") && name.Contains("fire"))
            {
                flyingFireContainers.Add(obstacle);
            }
            else if (name.Contains("flying") && name.Contains("circle"))
            {
                circleContainers.Add(obstacle);
            }
            else if (name.Contains("fire") && name.Contains("line"))
            {
                fireLineContainers.Add(obstacle);
            }
        }
        
        Debug.Log($"📊 {fireLineContainers.Count}F, {circleContainers.Count}C, {flyingFireContainers.Count}FF");
        
        float currentY = startY;
        int fireIndex = 0;
        int circleIndex = 0;
        int flyingFireIndex = 0;
        
        int totalGroups = Mathf.Max(
            Mathf.CeilToInt((float)fireLineContainers.Count / fireLinesPerGroup),
            Mathf.Max(
                Mathf.CeilToInt((float)circleContainers.Count / circlesPerGroup),
                Mathf.CeilToInt((float)flyingFireContainers.Count / flyingFiresPerGroup)
            )
        );
        
        for (int groupIndex = 0; groupIndex < totalGroups; groupIndex++)
        {
            // 1. Fire&Line
            for (int f = 0; f < fireLinesPerGroup && fireIndex < fireLineContainers.Count; f++)
            {
                Vector3 pos = fireLineContainers[fireIndex].transform.position;
                pos.y = currentY;
                fireLineContainers[fireIndex].transform.position = pos;
                
                currentY += spacingY;
                fireIndex++;
            }
            
            currentY += groupSpacing;
            
            // 2. Circle
            for (int c = 0; c < circlesPerGroup && circleIndex < circleContainers.Count; c++)
            {
                Vector3 pos = circleContainers[circleIndex].transform.position;
                pos.y = currentY;
                circleContainers[circleIndex].transform.position = pos;
                
                currentY += spacingY;
                circleIndex++;
            }
            
            // ⭐ BỎ KHOẢNG CÁCH SAU CIRCLE - FlyingFire sẽ spawn sát luôn
            // currentY += groupSpacing;  // ⬅️ ĐÃ COMMENT
            
            // 3. FlyingFire
            for (int ff = 0; ff < flyingFiresPerGroup && flyingFireIndex < flyingFireContainers.Count; ff++)
            {
                Vector3 pos = flyingFireContainers[flyingFireIndex].transform.position;
                pos.y = currentY;
                flyingFireContainers[flyingFireIndex].transform.position = pos;
                
                currentY += spacingY;
                flyingFireIndex++;
            }
            
            currentY += groupSpacing;
        }
        
        Debug.Log($"✅ Sắp xếp xong! Tổng {totalGroups} groups");
    }
    
    // ⭐⭐⭐ LOGIC LOOP MỚI - KIỂM TRA LIÊN TỤC
    void CheckAndLoopObstacles()
    {
        if (allObstacles.Count == 0 || player == null) return;
        
        float ghostY = player.position.y;
        float cameraBottomEdge = mainCamera.transform.position.y - mainCamera.orthographicSize;
        
        GhostController ghost = player.GetComponent<GhostController>();
        float ghostVelocityY = ghost != null ? ghost.Velocity.y : 0f;
        
        float dynamicSpawnDistance = baseSpawnDistance + (Mathf.Abs(ghostVelocityY) * velocityMultiplier);
        dynamicSpawnDistance = Mathf.Min(dynamicSpawnDistance, maxSpawnDistance);
        
        float loopLine = cameraBottomEdge - (screenHeight * loopThreshold);
        
        // Tìm vật cản thấp nhất và cao nhất
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
        
        float requiredHighestY = ghostY + dynamicSpawnDistance;
        
        // ⭐⭐⭐ LOOP KHI: Vật cản thấp nhất qua line HOẶC cần spawn thêm phía trước
        bool needLoopBecauseBehind = lowestY < loopLine;
        bool needLoopBecauseAhead = highestY < requiredHighestY;
        
        if (needLoopBecauseBehind || needLoopBecauseAhead)
        {
            if (showDebugInfo && Time.frameCount % 60 == 0)
            {
                Debug.Log($"🔄 Need loop: Behind={needLoopBecauseBehind}, Ahead={needLoopBecauseAhead}, LowestY={lowestY:F1}, HighestY={highestY:F1}, Required={requiredHighestY:F1}");
            }
            
            LoopOneCompleteGroup(loopLine, highestY);
        }
    }
    
    // ⭐⭐⭐ LOOP ĐÚNG 1 GROUP HOÀN CHỈNH: 4F + 1C + 4FF
    void LoopOneCompleteGroup(float loopLine, float currentHighestY)
    {
        // ⭐ PHÂN LOẠI VẬT CẢN
        List<GameObject> fireLines = new List<GameObject>();
        List<GameObject> circles = new List<GameObject>();
        List<GameObject> flyingFires = new List<GameObject>();
        
        foreach (GameObject obstacle in allObstacles)
        {
            if (obstacle == null) continue;
            
            string name = obstacle.name.ToLower();
            
            if (name.Contains("flying") && name.Contains("fire"))
            {
                flyingFires.Add(obstacle);
            }
            else if (name.Contains("flying") && name.Contains("circle"))
            {
                circles.Add(obstacle);
            }
            else if (name.Contains("fire") && name.Contains("line"))
            {
                fireLines.Add(obstacle);
            }
        }
        
        // ⭐⭐⭐ SẮP XẾP THEO Y (thấp nhất lên đầu)
        fireLines = fireLines.OrderBy(o => o.transform.position.y).ToList();
        circles = circles.OrderBy(o => o.transform.position.y).ToList();
        flyingFires = flyingFires.OrderBy(o => o.transform.position.y).ToList();
        
        // ⭐⭐⭐ LẤY ĐÚNG SỐ LƯỢNG CỦA 1 GROUP
        List<GameObject> toLoopFire = fireLines.Take(fireLinesPerGroup).ToList();
        List<GameObject> toLoopCircle = circles.Take(circlesPerGroup).ToList();
        List<GameObject> toLoopFlyingFire = flyingFires.Take(flyingFiresPerGroup).ToList();
        
        // ⭐⭐⭐ KIỂM TRA: PHẢI ĐỦ CẢ GROUP MỚI LOOP
        if (toLoopFire.Count < fireLinesPerGroup || 
            toLoopCircle.Count < circlesPerGroup || 
            toLoopFlyingFire.Count < flyingFiresPerGroup)
        {
            if (showDebugInfo)
            {
                Debug.LogWarning($"⚠️ Chưa đủ group: F={toLoopFire.Count}/{fireLinesPerGroup}, C={toLoopCircle.Count}/{circlesPerGroup}, FF={toLoopFlyingFire.Count}/{flyingFiresPerGroup}");
            }
            return;
        }
        
        // ⭐⭐⭐ LOOP CẢ GROUP CÙNG LÚC
        float newY = currentHighestY + groupSpacing;
        
        if (showDebugInfo)
        {
            Debug.Log($"🔄 Loop group từ Y={newY:F1}");
        }
        
        // 1. Loop Fire&Line
        foreach (GameObject fire in toLoopFire)
        {
            Vector3 pos = fire.transform.position;
            pos.y = newY;
            fire.transform.position = pos;
            
            if (randomizeOnLoop)
                RandomizeObstacle(fire);
            
            ApplyCurrentSpeedToObstacle(fire);
            
            newY += spacingY;
            totalLooped++;
        }
        
        newY += groupSpacing;
        
        // 2. Loop Circle
        foreach (GameObject circle in toLoopCircle)
        {
            Vector3 pos = circle.transform.position;
            pos.y = newY;
            circle.transform.position = pos;
            
            if (randomizeOnLoop)
                RandomizeObstacle(circle);
            
            ApplyCurrentSpeedToObstacle(circle);
            
            newY += spacingY;
            totalLooped++;
        }
        
        // ⭐ BỎ KHOẢNG CÁCH SAU CIRCLE - FlyingFire sẽ loop sát luôn
        // newY += groupSpacing;  // ⬅️ ĐÃ COMMENT
        
        // 3. Loop FlyingFire
        foreach (GameObject flyingFire in toLoopFlyingFire)
        {
            Vector3 pos = flyingFire.transform.position;
            pos.y = newY;
            flyingFire.transform.position = pos;
            
            if (randomizeOnLoop)
                RandomizeObstacle(flyingFire);
            
            ApplyCurrentSpeedToObstacle(flyingFire);
            
            newY += spacingY;
            totalLooped++;
        }
        
        IncreaseWaveDifficulty();
        
        if (showDebugInfo)
        {
            Debug.Log($"✅ Loop HOÀN TẤT: {toLoopFire.Count}F + {toLoopCircle.Count}C + {toLoopFlyingFire.Count}FF (Wave {currentWave}, Y cuối={newY:F1})");
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
    
    void IncreaseWaveDifficulty()
    {
        currentWave++;
        
        currentSpeed = Mathf.Min(currentSpeed + speedIncrement, maxSpeed);
        currentRotationSpeed = Mathf.Min(currentRotationSpeed + rotationSpeedIncrement, maxRotationSpeed);
        
        if (showDebugInfo && currentWave % 3 == 0)
        {
            Debug.Log($"📈 Wave {currentWave}: Speed={currentSpeed:F1}, Rotation={currentRotationSpeed:F0}°/s");
        }
    }
    
    void PrintInfo()
    {
        Debug.Log("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        Debug.Log("🎮 PROGRESSIVE SPAWNER - SYNCHRONIZED LOOP");
        Debug.Log("✨ UPDATE: FlyingFire spawn SÁT sau Circle");
        Debug.Log("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        Debug.Log($"📊 Pattern: {fireLinesPerGroup}F → {circlesPerGroup}C → {flyingFiresPerGroup}FF");
        Debug.Log($"✅ Loop đồng bộ - KHÔNG BAO GIỜ LOẠN");
        Debug.Log($"🎯 Tổng: {allObstacles.Count} vật cản");
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
        
        float dynamicSpawnDistance = baseSpawnDistance + (Mathf.Abs(ghostVelocity) * velocityMultiplier);
        dynamicSpawnDistance = Mathf.Min(dynamicSpawnDistance, maxSpawnDistance);
        
        float highestY = float.MinValue;
        foreach (GameObject obstacle in allObstacles)
        {
            if (obstacle != null && obstacle.transform.position.y > highestY)
            {
                highestY = obstacle.transform.position.y;
            }
        }
        
        float distanceAhead = highestY - player.position.y;
        
        int fireCount = 0;
        int circleCount = 0;
        int flyingFireCount = 0;
        
        foreach (GameObject obstacle in allObstacles)
        {
            if (obstacle == null) continue;
            string name = obstacle.name.ToLower();
            
            if (name.Contains("fire") && name.Contains("line"))
                fireCount++;
            else if (name.Contains("circle"))
                circleCount++;
            else if (name.Contains("flying") && name.Contains("fire"))
                flyingFireCount++;
        }
        
        string info = "🎮 SYNCHRONIZED SPAWNER\n";
        info += "━━━━━━━━━━━━━━━━━\n";
        info += $"Pattern: {fireLinesPerGroup}F→{circlesPerGroup}C→{flyingFiresPerGroup}FF\n";
        info += $"F:{fireCount} C:{circleCount} FF:{flyingFireCount}\n";
        info += $"Wave: {currentWave}\n";
        info += $"Speed: {currentSpeed:F1}\n";
        info += $"━━━━━━━━━━━━━━━━━\n";
        info += $"Ghost Vel: {ghostVelocity:F1}\n";
        info += $"Spawn Dist: {dynamicSpawnDistance:F1}\n";
        info += $"Ahead: {distanceAhead:F1}\n";
        info += $"Looped: {totalLooped}";
        
        if (distanceAhead < dynamicSpawnDistance * 0.5f)
        {
            style.normal.textColor = Color.red;
            info += "\n⚠️ SPAWNING!";
        }
        
        GUI.Label(new Rect(10, Screen.height - 240, 300, 240), info, style);
    }
    
    public void ResetDifficulty()
    {
        currentWave = 0;
        currentSpeed = initialSpeed;
        currentRotationSpeed = initialRotationSpeed;
        totalLooped = 0;
        Debug.Log("🔄 Reset!");
    }
}