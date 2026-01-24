using UnityEngine;

/// <summary>
/// 🤖 AI BOT - PHIÊN BẢN ĐƠN GIẢN
/// ⭐ CHIẾN LƯỢC: Bay lên liên tục + Tránh vật cản
/// </summary>
public class GhostAI : MonoBehaviour
{
    [Header("=== AI SETTINGS ===")]
    [SerializeField] private bool enableAI = true;
    [SerializeField] private float autoSwipeInterval = 1.2f;    // ⭐ Tự động vuốt mỗi X giây
    [SerializeField] private float swipeForce = 25f;            // Lực vuốt
    [SerializeField] private float obstacleCheckDistance = 2.5f; // Khoảng cách check vật cản
    
    [Header("=== OBSTACLE DETECTION ===")]
    [SerializeField] private string obstacleTag = "Obstacle";
    [SerializeField] private float horizontalCheckRadius = 1.0f; // Bán kính check ngang
    [SerializeField] private bool showDebug = true;
    
    [Header("=== HEIGHT LIMITS ===")]
    [SerializeField] private float minHeight = 0.5f;            // Độ cao tối thiểu
    [SerializeField] private float emergencySwipeForce = 35f;   // Lực vuốt khẩn cấp
    
    // Private
    private GhostController ghostController;
    private Rigidbody2D rb;
    private float timeSinceLastSwipe;
    private bool hasObstacleAhead;
    
    void Awake()
    {
        ghostController = GetComponent<GhostController>();
        rb = GetComponent<Rigidbody2D>();
        
        if (ghostController == null)
        {
            Debug.LogError("❌ GhostAI: Không tìm thấy GhostController!");
            enabled = false;
            return;
        }
        
        Debug.Log("🤖 AI BOT: SIMPLE MODE ACTIVATED!");
        Debug.Log("⏱️ Auto Swipe Every: " + autoSwipeInterval + "s");
        Debug.Log("🎯 Strategy: Bay lên liên tục + Tránh vật cản");
    }
    
    void Update()
    {
        if (!enableAI) return;
        if (ghostController.IsGameOver) return;
        if (ghostController.HitObstacle) return;
        
        timeSinceLastSwipe += Time.deltaTime;
        
        // ⭐⭐ CHIẾN LƯỢC ĐƠN GIẢN: BAY LÊN LIÊN TỤC!
        
        // 1. Check khẩn cấp - quá thấp
        if (transform.position.y < minHeight)
        {
            SwipeUp(emergencySwipeForce, "🚨 KHẨN CẤP - QUÁ THẤP!");
            return;
        }
        
        // 2. Check vật cản phía trước
        CheckForObstacles();
        
        // 3. Nếu có vật cản → vuốt ngay
        if (hasObstacleAhead)
        {
            if (timeSinceLastSwipe >= autoSwipeInterval * 0.5f) // Nhanh hơn khi có vật cản
            {
                SwipeUp(swipeForce * 1.2f, "🚨 TRÁNH VẬT CẢN!");
            }
        }
        // 4. Không có vật cản → vuốt đều đặn để bay lên
        else
        {
            if (timeSinceLastSwipe >= autoSwipeInterval)
            {
                SwipeUp(swipeForce, "⬆️ TỰ ĐỘNG BAY LÊN");
            }
        }
    }
    
    /// <summary>
    /// ⭐ CHECK VẬT CẢN PHÍA TRÊN (ĐƠN GIẢN)
    /// </summary>
    void CheckForObstacles()
    {
        hasObstacleAhead = false;
        
        // Tìm tất cả vật cản
        GameObject[] obstacles = GameObject.FindGameObjectsWithTag(obstacleTag);
        
        foreach (GameObject obs in obstacles)
        {
            Vector3 obsPos = obs.transform.position;
            Vector3 myPos = transform.position;
            
            // Tính khoảng cách
            float verticalDist = obsPos.y - myPos.y;      // Khoảng cách dọc
            float horizontalDist = Mathf.Abs(obsPos.x - myPos.x); // Khoảng cách ngang
            
            // ⭐ VẬT CẢN Ở PHÍA TRÊN + ĐÚNG ĐƯỜNG BAY
            if (verticalDist > 0 && verticalDist < obstacleCheckDistance)
            {
                if (horizontalDist < horizontalCheckRadius)
                {
                    hasObstacleAhead = true;
                    
                    if (showDebug)
                    {
                        Debug.DrawLine(myPos, obsPos, Color.red);
                        Debug.Log("🚨 Phát hiện vật cản: " + obs.name + " | Dọc: " + verticalDist.ToString("F2") + " | Ngang: " + horizontalDist.ToString("F2"));
                    }
                    
                    return; // Tìm thấy 1 cái là đủ
                }
            }
        }
        
        // Debug - không có vật cản
        if (showDebug && Time.frameCount % 60 == 0)
        {
            Debug.Log("✅ Không có vật cản phía trước");
        }
    }
    
    /// <summary>
    /// ⭐ VUỐT LÊN
    /// </summary>
    void SwipeUp(float force, string reason)
    {
        // Nếu đang rớt → dừng rớt
        if (ghostController.IsFalling)
        {
            ghostController.StopFalling();
        }
        
        // Áp lực lên
        rb.AddForce(Vector2.up * force, ForceMode2D.Impulse);
        
        // Reset timer
        timeSinceLastSwipe = 0f;
        
        if (showDebug)
        {
            Debug.Log("⬆️ AI VUỐT: " + reason + " | Force: " + force.ToString("F1"));
        }
    }
    
    // ==================== PUBLIC METHODS ====================
    
    public void SetAIEnabled(bool enabled)
    {
        enableAI = enabled;
        Debug.Log("🤖 AI: " + (enabled ? "BẬT" : "TẮT"));
    }
    
    public void SetSwipeInterval(float interval)
    {
        autoSwipeInterval = interval;
        Debug.Log("⏱️ AI Swipe Interval: " + interval + "s");
    }
    
    // ==================== DEBUG VISUALIZATION ====================
    
    void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;
        if (!enableAI) return;
        
        // Vẽ vùng check vật cản (hình chữ nhật phía trên)
        Gizmos.color = hasObstacleAhead ? Color.red : Color.green;
        
        Vector3 boxCenter = transform.position + Vector3.up * (obstacleCheckDistance / 2);
        Vector3 boxSize = new Vector3(horizontalCheckRadius * 2, obstacleCheckDistance, 0.1f);
        
        Gizmos.DrawWireCube(boxCenter, boxSize);
        
        // Vẽ độ cao tối thiểu
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(new Vector3(-10, minHeight, 0), new Vector3(10, minHeight, 0));
        
        // Vẽ vị trí hiện tại
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 0.3f);
    }
}