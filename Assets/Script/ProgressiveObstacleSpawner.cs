using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// ⭐ PROGRESSIVE SPAWNER - PATTERN RÕ RÀNG
/// - Pattern: 4 Fire&Line → 1 FlyingCircle → lặp lại
/// - Không bị đè lên nhau
/// - Tăng tốc độ dần dần
/// </summary>
public class ProgressiveObstacleSpawner : MonoBehaviour
{
    [Header("=== PREFABS ===")]
    [SerializeField] private GameObject fireLinePrefab;
    [SerializeField] private GameObject flyingCirclePrefab;
    [SerializeField] private GameObject flyingFirePrefab;  // ⭐ MỚI
    
    [Header("=== PATTERN ===")]
    [Tooltip("Số Fire&Line trong mỗi nhóm")]
    [SerializeField] private int fireLinesPerGroup = 4;  // ⭐ Trở lại 4
    
    [Tooltip("Số FlyingCircle sau Fire&Line")]
    [SerializeField] private int circlesPerGroup = 1;
    
    [Tooltip("Số FlyingFire sau FlyingCircle")]
    [SerializeField] private int flyingFiresPerGroup = 4;  // ⭐ MỚI
    
    [Header("=== SPAWN POSITION ===")]
    [Tooltip("Khoảng cách CƠ BẢN từ Ghost đến vật cản tiếp theo")]
    [SerializeField] private float baseSpawnDistance = 20f;  // ⭐⭐ TĂNG: 12 → 20
    
    [Tooltip("Hệ số nhân với tốc độ Ghost (càng cao = spawn càng xa)")]
    [SerializeField] private float velocityMultiplier = 2.5f;  // ⭐⭐ TĂNG: 1.5 → 2.5
    
    [Tooltip("Khoảng cách spawn tối đa")]
    [SerializeField] private float maxSpawnDistance = 50f;  // ⭐⭐ TĂNG: 30 → 50
    
    [Header("=== SPACING ===")]
    [Tooltip("Khoảng cách giữa các vật cản")]
    [SerializeField] private float spacingY = 6.0f;  // ⭐ TĂNG: 5.0 → 6.0
    
    [Tooltip("Khoảng cách thêm giữa các nhóm (Fire→Circle)")]
    [SerializeField] private float groupSpacing = 5.0f;  // ⭐⭐ TĂNG: 3.0 → 5.0
    
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
    [Tooltip("Sử dụng vật cản có sẵn trong scene (con của object này)")]
    [SerializeField] private bool useExistingObstacles = true;
    
    [Tooltip("Tự động sắp xếp lại khi Start (bỏ tick nếu muốn giữ nguyên vị trí)")]
    [SerializeField] private bool autoArrangeOnStart = true;
    
    [Header("=== LOOP ===")]
    [SerializeField] private float loopThreshold = 0.5f;
    [SerializeField] private bool randomizeOnLoop = true;
    
    [Header("=== DEBUG ===")]
    [SerializeField] private bool showDebugInfo = true;
    [SerializeField] private bool manuallyEnableCircles = false;  // ⭐ MỚI: Bật thủ công
    
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
        
        // ⭐ TẮT CIRCLEROTATION TRƯỚC KHI SẮP XẾP
        DisableCircleRotations();
        
        // Sắp xếp
        Debug.Log("🔧 Chuẩn bị sắp xếp vật cản...");
        ArrangeAllObstaclesInPattern();
        
        // ⭐⭐ BẬT LẠI SAU KHI SẮP XẾP (Coroutine)
        StartCoroutine(EnableCircleRotationsDelayed());
        
        // ⭐⭐ VÀ CŨNG BẬT NGAY SAU 0.5 GIÂY ĐỂ ĐẢM BẢO
        Invoke("ForceEnableAllCircles", 0.5f);
        
