using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class ImprovedObstacleSpawner : MonoBehaviour
{
    [Header("=== GROUP CONFIGURATION ===")]
    [SerializeField] private int obstaclesPerType = 4;
    
    [Header("=== SPACING SETTINGS ===")]
    [SerializeField] private float obstacleSpacing = 3f;
    [SerializeField] private float typeGap = 2f;
    [SerializeField] private float groupGap = 0.5f;
    
    [Header("=== SPAWN/DESPAWN ===")]
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

    [Header("=== SCORE SETTINGS ===")]
    [SerializeField] private int pointsPerObstacle = 2;
    [SerializeField] private float scoreDetectionOffsetY = 1f;

    [Header("=== SCREEN FIT ===")]
    [Tooltip("Aspect ratio của device bạn thiết kế obstacle ban đầu.")]
    [SerializeField] private float referenceAspectRatio = 0.462f;
    [Tooltip("Bật để tự scale obstacle theo màn hình")]
    [SerializeField] private bool fitObstaclesToScreen = true;
    [Tooltip("Tinh chỉnh scale thêm. 1 = theo aspect, giảm xuống 0.8~0.9 nếu thấy to quá")]
    [SerializeField] [Range(0.5f, 1.5f)] private float scaleMultiplier = 0.85f;

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
        public bool isFireNLine;
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
                foreach (var obj in typeList)
                    if (obj) obj.SetActive(active);
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

    // Lưu scale gốc của từng obstacle (trước khi bị nhân scaleFactor)
    private Dictionary<GameObject, Vector3> originalScales = new Dictionary<GameObject, Vector3>();

    private int groupSpawnCount = 0;
    
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
        if (mainCamera == null) mainCamera = Camera.main;
        
        if (player == null)
        {
            GhostController ghost = FindAnyObjectByType<GhostController>();
            if (ghost != null) player = ghost.transform;
        }
        
        if (mainCamera != null)
            screenHeight = mainCamera.orthographicSize * 2f;
        
        currentSpeed = initialSpeed;
        currentRotationSpeed = initialRotationSpeed;
        totalGroupsPassed = 0;
        
        if (player != null)
            nextSpawnY = player.position.y + screenHeight * 0.5f;
    }

    // ─────────────────────────────────────────────────────────────────────
    // SCREEN FIT: scale obstacle theo tỉ lệ aspect ratio
    // ─────────────────────────────────────────────────────────────────────
    void ScaleObstacleToScreen(GameObject obj)
    {
        if (!fitObstaclesToScreen || mainCamera == null) return;

        Vector3 baseScale;
        if (originalScales.ContainsKey(obj))
            baseScale = originalScales[obj];
        else
        {
            baseScale = obj.transform.localScale;
            originalScales[obj] = baseScale;
        }

        float scaleFactor = (mainCamera.aspect / referenceAspectRatio) * scaleMultiplier;

        obj.transform.localScale = new Vector3(
            baseScale.x * scaleFactor,
            baseScale.y * scaleFactor,
            baseScale.z
        );
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
                bool isFireLine = typeName.ToLower().Contains("fireline")
                               || typeName.ToLower().Contains("firenline");
                
                ObstacleType newType = new ObstacleType
                {
                    name = typeName,
                    isCircle = typeName.ToLower().Contains("circle"),
                    priority = GetPriority(typeName),
                    stackable = IsStackable(typeName),
                    isFireNLine = isFireLine
                };
                
                newType.objects.Add(child.gameObject);
                obstacleTypes.Add(newType);
                SetupObstacleInitial(child.gameObject, newType);
            }
            else
            {
                existingType.objects.Add(child.gameObject);
                SetupObstacleInitial(child.gameObject, existingType);
            }
        }
        
        obstacleTypes = obstacleTypes.OrderBy(t => t.priority).ToList();
    }
    
    bool IsStackable(string typeName)
    {
        string lower = typeName.ToLower();
        return lower.Contains("circle") || lower.Contains("flyingfire") || lower.Contains("lightning");
    }
    
    int GetPriority(string typeName)
    {
        string lower = typeName.ToLower();
        if (lower.Contains("fireline") || lower.Contains("firenline")) return 0;
        if (lower.Contains("circle")) return 10;
        if (lower.Contains("lightning")) return 30;
        if (lower.Contains("flyingfire")) return 100;
        return 50;
    }
    
    // ─────────────────────────────────────────────────────────────────────
    // SETUP LẦN ĐẦU: gắn component + Configure + Scale, KHÔNG Activate
    // ─────────────────────────────────────────────────────────────────────
    void SetupObstacleInitial(GameObject obj, ObstacleType type)
    {
        if (type.isFireNLine)
        {
            ObstacleScoreDetector parentDetector = obj.GetComponent<ObstacleScoreDetector>();
            if (parentDetector == null)
                parentDetector = obj.AddComponent<ObstacleScoreDetector>();
            parentDetector.Configure(0, 0, true);

            foreach (Transform child in obj.transform)
            {
                ObstacleScoreDetector childDetector = child.GetComponent<ObstacleScoreDetector>();
                if (childDetector == null)
                    childDetector = child.gameObject.AddComponent<ObstacleScoreDetector>();
                childDetector.Configure(pointsPerObstacle, scoreDetectionOffsetY, false);
            }

            if (showDebugInfo)
                Debug.Log($"[Setup] FireNLine '{obj.name}' → {obj.transform.childCount} child detectors");
        }
        else
        {
            ObstacleScoreDetector detector = obj.GetComponent<ObstacleScoreDetector>();
            if (detector == null)
                detector = obj.AddComponent<ObstacleScoreDetector>();
            detector.Configure(pointsPerObstacle, scoreDetectionOffsetY, false);
        }

        if (type.isCircle)
        {
            FlyingCircleController controller = obj.GetComponent<FlyingCircleController>();
            if (controller != null)
                controller.SetRotationSpeed(currentRotationSpeed);
        }
        else
        {
            ObstacleLooper looper = obj.GetComponent<ObstacleLooper>();
            if (looper != null) looper.enabled = false;
            
            ObstacleSpawner spawner = obj.GetComponent<ObstacleSpawner>();
            if (spawner != null)
                StartCoroutine(ApplySpeedToSpawnedObstacles(obj, currentSpeed));
        }

        // Scale theo màn hình — gọi TRƯỚC SetActive(false) để lưu scale gốc đúng
        ScaleObstacleToScreen(obj);
        
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
                if (movement != null) movement.SetSpeed(speed);
            }
        }
    }
    
    void CreateGroupsFromObstacles()
    {
        if (obstacleTypes.Count == 0) return;
        
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
                    objectsForThisGroup.Add(type.objects[i]);
                
                if (objectsForThisGroup.Count > 0)
                    group.obstaclesByType[type.name] = objectsForThisGroup;
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
            SpawnGroup(nextSpawnY);
    }
    
    void SpawnGroup(float yPosition)
    {
        if (allGroups.Count == 0) return;
        
        ObstacleGroup group = allGroups[nextGroupIndex];
        nextGroupIndex = (nextGroupIndex + 1) % allGroups.Count;
        
        group.startY = yPosition;
        float groupHeight = CalculateGroupHeight();
        group.endY = yPosition + groupHeight;
        group.isPassed = false;

        bool randomizeOrder = groupSpawnCount >= 1;
        groupSpawnCount++;
        
        PositionGroup(group, yPosition, randomizeOrder);
        group.SetActive(true);
        ActivateDetectorsInGroup(group);
        
        activeGroups.Add(group);
        lastObstacleEndY = group.endY;
        nextSpawnY = group.endY + groupGap;
    }

    void ActivateDetectorsInGroup(ObstacleGroup group)
    {
        foreach (var typeList in group.obstaclesByType.Values)
        {
            foreach (var obj in typeList)
            {
                if (obj == null) continue;

                ObstacleType type = obstacleTypes.Find(t =>
                    group.obstaclesByType.ContainsKey(t.name) &&
                    group.obstaclesByType[t.name].Contains(obj));

                if (type != null && type.isFireNLine)
                {
                    foreach (Transform child in obj.transform)
                    {
                        ObstacleScoreDetector d = child.GetComponent<ObstacleScoreDetector>();
                        if (d != null) d.Activate();
                    }
                }
                else
                {
                    ObstacleScoreDetector d = obj.GetComponent<ObstacleScoreDetector>();
                    if (d != null) d.Activate();
                }
            }
        }
    }
    
    List<T> ShuffleList<T>(List<T> list)
    {
        List<T> shuffled = new List<T>(list);
        for (int i = shuffled.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            T temp = shuffled[i];
            shuffled[i] = shuffled[j];
            shuffled[j] = temp;
        }
        return shuffled;
    }

    void PositionGroup(ObstacleGroup group, float startY, bool randomizeOrder = false)
    {
        float currentY = startY;

        List<ObstacleType> typesInGroup = obstacleTypes
            .Where(t => group.obstaclesByType.ContainsKey(t.name))
            .ToList();

        if (randomizeOrder)
            typesInGroup = ShuffleList(typesInGroup);

        if (showDebugInfo)
        {
            string order = string.Join(" → ", typesInGroup.Select(t => t.name));
            Debug.Log($"[PositionGroup] {group.name} | randomized={randomizeOrder} | order: {order}");
        }

        foreach (var type in typesInGroup)
        {
            List<GameObject> objectsOfThisType = group.obstaclesByType[type.name];
            
            if (type.stackable)
            {
                foreach (var obj in objectsOfThisType)
                {
                    if (obj == null) continue;
                    if (type.isFireNLine) MoveFireNLineToY(obj.transform, currentY);
                    else ForceYPosition(obj.transform, currentY);
                }
            }
            else
            {
                for (int i = 0; i < objectsOfThisType.Count; i++)
                {
                    GameObject obj = objectsOfThisType[i];
                    if (obj == null) continue;
                    if (type.isFireNLine) MoveFireNLineToY(obj.transform, currentY);
                    else ForceYPosition(obj.transform, currentY);
                    currentY += obstacleSpacing;
                }
            }
            
            currentY += typeGap;
        }
    }
    
    void MoveFireNLineToY(Transform t, float targetY)
    {
        Vector3 pos = t.position;
        pos.y = targetY;
        t.position = pos;
    }
    
    void ForceYPosition(Transform obj, float targetY)
    {
        Vector3 pos = obj.position;
        pos.y = targetY;
        obj.position = pos;
        if (obj.childCount > 0)
            ForceYPositionRecursive(obj, targetY);
    }

    void ForceYPositionRecursive(Transform parent, float targetY)
    {
        foreach (Transform child in parent)
        {
            Vector3 childPos = child.position;
            childPos.y = targetY;
            child.position = childPos;
            if (child.childCount > 0)
                ForceYPositionRecursive(child, targetY);
        }
    }
    
    float CalculateGroupHeight()
    {
        float height = 0;
        foreach (var type in obstacleTypes)
        {
            if (!type.stackable)
                height += (obstaclesPerType - 1) * obstacleSpacing;
            height += typeGap;
        }
        if (obstacleTypes.Count > 0) height -= typeGap;
        return height;
    }
    
    void CheckSpawnNewGroup()
    {
        if (player.position.y + (screenHeight * 0.5f) > lastObstacleEndY)
            SpawnGroup(nextSpawnY);
    }
    
    void CheckDespawnOldGroups()
    {
        float playerY = player.position.y;
        while (activeGroups.Count > 2)
        {
            ObstacleGroup oldest = activeGroups[0];
            if (playerY > oldest.endY)
            {
                oldest.SetActive(false);
                activeGroups.RemoveAt(0);
            }
            else break;
        }
    }
    
    void CheckGroupsPassed()
    {
        float playerY = player.position.y;
        foreach (var group in activeGroups)
        {
            float mid = group.startY + (group.endY - group.startY) * 0.5f;
            if (!group.isPassed && playerY > mid)
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
        foreach (var group in activeGroups)
        {
            foreach (var typeList in group.obstaclesByType.Values)
            {
                foreach (var obj in typeList)
                {
                    if (obj == null) continue;
                    FlyingCircleController fc = obj.GetComponent<FlyingCircleController>();
                    if (fc != null) fc.SetRotationSpeed(currentRotationSpeed);
                    else ApplySpeedToObstacle(obj, currentSpeed);
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
    
    public bool HasActiveObstacleType(string obstacleTypeName, float detectionRange)
    {
        if (mainCamera == null || player == null) return false;
        float cameraY = player.position.y;
        float minY = cameraY - detectionRange;
        float maxY = cameraY + detectionRange;
        
        foreach (var group in activeGroups)
        {
            if (!group.isActive || group.endY < minY || group.startY > maxY) continue;
            foreach (var kvp in group.obstaclesByType)
            {
                if (kvp.Key.ToLower().Contains(obstacleTypeName.ToLower()))
                    foreach (var obj in kvp.Value)
                        if (obj != null && obj.activeInHierarchy) return true;
            }
        }
        return false;
    }
    
    void OnDrawGizmos()
    {
        if (!showGizmos || !Application.isPlaying || player == null) return;
        
        Gizmos.color = Color.green;
        Gizmos.DrawLine(new Vector3(-15, lastObstacleEndY, 0), new Vector3(15, lastObstacleEndY, 0));
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(player.position, 0.5f);
        
        for (int i = 0; i < activeGroups.Count; i++)
        {
            var group = activeGroups[i];
            Gizmos.color = i == 0 ? new Color(1f, 0.5f, 0.5f) : Color.cyan;
            float groupCenter = group.startY + (group.endY - group.startY) * 0.5f;
            Gizmos.DrawWireCube(new Vector3(0, groupCenter, 0), new Vector3(10, group.endY - group.startY, 0.1f));
            
            #if UNITY_EDITOR
            UnityEditor.Handles.Label(new Vector3(0, group.startY, 0),
                $"{group.name}\nStart:{group.startY:F1} End:{group.endY:F1}");
            #endif
        }
    }
    
    public void ResetGame()
    {
        foreach (var group in activeGroups)
            group.SetActive(false);
        activeGroups.Clear();
        nextGroupIndex = 0;
        totalGroupsPassed = 0;
        currentSpeed = initialSpeed;
        currentRotationSpeed = initialRotationSpeed;
        groupSpawnCount = 0;
        if (player != null)
        {
            nextSpawnY = player.position.y + screenHeight * 0.5f;
            lastObstacleEndY = 0f;
        }
        InitializeFirstGroup();
    }
}