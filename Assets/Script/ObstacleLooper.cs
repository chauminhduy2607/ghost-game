using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// ⭐ SCRIPT LOOP VẬT CẢN - CHỈ LO VIỆC RECYCLE
/// - Theo dõi vật cản ra khỏi màn hình
/// - Đưa vật cản từ dưới lên trên (infinite scroll)
/// - Không spawn, không di chuyển
/// </summary>
public class ObstacleLooper : MonoBehaviour
{
    [Header("=== REFERENCE ===")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private ObstacleSpawner spawner;
    
    [Header("=== LOOP SETTINGS ===")]
    [Tooltip("Khoảng cách Y giữa các vật cản khi loop")]
    [SerializeField] private float spacingY = 1.5f;
    
    [Tooltip("Khoảng cách dưới màn hình để bắt đầu loop")]
    [SerializeField] private float loopThreshold = 0.5f;
    
    [Header("=== RANDOMIZE ===")]
    [Tooltip("Random lại chuyển động khi loop")]
    [SerializeField] private bool randomizeOnLoop = true;
    
    [Header("=== DEBUG ===")]
    [SerializeField] private bool showDebugInfo = true;
    
    // Private
    private int totalLooped = 0;
    private float screenHeight;
    
    // Public
    public int TotalLooped => totalLooped;
    
    void Start()
    {
        // Tự động tìm references
        if (mainCamera == null)
            mainCamera = Camera.main;
        
        if (spawner == null)
            spawner = GetComponent<ObstacleSpawner>();
        
        if (spawner == null)
        {
            Debug.LogError("❌ ObstacleLooper: Không tìm thấy ObstacleSpawner!");
            enabled = false;
            return;
        }
        
        screenHeight = mainCamera.orthographicSize * 2f;
        
        Debug.Log("♻️ ObstacleLooper: Sẵn sàng!");
    }
    
    void Update()
    {
        CheckAndLoopObstacles();
    }
    
    // ==================== KIỂM TRA VÀ LOOP ====================
    void CheckAndLoopObstacles()
    {
        if (spawner.ObstacleCount == 0) return;
        
        float cameraBottomEdge = mainCamera.transform.position.y - mainCamera.orthographicSize;
        float loopLine = cameraBottomEdge - (screenHeight * loopThreshold);
        
        GameObject lowestObstacle = null;
        GameObject highestObstacle = null;
        float lowestY = float.MaxValue;
        float highestY = float.MinValue;
        
        // Tìm vật cản thấp nhất và cao nhất
        foreach (GameObject obstacle in spawner.SpawnedObstacles)
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
                highestObstacle = obstacle;
            }
        }
        
        // ⭐ LOOP: Vật cản thấp nhất đi qua line → đưa lên trên
        if (lowestObstacle != null && lowestY < loopLine)
        {
            LoopObstacle(lowestObstacle, highestY);
        }
    }
    
    // ==================== LOOP MỘT VẬT CẢN ====================
    void LoopObstacle(GameObject obstacle, float currentHighestY)
    {
        // Tính vị trí Y mới (trên vật cản cao nhất)
        float newY = currentHighestY + spacingY;
        
        // Đặt vị trí Y mới
        Vector3 pos = obstacle.transform.position;
        pos.y = newY;
        obstacle.transform.position = pos;
        
        // Random lại chuyển động nếu cần
        if (randomizeOnLoop)
        {
            ObstacleController controller = obstacle.GetComponent<ObstacleController>();
            if (controller != null)
            {
                controller.RandomizeOnly();
            }
        }
        
        totalLooped++;
        
        if (showDebugInfo && totalLooped % 5 == 0)
        {
            Debug.Log($"♻️ Đã loop {totalLooped} lần");
        }
    }
    
    // ==================== PUBLIC METHODS ====================
    
    /// <summary>
    /// Reset bộ đếm loop
    /// </summary>
    public void ResetCounter()
    {
        totalLooped = 0;
        Debug.Log("🔄 Reset loop counter");
    }
    
    /// <summary>
    /// Thay đổi khoảng cách khi loop
    /// </summary>
    public void SetSpacing(float spacing)
    {
        spacingY = Mathf.Max(0.5f, spacing);
    }
    
    /// <summary>
    /// Bật/tắt randomize khi loop
    /// </summary>
    public void SetRandomizeOnLoop(bool value)
    {
        randomizeOnLoop = value;
    }
    
}