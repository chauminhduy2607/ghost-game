using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// IMPROVED Obstacle Spawner V3 - HỖ TRỢ CHỒNG VẬT CẢN
/// FIX: Khoảng cách giữa Fire&Line và Circle
/// </summary>
public class ImprovedObstacleSpawner : MonoBehaviour
{
    [Header("=== GROUP CONFIGURATION ===")]
    [Tooltip("Số obstacles mỗi loại trong 1 group")]
    [SerializeField] private int obstaclesPerType = 4;
    
    [Header("=== SPACING SETTINGS ===")]
    [Tooltip("Khoảng cách giữa các obstacles cùng loại (nếu KHÔNG stackable)")]
    [SerializeField] private float obstacleSpacing = 6f;
    
    [Tooltip("Khoảng cách giữa các loại vật cản khác nhau")]
    [SerializeField] private float typeGap = 12f;
    
    [Tooltip("Khoảng cách giữa các groups")]
    [SerializeField] private float groupGap = 15f;
    
    [Header("=== SPAWN/DESPAWN ===")]
    [Tooltip("Khoảng cách spawn trước player")]
    [SerializeField] private float spawnDistance = 30f;
    
    [Tooltip("Khoảng cách despawn sau player")]
    [SerializeField] private float despawnDistance = -20f;
    
    [Tooltip("Số groups active tối đa cùng lúc")]
    [SerializeField] private int maxActiveGroups = 5;
    
    [Header("=== SPEED PROGRESSION ===")]
    [SerializeField] private float initialSpeed = 1.0f;
    [SerializeField] private float speedIncrement = 0.2f;
    [SerializeField] private float maxSpeed = 5.0f;
    [SerializeField] private int groupsPerSpeedIncrease = 2;
    
    [Header("=== ROTATION (for Circles) ===")]
    [SerializeField] private float initialRotationSpeed = 120f;
    [SerializeField] private float rotationSpeedIncrement = 20f;
    [SerializeField] private float maxRotationSpeed = 360f;
    
    [Header("=== REFERENCES ===")]
    [SerializeField] private Transform player;
    [SerializeField] private Camera mainCamera;
    
    [Header("=== DEBUG ===")]
    [SerializeField] private bool showDebugInfo = true;
    [SerializeField] private bool showGizmos = true;
    
    // ========== PRIVATE VARIABLES ==========
    
    private class ObstacleType
    {
        public string name;
        public List<GameObject> objects = new List<GameObject>();
        public bool isCircle;
        public int priority;
        public bool stackable;
    }
    
    private class ObstacleGroup
    {
        public string name;
        public Dictionary<string, List<GameObject>> obstaclesByType = new Dictionary<string, List<GameObject>>();
        public float centerY;
        public bool isActive;
        public bool isPassed;
        
        public void SetActive(bool active)
        {
            isActive = active;
            foreach (var typeList in obstaclesByType.Values)
            {
                foreach (var obj in typeList)
                {
                    if (obj) obj.SetActive(active);
                }
            }
        }
        
        public int TotalCount
        {
            get
            {
                int count = 0;
                foreach (var list in obstaclesByType.Values)
                    count += list.Count;
                return count;
            }
        }
    }
    
    private List<ObstacleType> obstacleTypes = new List<ObstacleType>();
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
        AutoDetectObstacleTypes();
        CreateGroupsFromObstacles();
        InitializeFirstGroups();
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
        
