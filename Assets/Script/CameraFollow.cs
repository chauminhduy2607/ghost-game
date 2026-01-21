using UnityEngine;

/// <summary>
/// ⭐ CAMERA THEO MA - DI CHUYỂN MỀM MẠI
/// - Camera chỉ theo trục Y (lên xuống)
/// - Tốc độ theo mượt, không giật
/// - Có vùng "dead zone" để camera không nhảy liên tục
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
    [SerializeField] private float deadZoneHeight = 2f;  // Ma phải bay cao hơn 2 đơn vị
    
    [Header("=== OFFSET ===")]
    [Tooltip("Khoảng cách camera cách ma (theo trục Y)")]
    [SerializeField] private float yOffset = 0f;  // Camera ngang tầm với ma
    
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
    
    void Start()
    {
        // Tự động tìm GhostController nếu chưa gán
        if (target == null)
        {
            GhostController ghost = FindObjectOfType<GhostController>();
            if (ghost != null)
            {
                target = ghost.transform;
                Debug.Log("✅ Đã tự động tìm thấy Ghost!");
            }
            else
            {
                Debug.LogError("❌ Không tìm thấy Ghost! Hãy gán Target trong Inspector.");
                enabled = false;
                return;
            }
        }
        
        // Lưu vị trí Z ban đầu (camera 2D thường ở Z = -10)
        initialZ = transform.position.z;
        
        Debug.Log("📷 CameraFollow: Sẵn sàng!");
        Debug.Log($"📊 Tốc độ theo: {smoothSpeed}");
        Debug.Log($"📊 Dead Zone: {deadZoneHeight}");
        Debug.Log($"📊 Y Offset: {yOffset}");
    }
    
    void LateUpdate()
    {
        if (target == null) return;
        
        // Tính vị trí mục tiêu của camera
        float targetY = target.position.y + yOffset;
        
        // ⭐ DEAD ZONE: Chỉ theo khi ma di chuyển đủ xa
        float distanceToTarget = targetY - transform.position.y;
        
        if (Mathf.Abs(distanceToTarget) < deadZoneHeight)
        {
            // Ma vẫn trong vùng dead zone → Camera không di chuyển
            return;
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
    
    void OnGUI()
    {
        if (!showDebugInfo || !Application.isPlaying) return;
        
        GUIStyle style = new GUIStyle();
        style.fontSize = 18;
        style.normal.textColor = Color.yellow;
        style.alignment = TextAnchor.UpperRight;
        style.fontStyle = FontStyle.Bold;
        
        string info = "📷 CAMERA INFO\n";
        info += "━━━━━━━━━━━━━━\n";
        info += $"Camera Y: {transform.position.y:F1}\n";
        
        if (target != null)
        {
            float distance = target.position.y - transform.position.y;
            info += $"Ghost Y: {target.position.y:F1}\n";
            info += $"Distance: {distance:F1}\n";
            
            if (Mathf.Abs(distance) < deadZoneHeight)
                info += "⏸️ DEAD ZONE";
            else
                info += "▶️ ĐANG THEO";
        }
        
        GUI.Label(new Rect(Screen.width - 250, 10, 240, 200), info, style);
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
        Debug.Log($"📷 Đổi target: {newTarget.name}");
    }
}