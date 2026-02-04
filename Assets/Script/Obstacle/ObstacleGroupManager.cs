using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Quản lý obstacles theo nhóm với object pooling và culling
/// Tối ưu cho FPS và memory
/// </summary>
public class ObstacleGroupManager : MonoBehaviour
{
    [System.Serializable]
    public class ObstacleGroup
    {
        public string groupName;
        public List<GameObject> obstacles = new List<GameObject>();
        public float groupYPosition;
        public bool isActive;
        public bool isPassed; // Đã đi qua chưa
        
        public void SetActive(bool active)
        {
            isActive = active;
            foreach (GameObject obs in obstacles)
            {
                if (obs != null)
                    obs.SetActive(active);
            }
        }
        
        public void ResetGroup()
        {
            isPassed = false;
            foreach (GameObject obs in obstacles)
            {
                if (obs != null)
                {
                    // Reset vị trí và trạng thái
                    var controller = obs.GetComponent<ObstacleController>();
                    if (controller != null)
                        controller.RandomizeOnly();
                }
            }
        }
    }
    
    [Header("=== PREFABS ===")]
    [SerializeField] private GameObject fireLinePrefab;
    [SerializeField] private GameObject flyingCirclePrefab;
    [SerializeField] private GameObject flyingFirePrefab;
    
    [Header("=== GROUP SETTINGS ===")]
    [SerializeField] private int fireLinesPerGroup = 4;
    [SerializeField] private int circlesPerGroup = 1;
    [SerializeField] private int flyingFiresPerGroup = 4;
    [SerializeField] private float groupSpacing = 15f;
    [SerializeField] private float obstacleSpacing = 5f;
    
    [Header("=== POOL SETTINGS ===")]
    [SerializeField] private int maxActiveGroups = 5; // Số group hiển thị cùng lúc
    [SerializeField] private int totalPooledGroups = 8; // Tổng số group trong pool
    
    [Header("=== SPAWN SETTINGS ===")]
    [SerializeField] private float spawnDistance = 30f; // Khoảng cách spawn trước player
    [SerializeField] private float despawnDistance = -20f; // Khoảng cách xóa sau player
    
    [Header("=== SPEED PROGRESSION ===")]
    [SerializeField] private float initialSpeed = 1.0f;
    [SerializeField] private float speedIncrement = 0.2f;
    [SerializeField] private float maxSpeed = 5.0f;
    [SerializeField] private int groupsPassedPerSpeedIncrease = 2;
    
    [Header("=== REFERENCES ===")]
    [SerializeField] private Transform player;
    [SerializeField] private Camera mainCamera;
    
    [Header("=== CIRCLE SETTINGS ===")]
    [SerializeField] private float circleRadius = 2.5f;
    [SerializeField] private float circleToFireSpacing = 3f;
    [SerializeField] private bool spawnFireAboveCircle = false;
    [SerializeField] private float initialRotationSpeed = 120f;
    [SerializeField] private float rotationSpeedIncrement = 20f;
    [SerializeField] private float maxRotationSpeed = 360f;
    
    [Header("=== DEBUG ===")]
    [SerializeField] private bool showDebugInfo = true;
    [SerializeField] private bool showGizmos = true;
    
    // Private variables
    private List<ObstacleGroup> allGroups = new List<ObstacleGroup>();
    private Queue<ObstacleGroup> inactiveGroups = new Queue<ObstacleGroup>();
    private List<ObstacleGroup> activeGroups = new List<ObstacleGroup>();
    
    private float currentSpeed;
    private float currentRotationSpeed;
    private int totalGroupsPassed = 0;
    private float nextSpawnY;
    private float screenHeight;
    
    void Start()
    {
        InitializeReferences();
        InitializePool();
        SpawnInitialGroups();
    }
    
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
        
