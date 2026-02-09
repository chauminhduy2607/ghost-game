using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Tag-Based Obstacle Spawner - Quản lý hoàn toàn theo Tag
/// - Tag kết thúc bằng "Obstacle" sẽ được tự động phát hiện
/// - Các obstacle cùng tag sẽ được đặt cùng vị trí (stack)
/// - Các tag khác nhau sẽ cách nhau một khoảng typeGap
/// </summary>
public class TagBasedObstacleSpawner : MonoBehaviour
{
    [Header("=== GROUP CONFIGURATION ===")]
    [Tooltip("Số lượng obstacles mỗi tag trong 1 group")]
    [SerializeField] private int obstaclesPerTag = 4;
    
    [Header("=== SPACING SETTINGS ===")]
    [Tooltip("Khoảng cách giữa các tag khác nhau")]
    [SerializeField] private float tagGap = 12f;
    
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
    
    [Header("=== ROTATION (for rotating obstacles) ===")]
    [SerializeField] private float initialRotationSpeed = 120f;
    [SerializeField] private float rotationSpeedIncrement = 20f;
    [SerializeField] private float maxRotationSpeed = 360f;
    
    [Header("=== REFERENCES ===")]
    [SerializeField] private Transform player;
    [SerializeField] private Camera mainCamera;
    
    [Header("=== DEBUG ===")]
    [SerializeField] private bool showDebugInfo = true;
    [SerializeField] private bool showGizmos = true;
    
    // ========== PRIVATE CLASSES ==========
    
    /// <summary>
    /// Lưu trữ thông tin về một loại obstacle (theo tag)
    /// </summary>
    private class ObstacleTag
    {
        public string tagName;                          // Tên tag đầy đủ (VD: "FireLineObstacle")
        public List<GameObject> objects = new List<GameObject>();  // Danh sách các objects có tag này
        
        public ObstacleTag(string tag)
        {
            tagName = tag;
        }
    }
    
    /// <summary>
    /// Một group chứa nhiều obstacles từ các tag khác nhau
    /// </summary>
    private class ObstacleGroup
    {
        public string groupName;
        public Dictionary<string, List<GameObject>> obstaclesByTag = new Dictionary<string, List<GameObject>>();
        public float centerY;           // Vị trí Y trung tâm của group
        public bool isActive;
        public bool isPassed;           // Đã vượt qua group này chưa
        
        /// <summary>
        /// Bật/tắt tất cả obstacles trong group
        /// </summary>
        public void SetActive(bool active)
        {
            isActive = active;
            foreach (var tagList in obstaclesByTag.Values)
            {
                foreach (var obj in tagList)
                {
                    if (obj != null)
                        obj.SetActive(active);
                }
            }
        }
        
        /// <summary>
        /// Tổng số obstacles trong group
        /// </summary>
        public int TotalCount
        {
            get
            {
                int count = 0;
                foreach (var list in obstaclesByTag.Values)
                    count += list.Count;
                return count;
            }
        }
    }
    
    // ========== PRIVATE VARIABLES ==========
    
    private List<ObstacleTag> obstacleTags = new List<ObstacleTag>();
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
        DetectObstaclesByTag();
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
    
    /// <summary>
    /// Khởi tạo references và các giá trị ban đầu
    /// </summary>
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
        
