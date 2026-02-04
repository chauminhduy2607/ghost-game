using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// IMPROVED Obstacle Spawner - Làm việc trực tiếp với obstacles có sẵn trong Hierarchy
/// KHÔNG cần prefabs, chỉ cần thay script này vào ObstacleManager
/// 
/// HƯỚNG DẪN SỬ DỤNG:
/// 1. Select ObstacleManager trong Hierarchy
/// 2. Disable/Remove script cũ (ProgressiveObstacleSpawner)
/// 3. Add Component → ImprovedObstacleSpawner
/// 4. Không cần setup gì thêm - script tự động detect!
/// </summary>
public class ImprovedObstacleSpawner : MonoBehaviour
{
    [Header("=== GROUP CONFIGURATION ===")]
    [Tooltip("Số Fire&Line mỗi group")]
    [SerializeField] private int fireLinesPerGroup = 4;
    
    [Tooltip("Số FlyingCircle mỗi group")]
    [SerializeField] private int circlesPerGroup = 1;
    
    [Tooltip("Số FlyingFire mỗi group")]
    [SerializeField] private int flyingFiresPerGroup = 4;
    
    [Header("=== SPACING SETTINGS ===")]
    [Tooltip("Khoảng cách giữa các obstacles trong group")]
    [SerializeField] private float obstacleSpacing = 5f;
    
    [Tooltip("Khoảng cách giữa các groups")]
    [SerializeField] private float groupSpacing = 15f;
    
    [Tooltip("Khoảng cách từ Circle đến FlyingFire")]
    [SerializeField] private float circleToFireSpacing = 3f;
    
    [Tooltip("Bán kính của Circle (để tính vị trí)")]
    [SerializeField] private float circleRadius = 2.5f;
    
    [Tooltip("FlyingFire spawn ở trên hay dưới Circle")]
    [SerializeField] private bool spawnFireAboveCircle = false;
    
    [Header("=== SPAWN/DESPAWN ===")]
    [Tooltip("Khoảng cách spawn trước player")]
    [SerializeField] private float spawnDistance = 30f;
    
    [Tooltip("Khoảng cách despawn sau player (âm = phía dưới)")]
    [SerializeField] private float despawnDistance = -20f;
    
    [Tooltip("Số groups active tối đa cùng lúc")]
    [SerializeField] private int maxActiveGroups = 5;
    
    [Header("=== SPEED PROGRESSION ===")]
    [Tooltip("Tốc độ ban đầu")]
    [SerializeField] private float initialSpeed = 1.0f;
    
    [Tooltip("Tăng speed mỗi lần")]
    [SerializeField] private float speedIncrement = 0.2f;
    
    [Tooltip("Tốc độ tối đa")]
    [SerializeField] private float maxSpeed = 5.0f;
    
    [Tooltip("Số groups đi qua để tăng speed")]
    [SerializeField] private int groupsPerSpeedIncrease = 2;
    
    [Header("=== ROTATION (for Circles) ===")]
    [Tooltip("Tốc độ quay ban đầu")]
    [SerializeField] private float initialRotationSpeed = 120f;
    
    [Tooltip("Tăng rotation mỗi lần")]
    [SerializeField] private float rotationSpeedIncrement = 20f;
    
    [Tooltip("Tốc độ quay tối đa")]
    [SerializeField] private float maxRotationSpeed = 360f;
    
    [Header("=== REFERENCES ===")]
    [SerializeField] private Transform player;
    [SerializeField] private Camera mainCamera;
    
    [Header("=== DEBUG ===")]
    [SerializeField] private bool showDebugInfo = true;
    [SerializeField] private bool showGizmos = true;
    
    // ========== PRIVATE VARIABLES ==========
    
    private class ObstacleGroup
    {
        public string name;
        public List<GameObject> fireLines = new List<GameObject>();
        public List<GameObject> circles = new List<GameObject>();
        public List<GameObject> flyingFires = new List<GameObject>();
        public float centerY;
        public bool isActive;
        public bool isPassed;
        
