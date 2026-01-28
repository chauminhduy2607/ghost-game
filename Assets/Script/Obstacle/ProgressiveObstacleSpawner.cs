using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// ⭐⭐⭐ PROGRESSIVE SPAWNER - MANUAL RADIUS
/// - NHẬP THỦ CÔNG bán kính Circle trong Inspector
/// - FlyingFire spawn từ VIỀN NGOÀI Circle
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
    [SerializeField] private float groupSpacing = 3.0f;
    
    [Header("=== ⭐ CIRCLE SETTINGS - QUAN TRỌNG! ===")]
    [Tooltip("⭐⭐⭐ Bán kính của Circle (đo từ Scene). VD: nếu Circle có đường kính 5 thì radius = 2.5")]
    [SerializeField] private float circleRadius = 2.5f;
    
    [Tooltip("Khoảng cách từ VIỀN NGOÀI Circle đến FlyingFire đầu tiên")]
    [SerializeField] private float circleToFireSpacing = 0.0f;
    
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
    
    // ⭐⭐⭐ CÔNG THỨC: FlyingFire Y = Circle Center Y + Circle Radius + Spacing
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
        
        Debug.Log($"\n🎯 Sắp xếp với Circle Radius = {circleRadius}");
        Debug.Log($"📏 Khoảng cách từ viền = {circleToFireSpacing}");
        
        for (int groupIndex = 0; groupIndex < totalGroups; groupIndex++)
        {
            Debug.Log($"\n━━━ GROUP {groupIndex + 1} ━━━");
            
            // 1. Fire&Line
            for (int f = 0; f < fireLinesPerGroup && fireIndex < fireLineContainers.Count; f++)
            {
                Vector3 pos = fireLineContainers[fireIndex].transform.position;
                pos.y = currentY;
                fireLineContainers[fireIndex].transform.position = pos;
                
                Debug.Log($"🔥 Fire&Line[{fireIndex}] at Y = {currentY:F2}");
                
                currentY += spacingY;
                fireIndex++;
            }
            
            currentY += groupSpacing;
            Debug.Log($"   ↓ +{groupSpacing} (group spacing)");
            
            // 2. Circle - Đặt TÂM
            float circleCenterY = currentY;
            
            for (int c = 0; c < circlesPerGroup && circleIndex < circleContainers.Count; c++)
            {
                Vector3 pos = circleContainers[circleIndex].transform.position;
                pos.y = circleCenterY;
                circleContainers[circleIndex].transform.position = pos;
                
                float circleTopEdge = circleCenterY + circleRadius;
                
                Debug.Log($"⭕ Circle[{circleIndex}]:");
                Debug.Log($"   Tâm Y = {circleCenterY:F2}");
                Debug.Log($"   Viền trên = {circleTopEdge:F2}");
                
                circleIndex++;
            }
            
            // ⭐⭐⭐ CÔNG THỨC: Tâm + Bán kính + Spacing
            float circleTopEdgeFinal = circleCenterY + circleRadius;
            currentY = circleTopEdgeFinal + circleToFireSpacing;
            
            Debug.Log($"   ⭐ FlyingFire sẽ spawn từ Y = {currentY:F2}");
            Debug.Log($"   (Viền {circleTopEdgeFinal:F2} + Spacing {circleToFireSpacing:F2})");
            
            // 3. FlyingFire
            for (int ff = 0; ff < flyingFiresPerGroup && flyingFireIndex < flyingFireContainers.Count; ff++)
            {
                Vector3 pos = flyingFireContainers[flyingFireIndex].transform.position;
                pos.y = currentY;
                flyingFireContainers[flyingFireIndex].transform.position = pos;
                
                Debug.Log($"🌀 FlyingFire[{flyingFireIndex}] at Y = {currentY:F2}");
                
                currentY += spacingY;
                flyingFireIndex++;
            }
            
            currentY += groupSpacing;
        }
        
        Debug.Log($"\n✅ Sắp xếp hoàn tất!");
    }
    
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
        
        bool needLoopBecauseBehind = lowestY < loopLine;
        bool needLoopBecauseAhead = highestY < requiredHighestY;
        
        if (needLoopBecauseBehind || needLoopBecauseAhead)
        {
            LoopOneCompleteGroup(loopLine, highestY);
        }
    }
    
    void LoopOneCompleteGroup(float loopLine, float currentHighestY)
    {
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
        
        fireLines = fireLines.OrderBy(o => o.transform.position.y).ToList();
        circles = circles.OrderBy(o => o.transform.position.y).ToList();
        flyingFires = flyingFires.OrderBy(o => o.transform.position.y).ToList();
        
        List<GameObject> toLoopFire = fireLines.Take(fireLinesPerGroup).ToList();
        List<GameObject> toLoopCircle = circles.Take(circlesPerGroup).ToList();
        List<GameObject> toLoopFlyingFire = flyingFires.Take(flyingFiresPerGroup).ToList();
        
        if (toLoopFire.Count < fireLinesPerGroup || 
            toLoopCircle.Count < circlesPerGroup || 
            toLoopFlyingFire.Count < flyingFiresPerGroup)
        {
            return;
        }
        
        float newY = currentHighestY + groupSpacing;
        
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
        float circleCenterY = newY;
        
        foreach (GameObject circle in toLoopCircle)
        {
            Vector3 pos = circle.transform.position;
            pos.y = circleCenterY;
            circle.transform.position = pos;
            
            if (randomizeOnLoop)
                RandomizeObstacle(circle);
            
            ApplyCurrentSpeedToObstacle(circle);
            
            totalLooped++;
        }
        
        // ⭐⭐⭐ CÔNG THỨC: Tâm + Bán kính + Spacing
        newY = circleCenterY + circleRadius + circleToFireSpacing;
        
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
    }
    
    void PrintInfo()
    {
        Debug.Log("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        Debug.Log("🎮 PROGRESSIVE SPAWNER - MANUAL RADIUS");
        Debug.Log($"⭐ Circle Radius (bán kính): {circleRadius}");
        Debug.Log($"⭐ Spacing từ viền Circle: {circleToFireSpacing}");
        Debug.Log($"📐 Công thức: FlyingFire Y = Circle Center + {circleRadius} + {circleToFireSpacing}");
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
        
        string info = $"🎮 MANUAL RADIUS\n";
        info += $"━━━━━━━━━━━━━━━━━\n";
        info += $"F:{fireCount} C:{circleCount} FF:{flyingFireCount}\n";
        info += $"Wave: {currentWave}\n";
        info += $"⭐ Circle R: {circleRadius:F1}\n";
        info += $"⭐ Edge gap: {circleToFireSpacing:F1}";
        
        GUI.Label(new Rect(10, Screen.height - 140, 300, 140), info, style);
    }
    
    public void ResetDifficulty()
    {
        currentWave = 0;
        currentSpeed = initialSpeed;
        currentRotationSpeed = initialRotationSpeed;
        totalLooped = 0;
    }
}