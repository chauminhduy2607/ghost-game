using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// ⭐ QUẢN LÝ TẤT CẢ VẬT CẢN - GẮN VÀO FIRE&LINE
/// - Tự động thêm ObstacleController vào TẤT CẢ con
/// - Tự động loop vật cản khi ra khỏi màn hình
/// - Chỉ cần gắn script này vào Fire&Line là xong!
/// </summary>
public class FireLineManager : MonoBehaviour
{
    [Header("=== REFERENCE ===")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Transform player;  // Ghost
    
    [Header("=== SETTINGS ===")]
    [Tooltip("Khoảng cách giữa các vật cản (theo trục Y)")]
    [SerializeField] private float spacingY = 1.5f;
    
    [Tooltip("Tự động thêm ObstacleController vào tất cả con")]
    [SerializeField] private bool autoAddControllers = true;
    
    [Header("=== PARALLAX ===")]
    [Tooltip("Tốc độ vật cản di chuyển xuống khi camera lên (0-1)")]
    [SerializeField] private float parallaxSpeed = 0.8f;
    
    [Header("=== DI CHUYỂN NGANG ===")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float minSpeed = 1f;
    [SerializeField] private float maxSpeed = 4f;
    [SerializeField] private float screenPadding = 0.5f;
    
    [Header("=== DEBUG ===")]
    [SerializeField] private bool showDebugInfo = true;
    
    // Private
    private List<ObstacleController> obstacles = new List<ObstacleController>();
    private Vector3 lastCameraPosition;
    private float screenHeight;
    private int totalRecycled = 0;
    
    void Start()
    {
        // Tự động tìm camera và player
        if (mainCamera == null)
            mainCamera = Camera.main;
        
        if (player == null)
        {
            GhostController ghost = FindObjectOfType<GhostController>();
            if (ghost != null)
                player = ghost.transform;
        }
        
        screenHeight = mainCamera.orthographicSize * 2f;
        lastCameraPosition = mainCamera.transform.position;
        
        // ⭐ TỰ ĐỘNG SETUP
        SetupObstacles();
        ArrangeObstacles();
        
        Debug.Log("🔥 FireLineManager: Sẵn sàng!");
        Debug.Log($"📊 Quản lý {obstacles.Count} vật cản");
    }
    
    void Update()
    {
        MoveObstaclesWithCamera();
        CheckAndRecycleObstacles();
        lastCameraPosition = mainCamera.transform.position;
    }
    
    // ==================== TỰ ĐỘNG SETUP ====================
    void SetupObstacles()
    {
        // Tìm tất cả con
        foreach (Transform child in transform)
        {
            if (child == null) continue;
            
            // ⭐ TỰ ĐỘNG THÊM ObstacleController nếu chưa có
            ObstacleController controller = child.GetComponent<ObstacleController>();
            
            if (controller == null && autoAddControllers)
            {
                controller = child.gameObject.AddComponent<ObstacleController>();
                Debug.Log($"➕ Đã thêm ObstacleController vào {child.name}");
            }
            
            if (controller != null)
            {
                obstacles.Add(controller);
            }
        }
        
        if (obstacles.Count == 0)
        {
            Debug.LogError("❌ Không tìm thấy vật cản nào trong Fire&Line!");
            return;
        }
        
        Debug.Log($"✅ Tìm thấy {obstacles.Count} vật cản:");
        foreach (var obs in obstacles)
        {
            Debug.Log($"   - {obs.gameObject.name}");
        }
    }
    
    // ==================== SẮP XẾP VẬT CẢN ====================
    void ArrangeObstacles()
    {
        if (obstacles.Count == 0) return;
        
        float startY = player != null ? player.position.y + screenHeight * 0.3f : 0f;
        
        for (int i = 0; i < obstacles.Count; i++)
        {
            float yPos = startY + (i * spacingY);
            obstacles[i].SetPositionY(yPos);
            
            Debug.Log($"📍 {obstacles[i].gameObject.name}: Y = {yPos:F1}");
        }
    }
    
    // ==================== DI CHUYỂN VẬT CẢN ====================
    void MoveObstaclesWithCamera()
    {
        float cameraDeltaY = mainCamera.transform.position.y - lastCameraPosition.y;
        if (Mathf.Abs(cameraDeltaY) < 0.001f) return;
        
        foreach (ObstacleController obstacle in obstacles)
        {
            if (obstacle != null)
            {
                float moveDelta = -cameraDeltaY * parallaxSpeed;
                Vector3 pos = obstacle.transform.position;
                pos.y += moveDelta;
                obstacle.transform.position = pos;
            }
        }
    }
    
    // ==================== KIỂM TRA VÀ LOOP ====================
    void CheckAndRecycleObstacles()
    {
        float cameraBottomEdge = mainCamera.transform.position.y - mainCamera.orthographicSize;
        
        ObstacleController lowestObstacle = null;
        float lowestY = float.MaxValue;
        float highestY = float.MinValue;
        
        foreach (ObstacleController obstacle in obstacles)
        {
            if (obstacle == null) continue;
            
            float obstacleY = obstacle.transform.position.y;
            
            if (obstacleY < lowestY)
            {
                lowestY = obstacleY;
                lowestObstacle = obstacle;
            }
            
            if (obstacleY > highestY)
            {
                highestY = obstacleY;
            }
        }
        
        // ⭐ LOOP: Vật cản dưới cùng ra khỏi màn hình → đưa lên trên
        if (lowestObstacle != null && lowestY < cameraBottomEdge - screenHeight * 0.5f)
        {
            float newY = highestY + spacingY;
            lowestObstacle.SetPositionY(newY);
            lowestObstacle.RandomizeOnly();
            
            totalRecycled++;
            
            if (totalRecycled % 5 == 0)
                Debug.Log($"♻️ Đã loop {totalRecycled} lần");
        }
    }
    
    // ==================== DEBUG ====================
    void OnGUI()
    {
        if (!showDebugInfo || !Application.isPlaying) return;
        
        GUIStyle style = new GUIStyle();
        style.fontSize = 18;
        style.normal.textColor = Color.green;
        style.alignment = TextAnchor.LowerRight;
        style.fontStyle = FontStyle.Bold;
        
        int onScreen = 0;
        float cameraBottom = mainCamera.transform.position.y - mainCamera.orthographicSize;
        float cameraTop = mainCamera.transform.position.y + mainCamera.orthographicSize;
        
        foreach (ObstacleController obstacle in obstacles)
        {
            if (obstacle == null) continue;
            float y = obstacle.transform.position.y;
            if (y >= cameraBottom && y <= cameraTop)
                onScreen++;
        }
        
        string info = "🔥 FIRE&LINE\n";
        info += "━━━━━━━━━━━━\n";
        info += $"Total: {obstacles.Count}\n";
        info += $"On Screen: {onScreen}\n";
        info += $"Looped: {totalRecycled}";
        
        GUI.Label(new Rect(Screen.width - 220, Screen.height - 130, 210, 130), info, style);
    }
    
    // ==================== PUBLIC METHODS ====================
    
    /// <summary>
    /// Reset tất cả vật cản
    /// </summary>
    public void ResetAll()
    {
        ArrangeObstacles();
        totalRecycled = 0;
        Debug.Log("🔄 Reset Fire&Line!");
    }
    
    /// <summary>
    /// Thay đổi khoảng cách
    /// </summary>
    public void SetSpacing(float spacing)
    {
        spacingY = Mathf.Max(1f, spacing);
        ArrangeObstacles();
    }
}