using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// IMPROVED Obstacle Spawner V4 - INFINITE LOOP + SPAWN FIX
/// ✅ Spawn khi Ghost qua vật cản cuối + 4 đơn vị
/// ✅ Loop vô hạn các groups
/// ✅ Hỗ trợ chồng vật cản (stackable)
/// </summary>
public class ImprovedObstacleSpawner : MonoBehaviour
{
    [Header("=== GROUP CONFIGURATION ===")]
    [Tooltip("Số obstacles mỗi loại trong 1 group")]
    [SerializeField] private int obstaclesPerType = 4;
    
    [Header("=== SPACING SETTINGS ===")]
    [Tooltip("Khoảng cách giữa các obstacles cùng loại (nếu KHÔNG stackable)")]
    [SerializeField] private float obstacleSpacing = 3f;
    
    [Tooltip("Khoảng cách giữa các loại vật cản khác nhau")]
    [SerializeField] private float typeGap = 2f;
    
    [Tooltip("Khoảng cách giữa các groups")]
    [SerializeField] private float groupGap = 2f;
    
    [Header("=== SPAWN/DESPAWN ===")]
    [Tooltip("Khoảng cách sau vật cản cuối để spawn group mới")]
    [SerializeField] private float spawnTriggerDistance = 4f;
    
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
        public float startY;      // ✅ Vị trí Y của vật cản ĐẦU TIÊN
        public float endY;        // ✅ Vị trí Y của vật cản CUỐI CÙNG
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
    private List<ObstacleGroup> allGroups = new List<ObstacleGroup>(); // ✅ Lưu TẤT CẢ groups để loop
    private int nextGroupIndex = 0; // ✅ Index của group tiếp theo cần spawn
    private List<ObstacleGroup> activeGroups = new List<ObstacleGroup>();
    
    private float currentSpeed;
    private float currentRotationSpeed;
    private int totalGroupsPassed;
    private float nextSpawnY;
    private float screenHeight;
    private float lastObstacleEndY = 0f; // ✅ Vị trí Y cuối cùng của vật cản cuối
    
    // ========== UNITY LIFECYCLE ==========
    
    void Start()
    {
        InitializeReferences();
        AutoDetectObstacleTypes();
        CreateGroupsFromObstacles();
        InitializeFirstGroup();
    }
    
    void Update()
    {
        if (player == null) return;
        
        CheckSpawnNewGroup();
        // ✅ KHÔNG XÓA GROUPS - chỉ spawn thêm
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
        if (lower.Contains("lightning")) return true;
        return false;
    }
    
