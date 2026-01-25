using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 💾 LƯU VỊ TRÍ GHOST MỖI 0.5S
/// Để hồi sinh khi Continue
/// </summary>
public class PositionSaver : MonoBehaviour
{
    [Header("=== SETTINGS ===")]
    [SerializeField] private float saveInterval = 0.5f; // Lưu mỗi 0.5s
    [SerializeField] private int maxPositions = 10;     // Lưu tối đa 10 vị trí (5 giây)
    
    // Queue để lưu vị trí (FIFO - First In First Out)
    private Queue<Vector3> savedPositions = new Queue<Vector3>();
    private Queue<float> savedVelocitiesY = new Queue<float>();
    
    private float timeSinceLastSave = 0f;
    private Rigidbody2D rb;
    
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }
    
    void Update()
    {
        // Đếm thời gian
        timeSinceLastSave += Time.deltaTime;
        
        // Lưu vị trí mỗi 0.5s
        if (timeSinceLastSave >= saveInterval)
        {
            SaveCurrentPosition();
            timeSinceLastSave = 0f;
        }
    }
    
    /// <summary>
    /// Lưu vị trí hiện tại
    /// </summary>
    void SaveCurrentPosition()
    {
        // Thêm vị trí mới vào queue
        savedPositions.Enqueue(transform.position);
        savedVelocitiesY.Enqueue(rb != null ? rb.linearVelocity.y : 0f);
        
        // Nếu vượt quá giới hạn → xóa vị trí cũ nhất
        if (savedPositions.Count > maxPositions)
        {
            savedPositions.Dequeue();
            savedVelocitiesY.Dequeue();
        }
        
        if (Time.frameCount % 30 == 0) // Log mỗi 30 frame
        {
            Debug.Log("💾 Saved position: " + transform.position + " | Total: " + savedPositions.Count);
        }
    }
    
    /// <summary>
    /// Lấy vị trí 0.5s trước (vị trí cuối cùng trong queue)
    /// </summary>
    public Vector3 GetLastSavedPosition()
    {
        if (savedPositions.Count > 0)
        {
            // Lấy vị trí đầu tiên (cũ nhất = 0.5s trước khi chết)
            Vector3[] positions = savedPositions.ToArray();
            return positions[0];
        }
        
        // Nếu không có → trả về vị trí hiện tại
        return transform.position;
    }
    
    /// <summary>
    /// Lấy vận tốc 0.5s trước
    /// </summary>
    public float GetLastSavedVelocity()
    {
        if (savedVelocitiesY.Count > 0)
        {
            float[] velocities = savedVelocitiesY.ToArray();
            return velocities[0];
        }
        
        return 0f;
    }
    
    /// <summary>
    /// Hồi sinh Ghost về vị trí 0.5s trước
    /// </summary>
    public void RespawnAtLastPosition()
    {
        Vector3 lastPos = GetLastSavedPosition();
        float lastVel = GetLastSavedVelocity();
        
        // Teleport về vị trí cũ
        transform.position = lastPos;
        
        // Khôi phục vận tốc
        if (rb != null)
        {
            rb.linearVelocity = new Vector2(0, lastVel);
        }
        
        Debug.Log("🔄 RESPAWN at: " + lastPos + " | Velocity: " + lastVel);
    }
    
    /// <summary>
    /// Xóa tất cả vị trí đã lưu (khi chết hoàn toàn)
    /// </summary>
    public void ClearSavedPositions()
    {
        savedPositions.Clear();
        savedVelocitiesY.Clear();
        Debug.Log("🗑️ Cleared all saved positions");
    }
}