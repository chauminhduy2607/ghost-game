using UnityEngine;

/// <summary>
/// 🤖 AI BOT - TỰ ĐỘNG CHƠI GAME
/// Tự động phát hiện chướng ngại vật và vuốt lên để tránh
/// </summary>
public class GhostAI : MonoBehaviour
{
    [Header("=== AI SETTINGS ===")]
    [SerializeField] private bool enableAI = true;              // Bật/tắt AI
    [SerializeField] private float detectionDistance = 5f;      // Khoảng cách phát hiện chướng ngại vật
    [SerializeField] private float safeHeight = 1f;             // Độ cao an toàn phía trên chướng ngại vật
    [SerializeField] private float swipeForce = 25f;            // Lực vuốt của AI
    [SerializeField] private float reactionDelay = 0.1f;        // Độ trễ phản ứng (giống người thật)
    
    [Header("=== DETECTION ===")]
    [SerializeField] private LayerMask obstacleLayer;           // Layer của chướng ngại vật
    [SerializeField] private float raycastWidth = 0.5f;         // Độ rộng quét
    [SerializeField] private bool showDebugRays = true;         // Hiển thị tia debug
    
    [Header("=== BEHAVIOR ===")]
    [SerializeField] private float minSwipeInterval = 0.3f;     // Khoảng cách tối thiểu giữa các lần vuốt
    [SerializeField] private float maxHeight = 4f;              // Độ cao tối đa (không bay quá cao)
    [SerializeField] private float minHeight = 0.5f;            // Độ cao tối thiểu (không rơi quá thấp)
    
    // Private variables
    private GhostController ghostController;
    private Rigidbody2D rb;
    private float lastSwipeTime;
    private float reactionTimer;
    private bool shouldSwipe;
    
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
        
        Debug.Log("🤖 AI BOT: ACTIVATED!");
        Debug.Log("📊 Detection Distance: " + detectionDistance);
        Debug.Log("📊 Safe Height: " + safeHeight);
    }
    
    void Update()
    {
        if (!enableAI) return;
        if (ghostController.IsGameOver) return;
        if (ghostController.HitObstacle) return;
        
        // Phát hiện chướng ngại vật
        DetectObstacles();
        
        // Xử lý phản ứng với độ trễ
        if (shouldSwipe)
        {
            reactionTimer += Time.deltaTime;
            if (reactionTimer >= reactionDelay)
            {
                PerformSwipe();
                shouldSwipe = false;
                reactionTimer = 0f;
            }
        }
        
        // Duy trì độ cao an toàn
        MaintainSafeHeight();
    }
    
    /// <summary>
    /// Phát hiện chướng ngại vật phía trước
    /// </summary>
    void DetectObstacles()
    {
        Vector2 origin = transform.position;
        Vector2 direction = Vector2.right;
        
        // Quét 3 tia: giữa, trên, dưới
        RaycastHit2D hitCenter = Physics2D.Raycast(origin, direction, detectionDistance, obstacleLayer);
        RaycastHit2D hitTop = Physics2D.Raycast(origin + Vector2.up * raycastWidth, direction, detectionDistance, obstacleLayer);
        RaycastHit2D hitBottom = Physics2D.Raycast(origin + Vector2.down * raycastWidth, direction, detectionDistance, obstacleLayer);
        
        // Debug rays
        if (showDebugRays)
        {
            Color rayColor = (hitCenter || hitTop || hitBottom) ? Color.red : Color.green;
            Debug.DrawRay(origin, direction * detectionDistance, rayColor);
            Debug.DrawRay(origin + Vector2.up * raycastWidth, direction * detectionDistance, rayColor);
            Debug.DrawRay(origin + Vector2.down * raycastWidth, direction * detectionDistance, rayColor);
        }
        
        // Nếu phát hiện chướng ngại vật
        if (hitCenter || hitTop || hitBottom)
        {
            RaycastHit2D hit = hitCenter ? hitCenter : (hitTop ? hitTop : hitBottom);
            
            // Tính độ cao cần thiết để vượt qua
            float obstacleTop = hit.collider.bounds.max.y;
            float requiredHeight = obstacleTop + safeHeight;
            
            // Nếu đang ở dưới độ cao cần thiết → vuốt lên
            if (transform.position.y < requiredHeight)
            {
                if (Time.time - lastSwipeTime >= minSwipeInterval)
                {
                    shouldSwipe = true;
                    Debug.Log("🚨 AI: Phát hiện chướng ngại vật! Chuẩn bị vuốt lên...");
                }
            }
        }
    }
    
    /// <summary>
    /// Thực hiện vuốt lên
    /// </summary>
    void PerformSwipe()
    {
        if (ghostController.IsFalling)
        {
            ghostController.StopFalling();
        }
        
        rb.AddForce(Vector2.up * swipeForce, ForceMode2D.Impulse);
        lastSwipeTime = Time.time;
        
        Debug.Log("⬆️ AI: VUỐT LÊN! Force: " + swipeForce);
    }
    
    /// <summary>
    /// Duy trì độ cao an toàn (không bay quá cao, không rơi quá thấp)
    /// </summary>
    void MaintainSafeHeight()
    {
        float currentHeight = transform.position.y;
        
        // Quá thấp → vuốt nhẹ lên
        if (currentHeight < minHeight && rb.linearVelocity.y < 0)
        {
            if (Time.time - lastSwipeTime >= minSwipeInterval)
            {
                shouldSwipe = true;
                Debug.Log("⚠️ AI: Quá thấp! Vuốt lên...");
            }
        }
        
        // Quá cao → để tự rơi xuống
        if (currentHeight > maxHeight && rb.linearVelocity.y > 0)
        {
            // Không làm gì, để trọng lực kéo xuống tự nhiên
            Debug.Log("⚠️ AI: Quá cao! Để rơi xuống...");
        }
    }
    
    /// <summary>
    /// Bật/tắt AI từ code khác
    /// </summary>
    public void SetAIEnabled(bool enabled)
    {
        enableAI = enabled;
        Debug.Log("🤖 AI: " + (enabled ? "BẬT" : "TẮT"));
    }
    
    /// <summary>
    /// Điều chỉnh độ nhạy của AI
    /// </summary>
    public void SetDifficulty(float distance, float reaction)
    {
        detectionDistance = distance;
        reactionDelay = reaction;
        Debug.Log("🎮 AI Difficulty: Detection=" + distance + ", Reaction=" + reaction);
    }
    
    void OnDrawGizmosSelected()
    {
        // Vẽ vùng phát hiện
        Gizmos.color = Color.yellow;
        Vector3 pos = transform.position;
        Gizmos.DrawWireCube(pos + Vector3.right * detectionDistance / 2, 
                           new Vector3(detectionDistance, raycastWidth * 2, 0.1f));
        
        // Vẽ độ cao an toàn
        Gizmos.color = Color.green;
        Gizmos.DrawLine(new Vector3(-10, minHeight, 0), new Vector3(10, minHeight, 0));
        Gizmos.color = Color.red;
        Gizmos.DrawLine(new Vector3(-10, maxHeight, 0), new Vector3(10, maxHeight, 0));
    }
}