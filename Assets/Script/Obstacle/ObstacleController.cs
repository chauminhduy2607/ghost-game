using UnityEngine;

/// <summary>
/// ⭐ OBSTACLE CONTROLLER - CONNECTOR ĐƠN GIẢN
/// - Chỉ kết nối các component khác
/// - Cung cấp interface thống nhất
/// - Không logic phức tạp
/// </summary>
[RequireComponent(typeof(ObstacleMovement))]
public class ObstacleController : MonoBehaviour
{
    // Components
    private ObstacleMovement movement;
    
    // Properties
    public float CurrentX => movement != null ? movement.CurrentX : 0f;
    public float TargetX => movement != null ? movement.TargetX : 0f;
    
    void Awake()
    {
        // Lấy components
        movement = GetComponent<ObstacleMovement>();
        
        // Tự động thêm ObstacleMovement nếu chưa có
        if (movement == null)
        {
            movement = gameObject.AddComponent<ObstacleMovement>();
        }
    }
    
    // ==================== PUBLIC METHODS ====================
    
    /// <summary>
    /// Random lại chuyển động (cho Looper gọi)
    /// </summary>
    public void RandomizeOnly()
    {
        if (movement != null)
        {
            movement.Randomize();
        }
    }
    
    /// <summary>
    /// Đặt vị trí Y (cho Spawner/Looper gọi)
    /// </summary>
    public void SetPositionY(float y)
    {
        Vector3 pos = transform.position;
        pos.y = y;
        transform.position = pos;
    }
    
    /// <summary>
    /// Reset vật cản
    /// </summary>
    public void ResetObstacle()
    {
        if (movement != null)
        {
            movement.ResetPosition();
        }
    }
    
    /// <summary>
    /// Đặt tốc độ
    /// </summary>
    public void SetSpeed(float speed)
    {
        if (movement != null)
        {
            movement.SetSpeed(speed);
        }
    }
    
    /// <summary>
    /// Dừng di chuyển
    /// </summary>
    public void Stop()
    {
        if (movement != null)
        {
            movement.Stop();
        }
    }
    
    /// <summary>
    /// Tiếp tục di chuyển
    /// </summary>
    public void Resume()
    {
        if (movement != null)
        {
            movement.Resume();
        }
    }
}