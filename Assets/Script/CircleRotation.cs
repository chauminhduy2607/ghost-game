using UnityEngine;

/// <summary>
/// ⭐ QUAY QUANH ĐIỂM TÂM
/// - Object quay quanh tâm (center point)
/// - Tạo thành hình tròn khi 2 object quay
/// - Gắn vào Circle (1) và Circle
/// </summary>
public class CircleRotation : MonoBehaviour
{
    [Header("=== ROTATION SETTINGS ===")]
    [Tooltip("Tốc độ quay (độ/giây)")]
    [SerializeField] private float rotationSpeed = 180f;
    
    [Tooltip("Bán kính (khoảng cách từ tâm)")]
    [SerializeField] private float radius = 1f;
    
    [Tooltip("Góc bắt đầu (0-360)")]
    [SerializeField] private float startAngle = 0f;
    
    [Header("=== CENTER POINT ===")]
    [Tooltip("Điểm tâm để quay quanh (nếu null thì dùng parent position)")]
    [SerializeField] private Transform centerPoint;
    
    [Header("=== SPRITE ROTATION ===")]
    [Tooltip("Sprite có tự xoay theo hướng di chuyển không")]
    [SerializeField] private bool rotateSprite = true;
    
    [Tooltip("Offset góc xoay sprite (nếu sprite không nhìn đúng hướng)")]
    [SerializeField] private float spriteRotationOffset = 90f;
    
    [Tooltip("Giữ flip ban đầu của sprite (Flip X/Y trong SpriteRenderer)")]
    [SerializeField] private bool keepOriginalFlip = true;
    
    [Header("=== DEBUG ===")]
    [SerializeField] private bool showDebugLine = true;
    
    // Private
    private float currentAngle;
    private Vector3 centerPosition;
    private Vector3 lastPosition;
    private SpriteRenderer spriteRenderer;
    private bool originalFlipX;
    private bool originalFlipY;
    
    void Start()
    {
        // Tìm điểm tâm
        if (centerPoint == null && transform.parent != null)
        {
            // Dùng vị trí parent làm tâm
            centerPosition = transform.parent.position;
        }
        else if (centerPoint != null)
        {
            centerPosition = centerPoint.position;
        }
        else
        {
            // Dùng vị trí hiện tại
            centerPosition = transform.position;
        }
        
        // Lưu flip ban đầu của sprite
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null && keepOriginalFlip)
        {
            originalFlipX = spriteRenderer.flipX;
            originalFlipY = spriteRenderer.flipY;
        }
        
        // Đặt góc bắt đầu
        currentAngle = startAngle;
        
        // Đặt vị trí ban đầu
        UpdatePosition();
        
        // Lưu vị trí ban đầu
        lastPosition = transform.position;
        