        public void SetActive(bool active)
        {
            isActive = active;
            foreach (var obj in fireLines) if (obj) obj.SetActive(active);
            foreach (var obj in circles) if (obj) obj.SetActive(active);
            foreach (var obj in flyingFires) if (obj) obj.SetActive(active);
        }
        
        public int TotalCount => fireLines.Count + circles.Count + flyingFires.Count;
    }
    
    private List<GameObject> allFireLines = new List<GameObject>();
    private List<GameObject> allCircles = new List<GameObject>();
    private List<GameObject> allFlyingFires = new List<GameObject>();
    
    private Queue<ObstacleGroup> inactiveGroups = new Queue<ObstacleGroup>();
    private List<ObstacleGroup> activeGroups = new List<ObstacleGroup>();
    
    private float currentSpeed;
    private float currentRotationSpeed;
    private int totalGroupsPassed;
    private float nextSpawnY;
    private float screenHeight;
    
    // ========== UNITY LIFECYCLE ==========
    
    void Start()
    {
        InitializeReferences();
        CollectAllObstacles();
        CreateGroupsFromObstacles();
        DisableAllCircleRotations();
        InitializeFirstGroups();
        EnableCircleRotationsDelayed();
    }
    
    void Update()
    {
        if (player == null) return;
        
        CheckSpawnNewGroup();
        CheckDespawnOldGroups();
        CheckGroupsPassed();
    }
    
    // ========== INITIALIZATION ==========
    
    void InitializeReferences()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;
        
        if (player == null)
        {
            GhostController ghost = FindAnyObjectByType<GhostController>();
            if (ghost != null)
                player = ghost.transform;
        }
        
        if (mainCamera != null)
            screenHeight = mainCamera.orthographicSize * 2f;
        
        currentSpeed = initialSpeed;
        currentRotationSpeed = initialRotationSpeed;
        totalGroupsPassed = 0;
        
        if (player != null)
            nextSpawnY = player.position.y + screenHeight * 0.5f;
        