        Debug.Log($"[TagBasedSpawner] ✅ Initialized - Speed: {currentSpeed}, Rotation: {currentRotationSpeed}");
    }
    
    /// <summary>
    /// Xác định thứ tự ưu tiên của tag dựa trên SỐ trong tên tag
    /// VD: "FireNLine1Obstacle" → priority = 1
    ///     "FlyingCircle2Obstacle" → priority = 2
    ///     "Lightning3Obstacle" → priority = 3
    ///     "SomethingObstacle" (không có số) → priority = 99
    /// 
    /// Quy tắc đặt tag: [TênVậtCản][SốThứTự]Obstacle
    /// </summary>
    int GetTagPriority(string tagName)
    {
        // Tìm chữ số trong tên tag
        for (int i = 0; i < tagName.Length; i++)
        {
            if (char.IsDigit(tagName[i]))
            {
                // Lấy toàn bộ số liên tiếp
                string numberStr = "";
                for (int j = i; j < tagName.Length && char.IsDigit(tagName[j]); j++)
                {
                    numberStr += tagName[j];
                }
                
                if (int.TryParse(numberStr, out int priority))
                {
                    return priority;
                }
            }
        }
        
        // Nếu không có số trong tag → priority mặc định = 99 (xuất hiện cuối)
        return 99;
    }
    
    /// <summary>
    /// Tự động phát hiện tất cả obstacles theo tag
    /// Tag phải kết thúc bằng "Obstacle"
    /// </summary>
    void DetectObstaclesByTag()
    {
        obstacleTags.Clear();
        
        // Duyệt qua tất cả child objects
        foreach (Transform child in transform)
        {
            if (child == null) continue;
            
            string tag = child.tag;
            
            // Bỏ qua nếu không phải obstacle tag
            if (!tag.EndsWith("Obstacle"))
                continue;
            
            // Tìm xem tag này đã tồn tại chưa
            ObstacleTag existingTag = obstacleTags.Find(t => t.tagName == tag);
            
            if (existingTag == null)
            {
                // Tạo mới ObstacleTag
                ObstacleTag newTag = new ObstacleTag(tag);
                newTag.objects.Add(child.gameObject);
                obstacleTags.Add(newTag);
                
                if (showDebugInfo)
                    Debug.Log($"[TagBasedSpawner] 🆕 Detected new tag: {tag}");
            }
            else
            {
                // Thêm vào tag đã có
                existingTag.objects.Add(child.gameObject);
            }
            
            // Setup obstacle
            SetupObstacle(child.gameObject);
        }
        
        // ✅ SẮP XẾP THEO THỨ TỰ ƯU TIÊN
        obstacleTags = obstacleTags.OrderBy(t => GetTagPriority(t.tagName)).ToList();
        
        if (showDebugInfo)
        {
            Debug.Log($"[TagBasedSpawner] 📊 Total tags detected: {obstacleTags.Count}");
            foreach (var tag in obstacleTags)
            {
                Debug.Log($"  • {tag.tagName}: {tag.objects.Count} objects (Priority: {GetTagPriority(tag.tagName)})");
            }
        }
    }
    
    /// <summary>
    /// Setup ban đầu cho obstacle (tắt các component không cần thiết)
    /// </summary>
    void SetupObstacle(GameObject obj)
    {
        // Tắt ObstacleLooper nếu có (vì không dùng loop nữa)
        ObstacleLooper looper = obj.GetComponent<ObstacleLooper>();
        if (looper != null)
        {
            looper.enabled = false;
        }
        
        // Setup rotation speed cho FlyingCircleController
        FlyingCircleController circleController = obj.GetComponent<FlyingCircleController>();
        if (circleController != null)
        {
            circleController.SetRotationSpeed(currentRotationSpeed);
        }
        
        // Setup speed cho ObstacleSpawner (nếu có spawned children)
        ObstacleSpawner spawner = obj.GetComponent<ObstacleSpawner>();
        if (spawner != null)
        {
            StartCoroutine(ApplySpeedToSpawnedObstacles(spawner, currentSpeed));
        }
    }
    
    /// <summary>
    /// Apply speed cho các obstacles được spawn bởi ObstacleSpawner
    /// </summary>
    System.Collections.IEnumerator ApplySpeedToSpawnedObstacles(ObstacleSpawner spawner, float speed)
    {
        yield return new WaitForEndOfFrame();
        
        if (spawner != null && spawner.SpawnedObstacles != null)
        {
            foreach (GameObject spawnedObj in spawner.SpawnedObstacles)
            {
                if (spawnedObj == null) continue;
                
                ObstacleMovement movement = spawnedObj.GetComponent<ObstacleMovement>();
                if (movement != null)
                    movement.SetSpeed(speed);
            }
        }
    }
    
    /// <summary>
    /// Tạo các groups từ danh sách obstacles đã detect
    /// </summary>
    void CreateGroupsFromObstacles()
    {
        if (obstacleTags.Count == 0)
        {
            Debug.LogError("[TagBasedSpawner] ❌ No obstacle tags detected!");
            return;
        }
        
        // Tính số groups cần tạo (dựa trên tag có nhiều objects nhất)
        int maxGroups = 0;
        foreach (var tag in obstacleTags)
        {
            int groupsForThisTag = Mathf.CeilToInt((float)tag.objects.Count / obstaclesPerTag);
            maxGroups = Mathf.Max(maxGroups, groupsForThisTag);
        }
        
        // Tạo từng group
        for (int groupIndex = 0; groupIndex < maxGroups; groupIndex++)
        {
            ObstacleGroup group = new ObstacleGroup();
            group.groupName = $"Group_{groupIndex}";
            
            // Với mỗi tag, lấy một số lượng obstacles nhất định cho group này
            foreach (var tag in obstacleTags)
            {
                List<GameObject> objectsForThisGroup = new List<GameObject>();
                
                int startIndex = groupIndex * obstaclesPerTag;
                int endIndex = Mathf.Min(startIndex + obstaclesPerTag, tag.objects.Count);
                
                for (int i = startIndex; i < endIndex; i++)
                {
                    objectsForThisGroup.Add(tag.objects[i]);
                }
                
                if (objectsForThisGroup.Count > 0)
                {
                    group.obstaclesByTag[tag.tagName] = objectsForThisGroup;
                }
            }
            
            // Thêm group vào queue nếu có obstacles
            if (group.TotalCount > 0)
            {
                group.SetActive(false);
                inactiveGroups.Enqueue(group);
            }
        }
        
        Debug.Log($"[TagBasedSpawner] 🎯 Created {maxGroups} groups from {obstacleTags.Count} tags");
    }
    
    /// <summary>
    /// Spawn các groups đầu tiên
    /// </summary>
    void InitializeFirstGroups()
    {
        float currentY = nextSpawnY;
        
        int groupsToSpawn = Mathf.Min(maxActiveGroups, inactiveGroups.Count);
        
        for (int i = 0; i < groupsToSpawn; i++)
        {
            SpawnGroup(currentY);
            currentY += CalculateGroupHeight() + groupGap;
        }
        
        Debug.Log($"[TagBasedSpawner] 🚀 Spawned {groupsToSpawn} initial groups");
    }
    
    // ========== SPAWNING & POSITIONING ==========
    
    /// <summary>
    /// Spawn một group tại vị trí Y
    /// </summary>
    void SpawnGroup(float yPosition)
    {
        if (inactiveGroups.Count == 0)
        {
            Debug.LogWarning("[TagBasedSpawner] ⚠️ No inactive groups available!");
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
            Debug.Log($"[TagBasedSpawner] 📍 Spawned {group.groupName} at Y={yPosition:F1}");
    }
    
    /// <summary>
    /// Đặt vị trí cho tất cả obstacles trong group
    /// - Obstacles cùng tag sẽ cùng vị trí Y (stack)
    /// - Các tag khác nhau cách nhau tagGap
    /// </summary>
    void PositionGroup(ObstacleGroup group, float startY)
    {
        float currentY = startY;
        
        foreach (var tag in obstacleTags)
        {
            // Bỏ qua nếu group này không có obstacles của tag này
            if (!group.obstaclesByTag.ContainsKey(tag.tagName))
                continue;
            
            List<GameObject> objectsOfThisTag = group.obstaclesByTag[tag.tagName];
            
            // Đặt TẤT CẢ obstacles cùng tag ở CÙNG VỊ TRÍ Y
            foreach (var obj in objectsOfThisTag)
            {
                if (obj == null) continue;
                
                Vector3 pos = obj.transform.position;
                pos.y = currentY;
                obj.transform.position = pos;
            }
            
            if (showDebugInfo)
                Debug.Log($"  🔗 Stacked {objectsOfThisTag.Count}x {tag.tagName} at Y={currentY:F1}");
            
            // Chuyển sang tag tiếp theo
            currentY += tagGap;
        }
    }
    
    /// <summary>
    /// Tính chiều cao của một group
    /// </summary>
    float CalculateGroupHeight()
    {
        // Chiều cao = (số tag - 1) * tagGap
        // Vì các obstacles cùng tag được stack, chỉ cần tính khoảng cách giữa các tag
        int numberOfTags = obstacleTags.Count;
        
        if (numberOfTags <= 1)
            return 0;
        
        return (numberOfTags - 1) * tagGap;
    }
    
    // ========== UPDATE LOGIC ==========
    
    /// <summary>
    /// Kiểm tra và spawn group mới khi cần
    /// </summary>
    void CheckSpawnNewGroup()
    {
        if (player == null) return;
        
        if (nextSpawnY < player.position.y + spawnDistance && inactiveGroups.Count > 0)
        {
            SpawnGroup(nextSpawnY);
        }
    }
    
    /// <summary>
    /// Kiểm tra và despawn các group cũ
    /// </summary>
    void CheckDespawnOldGroups()
    {
        if (player == null) return;
        
        for (int i = activeGroups.Count - 1; i >= 0; i--)
        {
            if (activeGroups[i].centerY < player.position.y + despawnDistance)
            {
                ObstacleGroup group = activeGroups[i];
                group.SetActive(false);
                inactiveGroups.Enqueue(group);
                activeGroups.RemoveAt(i);
                
                if (showDebugInfo)
                    Debug.Log($"[TagBasedSpawner] 🗑️ Despawned {group.groupName}");
            }
        }
    }
    
    /// <summary>
    /// Kiểm tra xem player đã vượt qua group nào chưa
    /// </summary>
    void CheckGroupsPassed()
    {
        if (player == null) return;
        
        foreach (var group in activeGroups)
        {
            if (!group.isPassed && player.position.y > group.centerY + CalculateGroupHeight() * 0.5f)
            {
                group.isPassed = true;
                totalGroupsPassed++;
                
                if (showDebugInfo)
                    Debug.Log($"[TagBasedSpawner] ✅ Passed {group.groupName} (Total: {totalGroupsPassed})");
                
                // Tăng tốc sau mỗi X groups
                if (totalGroupsPassed % groupsPerSpeedIncrease == 0)
                {
                    IncreaseSpeed();
                }
            }
        }
    }
    
    // ========== SPEED MANAGEMENT ==========
    
    /// <summary>
    /// Tăng tốc độ và rotation speed
    /// </summary>
    void IncreaseSpeed()
    {
        float oldSpeed = currentSpeed;
        float oldRotation = currentRotationSpeed;
        
        currentSpeed = Mathf.Min(currentSpeed + speedIncrement, maxSpeed);
        currentRotationSpeed = Mathf.Min(currentRotationSpeed + rotationSpeedIncrement, maxRotationSpeed);
        
        if (showDebugInfo)
            Debug.Log($"[TagBasedSpawner] ⚡ Speed: {oldSpeed:F1} → {currentSpeed:F1} | Rotation: {oldRotation:F0} → {currentRotationSpeed:F0}");
        
        ApplySpeedToAllActive();
    }
    
    /// <summary>
    /// Apply speed mới cho tất cả active obstacles
    /// </summary>
    void ApplySpeedToAllActive()
    {
        foreach (var group in activeGroups)
        {
            foreach (var tagList in group.obstaclesByTag.Values)
            {
                foreach (var obj in tagList)
                {
                    if (obj == null) continue;
                    
                    // Update rotation speed
                    FlyingCircleController circleController = obj.GetComponent<FlyingCircleController>();
                    if (circleController != null)
                    {
                        circleController.SetRotationSpeed(currentRotationSpeed);
                    }
                    
                    // Update movement speed cho spawned obstacles
                    ObstacleSpawner spawner = obj.GetComponent<ObstacleSpawner>();
                    if (spawner != null && spawner.SpawnedObstacles != null)
                    {
                        foreach (var spawnedObj in spawner.SpawnedObstacles)
                        {
                            if (spawnedObj == null) continue;
                            
                            ObstacleMovement movement = spawnedObj.GetComponent<ObstacleMovement>();
                            if (movement != null)
                                movement.SetSpeed(currentSpeed);
                        }
                    }
                }
            }
        }
    }
    
    // ========== DEBUG VISUALIZATION ==========
    
    void OnDrawGizmos()
    {
        if (!showGizmos || !Application.isPlaying || player == null) return;
        
        float playerY = player.position.y;
        
        // Spawn line (màu xanh lá)
        Gizmos.color = Color.green;
        Gizmos.DrawLine(new Vector3(-15, playerY + spawnDistance, 0),
                       new Vector3(15, playerY + spawnDistance, 0));
        
        // Despawn line (màu đỏ)
        Gizmos.color = Color.red;
        Gizmos.DrawLine(new Vector3(-15, playerY + despawnDistance, 0),
                       new Vector3(15, playerY + despawnDistance, 0));
        
        // Vẽ từng active group
        foreach (var group in activeGroups)
        {
            Gizmos.color = group.isPassed ? Color.yellow : Color.cyan;
            Gizmos.DrawWireCube(new Vector3(0, group.centerY, 0),
                               new Vector3(10, CalculateGroupHeight(), 0.1f));
        }
    }
    
    // ========== PUBLIC METHODS ==========
    
    /// <summary>
    /// Reset game về trạng thái ban đầu
    /// </summary>
    public void ResetGame()
    {
        // Đưa tất cả active groups về inactive
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
        
        Debug.Log("[TagBasedSpawner] 🔄 Game Reset!");
    }
    
    /// <summary>
    /// Lấy thông tin debug
    /// </summary>
    public string GetDebugInfo()
    {
        return $"Tags: {obstacleTags.Count} | Active Groups: {activeGroups.Count} | " +
               $"Passed: {totalGroupsPassed} | Speed: {currentSpeed:F1} | Rotation: {currentRotationSpeed:F0}";
    }
}