    int GetPriority(string typeName)
    {
        string lower = typeName.ToLower();
        
        if (lower.Contains("fireline") || lower.Contains("firenline"))
            return 0;
        else if (lower.Contains("circle"))
            return 10;
        else if (lower.Contains("lightning"))
            return 30;
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
        
        // ✅ Disable tất cả obstacles ban đầu
        obj.SetActive(false);
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
        
        allGroups.Clear(); // ✅ Lưu vào allGroups thay vì queue
        
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
                allGroups.Add(group); // ✅ Thêm vào list thay vì queue
            }
        }
        
        Debug.Log($"[ImprovedSpawner] 🎯 Created {allGroups.Count} groups for infinite loop");
    }
    
    void InitializeFirstGroup()
    {
        if (allGroups.Count > 0)
        {
            SpawnGroup(nextSpawnY);
        }
    }
    
    // ========== SPAWNING ==========
    
    void SpawnGroup(float yPosition)
    {
        if (allGroups.Count == 0)
        {
            Debug.LogWarning("[ImprovedSpawner] No groups available!");
            return;
        }
        
        // ✅ Lấy group theo vòng lặp (loop infinitely)
        ObstacleGroup group = allGroups[nextGroupIndex];
        nextGroupIndex = (nextGroupIndex + 1) % allGroups.Count; // ✅ Wrap around
        
        // ✅ Tính startY và endY
        group.startY = yPosition;
        float groupHeight = CalculateGroupHeight();
        group.endY = yPosition + groupHeight;
        group.isPassed = false;
        
        PositionGroup(group, yPosition);
        group.SetActive(true);
        
        activeGroups.Add(group);
        
        // ✅ Cập nhật vị trí vật cản cuối
        lastObstacleEndY = group.endY;
        nextSpawnY = group.endY + groupGap; // ✅ Update nextSpawnY
        
        if (showDebugInfo)
            Debug.Log($"[ImprovedSpawner] 📍 Spawned {group.name} | Start Y={group.startY:F1} | End Y={group.endY:F1}");
    }
    
    void PositionGroup(ObstacleGroup group, float startY)
    {
        float currentY = startY;
        
        foreach (var type in obstacleTypes)
        {
            if (!group.obstaclesByType.ContainsKey(type.name))
                continue;
            
            List<GameObject> objectsOfThisType = group.obstaclesByType[type.name];
            
            bool isFireNLine = type.name.ToLower().Contains("fireline") || type.name.ToLower().Contains("firenline");
            
            if (type.stackable)
            {
                foreach (var obj in objectsOfThisType)
                {
                    if (obj == null) continue;
                    
                    if (!isFireNLine)
                    {
                        ForceYPosition(obj.transform, currentY);
                    }
                }
                
                if (showDebugInfo)
                    Debug.Log($"  🔗 Stacked {objectsOfThisType.Count}x {type.name} at Y={currentY:F1}");
            }
            else
            {
                for (int i = 0; i < objectsOfThisType.Count; i++)
                {
                    GameObject obj = objectsOfThisType[i];
                    if (obj == null) continue;
                    
                    if (!isFireNLine)
                    {
                        ForceYPosition(obj.transform, currentY);
                    }
                    
                    currentY += obstacleSpacing;
                }
            }
            
            currentY += typeGap;
        }
    }
    
    void ForceYPosition(Transform obj, float targetY)
    {
        Vector3 pos = obj.position;
        pos.y = targetY;
        obj.position = pos;
        
        if (obj.childCount > 0)
        {
            ForceYPositionRecursive(obj, targetY);
        }
    }

    void ForceYPositionRecursive(Transform parent, float targetY)
    {
        foreach (Transform child in parent)
        {
            Vector3 childPos = child.position;
            childPos.y = targetY;
            child.position = childPos;
            
            if (child.childCount > 0)
            {
                ForceYPositionRecursive(child, targetY);
            }
        }
    }
    
    float CalculateGroupHeight()
    {
        float height = 0;
        
        foreach (var type in obstacleTypes)
        {
            if (type.stackable)
            {
                // Stackable: không cộng gì
            }
            else
            {
                height += (obstaclesPerType - 1) * obstacleSpacing;
            }
            
            height += typeGap;
        }
        
        if (obstacleTypes.Count > 0)
            height -= typeGap;
        
        return height;
    }
    
    // ========== UPDATE ==========
    
    // ✅ Spawn khi Ghost bay qua vật cản cuối + spawnTriggerDistance (4 đơn vị)
    void CheckSpawnNewGroup()
    {
        if (player == null) return;
        
        float playerY = player.position.y;
        
        // ✅ Kiểm tra xem Ghost đã qua vật cản cuối + 4 đơn vị chưa
        if (playerY > lastObstacleEndY + spawnTriggerDistance)
        {
            SpawnGroup(nextSpawnY);
            
            if (showDebugInfo)
                Debug.Log($"[ImprovedSpawner] ✨ Ghost passed last obstacle + {spawnTriggerDistance} | Ghost Y={playerY:F1} | Last obstacle end={lastObstacleEndY:F1}");
        }
    }
    
    // ✅ KHÔNG CẦN DESPAWN - Giữ tất cả groups active
    /*
    void CheckDespawnOldGroups()
    {
        if (player == null) return;
        
        float playerY = player.position.y;
        
        // Despawn groups cũ khi Ghost đã qua xa (để tránh lag)
        for (int i = activeGroups.Count - 1; i >= 0; i--)
        {
            if (playerY > activeGroups[i].endY + despawnDistance)
            {
                if (showDebugInfo)
                    Debug.Log($"[ImprovedSpawner] 🗑️ Despawned {activeGroups[i].name}");
                
                activeGroups[i].SetActive(false);
                activeGroups.RemoveAt(i);
            }
        }
    }
    */

    
    void CheckGroupsPassed()
    {
        if (player == null) return;
        
        float playerY = player.position.y;
        
        foreach (var group in activeGroups)
        {
            // Đánh dấu passed khi qua NỬA chiều cao group
            float groupMidY = group.startY + (group.endY - group.startY) * 0.5f;
            
            if (!group.isPassed && playerY > groupMidY)
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
        
        if (showDebugInfo)
            Debug.Log($"[ImprovedSpawner] ⚡ Speed increased | Speed: {currentSpeed:F1} | Rotation: {currentRotationSpeed:F0}");
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
        
        // ✅ Vẽ spawn trigger line (vật cản cuối + 4 đơn vị)
        float spawnTriggerY = lastObstacleEndY + spawnTriggerDistance;
        
        Gizmos.color = Color.green;
        Gizmos.DrawLine(new Vector3(-15, spawnTriggerY, 0),
                       new Vector3(15, spawnTriggerY, 0));
        
        #if UNITY_EDITOR
        UnityEditor.Handles.Label(new Vector3(0, spawnTriggerY, 0), 
            $"SPAWN TRIGGER\nLast obstacle end: {lastObstacleEndY:F1}\nTrigger: {spawnTriggerY:F1}");
        #endif
        
        // ✅ Vẽ vị trí Ghost
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(player.position, 0.5f);
        
        // ✅ Vẽ groups
        foreach (var group in activeGroups)
        {
            Gizmos.color = group.isPassed ? Color.yellow : Color.cyan;
            
            float groupHeight = group.endY - group.startY;
            float groupCenter = group.startY + groupHeight * 0.5f;
            
            Gizmos.DrawWireCube(new Vector3(0, groupCenter, 0),
                               new Vector3(10, groupHeight, 0.1f));
            
            #if UNITY_EDITOR
            UnityEditor.Handles.Label(new Vector3(0, group.startY, 0), 
                $"{group.name}\nStart: {group.startY:F1}\nEnd: {group.endY:F1}");
            #endif
        }
    }
    
    // ========== PUBLIC ==========
    
    public void ResetGame()
    {
        foreach (var group in activeGroups)
        {
            group.SetActive(false);
        }
        
        activeGroups.Clear();
        nextGroupIndex = 0; // ✅ Reset index
        totalGroupsPassed = 0;
        currentSpeed = initialSpeed;
        currentRotationSpeed = initialRotationSpeed;
        
        if (player != null)
        {
            nextSpawnY = player.position.y + screenHeight * 0.5f;
            lastObstacleEndY = 0f;
        }
        
        InitializeFirstGroup();
        
        Debug.Log("[ImprovedSpawner] 🔄 Game Reset");
    }
}