        Debug.Log($"[ImprovedSpawner] Initialized - Screen Height: {screenHeight}");
    }
    
    void CollectAllObstacles()
    {
        allFireLines.Clear();
        allCircles.Clear();
        allFlyingFires.Clear();
        
        // Duyệt qua tất cả children của ObstacleManager
        foreach (Transform child in transform)
        {
            if (child == null) continue;
            
            string name = child.name.ToLower();
            
            if (name.Contains("fire") && name.Contains("line"))
            {
                allFireLines.Add(child.gameObject);
                SetupFireLine(child.gameObject);
            }
            else if (name.Contains("flying") && name.Contains("circle"))
            {
                allCircles.Add(child.gameObject);
                SetupCircle(child.gameObject);
            }
            else if (name.Contains("flying") && name.Contains("fire"))
            {
                allFlyingFires.Add(child.gameObject);
                SetupFlyingFire(child.gameObject);
            }
        }
        
        Debug.Log($"[ImprovedSpawner] Collected: {allFireLines.Count} FireLines, " +
                  $"{allCircles.Count} Circles, {allFlyingFires.Count} FlyingFires");
    }
    
    void CreateGroupsFromObstacles()
    {
        // Tính số groups có thể tạo
        int maxGroups = Mathf.Max(
            Mathf.CeilToInt((float)allFireLines.Count / fireLinesPerGroup),
            Mathf.Max(
                Mathf.CeilToInt((float)allCircles.Count / circlesPerGroup),
                Mathf.CeilToInt((float)allFlyingFires.Count / flyingFiresPerGroup)
            )
        );
        
        int fireIndex = 0;
        int circleIndex = 0;
        int fireIndex2 = 0;
        
        for (int i = 0; i < maxGroups; i++)
        {
            ObstacleGroup group = new ObstacleGroup();
            group.name = $"Group_{i}";
            
            // Add Fire&Lines
            for (int f = 0; f < fireLinesPerGroup && fireIndex < allFireLines.Count; f++)
            {
                group.fireLines.Add(allFireLines[fireIndex]);
                fireIndex++;
            }
            
            // Add Circles
            for (int c = 0; c < circlesPerGroup && circleIndex < allCircles.Count; c++)
            {
                group.circles.Add(allCircles[circleIndex]);
                circleIndex++;
            }
            
            // Add FlyingFires
            for (int ff = 0; ff < flyingFiresPerGroup && fireIndex2 < allFlyingFires.Count; ff++)
            {
                group.flyingFires.Add(allFlyingFires[fireIndex2]);
                fireIndex2++;
            }
            
            if (group.TotalCount > 0)
            {
                group.SetActive(false); // Tắt hết trước
                inactiveGroups.Enqueue(group);
            }
        }
        
        Debug.Log($"[ImprovedSpawner] Created {maxGroups} groups, {inactiveGroups.Count} in pool");
    }
    
    void InitializeFirstGroups()
    {
        float currentY = nextSpawnY;
        
        int groupsToSpawn = Mathf.Min(maxActiveGroups, inactiveGroups.Count);
        
        for (int i = 0; i < groupsToSpawn; i++)
        {
            SpawnGroup(currentY);
            currentY += CalculateGroupHeight() + groupSpacing;
        }
        
        Debug.Log($"[ImprovedSpawner] Spawned {groupsToSpawn} initial groups");
    }
    
    // ========== SETUP METHODS ==========
    
    void SetupFireLine(GameObject obj)
    {
        // Tắt ObstacleLooper nếu có
        ObstacleLooper looper = obj.GetComponent<ObstacleLooper>();
        if (looper != null)
            looper.enabled = false;
        
        // Setup spawner nếu có
        ObstacleSpawner spawner = obj.GetComponent<ObstacleSpawner>();
        if (spawner != null)
        {
            StartCoroutine(ApplySpeedToSpawnedObstacles(obj, currentSpeed));
        }
    }
    
    void SetupCircle(GameObject obj)
    {
        FlyingCircleController controller = obj.GetComponent<FlyingCircleController>();
        if (controller != null)
        {
            controller.SetRotationSpeed(currentRotationSpeed);
        }
    }
    
    void SetupFlyingFire(GameObject obj)
    {
        // Tắt ObstacleLooper nếu có
        ObstacleLooper looper = obj.GetComponent<ObstacleLooper>();
        if (looper != null)
            looper.enabled = false;
        
        // Setup spawner nếu có
        ObstacleSpawner spawner = obj.GetComponent<ObstacleSpawner>();
        if (spawner != null)
        {
            StartCoroutine(ApplySpeedToSpawnedObstacles(obj, currentSpeed));
        }
    }
    
    System.Collections.IEnumerator ApplySpeedToSpawnedObstacles(GameObject parent, float speed)
    {
        yield return new WaitForEndOfFrame();
        
        ObstacleSpawner spawner = parent.GetComponent<ObstacleSpawner>();
        if (spawner != null && spawner.SpawnedObstacles != null)
        {
            foreach (GameObject obs in spawner.SpawnedObstacles)
            {
                if (obs == null) continue;
                
                ObstacleMovement movement = obs.GetComponent<ObstacleMovement>();
                if (movement != null)
                    movement.SetSpeed(speed);
            }
        }
    }
    
    void DisableAllCircleRotations()
    {
        CircleRotation[] rotations = FindObjectsByType<CircleRotation>(FindObjectsSortMode.None);
        foreach (var rotation in rotations)
        {
            rotation.enabled = false;
        }
    }
    
    void EnableCircleRotationsDelayed()
    {
        Invoke(nameof(EnableAllCircleRotations), 0.2f);
    }
    
    void EnableAllCircleRotations()
    {
        CircleRotation[] rotations = FindObjectsByType<CircleRotation>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var rotation in rotations)
        {
            rotation.enabled = true;
        }
    }
    
    // ========== SPAWNING LOGIC ==========
    
    void SpawnGroup(float yPosition)
    {
        if (inactiveGroups.Count == 0)
        {
            Debug.LogWarning("[ImprovedSpawner] No inactive groups!");
            return;
        }
        
        ObstacleGroup group = inactiveGroups.Dequeue();
        group.centerY = yPosition;
        group.isPassed = false;
        
        PositionGroup(group, yPosition);
        group.SetActive(true);
        
        activeGroups.Add(group);
        nextSpawnY = yPosition + CalculateGroupHeight() + groupSpacing;
        
        if (showDebugInfo)
            Debug.Log($"[ImprovedSpawner] Spawned {group.name} at Y={yPosition:F1}");
    }
    
    void PositionGroup(ObstacleGroup group, float startY)
    {
        float currentY = startY;
        
        // 1. Đặt Fire&Lines - GIỮ NGUYÊN khoảng cách như code cũ
        foreach (var fireLine in group.fireLines)
        {
            if (fireLine == null) continue;
            
            Vector3 pos = fireLine.transform.position;
            pos.y = currentY;
            fireLine.transform.position = pos;
            
            currentY += obstacleSpacing;
        }
        
        // Thêm khoảng cách giữa Fire&Line và Circle
        currentY += groupSpacing;
        
        // 2. Đặt Circles (ở giữa)
        float circleCenterY = currentY;
        foreach (var circle in group.circles)
        {
            if (circle == null) continue;
            
            Vector3 pos = circle.transform.position;
            pos.y = circleCenterY;
            circle.transform.position = pos;
        }
        
        // 3. Đặt FlyingFires - Tính vị trí dựa trên Circle
        float flyingFireStartY;
        if (spawnFireAboveCircle)
        {
            // Nếu spawn ở TRÊN circle
            flyingFireStartY = circleCenterY + circleRadius + circleToFireSpacing;
        }
        else
        {
            // Nếu spawn ở DƯỚI circle
            flyingFireStartY = circleCenterY - circleRadius - circleToFireSpacing;
        }
        
        currentY = flyingFireStartY;
        foreach (var flyingFire in group.flyingFires)
        {
            if (flyingFire == null) continue;
            
            Vector3 pos = flyingFire.transform.position;
            pos.y = currentY;
            flyingFire.transform.position = pos;
            
            // Tăng Y cho obstacle tiếp theo
            if (spawnFireAboveCircle)
                currentY += obstacleSpacing; // Đi lên
            else
                currentY -= obstacleSpacing; // Đi xuống (nếu dưới circle)
        }
    }
    
    float CalculateGroupHeight()
    {
        // Tính chiều cao tổng của 1 group
        float height = 0;
        
        // Fire&Lines spacing
        height += fireLinesPerGroup * obstacleSpacing;
        
        // Group spacing giữa Fire&Line và Circle
        height += groupSpacing;
        
        // Circle diameter
        height += circleRadius * 2;
        
        // Spacing từ circle đến FlyingFire
        height += circleToFireSpacing;
        
        // FlyingFires spacing
        height += flyingFiresPerGroup * obstacleSpacing;
        
        return height;
    }
    
    // ========== UPDATE LOGIC ==========
    
    void CheckSpawnNewGroup()
    {
        if (player == null) return;
        
        float playerY = player.position.y;
        
        // Spawn group mới nếu cần
        if (nextSpawnY < playerY + spawnDistance && inactiveGroups.Count > 0)
        {
            SpawnGroup(nextSpawnY);
        }
    }
    
    void CheckDespawnOldGroups()
    {
        if (player == null) return;
        
        float playerY = player.position.y;
        
        for (int i = activeGroups.Count - 1; i >= 0; i--)
        {
            ObstacleGroup group = activeGroups[i];
            
            // Despawn nếu quá xa phía sau
            if (group.centerY < playerY + despawnDistance)
            {
                group.SetActive(false);
                activeGroups.RemoveAt(i);
                inactiveGroups.Enqueue(group);
                
                if (showDebugInfo)
                    Debug.Log($"[ImprovedSpawner] Despawned {group.name}");
            }
        }
    }
    
    void CheckGroupsPassed()
    {
        if (player == null) return;
        
        float playerY = player.position.y;
        
        foreach (var group in activeGroups)
        {
            if (!group.isPassed && playerY > group.centerY + CalculateGroupHeight() * 0.5f)
            {
                group.isPassed = true;
                totalGroupsPassed++;
                
                if (showDebugInfo)
                    Debug.Log($"[ImprovedSpawner] Passed {group.name}, Total: {totalGroupsPassed}");
                
                // Tăng tốc độ
                if (totalGroupsPassed % groupsPerSpeedIncrease == 0)
                {
                    IncreaseSpeed();
                }
            }
        }
    }
    
    // ========== SPEED PROGRESSION ==========
    
    void IncreaseSpeed()
    {
        currentSpeed = Mathf.Min(currentSpeed + speedIncrement, maxSpeed);
        currentRotationSpeed = Mathf.Min(currentRotationSpeed + rotationSpeedIncrement, maxRotationSpeed);
        
        ApplySpeedToAllActive();
        
        if (showDebugInfo)
            Debug.Log($"[ImprovedSpawner] Speed: {currentSpeed:F2}, Rotation: {currentRotationSpeed:F0}°/s");
    }
    
    void ApplySpeedToAllActive()
    {
        foreach (var group in activeGroups)
        {
            // Fire&Lines
            foreach (var fireLine in group.fireLines)
            {
                if (fireLine == null) continue;
                ApplySpeedToObstacle(fireLine, currentSpeed);
            }
            
            // Circles
            foreach (var circle in group.circles)
            {
                if (circle == null) continue;
                
                FlyingCircleController controller = circle.GetComponent<FlyingCircleController>();
                if (controller != null)
                    controller.SetRotationSpeed(currentRotationSpeed);
            }
            
            // FlyingFires
            foreach (var flyingFire in group.flyingFires)
            {
                if (flyingFire == null) continue;
                ApplySpeedToObstacle(flyingFire, currentSpeed);
            }
        }
    }
    
    void ApplySpeedToObstacle(GameObject obj, float speed)
    {
        ObstacleSpawner spawner = obj.GetComponent<ObstacleSpawner>();
        if (spawner != null && spawner.SpawnedObstacles != null)
        {
            foreach (var spawnedObj in spawner.SpawnedObstacles)
            {
                if (spawnedObj == null) continue;
                
                ObstacleMovement movement = spawnedObj.GetComponent<ObstacleMovement>();
                if (movement != null)
                    movement.SetSpeed(speed);
            }
        }
    }
    
    // ========== DEBUG & GIZMOS =========
    
    void OnDrawGizmos()
    {
        if (!showGizmos || !Application.isPlaying || player == null) return;
        
        float playerY = player.position.y;
        
        // Spawn line (xanh lá)
        Gizmos.color = Color.green;
        Gizmos.DrawLine(new Vector3(-15, playerY + spawnDistance, 0),
                       new Vector3(15, playerY + spawnDistance, 0));
        
        // Despawn line (đỏ)
        Gizmos.color = Color.red;
        Gizmos.DrawLine(new Vector3(-15, playerY + despawnDistance, 0),
                       new Vector3(15, playerY + despawnDistance, 0));
        
        // Active groups (vàng = passed, cyan = not passed)
        foreach (var group in activeGroups)
        {
            Gizmos.color = group.isPassed ? Color.yellow : Color.cyan;
            float height = CalculateGroupHeight();
            Gizmos.DrawWireCube(new Vector3(0, group.centerY, 0),
                               new Vector3(10, height, 0.1f));
        }
    }
    
    // ========== PUBLIC METHODS ==========
    
    public void ResetGame()
    {
        // Tắt tất cả active groups
        foreach (var group in activeGroups)
        {
            group.SetActive(false);
            inactiveGroups.Enqueue(group);
        }
        
        activeGroups.Clear();
        totalGroupsPassed = 0;
        currentSpeed = initialSpeed;
        currentRotationSpeed = initialRotationSpeed;
        
        if (player != null)
            nextSpawnY = player.position.y + screenHeight * 0.5f;
        
        InitializeFirstGroups();
        
        Debug.Log("[ImprovedSpawner] Game reset!");
    }
}