        screenHeight = mainCamera.orthographicSize * 2f;
        currentSpeed = initialSpeed;
        currentRotationSpeed = initialRotationSpeed;
        nextSpawnY = player.position.y + screenHeight;
    }
    
    void InitializePool()
    {
        // Kiểm tra xem có sử dụng obstacles có sẵn không
        if (transform.childCount > 0)
        {
            CreateGroupsFromExisting();
        }
        else
        {
            CreateNewGroups();
        }
        
        // Đưa tất cả vào inactive pool
        foreach (var group in allGroups)
        {
            group.SetActive(false);
            inactiveGroups.Enqueue(group);
        }
    }
    
    void CreateGroupsFromExisting()
    {
        List<GameObject> fireLines = new List<GameObject>();
        List<GameObject> circles = new List<GameObject>();
        List<GameObject> flyingFires = new List<GameObject>();
        
        // Phân loại các obstacles có sẵn
        foreach (Transform child in transform)
        {
            string name = child.name.ToLower();
            
            if (name.Contains("flying") && name.Contains("fire"))
                flyingFires.Add(child.gameObject);
            else if (name.Contains("flying") && name.Contains("circle"))
                circles.Add(child.gameObject);
            else if (name.Contains("fire") && name.Contains("line"))
                fireLines.Add(child.gameObject);
        }
        
        // Tạo groups từ các obstacles có sẵn
        int groupCount = Mathf.Max(
            Mathf.CeilToInt((float)fireLines.Count / fireLinesPerGroup),
            Mathf.Max(
                Mathf.CeilToInt((float)circles.Count / circlesPerGroup),
                Mathf.CeilToInt((float)flyingFires.Count / flyingFiresPerGroup)
            )
        );
        
        for (int i = 0; i < groupCount; i++)
        {
            ObstacleGroup group = new ObstacleGroup();
            group.groupName = $"Group_{i}";
            
            // Thêm fire lines
            for (int f = 0; f < fireLinesPerGroup && (i * fireLinesPerGroup + f) < fireLines.Count; f++)
            {
                int index = i * fireLinesPerGroup + f;
                group.obstacles.Add(fireLines[index]);
                SetupObstacle(fireLines[index], currentSpeed);
            }
            
            // Thêm circles
            for (int c = 0; c < circlesPerGroup && (i * circlesPerGroup + c) < circles.Count; c++)
            {
                int index = i * circlesPerGroup + c;
                group.obstacles.Add(circles[index]);
                SetupCircle(circles[index], currentRotationSpeed);
            }
            
            // Thêm flying fires
            for (int ff = 0; ff < flyingFiresPerGroup && (i * flyingFiresPerGroup + ff) < flyingFires.Count; ff++)
            {
                int index = i * flyingFiresPerGroup + ff;
                group.obstacles.Add(flyingFires[index]);
                SetupObstacle(flyingFires[index], currentSpeed);
            }
            
            allGroups.Add(group);
        }
        
        Debug.Log($"[GroupManager] Created {groupCount} groups from existing obstacles");
    }
    
    void CreateNewGroups()
    {
        for (int i = 0; i < totalPooledGroups; i++)
        {
            ObstacleGroup group = new ObstacleGroup();
            group.groupName = $"Group_{i}";
            
            // Tạo fire lines
            for (int f = 0; f < fireLinesPerGroup; f++)
            {
                GameObject fireLine = Instantiate(fireLinePrefab, transform);
                fireLine.name = $"FireLine_{i}_{f}";
                SetupObstacle(fireLine, currentSpeed);
                group.obstacles.Add(fireLine);
            }
            
            // Tạo circles
            for (int c = 0; c < circlesPerGroup; c++)
            {
                GameObject circle = Instantiate(flyingCirclePrefab, transform);
                circle.name = $"Circle_{i}_{c}";
                SetupCircle(circle, currentRotationSpeed);
                group.obstacles.Add(circle);
            }
            
            // Tạo flying fires
            for (int ff = 0; ff < flyingFiresPerGroup; ff++)
            {
                GameObject flyingFire = Instantiate(flyingFirePrefab, transform);
                flyingFire.name = $"FlyingFire_{i}_{ff}";
                SetupObstacle(flyingFire, currentSpeed);
                group.obstacles.Add(flyingFire);
            }
            
            allGroups.Add(group);
        }
        
        Debug.Log($"[GroupManager] Created {totalPooledGroups} new groups");
    }
    
    void SetupObstacle(GameObject obj, float speed)
    {
        ObstacleSpawner spawner = obj.GetComponent<ObstacleSpawner>();
        if (spawner == null)
            spawner = obj.AddComponent<ObstacleSpawner>();
        
        // Disable looper nếu có
        ObstacleLooper looper = obj.GetComponent<ObstacleLooper>();
        if (looper != null)
            looper.enabled = false;
        
        StartCoroutine(ApplySpeedToSpawnedObstacles(obj, speed));
    }
    
    void SetupCircle(GameObject obj, float rotSpeed)
    {
        FlyingCircleController controller = obj.GetComponent<FlyingCircleController>();
        if (controller == null)
            controller = obj.AddComponent<FlyingCircleController>();
        
        controller.SetRotationSpeed(rotSpeed);
        
        // Disable rotation ban đầu
        CircleRotation rotation = obj.GetComponentInChildren<CircleRotation>();
        if (rotation != null)
            rotation.enabled = false;
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
    
    void SpawnInitialGroups()
    {
        float currentY = nextSpawnY;
        
        for (int i = 0; i < maxActiveGroups && inactiveGroups.Count > 0; i++)
        {
            SpawnGroup(currentY);
            currentY += CalculateGroupHeight() + groupSpacing;
        }
        
        // Enable circle rotations sau khi spawn xong
        StartCoroutine(EnableCircleRotationsDelayed());
    }
    
    System.Collections.IEnumerator EnableCircleRotationsDelayed()
    {
        yield return new WaitForSeconds(0.1f);
        
        CircleRotation[] rotations = FindObjectsByType<CircleRotation>(FindObjectsSortMode.None);
        foreach (CircleRotation rotation in rotations)
        {
            rotation.enabled = true;
        }
    }
    
    void SpawnGroup(float yPosition)
    {
        if (inactiveGroups.Count == 0)
        {
            Debug.LogWarning("[GroupManager] No inactive groups available!");
            return;
        }
        
        ObstacleGroup group = inactiveGroups.Dequeue();
        group.ResetGroup();
        group.groupYPosition = yPosition;
        
        PositionGroup(group, yPosition);
        group.SetActive(true);
        
        activeGroups.Add(group);
        nextSpawnY = yPosition + CalculateGroupHeight() + groupSpacing;
        
        if (showDebugInfo)
            Debug.Log($"[GroupManager] Spawned {group.groupName} at Y={yPosition:F1}");
    }
    
    void PositionGroup(ObstacleGroup group, float startY)
    {
        float currentY = startY;
        int fireCount = 0;
        int circleCount = 0;
        int flyingFireCount = 0;
        
        // Đếm số lượng từng loại
        foreach (GameObject obs in group.obstacles)
        {
            if (obs == null) continue;
            string name = obs.name.ToLower();
            
            if (name.Contains("fireline")) fireCount++;
            else if (name.Contains("circle")) circleCount++;
            else if (name.Contains("flyingfire")) flyingFireCount++;
        }
        
        // Đặt vị trí fire lines
        foreach (GameObject obs in group.obstacles)
        {
            if (obs == null) continue;
            string name = obs.name.ToLower();
            
            if (name.Contains("fireline"))
            {
                Vector3 pos = obs.transform.position;
                pos.y = currentY;
                obs.transform.position = pos;
                currentY += obstacleSpacing;
            }
        }
        
        currentY += groupSpacing * 0.5f; // Khoảng cách trước circle
        float circleCenterY = currentY;
        
        // Đặt vị trí circles
        foreach (GameObject obs in group.obstacles)
        {
            if (obs == null) continue;
            string name = obs.name.ToLower();
            
            if (name.Contains("circle"))
            {
                Vector3 pos = obs.transform.position;
                pos.y = circleCenterY;
                obs.transform.position = pos;
            }
        }
        
        // Tính vị trí flying fires
        float flyingFireStartY;
        if (spawnFireAboveCircle)
        {
            flyingFireStartY = circleCenterY + circleRadius + circleToFireSpacing;
        }
        else
        {
            flyingFireStartY = circleCenterY - circleRadius - circleToFireSpacing;
        }
        
        currentY = flyingFireStartY;
        
        // Đặt vị trí flying fires
        foreach (GameObject obs in group.obstacles)
        {
            if (obs == null) continue;
            string name = obs.name.ToLower();
            
            if (name.Contains("flyingfire"))
            {
                Vector3 pos = obs.transform.position;
                pos.y = currentY;
                obs.transform.position = pos;
                currentY += obstacleSpacing;
            }
        }
    }
    
    float CalculateGroupHeight()
    {
        return (fireLinesPerGroup * obstacleSpacing) + 
               (flyingFiresPerGroup * obstacleSpacing) + 
               (circleRadius * 2) + 
               (circleToFireSpacing * 2);
    }
    
    void Update()
    {
        if (player == null) return;
        
        CheckGroupsPassed();
        CheckSpawnNewGroup();
        CullDistantGroups();
    }
    
    void CheckGroupsPassed()
    {
        float playerY = player.position.y;
        
        for (int i = activeGroups.Count - 1; i >= 0; i--)
        {
            ObstacleGroup group = activeGroups[i];
            
            if (!group.isPassed && playerY > group.groupYPosition + CalculateGroupHeight())
            {
                group.isPassed = true;
                totalGroupsPassed++;
                
                if (showDebugInfo)
                    Debug.Log($"[GroupManager] Passed {group.groupName}, Total: {totalGroupsPassed}");
                
                // Tăng tốc độ
                if (totalGroupsPassed % groupsPassedPerSpeedIncrease == 0)
                {
                    IncreaseSpeed();
                }
            }
        }
    }
    
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
    
    void CullDistantGroups()
    {
        float playerY = player.position.y;
        
        for (int i = activeGroups.Count - 1; i >= 0; i--)
        {
            ObstacleGroup group = activeGroups[i];
            
            // Xóa group đã đi qua xa
            if (group.groupYPosition < playerY + despawnDistance)
            {
                group.SetActive(false);
                activeGroups.RemoveAt(i);
                inactiveGroups.Enqueue(group);
                
                if (showDebugInfo)
                    Debug.Log($"[GroupManager] Recycled {group.groupName}");
            }
        }
    }
    
    void IncreaseSpeed()
    {
        currentSpeed = Mathf.Min(currentSpeed + speedIncrement, maxSpeed);
        currentRotationSpeed = Mathf.Min(currentRotationSpeed + rotationSpeedIncrement, maxRotationSpeed);
        
        ApplySpeedToAllActive();
        
        if (showDebugInfo)
            Debug.Log($"[GroupManager] Speed increased to {currentSpeed:F2}");
    }
    
    void ApplySpeedToAllActive()
    {
        foreach (var group in activeGroups)
        {
            foreach (var obs in group.obstacles)
            {
                if (obs == null) continue;
                
                // Cập nhật speed cho obstacles
                ObstacleSpawner spawner = obs.GetComponent<ObstacleSpawner>();
                if (spawner != null && spawner.SpawnedObstacles != null)
                {
                    foreach (GameObject spawnedObs in spawner.SpawnedObstacles)
                    {
                        if (spawnedObs == null) continue;
                        
                        ObstacleMovement movement = spawnedObs.GetComponent<ObstacleMovement>();
                        if (movement != null)
                            movement.SetSpeed(currentSpeed);
                    }
                }
                
                // Cập nhật rotation speed cho circles
                FlyingCircleController circle = obs.GetComponent<FlyingCircleController>();
                if (circle != null)
                    circle.SetRotationSpeed(currentRotationSpeed);
            }
        }
    }
    
    void OnGUI()
    {
        if (!showDebugInfo || !Application.isPlaying) return;
        
        GUIStyle style = new GUIStyle();
        style.fontSize = 18;
        style.normal.textColor = Color.cyan;
        style.alignment = TextAnchor.UpperLeft;
        style.fontStyle = FontStyle.Bold;
        
        string info = $"=== OBSTACLE GROUP MANAGER ===\n";
        info += $"Active Groups: {activeGroups.Count}/{maxActiveGroups}\n";
        info += $"Pooled Groups: {inactiveGroups.Count}\n";
        info += $"Groups Passed: {totalGroupsPassed}\n";
        info += $"Current Speed: {currentSpeed:F2}/{maxSpeed:F1}\n";
        info += $"Rotation Speed: {currentRotationSpeed:F0}°/s\n";
        info += $"Next Spawn Y: {nextSpawnY:F1}\n";
        info += $"FPS: {(1f / Time.deltaTime):F0}";
        
        GUI.Label(new Rect(10, 10, 400, 200), info, style);
    }
    
    void OnDrawGizmos()
    {
        if (!showGizmos || !Application.isPlaying || player == null) return;
        
        float playerY = player.position.y;
        
        // Vẽ spawn line
        Gizmos.color = Color.green;
        Gizmos.DrawLine(new Vector3(-10, playerY + spawnDistance, 0), 
                       new Vector3(10, playerY + spawnDistance, 0));
        
        // Vẽ despawn line
        Gizmos.color = Color.red;
        Gizmos.DrawLine(new Vector3(-10, playerY + despawnDistance, 0), 
                       new Vector3(10, playerY + despawnDistance, 0));
        
        // Vẽ active groups
        foreach (var group in activeGroups)
        {
            Gizmos.color = group.isPassed ? Color.yellow : Color.cyan;
            Gizmos.DrawWireCube(new Vector3(0, group.groupYPosition, 0), 
                               new Vector3(8, CalculateGroupHeight(), 0.1f));
        }
    }
    
    public void ResetGame()
    {
        // Reset tất cả
        foreach (var group in activeGroups)
        {
            group.SetActive(false);
            inactiveGroups.Enqueue(group);
        }
        
        activeGroups.Clear();
        totalGroupsPassed = 0;
        currentSpeed = initialSpeed;
        currentRotationSpeed = initialRotationSpeed;
        nextSpawnY = player.position.y + screenHeight;
        
        SpawnInitialGroups();
        
        Debug.Log("[GroupManager] Game reset!");
    }
}