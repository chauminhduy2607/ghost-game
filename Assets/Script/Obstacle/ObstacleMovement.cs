using UnityEngine;

/// <summary>
/// ⭐ SCRIPT DI CHUYỂN VẬT CẢN - DI CHUYỂN THEO 9 LINE
/// - Di chuyển từ line này sang line khác
/// - Đi đủ 9 line (L0 đến L8)
/// - Chạm viền màn hình mới quẹo lại
/// - Mỗi vật cản có tốc độ và hướng độc lập
/// </summary>
public class ObstacleMovement : MonoBehaviour
{
    [Header("=== DI CHUYỂN NGANG ===")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float minSpeed = 1f;
    [SerializeField] private float maxSpeed = 4f;
    
    [Header("=== LINE SETTINGS ===")]
    [SerializeField] private bool randomStartLine = true;
    [SerializeField] private bool randomStartDirection = true;
    
    [Header("=== DEBUG ===")]
    [SerializeField] private bool showDebug = false;
    
    // Private
    private LineSystem lineSystem;
    private Camera mainCamera;
    private int currentLineIndex;
    private float currentTargetX;
    private bool movingRight = true;
    private float actualSpeed;
    
    // Public
    public float CurrentX => transform.position.x;
    public float TargetX => currentTargetX;
    
    void Awake()
    {
        mainCamera = Camera.main;
        lineSystem = LineSystem.Instance;
        
        if (lineSystem == null)
        {
            Debug.LogError($"❌ {gameObject.name}: Không tìm thấy LineSystem!");
            enabled = false;
            return;
        }
        
        RandomizeMovement();
        SetRandomStartPosition();
    }
    
    void Update()
    {
        MoveToTarget();
    }
    
    // ==================== RANDOM HÓA CHUYỂN ĐỘNG ====================
    void RandomizeMovement()
    {
        // Random tốc độ cho mỗi vật cản
        actualSpeed = Random.Range(minSpeed, maxSpeed);
        
        if (showDebug)
        {
            Debug.Log($"🎲 {gameObject.name}: Speed = {actualSpeed:F2}");
        }
    }
    
    // ==================== VỊ TRÍ BAN ĐẦU NGẪU NHIÊN ====================
    void SetRandomStartPosition()
    {
        // Random line bắt đầu
        if (randomStartLine)
        {
            currentLineIndex = Random.Range(0, lineSystem.GetTotalLines());
        }
        else
        {
            currentLineIndex = lineSystem.GetTotalLines() / 2; // Giữa màn hình
        }
        
        // Random hướng
        if (randomStartDirection)
        {
            movingRight = Random.value > 0.5f;
        }
        
        // Đặt vị trí X theo line
        Vector3 pos = transform.position;
        pos.x = lineSystem.GetLineX(currentLineIndex);
        transform.position = pos;
        
        // Chọn target tiếp theo
        SetNextTarget();
        
        if (showDebug)
        {
            Debug.Log($"📍 {gameObject.name}: Start Line {currentLineIndex}, Moving {(movingRight ? "RIGHT" : "LEFT")}");
        }
    }
    
    // ==================== CHỌN TARGET TIẾP THEO ====================
    void SetNextTarget()
    {
        int nextLineIndex;
        
        if (movingRight)
        {
            // Đi sang phải
            nextLineIndex = currentLineIndex + 1;
            
            // ⭐ Nếu vượt quá line cuối -> đổi hướng
            if (nextLineIndex >= lineSystem.GetTotalLines())
            {
                nextLineIndex = lineSystem.GetTotalLines() - 1;
                movingRight = false;
                
                if (showDebug)
                {
                    Debug.Log($"🔄 {gameObject.name}: Reached RIGHT edge! Turning LEFT");
                }
            }
        }
        else
        {
            // Đi sang trái
            nextLineIndex = currentLineIndex - 1;
            
            // ⭐ Nếu vượt quá line đầu -> đổi hướng
            if (nextLineIndex < 0)
            {
                nextLineIndex = 0;
                movingRight = true;
                
                if (showDebug)
                {
                    Debug.Log($"🔄 {gameObject.name}: Reached LEFT edge! Turning RIGHT");
                }
            }
        }
        
        // Cập nhật target
        currentLineIndex = nextLineIndex;
        currentTargetX = lineSystem.GetLineX(currentLineIndex);
        
        if (showDebug)
        {
            Debug.Log($"➡️ {gameObject.name}: Target Line {currentLineIndex} (X={currentTargetX:F2})");
        }
    }
    
    // ==================== DI CHUYỂN ĐẾN TARGET ====================
    void MoveToTarget()
    {
        Vector3 pos = transform.position;
        
        // Tính hướng di chuyển
        float direction = movingRight ? 1f : -1f;
        
        // Di chuyển
        pos.x += direction * actualSpeed * Time.deltaTime;
        
        // Kiểm tra đã đến target chưa
        bool reachedTarget = false;
        
        if (movingRight)
        {
            if (pos.x >= currentTargetX)
            {
                pos.x = currentTargetX;
                reachedTarget = true;
            }
        }
        else
        {
            if (pos.x <= currentTargetX)
            {
                pos.x = currentTargetX;
                reachedTarget = true;
            }
        }
        
        transform.position = pos;
        
        // Nếu đã đến target -> chọn target mới
        if (reachedTarget)
        {
            SetNextTarget();
        }
    }
    
    // ==================== PUBLIC METHODS ====================
    
    /// <summary>
    /// Random lại chuyển động
    /// </summary>
    public void Randomize()
    {
        RandomizeMovement();
    }
    
    /// <summary>
    /// Đặt tốc độ mới
    /// </summary>
    public void SetSpeed(float speed)
    {
        moveSpeed = Mathf.Clamp(speed, minSpeed, maxSpeed);
        actualSpeed = moveSpeed;
    }
    
    /// <summary>
    /// Reset về vị trí ban đầu
    /// </summary>
    public void ResetPosition()
    {
        SetRandomStartPosition();
    }
    
    /// <summary>
    /// Dừng di chuyển
    /// </summary>
    public void Stop()
    {
        enabled = false;
    }
    
    /// <summary>
    /// Tiếp tục di chuyển
    /// </summary>
    public void Resume()
    {
        enabled = true;
    }
    
    /// <summary>
    /// Lấy line hiện tại
    /// </summary>
    public int GetCurrentLine()
    {
        return currentLineIndex;
    }
    
    /// <summary>
    /// Kiểm tra đang di chuyển sang phải
    /// </summary>
    public bool IsMovingRight()
    {
        return movingRight;
    }
    
    // ==================== GIZMOS ====================
    void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying || lineSystem == null) return;
        
        // Vẽ target
        Gizmos.color = Color.cyan;
        Vector3 targetPos = new Vector3(currentTargetX, transform.position.y, 0f);
        Gizmos.DrawWireSphere(targetPos, 0.2f);
        
        // Vẽ line kết nối
        Gizmos.DrawLine(transform.position, targetPos);
        
        // Vẽ mũi tên hướng
        Vector3 arrowDir = movingRight ? Vector3.right : Vector3.left;
        Gizmos.color = movingRight ? Color.green : Color.red;
        Gizmos.DrawRay(transform.position, arrowDir * 0.3f);
    }
}