        PrintInfo();
    }
    
    // ⭐⭐ HÀM FORCE ENABLE ĐẢM BẢO 100% BẬT
    void ForceEnableAllCircles()
    {
        CircleRotation[] rotations = FindObjectsOfType<CircleRotation>(true); // true = tìm cả inactive
        
        if (rotations.Length == 0)
        {
            Debug.LogError("❌❌ KHÔNG TÌM THẤY SCRIPT CIRCLEROTATION!");
            Debug.LogError("→ Circle (1) và Circle PHẢI CÓ component CircleRotation!");
            return;
        }
        
        foreach (CircleRotation rotation in rotations)
        {
            rotation.enabled = true;
        }
        
        Debug.Log($"✅✅ FORCE ENABLE {rotations.Length} CircleRotation!");
    }
    
    // ==================== TẮT/BẬT CIRCLEROTATION ====================
    void DisableCircleRotations()
    {
        Debug.Log("🔴 Tạm tắt CircleRotation để sắp xếp...");
        
        CircleRotation[] rotations = FindObjectsOfType<CircleRotation>();
        foreach (CircleRotation rotation in rotations)
        {
            rotation.enabled = false;
            Debug.Log($"   ❌ Tắt: {rotation.gameObject.name}");
        }
    }
    
    System.Collections.IEnumerator EnableCircleRotationsDelayed()
    {
        yield return new WaitForEndOfFrame();
        
        Debug.Log("🟢 Bật lại CircleRotation...");
        
        CircleRotation[] rotations = FindObjectsOfType<CircleRotation>();
        
        if (rotations.Length == 0)
        {
            Debug.LogWarning("⚠️ KHÔNG TÌM THẤY CIRCLEROTATION NÀO!");
            Debug.LogWarning("   Có thể Circle (1) và Circle chưa có script CircleRotation!");
            yield break;
        }
        
        foreach (CircleRotation rotation in rotations)
        {
            rotation.enabled = true;
            Debug.Log($"   ✅ Bật: {rotation.gameObject.name}");
        }
        
        Debug.Log($"✅ Đã bật {rotations.Length} CircleRotation scripts!");
    }
    
    void Update()
    {
        CheckAndLoopObstacles();
        
        // ⭐ TEST: Nhấn phím SPACE để bật CircleRotation thủ công
        if (manuallyEnableCircles && Input.GetKeyDown(KeyCode.Space))
        {
            ManuallyEnableCircles();
        }
    }
    
    // ⭐ HÀM BẬT THỦ CÔNG
    void ManuallyEnableCircles()
    {
        Debug.Log("🟢 BẬT THỦ CÔNG CircleRotation...");
        
        CircleRotation[] rotations = FindObjectsOfType<CircleRotation>();
        
        if (rotations.Length == 0)
        {
            Debug.LogError("❌ KHÔNG TÌM THẤY CIRCLEROTATION NÀO!");
            Debug.LogError("   → Kiểm tra Circle (1) và Circle có script CircleRotation chưa!");
            return;
        }
        
        foreach (CircleRotation rotation in rotations)
        {
            rotation.enabled = true;
            Debug.Log($"   ✅ Đã bật: {rotation.gameObject.name}");
        }
    }
    
    // ==================== SỬ DỤNG VẬT CẢN CÓ SẴN ====================
    void UseExistingObstacles()
    {
        allObstacles.Clear();
        
        Debug.Log($"🔍 Bắt đầu tìm container trong {transform.childCount} objects...");
        
        // ⭐ TÌM CÁC PARENT CONTAINER
        foreach (Transform child in transform)
        {
            if (child == null) continue;
            
            string name = child.name.ToLower();
            
            // ⭐ FIRE&LINE
            if (name.Contains("fire") && name.Contains("line"))
            {
                Debug.Log($"🔥 Tìm thấy Fire&Line: {child.name}");
                SetupFireLine(child.gameObject, currentSpeed);
                allObstacles.Add(child.gameObject);
            }
            // ⭐ FLYINGCIRCLE
            else if (name.Contains("circle") && name.Contains("flying"))
            {
                Debug.Log($"⭕ Tìm thấy FlyingCircle: {child.name}");
                SetupFlyingCircle(child.gameObject, currentRotationSpeed);
                allObstacles.Add(child.gameObject);
            }
            // ⭐⭐ FLYINGFIRE (MỚI)
            else if (name.Contains("fire") && name.Contains("flying"))
            {
                Debug.Log($"🔥🔥 Tìm thấy FlyingFire: {child.name}");
                SetupFlyingFire(child.gameObject, currentSpeed);
                allObstacles.Add(child.gameObject);
            }
            else
            {
                Debug.Log($"⚠️ Bỏ qua: {child.name}");
            }
        }
        
        Debug.Log($"✅ Sử dụng {allObstacles.Count} container");
        
        if (allObstacles.Count == 0)
        {
            Debug.LogError("❌ KHÔNG TÌM THẤY CONTAINER NÀO!");
        }
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
        
        // Spawn 3 groups ban đầu
        for (int group = 0; group < 3; group++)
        {
            // Spawn Fire&Lines
            for (int i = 0; i < fireLinesPerGroup; i++)
            {
                GameObject fireLine = Instantiate(fireLinePrefab, transform);
                fireLine.name = $"Fire&Line_{spawnCounter++}";
                SetupFireLine(fireLine, currentSpeed);
                allObstacles.Add(fireLine);
            }
            
            // Sau đó spawn FlyingCircles
            for (int i = 0; i < circlesPerGroup; i++)
            {
                GameObject circle = Instantiate(flyingCirclePrefab, transform);
                circle.name = $"FlyingCircle_{spawnCounter++}";
                SetupFlyingCircle(circle, currentRotationSpeed);
                allObstacles.Add(circle);
            }
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
    
    // ==================== ⭐⭐ SETUP FLYING FIRE (MỚI) ====================
    void SetupFlyingFire(GameObject obj, float speed)
    {
        // FlyingFire có thể có animation hoặc movement riêng
        // Tương tự như Fire&Line
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
    
    // ==================== ⭐ SẮP XẾP THEO PATTERN ====================
    void ArrangeAllObstaclesInPattern()
    {
        if (allObstacles.Count == 0) return;
        
        // ⭐ PHÂN LOẠI 3 LOẠI VẬT CẢN
        List<GameObject> fireLineContainers = new List<GameObject>();
        List<GameObject> circleContainers = new List<GameObject>();
        List<GameObject> flyingFireContainers = new List<GameObject>();  // ⭐ MỚI
        
        foreach (GameObject obstacle in allObstacles)
        {
            if (obstacle == null) continue;
            
            string name = obstacle.name.ToLower();
            
            // ⭐⭐ FlyingFire - KIỂM TRA TRƯỚC!
            if (name.Contains("flying") && name.Contains("fire"))
            {
                flyingFireContainers.Add(obstacle);
                Debug.Log($"🔥🔥 FlyingFire: {obstacle.name}");
            }
            // ⭐ FlyingCircle
            else if (name.Contains("flying") && name.Contains("circle"))
            {
                circleContainers.Add(obstacle);
                Debug.Log($"⭕ FlyingCircle: {obstacle.name}");
            }
            // ⭐ Fire&Line
            else if (name.Contains("fire") && name.Contains("line"))
            {
                fireLineContainers.Add(obstacle);
                Debug.Log($"🔥 Fire&Line: {obstacle.name}");
            }
        }
        
        Debug.Log($"📊 Có {fireLineContainers.Count} Fire&Line, {circleContainers.Count} Circle, {flyingFireContainers.Count} FlyingFire");
        
        if (fireLineContainers.Count == 0 && circleContainers.Count == 0 && flyingFireContainers.Count == 0)
        {
            Debug.LogError("❌ Không tìm thấy container nào!");
            return;
        }
        
        // ⭐⭐ PATTERN MỚI: 4 Fire&Line → 1 Circle → 4 FlyingFire
        float currentY = startY;
        int fireIndex = 0;
        int circleIndex = 0;
        int flyingFireIndex = 0;
        
        Debug.Log($"🎯 Bắt đầu sắp xếp từ Y = {startY}");
        Debug.Log($"📊 Pattern: {fireLinesPerGroup} Fire → {circlesPerGroup} Circle → {flyingFiresPerGroup} FlyingFire");
        
        // Loop qua tất cả containers
        int maxIterations = Mathf.Max(
            Mathf.CeilToInt((float)fireLineContainers.Count / fireLinesPerGroup),
            Mathf.Max(circleContainers.Count, 
                     Mathf.CeilToInt((float)flyingFireContainers.Count / flyingFiresPerGroup))
        );
        
        for (int i = 0; i < maxIterations; i++)
        {
            // 1. Đặt Fire&Line (4 cái)
            for (int f = 0; f < fireLinesPerGroup && fireIndex < fireLineContainers.Count; f++)
            {
                Vector3 pos = fireLineContainers[fireIndex].transform.position;
                pos.y = currentY;
                fireLineContainers[fireIndex].transform.position = pos;
                
                Debug.Log($"  📍 {fireLineContainers[fireIndex].name} → Y = {currentY:F1}");
                
                currentY += spacingY;
                fireIndex++;
            }
            
            // Khoảng cách giữa Fire và Circle
            currentY += groupSpacing;
            
            // 2. Đặt Circle (1 cái)
            if (circleIndex < circleContainers.Count)
            {
                Vector3 pos = circleContainers[circleIndex].transform.position;
                pos.y = currentY;
                circleContainers[circleIndex].transform.position = pos;
                
                Debug.Log($"  📍 {circleContainers[circleIndex].name} → Y = {currentY:F1}");
                
                currentY += spacingY + groupSpacing;
                circleIndex++;
            }
            
            // 3. Đặt FlyingFire (4 cái) ⭐ MỚI
            for (int ff = 0; ff < flyingFiresPerGroup && flyingFireIndex < flyingFireContainers.Count; ff++)
            {
                Vector3 pos = flyingFireContainers[flyingFireIndex].transform.position;
                pos.y = currentY;
                flyingFireContainers[flyingFireIndex].transform.position = pos;
                
                Debug.Log($"  📍 {flyingFireContainers[flyingFireIndex].name} → Y = {currentY:F1}");
                
                currentY += spacingY;
                flyingFireIndex++;
            }
            
            // Khoảng cách trước khi bắt đầu group mới
            currentY += groupSpacing;
        }
        
        Debug.Log($"✅ Đã sắp xếp xong! Y cuối: {currentY:F1}");
    }
    
    // ==================== LOOP VẬT CẢN - THEO PATTERN ====================
    void CheckAndLoopObstacles()
    {
        if (allObstacles.Count == 0 || player == null) return;
        
        float ghostY = player.position.y;
        float cameraBottomEdge = mainCamera.transform.position.y - mainCamera.orthographicSize;
        
        // ⭐⭐ TÍNH KHOẢNG CÁCH SPAWN ĐỘNG DỰA VÀO TỐC ĐỘ GHOST
        GhostController ghost = player.GetComponent<GhostController>();
        float ghostVelocityY = ghost != null ? ghost.Velocity.y : 0f;
        
        // Khoảng cách spawn = base + (tốc độ * multiplier)
        float dynamicSpawnDistance = baseSpawnDistance + (Mathf.Abs(ghostVelocityY) * velocityMultiplier);
        dynamicSpawnDistance = Mathf.Min(dynamicSpawnDistance, maxSpawnDistance);
        
        // Debug mỗi 60 frame
        if (Time.frameCount % 60 == 0)
        {
            Debug.Log($"📊 Ghost Velocity: {ghostVelocityY:F1} → Spawn Distance: {dynamicSpawnDistance:F1}");
        }
        
        // ⭐ ĐIỀU CHỈNH LOOP THRESHOLD DỰA VÀO TỐC ĐỘ GHOST
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
        
        // ⭐⭐ KIỂM TRA XEM CẦN LOOP KHÔNG (dùng khoảng cách động)
        float requiredHighestY = ghostY + dynamicSpawnDistance;
        
        if (lowestY < loopLine || highestY < requiredHighestY)
        {
            LoopNextGroup(loopLine, highestY);
        }
    }
    
    // ⭐⭐ LOOP THEO PATTERN: FIRE&LINE → CIRCLE → FLYINGFIRE
    void LoopNextGroup(float loopLine, float currentHighestY)
    {
        // Tìm các container cần loop
        List<GameObject> fireLinesToLoop = new List<GameObject>();
        List<GameObject> circlesToLoop = new List<GameObject>();
        List<GameObject> flyingFireToLoop = new List<GameObject>();  // ⭐ MỚI
        
        foreach (GameObject obstacle in allObstacles)
        {
            if (obstacle == null) continue;
            
            if (obstacle.transform.position.y < loopLine)
            {
                string name = obstacle.name.ToLower();
                
                // ⭐⭐ KIỂM TRA FlyingFire TRƯỚC!
                if (name.Contains("flying") && name.Contains("fire"))
                {
                    flyingFireToLoop.Add(obstacle);
                }
                else if (name.Contains("flying") && name.Contains("circle"))
                {
                    circlesToLoop.Add(obstacle);
                }
                else if (name.Contains("fire") && name.Contains("line"))
                {
                    fireLinesToLoop.Add(obstacle);
                }
            }
        }
        
        if (fireLinesToLoop.Count == 0 && circlesToLoop.Count == 0 && flyingFireToLoop.Count == 0) return;
        
        float newY = currentHighestY + spacingY;
        
        // 1. Loop Fire&Line
        int fireCount = Mathf.Min(fireLinesToLoop.Count, fireLinesPerGroup);
        for (int i = 0; i < fireCount; i++)
        {
            Vector3 pos = fireLinesToLoop[i].transform.position;
            pos.y = newY;
            fireLinesToLoop[i].transform.position = pos;
            
            if (randomizeOnLoop)
                RandomizeObstacle(fireLinesToLoop[i]);
            
            ApplyCurrentSpeedToObstacle(fireLinesToLoop[i]);
            
            newY += spacingY;
            totalLooped++;
        }
        
        newY += groupSpacing;
        
        // 2. Loop Circle
        foreach (GameObject circle in circlesToLoop)
        {
            Vector3 pos = circle.transform.position;
            pos.y = newY;
            circle.transform.position = pos;
            
            if (randomizeOnLoop)
                RandomizeObstacle(circle);
            
            ApplyCurrentSpeedToObstacle(circle);
            
            newY += spacingY + groupSpacing;
            totalLooped++;
        }
        
        // 3. Loop FlyingFire ⭐ MỚI
        int flyingFireCount = Mathf.Min(flyingFireToLoop.Count, flyingFiresPerGroup);
        for (int i = 0; i < flyingFireCount; i++)
        {
            Vector3 pos = flyingFireToLoop[i].transform.position;
            pos.y = newY;
            flyingFireToLoop[i].transform.position = pos;
            
            if (randomizeOnLoop)
                RandomizeObstacle(flyingFireToLoop[i]);
            
            ApplyCurrentSpeedToObstacle(flyingFireToLoop[i]);
            
            newY += spacingY;
            totalLooped++;
        }
        
        newY += groupSpacing;
        
        // Tăng độ khó
        if (fireLinesToLoop.Count > 0 || circlesToLoop.Count > 0 || flyingFireToLoop.Count > 0)
        {
            IncreaseWaveDifficulty();
            
            if (showDebugInfo)
            {
                Debug.Log($"♻️ Loop: {fireCount}F + {circlesToLoop.Count}C + {flyingFireCount}FF (Wave {currentWave})");
            }
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
        
        if (showDebugInfo && currentWave % 3 == 0)
        {
            Debug.Log($"📈 Wave {currentWave}: Speed = {currentSpeed:F1}, Rotation = {currentRotationSpeed:F0}°/s");
        }
    }
    
    // ==================== DEBUG ====================
    void PrintInfo()
    {
        Debug.Log("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        Debug.Log("🎮 PROGRESSIVE SPAWNER - PATTERN MODE");
        Debug.Log("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        Debug.Log($"📊 Pattern: {fireLinesPerGroup} Fire → {circlesPerGroup} Circle → Lặp lại");
        Debug.Log($"📍 Khoảng cách spawn: {spacingY}");
        Debug.Log($"🏁 Tốc độ ban đầu: {initialSpeed:F1}");
        Debug.Log($"📈 Tăng tốc: +{speedIncrement:F1} mỗi wave");
        Debug.Log($"⚡ Loop động theo tốc độ Ghost");
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
        
        // ⭐⭐ Tính khoảng cách spawn động
        float dynamicSpawnDistance = baseSpawnDistance + (Mathf.Abs(ghostVelocity) * velocityMultiplier);
        dynamicSpawnDistance = Mathf.Min(dynamicSpawnDistance, maxSpawnDistance);
        
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
        
        // Đếm số Fire, Circle, FlyingFire
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
        
        string info = "🎮 DYNAMIC SPAWNER\n";
        info += "━━━━━━━━━━━━━━━━━\n";
        info += $"Pattern: {fireLinesPerGroup}F→{circlesPerGroup}C→{flyingFiresPerGroup}FF\n";
        info += $"F:{fireCount} C:{circleCount} FF:{flyingFireCount}\n";
        info += $"Wave: {currentWave}\n";
        info += $"Speed: {currentSpeed:F1}\n";
        info += $"Ghost Vel: {ghostVelocity:F1}\n";
        info += $"━━━━━━━━━━━━━━━━━\n";
        info += $"Spawn Dist: {dynamicSpawnDistance:F1}\n";
        info += $"Ahead: {distanceAhead:F1}\n";
        info += $"Looped: {totalLooped}";
        
        // ⭐⭐ CẢNH BÁO NẾU SẮP HẾT VẬT CẢN
        if (distanceAhead < dynamicSpawnDistance * 0.5f)
        {
            style.normal.textColor = Color.red;
            info += "\n⚠️ SPAWNING!";
        }
        
        GUI.Label(new Rect(10, Screen.height - 240, 280, 240), info, style);
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
    
    /// <summary>
    /// Thay đổi pattern spawn
    /// </summary>
    public void SetPattern(int fireCount, int circleCount)
    {
        fireLinesPerGroup = Mathf.Max(1, fireCount);
        circlesPerGroup = Mathf.Max(1, circleCount);
        
        Debug.Log($"🔧 Pattern mới: {fireLinesPerGroup} Fire → {circlesPerGroup} Circle");
    }
    
    /// <summary>
    /// Sắp xếp lại tất cả vật cản ngay lập tức
    /// </summary>
    public void RearrangeAll()
    {
        ArrangeAllObstaclesInPattern();
        Debug.Log($"🔄 Đã sắp xếp lại với spacing: {spacingY}, group spacing: {groupSpacing}");
    }
    
    /// <summary>
    /// Thay đổi spacing và sắp xếp lại
    /// </summary>
    public void SetSpacing(float newSpacing, float newGroupSpacing)
    {
        spacingY = Mathf.Max(1f, newSpacing);
        groupSpacing = Mathf.Max(0f, newGroupSpacing);
        ArrangeAllObstaclesInPattern();
        Debug.Log($"🔧 Spacing mới: {spacingY}, group: {groupSpacing}");
    }
}