using UnityEngine;

/// <summary>
/// ⭐ ANIMATION LẮC LƯ - CHO OBJECT CON (VISUAL)
/// - QUAN TRỌNG: Script này PHẢI gắn vào OBJECT CON (sprite), KHÔNG phải object cha (physics)!
/// - Chỉ lo animation visual, không ảnh hưởng physics
/// - Lắc ngang như bị gió thổi
/// - Nhấp nhô lên xuống như đang lơ lửng
/// - Nghiêng qua nghiêng lại
/// 
/// CÁCH DÙNG:
/// 1. Tạo Empty GameObject con trong "ghost"
/// 2. Di chuyển Sprite vào object con này
/// 3. Gắn script FloatingAnimation vào object con (có sprite)
/// 4. GhostController vẫn ở object cha (ghost)
/// </summary>
public class FloatingAnimation : MonoBehaviour
{
    [Header("=== BẬT/TẮT ANIMATION ===")]
    [SerializeField] private bool enableAnimation = true;
    
    [Header("=== LẮC NGANG (Sway) ===")]
    [SerializeField] private bool enableSway = true;
    [SerializeField] private float swayAmount = 0.7f;         // ⭐ Độ lắc ngang: 0.7 (tăng từ 0.4) - CỰC MẠNH!
    [SerializeField] private float swaySpeed = 2.2f;          // ⭐ Tốc độ lắc: 2.2 (tăng từ 1.5) - NHANH!
    
    [Header("=== NHẤP NHÔ (Bob) ===")]
    [SerializeField] private bool enableBob = true;
    [SerializeField] private float bobAmount = 0.5f;          // ⭐ Độ lên xuống: 0.5 (tăng từ 0.3) - MẠNH!
    [SerializeField] private float bobSpeed = 2.8f;           // ⭐ Tốc độ nhấp nhô: 2.8 (tăng từ 2.0) - CỰC NHANH!
    
    [Header("=== NGHIÊNG (Tilt) ===")]
    [SerializeField] private bool enableTilt = true;
    [SerializeField] private float tiltAmount = 18f;          // ⭐ Độ nghiêng: 18 độ (tăng từ 10) - NGHIÊNG MẠNH!
    [SerializeField] private float tiltSpeed = 1.8f;          // ⭐ Tốc độ nghiêng: 1.8 (tăng từ 1.3)
    
    [Header("=== PHÓNG TO/THU NHỎ (Scale Pulse) ===")]
    [SerializeField] private bool enableScalePulse = true;    // ⭐ BẬT
    [SerializeField] private float scaleAmount = 0.15f;       // ⭐ Độ phóng to/nhỏ: 0.15
    [SerializeField] private float scaleSpeed = 3.5f;         // ⭐ Tốc độ: 3.5
    
    // Private variables
    private Vector3 startPosition;
    private Vector3 startScale;
    private Quaternion startRotation;
    
    private float swayTimer = 0f;
    private float bobTimer = 0f;
    private float tiltTimer = 0f;
    private float scaleTimer = 0f;
    
    void Start()
    {
        // ⭐ Lưu vị trí, scale, rotation ban đầu
        startPosition = transform.localPosition;
        startScale = transform.localScale;
        startRotation = transform.localRotation;
        
        // Random timer để mỗi object không sync
        swayTimer = Random.Range(0f, 2f * Mathf.PI);
        bobTimer = Random.Range(0f, 2f * Mathf.PI);
        tiltTimer = Random.Range(0f, 2f * Mathf.PI);
        scaleTimer = Random.Range(0f, 2f * Mathf.PI);
        
        Debug.Log($"🌊 FloatingAnimation: Khởi động trên {gameObject.name}!");
        Debug.Log($"   Sway: {(enableSway ? "ON" : "OFF")} | Bob: {(enableBob ? "ON" : "OFF")} | Tilt: {(enableTilt ? "ON" : "OFF")}");
        
        // ⭐ CẢNH BÁO nếu gắn sai chỗ
        if (GetComponent<Rigidbody2D>() != null)
        {
            Debug.LogWarning("⚠️ FloatingAnimation KHÔNG NÊN gắn cùng object với Rigidbody2D!");
            Debug.LogWarning("   Hãy tạo object con và gắn script vào đó!");
        }
    }
    
    void Update()
    {
        if (!enableAnimation) return;
        
        // Tăng timer
        swayTimer += Time.deltaTime * swaySpeed;
        bobTimer += Time.deltaTime * bobSpeed;
        tiltTimer += Time.deltaTime * tiltSpeed;
        scaleTimer += Time.deltaTime * scaleSpeed;
        
        // ⭐ TÍNH TOÁN OFFSET
        Vector3 offset = Vector3.zero;
        
        // Lắc ngang
        if (enableSway)
        {
            offset.x = Mathf.Sin(swayTimer) * swayAmount;
        }
        
        // Nhấp nhô
        if (enableBob)
        {
            offset.y = Mathf.Sin(bobTimer) * bobAmount;
        }
        
        // ⭐ ÁP DỤNG VỊ TRÍ (LOCAL - chỉ ảnh hưởng object con này)
        transform.localPosition = startPosition + offset;
        
        // ⭐ NGHIÊNG
        if (enableTilt)
        {
            float tiltAngle = Mathf.Sin(tiltTimer) * tiltAmount;
            transform.localRotation = startRotation * Quaternion.Euler(0, 0, tiltAngle);
        }
        
        // ⭐ PHÓNG TO/THU NHỎ
        if (enableScalePulse)
        {
            float scaleFactor = 1f + Mathf.Sin(scaleTimer) * scaleAmount;
            transform.localScale = startScale * scaleFactor;
        }
    }
    
    // ==================== PUBLIC METHODS ====================
    
    /// <summary>
    /// Bật/tắt animation
    /// </summary>
    public void SetAnimationEnabled(bool enabled)
    {
        enableAnimation = enabled;
        
        // Khi tắt, reset về vị trí gốc
        if (!enabled)
        {
            transform.localPosition = startPosition;
            transform.localRotation = startRotation;
            transform.localScale = startScale;
        }
    }
    
    /// <summary>
    /// Đặt lại vị trí gốc (gọi khi object di chuyển)
    /// </summary>
    public void ResetStartPosition()
    {
        startPosition = transform.localPosition;
        startScale = transform.localScale;
        startRotation = transform.localRotation;
    }
    
    /// <summary>
    /// Thay đổi cường độ lắc lư
    /// </summary>
    public void SetIntensity(float intensity)
    {
        swayAmount = 0.2f * intensity;
        bobAmount = 0.15f * intensity;
        tiltAmount = 5f * intensity;
    }
}