using UnityEngine;

/// <summary>
/// ⭐ CAMERA THEO MA - DI CHUYỂN MỀM MẠI
/// - Camera chỉ theo trục Y (lên xuống)
/// - Khi ma RỚT: camera theo NGAY (không offset, không dead zone)
/// - Khi ma BAY LÊN: có offset + dead zone
/// - Tốc độ theo mượt, không giật
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [Header("=== TARGET ===")]
    [SerializeField] private Transform target;  // Con ma
    
    [Header("=== TỐC ĐỘ THEO ===")]
    [Tooltip("Tốc độ camera theo ma. Càng nhỏ = càng chậm, càng mượt")]
    [SerializeField] private float smoothSpeed = 3f;  // 3 = chậm, mượt
    
    [Header("=== DEAD ZONE (Vùng Chết) ===")]
    [Tooltip("Khoảng cách ma phải di chuyển trước khi camera bắt đầu theo")]
    [SerializeField] private float deadZoneHeight = 0f;  // Ma phải bay cao hơn 2 đơn vị
    
    [Header("=== OFFSET ===")]
    [Tooltip("Khoảng cách camera cách ma (theo trục Y) - CHỈ ÁP DỤNG KHI BAY LÊN")]
    [SerializeField] private float yOffset = 5f;  // Camera ở dưới ma 5 đơn vị khi bay lên
    
    [Header("=== GIỚI HẠN (Tùy Chọn) ===")]
    [SerializeField] private bool useMinY = false;
    [SerializeField] private float minY = -10f;  // Camera không xuống quá thấp
    
    [SerializeField] private bool useMaxY = false;
    [SerializeField] private float maxY = 100f;  // Camera không lên quá cao
    
    [Header("=== DEBUG ===")]
    [SerializeField] private bool showDebugInfo = true;
    
    // Private
    private Vector3 velocity = Vector3.zero;  // Dùng cho SmoothDamp
    private float initialZ;  // Vị trí Z ban đầu của camera
    private Rigidbody2D targetRb;  // Rigidbody của ma để lấy velocity
    private GhostController ghostController;
    
    void Start()
    {
        // Tự động tìm GhostController nếu chưa gán
        if (target == null)
        {
            GhostController ghost = FindObjectOfType<GhostController>();
            if (ghost != null)
            {
                target = ghost.transform;
                ghostController = ghost;
                Debug.Log("✅ Đã tự động tìm thấy Ghost!");
            }
            else
            {
                Debug.LogError("❌ Không tìm thấy Ghost! Hãy gán Target trong Inspector.");
                enabled = false;
                return;
            }
        }
        
        // Lấy Rigidbody2D của ma
        targetRb = target.GetComponent<Rigidbody2D>();
        if (targetRb == null)
        {
            Debug.LogWarning("⚠️ Ma không có Rigidbody2D! Camera sẽ luôn theo.");
        }
        
        // Lưu vị trí Z ban đầu (camera 2D thường ở Z = -10)
        initialZ = transform.position.z;
        
        Debug.Log("📷 CameraFollow: Sẵn sàng!");
        Debug.Log($"📊 Tốc độ theo: {smoothSpeed}");
        Debug.Log($"📊 Dead Zone: {deadZoneHeight}");
        Debug.Log($"📊 Y Offset (khi bay lên): {yOffset}");
    }
    
    void LateUpdate()
    {
        if (target == null) return;
        
        // ⭐ Ma chạm vật cản → camera dừng follow
        if (ghostController != null && ghostController.HitObstacle)
            return;
        
        // ⭐ KIỂM TRA MA ĐANG RỚT HAY BAY LÊN (qua velocity)
        bool isFalling = false;
        if (targetRb != null)
        {
            isFalling = targetRb.linearVelocity.y < -0.1f;  // Velocity âm = đang rơi
        }
        
        // ⭐ TÍNH VỊ TRÍ MỤC TIÊU (offset khác nhau tùy trạng thái)
        float targetY;
        if (isFalling)
        {
            // Ma đang rơi → KHÔNG CÓ OFFSET, theo sát luôn
            targetY = target.position.y;
            
            if (showDebugInfo)
            {
                Debug.Log($"📷 MA ĐANG RƠI! Velocity.y = {targetRb.linearVelocity.y:F2} → Camera theo KHÔNG OFFSET!");
            }
        }
        else
        {
            // Ma bay lên hoặc đứng yên → CÓ OFFSET
            targetY = target.position.y + yOffset;
            
            // Áp dụng dead zone
            float distanceToTarget = targetY - transform.position.y;
            
            if (Mathf.Abs(distanceToTarget) < deadZoneHeight)
            {
                // Ma vẫn trong vùng dead zone → Camera không di chuyển
                if (showDebugInfo)
                {
                    Debug.Log($"📷 Dead Zone: Ma trong vùng an toàn ({Mathf.Abs(distanceToTarget):F1} < {deadZoneHeight})");
                }
                return;
            }
        }
        
        // ⭐ TÍNH VỊ TRÍ MỚI (Smooth)
        Vector3 desiredPosition = new Vector3(
            transform.position.x,  // Giữ nguyên X
            targetY,
            initialZ  // Giữ nguyên Z
        );
        
        // ⭐ DI CHUYỂN MỀM MẠI với Lerp
        Vector3 smoothedPosition = Vector3.Lerp(
            transform.position,
            desiredPosition,
            smoothSpeed * Time.deltaTime
        );
        
        // ⭐ GIỚI HẠN (nếu bật)
        if (useMinY)
            smoothedPosition.y = Mathf.Max(smoothedPosition.y, minY);
        
        if (useMaxY)
            smoothedPosition.y = Mathf.Min(smoothedPosition.y, maxY);
        
        // Áp dụng vị trí mới
        transform.position = smoothedPosition;
    }
    
    
    // ==================== PUBLIC METHODS ====================
    
    /// <summary>
    /// Đặt tốc độ camera theo ma
    /// </summary>
    public void SetSmoothSpeed(float speed)
    {
        smoothSpeed = speed;
        Debug.Log($"📷 Đổi tốc độ camera: {speed}");
    }
    
    /// <summary>
    /// Đặt dead zone (vùng chết)
    /// </summary>
    public void SetDeadZone(float height)
    {
        deadZoneHeight = height;
        Debug.Log($"📷 Đổi dead zone: {height}");
    }
    
    /// <summary>
    /// Dịch chuyển camera ngay lập tức (không smooth)
    /// </summary>
    public void SnapToTarget()
    {
        if (target == null) return;
        
        Vector3 pos = transform.position;
        pos.y = target.position.y + yOffset;
        transform.position = pos;
        
        Debug.Log("📷 Camera nhảy đến vị trí ma!");
    }
    
    /// <summary>
    /// Đặt target mới
    /// </summary>
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        targetRb = newTarget.GetComponent<Rigidbody2D>();
        Debug.Log($"📷 Đổi target: {newTarget.name}");
    }
}