        Debug.Log($"⭕ {gameObject.name}: Quay quanh tâm tại {centerPosition}");
        Debug.Log($"   - Bán kính: {radius}");
        Debug.Log($"   - Góc bắt đầu: {startAngle}°");
        Debug.Log($"   - Tốc độ: {rotationSpeed}°/s");
        if (spriteRenderer != null)
        {
            Debug.Log($"   - Flip ban đầu: X={originalFlipX}, Y={originalFlipY}");
        }
    }
    
    void Update()
    {
        // Cập nhật tâm (nếu parent di chuyển)
        if (centerPoint != null)
        {
            centerPosition = centerPoint.position;
        }
        else if (transform.parent != null)
        {
            centerPosition = transform.parent.position;
        }
        
        // Tăng góc theo thời gian
        currentAngle += rotationSpeed * Time.deltaTime;
        
        // Giới hạn 0-360
        if (currentAngle >= 360f)
            currentAngle -= 360f;
        else if (currentAngle < 0f)
            currentAngle += 360f;
        
        // Cập nhật vị trí
        UpdatePosition();
        
        // Cập nhật rotation của sprite
        if (rotateSprite)
        {
            UpdateSpriteRotation();
        }
    }
    
    void UpdatePosition()
    {
        // Lưu vị trí cũ
        lastPosition = transform.position;
        
        // Chuyển góc sang radian
        float angleInRadians = currentAngle * Mathf.Deg2Rad;
        
        // Tính vị trí mới trên hình tròn
        float x = centerPosition.x + Mathf.Cos(angleInRadians) * radius;
        float y = centerPosition.y + Mathf.Sin(angleInRadians) * radius;
        
        // Đặt vị trí mới
        transform.position = new Vector3(x, y, transform.position.z);
    }
    
    void UpdateSpriteRotation()
    {
        // Tính hướng di chuyển
        Vector3 direction = transform.position - lastPosition;
        
        if (direction.magnitude > 0.001f)
        {
            // Tính góc xoay từ hướng di chuyển
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            
            // Áp dụng offset (nếu sprite không nhìn đúng hướng)
            angle += spriteRotationOffset;
            
            // Nếu giữ flip ban đầu, điều chỉnh góc xoay
            if (spriteRenderer != null && keepOriginalFlip)
            {
                // Nếu flip X, thêm 180 độ vào góc xoay
                if (originalFlipX)
                {
                    angle += 180f;
                }
                
                // Đảm bảo flip được giữ nguyên
                spriteRenderer.flipX = originalFlipX;
                spriteRenderer.flipY = originalFlipY;
            }
            
            // Xoay sprite
            transform.rotation = Quaternion.Euler(0, 0, angle);
        }
    }
    
    // ==================== DEBUG ====================
    void OnDrawGizmos()
    {
        if (!showDebugLine) return;
        
        // Vẽ đường tròn
        Vector3 center = Application.isPlaying ? centerPosition : 
                        (transform.parent != null ? transform.parent.position : transform.position);
        
        Gizmos.color = Color.yellow;
        
        // Vẽ hình tròn bằng nhiều đoạn thẳng
        int segments = 50;
        float angleStep = 360f / segments;
        
        for (int i = 0; i < segments; i++)
        {
            float angle1 = i * angleStep * Mathf.Deg2Rad;
            float angle2 = (i + 1) * angleStep * Mathf.Deg2Rad;
            
            Vector3 point1 = new Vector3(
                center.x + Mathf.Cos(angle1) * radius,
                center.y + Mathf.Sin(angle1) * radius,
                center.z
            );
            
            Vector3 point2 = new Vector3(
                center.x + Mathf.Cos(angle2) * radius,
                center.y + Mathf.Sin(angle2) * radius,
                center.z
            );
            
            Gizmos.DrawLine(point1, point2);
        }
        
        // Vẽ đường từ tâm đến object
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(center, transform.position);
        
        // Vẽ điểm tâm
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(center, 0.1f);
    }
    
    // ==================== PUBLIC METHODS ====================
    
    /// <summary>
    /// Đặt tốc độ quay mới
    /// </summary>
    public void SetRotationSpeed(float speed)
    {
        rotationSpeed = speed;
    }
    
    /// <summary>
    /// Đặt bán kính mới
    /// </summary>
    public void SetRadius(float newRadius)
    {
        radius = Mathf.Max(0.1f, newRadius);
        UpdatePosition();
    }
    
    /// <summary>
    /// Đặt góc hiện tại
    /// </summary>
    public void SetAngle(float angle)
    {
        currentAngle = angle % 360f;
        UpdatePosition();
    }
    
    /// <summary>
    /// Random góc bắt đầu
    /// </summary>
    public void RandomizeStartAngle()
    {
        currentAngle = Random.Range(0f, 360f);
        UpdatePosition();
    }
    
    /// <summary>
    /// Đổi hướng quay
    /// </summary>
    public void ReverseDirection()
    {
        rotationSpeed = -rotationSpeed;
    }
    
    /// <summary>
    /// Dừng quay
    /// </summary>
    public void Stop()
    {
        enabled = false;
    }
    
    /// <summary>
    /// Tiếp tục quay
    /// </summary>
    public void Resume()
    {
        enabled = true;
    }
}