        Debug.Log($"[ImprovedSpawner] Initialized");
    }
    
    void AutoDetectObstacleTypes()
    {
        obstacleTypes.Clear();
        
        foreach (Transform child in transform)
        {
            if (child == null) continue;
            
            string tag = child.tag;
            if (!tag.EndsWith("Obstacle")) continue;
            
            string typeName = tag.Replace("Obstacle", "");
            ObstacleType existingType = obstacleTypes.Find(t => t.name == typeName);
            
            if (existingType == null)
            {
                ObstacleType newType = new ObstacleType
                {
                    name = typeName,
                    isCircle = typeName.ToLower().Contains("circle"),
                    priority = GetPriority(typeName),
                    stackable = IsStackable(typeName)
                };
                
                newType.objects.Add(child.gameObject);
                obstacleTypes.Add(newType);
                SetupObstacle(child.gameObject, newType.isCircle);
                
                if (showDebugInfo)
                    Debug.Log($"[Spawner] 🆕 {typeName} (Circle: {newType.isCircle}, Stackable: {newType.stackable})");
            }
            else
            {
                existingType.objects.Add(child.gameObject);
                SetupObstacle(child.gameObject, existingType.isCircle);
            }
        }
        
        obstacleTypes = obstacleTypes.OrderBy(t => t.priority).ToList();
        
        if (showDebugInfo)
        {
            Debug.Log($"[ImprovedSpawner] 📊 {obstacleTypes.Count} types:");
            foreach (var type in obstacleTypes)
                Debug.Log($"  • {type.name}: {type.objects.Count} (P:{type.priority}, S:{type.stackable})");
        }
    }
    
    bool IsStackable(string typeName)
    {
        string lower = typeName.ToLower();
        if (lower.Contains("circle")) return true;
        if (lower.Contains("flyingfire")) return true;
        return false;
    }
    
    int GetPriority(string typeName)
    {
        string lower = typeName.ToLower();
        
        if (lower.Contains("fireline") || lower.Contains("firenline"))
            return 0;
        else if (lower.Contains("circle"))
        {
            if (lower.Contains("1")) return 1;
            if (lower.Contains("2")) return 2;
            if (lower.Contains("3")) return 3;
            return 10;
        }
        else if (lower.Contains("flyingfire"))
            return 100;
        else
            return 50;
    }
    
    void SetupObstacle(GameObject obj, bool isCircle)
    {
        if (isCircle)
        {
            FlyingCircleController controller = obj.GetComponent<FlyingCircleController>();
            if (controller != null)
            {
                controller.SetRotationSpeed(currentRotationSpeed);
            }
        }
        else
        {
            ObstacleLooper looper = obj.GetComponent<ObstacleLooper>();
            if (looper != null)
                looper.enabled = false;
            
            ObstacleSpawner spawner = obj.GetComponent<ObstacleSpawner>();
            if (spawner != null)
            {
                StartCoroutine(ApplySpeedToSpawnedObstacles(obj, currentSpeed));
            }
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
    
    void CreateGroupsFromObstacles()
    {
        if (obstacleTypes.Count == 0)
        {
            Debug.LogError("[ImprovedSpawner] No obstacle types detected!");
            return;
        }
        
        int maxGroups = 0;
        foreach (var type in obstacleTypes)
        {
            int groupsForType = Mathf.CeilToInt((float)type.objects.Count / obstaclesPerType);
            maxGroups = Mathf.Max(maxGroups, groupsForType);
        }
        
        for (int groupIndex = 0; groupIndex < maxGroups; groupIndex++)
        {
            ObstacleGroup group = new ObstacleGroup();
            group.name = $"Group_{groupIndex}";
            
            foreach (var type in obstacleTypes)
            {
                List<GameObject> objectsForThisGroup = new List<GameObject>();
                
                int startIndex = groupIndex * obstaclesPerType;
                int endIndex = Mathf.Min(startIndex + obstaclesPerType, type.objects.Count);
                
                for (int i = startIndex; i < endIndex; i++)
                {
                    objectsForThisGroup.Add(type.objects[i]);
                }
                
                if (objectsForThisGroup.Count > 0)
                {
                    group.obstaclesByType[type.name] = objectsForThisGroup;
                }
            }
            
            if (group.TotalCount > 0)
            {
                group.SetActive(false);
                inactiveGroups.Enqueue(group);
            }
        }
        
        Debug.Log($"[ImprovedSpawner] 🎯 Created {maxGroups} groups");
    }
    
    void InitializeFirstGroups()
    {
        float currentY = nextSpawnY;
        
        int groupsToSpawn = Mathf.Min(maxActiveGroups, inactiveGroups.Count);
        
        for (int i = 0; i < groupsToSpawn; i++)
        {
            SpawnGroup(currentY);
            currentY += CalculateGroupHeight() + groupGap;
        }
        
        Debug.Log($"[ImprovedSpawner] 🚀 Spawned {groupsToSpawn} initial groups");
    }
    
    // ========== SPAWNING ==========
    
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
        nextSpawnY = yPosition + CalculateGroupHeight() + groupGap;
        
        if (showDebugInfo)
            Debug.Log($"[ImprovedSpawner] 📍 Spawned {group.name} at Y={yPosition:F1}");
    }
    
    // ✅ FIX: HÀM ĐẶT VỊ TRÍ
    void PositionGroup(ObstacleGroup group, float startY)
    {
        float currentY = startY;
        
        foreach (var type in obstacleTypes)
        {
            if (!group.obstaclesByType.ContainsKey(type.name))
                continue;
            
            List<GameObject> objectsOfThisType = group.obstaclesByType[type.name];
            
            if (type.stackable)
            {
                // Chồng lên nhau - cùng Y
                foreach (var obj in objectsOfThisType)
                {
                    if (obj == null) continue;
                    
                    Vector3 pos = obj.transform.position;
                    pos.y = currentY;
                    obj.transform.position = pos;
                }
                
                if (showDebugInfo)
                    Debug.Log($"  🔗 Stacked {objectsOfThisType.Count}x {type.name} at Y={currentY:F1}");
            }
            else
            {
                // Không chồng - cách nhau obstacleSpacing
                for (int i = 0; i < objectsOfThisType.Count; i++)
                {
                    GameObject obj = objectsOfThisType[i];
                    if (obj == null) continue;
                    
                    Vector3 pos = obj.transform.position;
                    pos.y = currentY;
                    obj.transform.position = pos;
                    
                    currentY += obstacleSpacing;
                }
            }
            
            // ✅ FIX: Thêm typeGap trực tiếp
            currentY += typeGap;
        }
    }
    
    // ✅ FIX: TÍNH CHIỀU CAO
    float CalculateGroupHeight()
    {
        float height = 0;
        
        foreach (var type in obstacleTypes)
        {
            if (type.stackable)
            {
                // Stackable: không cộng gì (vì chồng lên nhau)
                // Chỉ cộng typeGap
            }
            else
            {
                // Non-stackable: cộng khoảng cách giữa các obstacles
                height += (obstaclesPerType - 1) * obstacleSpacing;
            }
            
            // Cộng typeGap cho mọi loại
            height += typeGap;
        }
        
        // Bỏ typeGap cuối cùng
        if (obstacleTypes.Count > 0)
            height -= typeGap;
        
        return height;
    }
    
    // ========== UPDATE ==========
    
    void CheckSpawnNewGroup()
    {
        if (player == null) return;
        
        if (nextSpawnY < player.position.y + spawnDistance && inactiveGroups.Count > 0)
            SpawnGroup(nextSpawnY);
    }
    
    void CheckDespawnOldGroups()
    {
        if (player == null) return;
        
        for (int i = activeGroups.Count - 1; i >= 0; i--)
        {
            if (activeGroups[i].centerY < player.position.y + despawnDistance)
            {
                activeGroups[i].SetActive(false);
                inactiveGroups.Enqueue(activeGroups[i]);
                activeGroups.RemoveAt(i);
            }
        }
    }
    
    void CheckGroupsPassed()
    {
        if (player == null) return;
        
        foreach (var group in activeGroups)
        {
            if (!group.isPassed && player.position.y > group.centerY + CalculateGroupHeight() * 0.5f)
            {
                group.isPassed = true;
                totalGroupsPassed++;
                
                if (totalGroupsPassed % groupsPerSpeedIncrease == 0)
                    IncreaseSpeed();
            }
        }
    }
    
    // ========== SPEED ==========
    
    void IncreaseSpeed()
    {
        currentSpeed = Mathf.Min(currentSpeed + speedIncrement, maxSpeed);
        currentRotationSpeed = Mathf.Min(currentRotationSpeed + rotationSpeedIncrement, maxRotationSpeed);
        ApplySpeedToAllActive();
    }
    
    void ApplySpeedToAllActive()
    {
        foreach (var group in activeGroups)
        {
            foreach (var typeList in group.obstaclesByType.Values)
            {
                foreach (var obj in typeList)
                {
                    if (obj == null) continue;
                    
                    FlyingCircleController controller = obj.GetComponent<FlyingCircleController>();
                    if (controller != null)
                    {
                        controller.SetRotationSpeed(currentRotationSpeed);
                    }
                    else
                    {
                        ApplySpeedToObstacle(obj, currentSpeed);
                    }
                }
            }
        }
    }
    
    void ApplySpeedToObstacle(GameObject obj, float speed)
    {
        ObstacleSpawner spawner = obj.GetComponent<ObstacleSpawner>();
        if (spawner?.SpawnedObstacles == null) return;
        
        foreach (var spawnedObj in spawner.SpawnedObstacles)
        {
            if (spawnedObj == null) continue;
            ObstacleMovement m = spawnedObj.GetComponent<ObstacleMovement>();
            if (m != null) m.SetSpeed(speed);
        }
    }
    
    // ========== GIZMOS ==========
    
    void OnDrawGizmos()
    {
        if (!showGizmos || !Application.isPlaying || player == null) return;
        
        float playerY = player.position.y;
        
        Gizmos.color = Color.green;
        Gizmos.DrawLine(new Vector3(-15, playerY + spawnDistance, 0),
                       new Vector3(15, playerY + spawnDistance, 0));
        
        Gizmos.color = Color.red;
        Gizmos.DrawLine(new Vector3(-15, playerY + despawnDistance, 0),
                       new Vector3(15, playerY + despawnDistance, 0));
        
        foreach (var group in activeGroups)
        {
            Gizmos.color = group.isPassed ? Color.yellow : Color.cyan;
            Gizmos.DrawWireCube(new Vector3(0, group.centerY, 0),
                               new Vector3(10, CalculateGroupHeight(), 0.1f));
        }
    }
    
    // ========== PUBLIC ==========
    
    public void ResetGame()
    {
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
    }
}