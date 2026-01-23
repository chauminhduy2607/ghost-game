using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// ⭐ PROGRESSIVE SPAWNER - PATTERN FIX
/// - Pattern: 4 Fire&Line → 1 FlyingCircle → 4 FlyingFire → lặp lại
/// - FIX: Không spawn cục thứ 5 bất ngờ
/// - Loop cả GROUP cùng lúc, không loop từng cái
/// </summary>
public class ProgressiveObstacleSpawner : MonoBehaviour
{
    [Header("=== PREFABS ===")]
    [SerializeField] private GameObject fireLinePrefab;
    [SerializeField] private GameObject flyingCirclePrefab;
    [SerializeField] private GameObject flyingFirePrefab;
    
    [Header("=== PATTERN ===")]
    [Tooltip("Số Fire&Line trong mỗi nhóm")]
    [SerializeField] private int fireLinesPerGroup = 4;
    
    [Tooltip("Số FlyingCircle sau Fire&Line")]
    [SerializeField] private int circlesPerGroup = 1;
    
    [Tooltip("Số FlyingFire sau FlyingCircle")]
    [SerializeField] private int flyingFiresPerGroup = 4;
    
    [Header("=== SPAWN POSITION ===")]
    [Tooltip("Khoảng cách CƠ BẢN từ Ghost đến vật cản tiếp theo")]
    [SerializeField] private float baseSpawnDistance = 20f;
    
    [Tooltip("Hệ số nhân với tốc độ Ghost")]
    [SerializeField] private float velocityMultiplier = 2.5f;
    
    [Tooltip("Khoảng cách spawn tối đa")]
    [SerializeField] private float maxSpawnDistance = 50f;
    
    [Header("=== SPACING ===")]
    [Tooltip("Khoảng cách giữa các vật cản")]
    [SerializeField] private float spacingY = 6.0f;
    
    [Tooltip("Khoảng cách thêm giữa các nhóm")]
    [SerializeField] private float groupSpacing = 5.0f;
    
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
    [SerializeField] private bool autoArrangeOnStart = true;
    
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
    
    // ⭐⭐ MỚI: Lưu nhóm vật cản để loop cả group
    private class ObstacleGroup
    {
        public List<GameObject> fireLines = new List<GameObject>();
        public List<GameObject> circles = new List<GameObject>();
        public List<GameObject> flyingFires = new List<GameObject>();
        
