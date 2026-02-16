using UnityEngine;
using System.Collections.Generic;
using System.Linq;

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
    [SerializeField] private float groupGap = 0.5f;
    
    [Header("=== SPAWN/DESPAWN ===")]
    [Tooltip("Khoảng cách sau vật cản cuối để spawn group mới")]
    [SerializeField] private float spawnTriggerDistance = 0f;
    
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
        public float startY;
        public float endY;
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
    private List<ObstacleGroup> allGroups = new List<ObstacleGroup>();
    private int nextGroupIndex = 0;
    private List<ObstacleGroup> activeGroups = new List<ObstacleGroup>();
    
    private float currentSpeed;
    private float currentRotationSpeed;
    private int totalGroupsPassed;
    private float nextSpawnY;
    private float screenHeight;
    private float lastObstacleEndY = 0f;
    
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
        CheckDespawnOldGroups();
        CheckGroupsPassed();
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
        
        if (mainCamera != null)
            screenHeight = mainCamera.orthographicSize * 2f;
        
        currentSpeed = initialSpeed;
        currentRotationSpeed = initialRotationSpeed;
        totalGroupsPassed = 0;
        
        if (player != null)
            nextSpawnY = player.position.y + screenHeight * 0.5f;
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
            }
            else
            {
                existingType.objects.Add(child.gameObject);
                SetupObstacle(child.gameObject, existingType.isCircle);
            }
        }
        
        obstacleTypes = obstacleTypes.OrderBy(t => t.priority).ToList();
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
            return;
        }
        
        int maxGroups = 0;
        foreach (var type in obstacleTypes)
        {
            int groupsForType = Mathf.CeilToInt((float)type.objects.Count / obstaclesPerType);
            maxGroups = Mathf.Max(maxGroups, groupsForType);
        }
        
        allGroups.Clear();
        
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
                allGroups.Add(group);
            }
        }
    }
    
    void InitializeFirstGroup()
    {
        if (allGroups.Count > 0)
        {
            SpawnGroup(nextSpawnY);
        }
    }
    
    void SpawnGroup(float yPosition)
    {
        if (allGroups.Count == 0)
        {
            return;
        }
        
        ObstacleGroup group = allGroups[nextGroupIndex];
        nextGroupIndex = (nextGroupIndex + 1) % allGroups.Count;
        
        group.startY = yPosition;
        float groupHeight = CalculateGroupHeight();
        group.endY = yPosition + groupHeight;
        group.isPassed = false;
        
        PositionGroup(group, yPosition);
        group.SetActive(true);
        
        activeGroups.Add(group);
        
        lastObstacleEndY = group.endY;
        nextSpawnY = group.endY + groupGap;
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
                    
                    if (isFireNLine)
                    {
                        MoveFireNLineToY(obj.transform, currentY);
                    }
                    else
                    {
                        ForceYPosition(obj.transform, currentY);
                    }
                }
            }
            else
            {
                for (int i = 0; i < objectsOfThisType.Count; i++)
                {
                    GameObject obj = objectsOfThisType[i];
                    if (obj == null) continue;
                    
                    if (isFireNLine)
                    {
                        MoveFireNLineToY(obj.transform, currentY);
                    }
                    else
                    {
                        ForceYPosition(obj.transform, currentY);
                    }
                    
                    currentY += obstacleSpacing;
                }
            }
            
            currentY += typeGap;
        }
    }
    
    void MoveFireNLineToY(Transform fireNLineParent, float targetY)
    {
        Vector3 pos = fireNLineParent.position;
        pos.y = targetY;
        fireNLineParent.position = pos;
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
    
    void CheckSpawnNewGroup()
    {
        if (player == null) return;
        
        float playerY = player.position.y;
        
        if (playerY + (screenHeight * 0.5f) > lastObstacleEndY)
        {
            SpawnGroup(nextSpawnY);
        }
    }
    
    void CheckDespawnOldGroups()
    {
        if (player == null) return;
        
        float playerY = player.position.y;
        
        while (activeGroups.Count > 2)
        {
            ObstacleGroup oldestGroup = activeGroups[0];
            
            if (playerY > oldestGroup.endY)
            {
                oldestGroup.SetActive(false);
                activeGroups.RemoveAt(0);
            }
            else
            {
                break;
            }
        }
    }
    
    void CheckGroupsPassed()
    {
        if (player == null) return;
        
        float playerY = player.position.y;
        
        foreach (var group in activeGroups)
        {
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
    
    // ===== THÊM METHOD MỚI ĐỂ MUSIC PLAYER SỬ DỤNG =====
    
    /// <summary>
    /// Kiểm tra xem có obstacle type nào đang active trong tầm nhìn camera không
    /// </summary>
    public bool HasActiveObstacleType(string obstacleTypeName, float detectionRange)
    {
        if (mainCamera == null || player == null) return false;
        
        float cameraY = player.position.y;
        float minY = cameraY - detectionRange;
        float maxY = cameraY + detectionRange;
        
        foreach (var group in activeGroups)
        {
            if (!group.isActive) continue;
            
            // Kiểm tra group có nằm trong tầm nhìn không
            if (group.endY < minY || group.startY > maxY) continue;
            
            // Kiểm tra group có chứa obstacle type này không
            foreach (var kvp in group.obstaclesByType)
            {
                string typeName = kvp.Key.ToLower();
                string searchName = obstacleTypeName.ToLower();
                
                // Kiểm tra tên có khớp không (hỗ trợ partial match)
                if (typeName.Contains(searchName) || searchName.Contains(typeName))
                {
                    // Kiểm tra ít nhất 1 obstacle trong list đang active
                    foreach (var obj in kvp.Value)
                    {
                        if (obj != null && obj.activeInHierarchy)
                        {
                            return true;
                        }
                    }
                }
            }
        }
        
        return false;
    }
    
    void OnDrawGizmos()
    {
        if (!showGizmos || !Application.isPlaying || player == null) return;
        
        float playerY = player.position.y;
        
        float spawnTriggerY = lastObstacleEndY;
        
        Gizmos.color = Color.green;
        Gizmos.DrawLine(new Vector3(-15, spawnTriggerY, 0),
                       new Vector3(15, spawnTriggerY, 0));
        
        #if UNITY_EDITOR
        UnityEditor.Handles.Label(new Vector3(0, spawnTriggerY, 0), 
            $"SPAWN TRIGGER\nY={spawnTriggerY:F1}\nGroups: {activeGroups.Count}/2");
        #endif
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(player.position, 0.5f);
        
        for (int i = 0; i < activeGroups.Count; i++)
        {
            var group = activeGroups[i];
            
            if (i == 0)
                Gizmos.color = new Color(1f, 0.5f, 0.5f);
            else
                Gizmos.color = Color.cyan;
            
            float groupHeight = group.endY - group.startY;
            float groupCenter = group.startY + groupHeight * 0.5f;
            
            Gizmos.DrawWireCube(new Vector3(0, groupCenter, 0),
                               new Vector3(10, groupHeight, 0.1f));
            
            #if UNITY_EDITOR
            string label = i == 0 ? "(OLD - Will despawn)" : "(CURRENT)";
            UnityEditor.Handles.Label(new Vector3(0, group.startY, 0), 
                $"{group.name} {label}\nStart: {group.startY:F1}\nEnd: {group.endY:F1}");
            #endif
        }
    }
    
    public void ResetGame()
    {
        foreach (var group in activeGroups)
        {
            group.SetActive(false);
        }
        
        activeGroups.Clear();
        nextGroupIndex = 0;
        totalGroupsPassed = 0;
        currentSpeed = initialSpeed;
        currentRotationSpeed = initialRotationSpeed;
        
        if (player != null)
        {
            nextSpawnY = player.position.y + screenHeight * 0.5f;
            lastObstacleEndY = 0f;
        }
        
        InitializeFirstGroup();
    }
}