        public bool IsComplete()
        {
            return fireLines.Count > 0 || circles.Count > 0 || flyingFires.Count > 0;
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
        
        if (rotations.Length == 0)
        {
            Debug.LogError("❌ KHÔNG TÌM THẤY SCRIPT CIRCLEROTATION!");
            return;
        }
        
        foreach (CircleRotation rotation in rotations)
        {
            rotation.enabled = true;
        }
        
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
        
        Debug.Log($"✅ Đã bật {rotations.Length} CircleRotation scripts!");
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
                Debug.Log($"🔥 Fire&Line: {child.name}");
                SetupFireLine(child.gameObject, currentSpeed);
                allObstacles.Add(child.gameObject);
            }
            else if (name.Contains("circle") && name.Contains("flying"))
            {
                Debug.Log($"⭕ FlyingCircle: {child.name}");
                SetupFlyingCircle(child.gameObject, currentRotationSpeed);
                allObstacles.Add(child.gameObject);
            }
            else if (name.Contains("fire") && name.Contains("flying"))
            {
                Debug.Log($"🔥🔥 FlyingFire: {child.name}");
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
        
        for (int group = 0; group < 3; group++)
        {
            // Fire&Lines
            for (int i = 0; i < fireLinesPerGroup; i++)
            {
                GameObject fireLine = Instantiate(fireLinePrefab, transform);
                fireLine.name = $"Fire&Line_{spawnCounter++}";
                SetupFireLine(fireLine, currentSpeed);
                allObstacles.Add(fireLine);
            }
            
            // FlyingCircles
            for (int i = 0; i < circlesPerGroup; i++)
            {
                GameObject circle = Instantiate(flyingCirclePrefab, transform);
                circle.name = $"FlyingCircle_{spawnCounter++}";
                SetupFlyingCircle(circle, currentRotationSpeed);
                allObstacles.Add(circle);
            }
            
            // FlyingFires
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
        if (looper == null)
            looper = obj.AddComponent<ObstacleLooper>();
        
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
        if (looper == null)
            looper = obj.AddComponent<ObstacleLooper>();
        
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
        
        int maxIterations = Mathf.Max(
            Mathf.CeilToInt((float)fireLineContainers.Count / fireLinesPerGroup),
            Mathf.Max(circleContainers.Count, 
                     Mathf.CeilToInt((float)flyingFireContainers.Count / flyingFiresPerGroup))
        );
        
        for (int i = 0; i < maxIterations; i++)
        {
            // 1. Fire&Line (4 cái)
            for (int f = 0; f < fireLinesPerGroup && fireIndex < fireLineContainers.Count; f++)
            {
                Vector3 pos = fireLineContainers[fireIndex].transform.position;
                pos.y = currentY;
                fireLineContainers[fireIndex].transform.position = pos;
                
                currentY += spacingY;
                fireIndex++;
            }
            
            currentY += groupSpacing;
            
            // 2. Circle (1 cái)
            if (circleIndex < circleContainers.Count)
            {
                Vector3 pos = circleContainers[circleIndex].transform.position;
                pos.y = currentY;
                circleContainers[circleIndex].transform.position = pos;
                
                currentY += spacingY + groupSpacing;
                circleIndex++;
            }
            
            // 3. FlyingFire (4 cái)
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
        
        Debug.Log($"✅ Sắp xếp xong! Y cuối: {currentY:F1}");
    }
    
    // ⭐⭐⭐ FIX CHÍNH: LOOP CẢ GROUP, KHÔNG LOOP TỪNG CÁI
    void CheckAndLoopObstacles()
    {
        if (allObstacles.Count == 0 || player == null) return;
        
        float ghostY = player.position.y;
        float cameraBottomEdge = mainCamera.transform.position.y - mainCamera.orthographicSize;
        
        GhostController ghost = player.GetComponent<GhostController>();
        float ghostVelocityY = ghost != null ? ghost.Velocity.y : 0f;
        
        float dynamicSpawnDistance = baseSpawnDistance + (Mathf.Abs(ghostVelocityY) * velocityMultiplier);
        dynamicSpawnDistance = Mathf.Min(dynamicSpawnDistance, maxSpawnDistance);
        
        float dynamicLoopThreshold = loopThreshold;
        if (ghostVelocityY > 5f)
            dynamicLoopThreshold = loopThreshold + 2f;
        else if (ghostVelocityY > 3f)
            dynamicLoopThreshold = loopThreshold + 1f;
        
        float loopLine = cameraBottomEdge - (screenHeight * dynamicLoopThreshold);
        
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
        
        if (lowestY < loopLine || highestY < requiredHighestY)
        {
            LoopCompleteGroup(loopLine, highestY);
        }
    }
    
    // ⭐⭐⭐ LOOP CẢ GROUP ĐẦY ĐỦ: 4F + 1C + 4FF
    void LoopCompleteGroup(float loopLine, float currentHighestY)
    {
        ObstacleGroup groupToLoop = new ObstacleGroup();
        
        // Thu thập vật cản cần loop
        foreach (GameObject obstacle in allObstacles)
        {
            if (obstacle == null) continue;
            
            if (obstacle.transform.position.y < loopLine)
            {
                string name = obstacle.name.ToLower();
                
                if (name.Contains("flying") && name.Contains("fire"))
                {
                    groupToLoop.flyingFires.Add(obstacle);
                }
                else if (name.Contains("flying") && name.Contains("circle"))
                {
                    groupToLoop.circles.Add(obstacle);
                }
                else if (name.Contains("fire") && name.Contains("line"))
                {
                    groupToLoop.fireLines.Add(obstacle);
                }
            }
        }
        
        // ⭐⭐⭐ CHỈ LOOP KHI ĐỦ 1 GROUP HOÀN CHỈNH
        int fireCount = Mathf.Min(groupToLoop.fireLines.Count, fireLinesPerGroup);
        int circleCount = Mathf.Min(groupToLoop.circles.Count, circlesPerGroup);
        int flyingFireCount = Mathf.Min(groupToLoop.flyingFires.Count, flyingFiresPerGroup);
        
        // ⭐ ĐIỀU KIỆN: Phải có ít nhất 1 trong 3 loại đủ số lượng
        bool hasCompleteFireGroup = fireCount >= fireLinesPerGroup;
        bool hasCompleteCircleGroup = circleCount >= circlesPerGroup;
        bool hasCompleteFlyingFireGroup = flyingFireCount >= flyingFiresPerGroup;
        
        if (!hasCompleteFireGroup && !hasCompleteCircleGroup && !hasCompleteFlyingFireGroup)
        {
            // Chưa đủ để loop
            return;
        }
        
        float newY = currentHighestY + spacingY;
        
        // 1. Loop Fire&Line (4 cái)
        for (int i = 0; i < fireCount; i++)
        {
            Vector3 pos = groupToLoop.fireLines[i].transform.position;
            pos.y = newY;
            groupToLoop.fireLines[i].transform.position = pos;
            
            if (randomizeOnLoop)
                RandomizeObstacle(groupToLoop.fireLines[i]);
            
            ApplyCurrentSpeedToObstacle(groupToLoop.fireLines[i]);
            
            newY += spacingY;
            totalLooped++;
        }
        
        newY += groupSpacing;
        
        // 2. Loop Circle (1 cái)
        for (int i = 0; i < circleCount; i++)
        {
            Vector3 pos = groupToLoop.circles[i].transform.position;
            pos.y = newY;
            groupToLoop.circles[i].transform.position = pos;
            
            if (randomizeOnLoop)
                RandomizeObstacle(groupToLoop.circles[i]);
            
            ApplyCurrentSpeedToObstacle(groupToLoop.circles[i]);
            
            newY += spacingY + groupSpacing;
            totalLooped++;
        }
        
        // 3. Loop FlyingFire (4 cái)
        for (int i = 0; i < flyingFireCount; i++)
        {
            Vector3 pos = groupToLoop.flyingFires[i].transform.position;
            pos.y = newY;
            groupToLoop.flyingFires[i].transform.position = pos;
            
            if (randomizeOnLoop)
                RandomizeObstacle(groupToLoop.flyingFires[i]);
            
            ApplyCurrentSpeedToObstacle(groupToLoop.flyingFires[i]);
            
            newY += spacingY;
            totalLooped++;
        }
        
        newY += groupSpacing;
        
        // Tăng độ khó
        IncreaseWaveDifficulty();
        
        if (showDebugInfo)
        {
            Debug.Log($"♻️ Loop GROUP: {fireCount}F + {circleCount}C + {flyingFireCount}FF (Wave {currentWave})");
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
            Debug.Log($"📈 Wave {currentWave}: Speed = {currentSpeed:F1}, Rotation = {currentRotationSpeed:F0}°/s");
        }
    }
    
    void PrintInfo()
    {
        Debug.Log("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        Debug.Log("🎮 PROGRESSIVE SPAWNER - FIXED LOOP");
        Debug.Log("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        Debug.Log($"📊 Pattern: {fireLinesPerGroup}F → {circlesPerGroup}C → {flyingFiresPerGroup}FF");
        Debug.Log($"✅ FIX: Loop cả GROUP, không loop từng cái");
        Debug.Log($"🎯 Tổng vật cản: {allObstacles.Count}");
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
            else if (name.Contains("fire") && name.Contains("flying"))
                flyingFireCount++;
        }
        
    }
    
    public void ResetDifficulty()
    {
        currentWave = 0;
        currentSpeed = initialSpeed;
        currentRotationSpeed = initialRotationSpeed;
        totalLooped = 0;
        
        Debug.Log("🔄 Reset độ khó!");
    }
    
    public void SetPattern(int fireCount, int circleCount, int flyingFireCount)
    {
        fireLinesPerGroup = Mathf.Max(1, fireCount);
        circlesPerGroup = Mathf.Max(1, circleCount);
        flyingFiresPerGroup = Mathf.Max(1, flyingFireCount);
        
        Debug.Log($"🔧 Pattern mới: {fireLinesPerGroup}F → {circlesPerGroup}C → {flyingFiresPerGroup}FF");
    }
    
    public void RearrangeAll()
    {
        ArrangeAllObstaclesInPattern();
        Debug.Log($"🔄 Đã sắp xếp lại");
    }
    
    public void SetSpacing(float newSpacing, float newGroupSpacing)
    {
        spacingY = Mathf.Max(1f, newSpacing);
        groupSpacing = Mathf.Max(0f, newGroupSpacing);
        ArrangeAllObstaclesInPattern();
        Debug.Log($"🔧 Spacing mới: {spacingY}, group: {groupSpacing}");
    }
}