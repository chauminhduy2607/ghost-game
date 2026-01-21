using UnityEngine;

/// <summary>
/// ⭐ VẬT LÝ THỰC TẾ: Dùng Force thay vì set Velocity!
/// - Trọng lực âm (-0.5) = đẩy nhẹ lên trên
/// - Lực tự bay (1.5) = bay lơ lửng tự nhiên
/// - Kháng không khí (1.5) = giảm tốc tự nhiên
/// - Vuốt lên = bay nhanh
/// - Thả = ngưng áp lực, vật lý tự xử lý
/// - Có quán tính, gia tốc, giảm tốc tự nhiên
/// 
/// ⭐ MỚI: Nhấn 1 cái (không vuốt) = rớt xuống!
/// </summary>
public class GhostController : MonoBehaviour
{
    [Header("=== VẬT LÝ GHOST ===")]
    [SerializeField] private float mass = 0.1f;               // ⭐ CỰC NHẸ: 0.1
    [SerializeField] private float linearDrag = 5.0f;         // ⭐ KHÁNG KHÍ RẤT CAO: 5.0 (tăng từ 3.5)
    [SerializeField] private float angularDrag = 1f;
    [SerializeField] private float gravityScale = 0f;         // ⭐ TẮT TRỌNG LỰC: 0 (từ -0.15)
    
    [Header("=== TỰ BAY LÊN ===")]
    [SerializeField] private float autoRiseForce = 0.03f;     // ⭐ LỰC TỰ BAY CỰC CHẬM: 0.03 (giảm 5 lần từ 0.15)
    [SerializeField] private float maxAutoRiseSpeed = 0.06f;  // ⭐ TỐC ĐỘ TỰ BAY TỐI ĐA: 0.06 (giảm 5 lần từ 0.3)
    
    [Header("=== RỚT XUỐNG KHI NHẤN (KHÔNG VUỐT) ===")]
    [SerializeField] private float fallForceMultiplier = 6.0f; // ⭐ HỆ SỐ RỚT: 6.0 × autoRiseForce (x4 từ 1.5)
    [SerializeField] private float tapThreshold = 0.001f;      // ⭐ NGƯỠNG: < 0.001 = tap, > 0.001 = swipe
    
    [Header("=== XOAY ĐẦU KHI RỚT ===")]
    [SerializeField] private bool enableRotation = true;       // ⭐ Bật/tắt xoay
    [SerializeField] private float rotationSpeed = 4f;         // ⭐ Tốc độ xoay MƯỢT (giảm từ 8 → 4)
    
    [Header("=== ĐIỀU KHIỂN VUỐT ===")]
    [SerializeField] private float swipeForceMultiplier = 50f; // ⭐ Hệ số lực vuốt IMPULSE: 50 (tăng từ 20)
    [SerializeField] private float maxSwipeForce = 25f;        // ⭐ GIỚI HẠN: lực vuốt tối đa 25 (tăng từ 8)
    [SerializeField] private float minSwipeDistance = 0.01f;   // ⭐ Khoảng cách tối thiểu để tính là vuốt
    
    [Header("=== TỐC ĐỘ ===")]
    [SerializeField] private float maxRiseSpeed = 5f;         // ⭐ Giới hạn tốc độ tối đa: 5 (giảm từ 8)
    
    [Header("=== HIỆU ỨNG MÓP ===")]
    [SerializeField] private bool enableSquashStretch = true;
    [SerializeField] private float squashAmount = 0.15f;
    [SerializeField] private float squashSpeed = 8f;
    [SerializeField] private float minVelocityForSquash = 2f;
    [SerializeField] private float groundSquashAmount = 0.6f;
    [SerializeField] private float groundSquashDuration = 0.35f;
    
    [Header("=== VISUAL EFFECTS ===")]
    [SerializeField] private bool enableTrail = true;
    [SerializeField] private bool enableColorChange = true;
    
    // Private variables
    private Rigidbody2D rb;
    private Camera mainCamera;
    private SpriteRenderer spriteRenderer;
    private TrailRenderer trail;
    private Color originalColor;
    
    // Vuốt
    private bool isDragging = false;
    private Vector2 dragStartPos;         // ⭐ Vị trí bắt đầu kéo
    private Vector2 lastMousePos;
    private bool hasAppliedSwipe = false; // ⭐ Đã áp dụng lực vuốt chưa (chỉ 1 lần/vuốt)
    private bool isFalling = false;       // ⭐ MỚI: Đang rớt xuống
    
    // Squash & Stretch
    private Vector3 originalScale;
    private Vector3 targetScale;
    private bool isGroundSquashing = false;
    private float groundSquashTimer = 0f;
    
    // Rotation
    private Quaternion targetRotation;  // ⭐ Góc xoay mục tiêu
    
    // Public properties
    public Rigidbody2D Rigidbody => rb;
    public bool IsGroundSquashing => isGroundSquashing;
    public Vector2 Velocity => rb.linearVelocity;
    public bool IsDragging => isDragging;
    public bool IsFalling => isFalling;  // ⭐ MỚI
    
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        mainCamera = Camera.main;
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        // Setup Rigidbody2D
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.mass = mass;
        rb.linearDamping = linearDrag;
        rb.angularDamping = angularDrag;
        rb.gravityScale = gravityScale;  // ⭐ TẮT TRỌNG LỰC
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        
        // Lưu scale gốc
        originalScale = transform.localScale;
        targetScale = originalScale;
        
        // Lưu góc xoay ban đầu (đầu hướng lên)
        targetRotation = Quaternion.Euler(0, 0, 0);
        transform.rotation = targetRotation;
        
        // Lưu màu gốc
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
        
        // Setup Trail
        if (enableTrail)
        {
            trail = GetComponent<TrailRenderer>();
            if (trail == null)
            {
                trail = gameObject.AddComponent<TrailRenderer>();
                trail.time = 0.4f;
                trail.startWidth = 0.25f;
                trail.endWidth = 0.05f;
                trail.material = new Material(Shader.Find("Sprites/Default"));
                trail.startColor = new Color(1f, 1f, 0f, 0.5f);
                trail.endColor = new Color(1f, 1f, 0f, 0f);
            }
        }
        
        Debug.Log("👻 GhostController: VẬT LÝ THỰC TẾ + NHẤN = RỚT!");
        Debug.Log($"📊 Lực tự bay: {autoRiseForce}");
        Debug.Log($"📊 Lực rớt: {autoRiseForce * fallForceMultiplier} (x6 SIÊU NHANH!)");
        Debug.Log($"📊 Tốc độ xoay: {rotationSpeed} (MƯỢT)");
        Debug.Log($"📊 Trọng lực: {gravityScale}");
        Debug.Log($"📊 Kháng không khí: {linearDrag}");
        Debug.Log($"📊 Khối lượng: {mass}");
        Debug.Log($"📊 Hệ số lực vuốt: {swipeForceMultiplier}");
    }
    
    void Update()
    {
        HandleSwipeInput();
        UpdateVisuals();
        
        if (enableSquashStretch)
        {
            UpdateSquashStretch();
        }
        
        if (enableRotation)
        {
            UpdateRotation();  // ⭐ Cập nhật xoay mượt
        }
    }
    
    void FixedUpdate()
    {
        ApplyMovement();
    }
    
    // ==================== INPUT: VUỐT ====================
    void HandleSwipeInput()
    {
        Vector2 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        
        if (Input.GetMouseButtonDown(0))
        {
            isDragging = true;
            dragStartPos = mouseWorldPos;  // ⭐ Lưu vị trí bắt đầu
            lastMousePos = mouseWorldPos;
            hasAppliedSwipe = false;       // ⭐ Reset trạng thái vuốt
            
            if (enableColorChange && spriteRenderer != null)
                spriteRenderer.color = Color.cyan;
            
            Debug.Log("🖐️ BẮT ĐẦU NHẤN!");
        }
        
        if (Input.GetMouseButton(0) && isDragging)
        {
            // Không làm gì cả - chỉ theo dõi vị trí chuột
            lastMousePos = mouseWorldPos;
        }
        
        if (Input.GetMouseButtonUp(0) && isDragging)
        {
            isDragging = false;
            
            // ⭐ TÍNH TỔNG QUÃNG ĐƯỜNG VUỐT
            Vector2 totalSwipe = mouseWorldPos - dragStartPos;
            
            if (totalSwipe.y > minSwipeDistance)
            {
                // ═══════════════════════════════════════
                // ⭐ VUỐT LÊN = ÁP DỤNG LỰC IMPULSE 1 LẦN
                // ═══════════════════════════════════════
                
                // Vuốt dài hơn = lực lớn hơn = bay xa hơn
                float swipeForce = totalSwipe.y * swipeForceMultiplier;
                swipeForce = Mathf.Min(swipeForce, maxSwipeForce);
                
                // ⭐ ÁP DỤNG LỰC IMPULSE (chỉ 1 lần)
                rb.AddForce(Vector2.up * swipeForce, ForceMode2D.Impulse);
                isFalling = false;
                
                // ⭐ XOAY ĐẦU LÊN KHI VUỐT
                if (enableRotation)
                {
                    targetRotation = Quaternion.Euler(0, 0, 0);  // Đầu hướng lên
                }
                
                // Đổi màu trail theo cường độ vuốt
                if (enableTrail && trail != null)
                {
                    float intensity = swipeForce / maxSwipeForce;
                    trail.startColor = Color.Lerp(
                        new Color(1f, 1f, 0f, 0.5f),  // Vàng nhạt (vuốt nhẹ)
                        new Color(0f, 1f, 1f, 0.9f),  // Cyan sáng (vuốt mạnh)
                        intensity
                    );
                    trail.startWidth = 0.2f + (0.3f * intensity);
                }
                
                if (enableColorChange && spriteRenderer != null)
                    spriteRenderer.color = originalColor;
                
                Debug.Log($"⬆️ VUỐT! Distance: {totalSwipe.y:F3} | Impulse: {swipeForce:F2}");
            }
            else if (Mathf.Abs(totalSwipe.y) <= tapThreshold)
            {
                // ═══════════════════════════════════════
                // ⭐ NHẤN 1 CÁI (TAP) = RỚT XUỐNG!
                // ═══════════════════════════════════════
                isFalling = true;
                
                if (enableColorChange && spriteRenderer != null)
                    spriteRenderer.color = Color.red;
                
                // ⭐ XOAY ĐẦU XUỐNG KHI RỚT
                if (enableRotation)
                {
                    targetRotation = Quaternion.Euler(0, 0, 180);  // Cắm đầu xuống
                }
                
                Debug.Log("⬇️ TAP! → BẮT ĐẦU RỚT!");
            }
            else
            {
                // ═══════════════════════════════════════
                // ⭐ VUỐT NGẮN HOẶC XUỐNG → VỀ TRẠNG THÁI TỰ BAY
                // ═══════════════════════════════════════
                isFalling = false;
                
                if (enableColorChange && spriteRenderer != null)
                    spriteRenderer.color = originalColor;
                
                // ⭐ XOAY ĐẦU LÊN KHI VỀ TRẠNG THÁI BÌNH THƯỜNG
                if (enableRotation)
                {
                    targetRotation = Quaternion.Euler(0, 0, 0);  // Đầu hướng lên
                }
                
                Debug.Log($"✋ THẢ! Total swipe: {totalSwipe.y:F3}");
            }
        }
    }
    
    // ==================== CHUYỂN ĐỘNG ====================
    void ApplyMovement()
    {
        if (isFalling)
        {
            // ⭐ ĐANG RỚT: Áp dụng lực xuống
            float fallForce = autoRiseForce * fallForceMultiplier;
            rb.AddForce(Vector2.down * fallForce, ForceMode2D.Force);
            
            if (Time.frameCount % 30 == 0)
            {
                Debug.Log($"⬇️ RỚT! Force: {fallForce:F3} | Velocity Y: {rb.linearVelocity.y:F2}");
            }
        }
        else if (!isDragging)
        {
            // ⭐ CHỈ ÁP DỤNG LỰC TỰ BAY NẾU CHƯA ĐẠT TỐC ĐỘ TỐI ĐA
            if (rb.linearVelocity.y < maxAutoRiseSpeed)
            {
                rb.AddForce(Vector2.up * autoRiseForce, ForceMode2D.Force);
            }
        }
        
        // ⭐ GIỚI HẠN TỐC ĐỘ TỐI ĐA KHI VUỐT
        if (rb.linearVelocity.y > maxRiseSpeed)
        {
            Vector2 vel = rb.linearVelocity;
            vel.y = maxRiseSpeed;
            rb.linearVelocity = vel;
        }
        
        // Debug
        if (Time.frameCount % 30 == 0 && !isFalling)
        {
            string status = isDragging ? "ĐANG VUỐT" : "TỰ BAY";
            Debug.Log($"🎯 {status} | Velocity Y: {rb.linearVelocity.y:F2} | Max Auto: {maxAutoRiseSpeed}");
        }
    }
    
    // ==================== XOAY ĐẦU ====================
    void UpdateRotation()
    {
        // ⭐ XOAY MƯỢT với Lerp
        transform.rotation = Quaternion.Lerp(
            transform.rotation,
            targetRotation,
            Time.deltaTime * rotationSpeed
        );
    }
    
    // ==================== SQUASH & STRETCH ====================
    void UpdateSquashStretch()
    {
        if (isGroundSquashing)
        {
            groundSquashTimer += Time.deltaTime;
            
            if (groundSquashTimer < groundSquashDuration)
            {
                float progress = groundSquashTimer / groundSquashDuration;
                float squashCurve = Mathf.Pow(1f - progress, 2f);
                
                targetScale = new Vector3(
                    originalScale.x * (1f + groundSquashAmount * squashCurve),
                    originalScale.y * (1f - groundSquashAmount * 0.8f * squashCurve),
                    originalScale.z
                );
                
                transform.localScale = targetScale;
                return;
            }
            else
            {
                isGroundSquashing = false;
                groundSquashTimer = 0f;
            }
        }
        
        float velocityY = rb.linearVelocity.y;
        
        // Hiệu ứng bay lên
        if (velocityY > minVelocityForSquash)
        {
            float squashFactor = Mathf.Clamp(velocityY / 10f, 0f, 1f);
            float squash = squashAmount * squashFactor;
            
            targetScale = new Vector3(
                originalScale.x * (1f - squash),
                originalScale.y * (1f + squash),
                originalScale.z
            );
        }
        // Hiệu ứng rớt xuống
        else if (velocityY < -minVelocityForSquash)
        {
            float squashFactor = Mathf.Clamp(-velocityY / 10f, 0f, 1f);
            float squash = squashAmount * squashFactor;
            
            targetScale = new Vector3(
                originalScale.x * (1f + squash * 0.5f),
                originalScale.y * (1f - squash * 0.5f),
                originalScale.z
            );
        }
        // Bình thường
        else
        {
            targetScale = originalScale;
        }
        
        transform.localScale = Vector3.Lerp(
            transform.localScale, 
            targetScale, 
            Time.deltaTime * squashSpeed
        );
    }
    
    public void TriggerGroundSquash()
    {
        if (!enableSquashStretch) return;
        
        isGroundSquashing = true;
        groundSquashTimer = 0f;
        Debug.Log("💥 CHẠM ĐẤT - BẸT!");
    }
    
    // ==================== VISUAL ====================
    void UpdateVisuals()
    {
        if (!enableColorChange || spriteRenderer == null) return;
        
        // Đổi màu theo tốc độ
        float totalSpeed = rb.linearVelocity.y;
        
        if (isFalling)
        {
            // Khi rớt = màu đỏ
            spriteRenderer.color = Color.Lerp(spriteRenderer.color, Color.red, Time.deltaTime * 5f);
        }
        else if (isDragging)
        {
            // Khi vuốt = màu cyan
            spriteRenderer.color = Color.Lerp(spriteRenderer.color, Color.cyan, Time.deltaTime * 5f);
        }
        else if (totalSpeed > 3f)
        {
            // Bay nhanh = màu xanh
            spriteRenderer.color = Color.Lerp(spriteRenderer.color, Color.green, Time.deltaTime * 3f);
        }
        else
        {
            // Bay chậm = màu gốc
            spriteRenderer.color = Color.Lerp(spriteRenderer.color, originalColor, Time.deltaTime * 3f);
        }
        
        // Trail theo tốc độ
        if (enableTrail && trail != null)
        {
            if (isFalling)
            {
                trail.startWidth = 0.15f;
                trail.startColor = new Color(1f, 0.3f, 0f, 0.6f);  // Cam đỏ
            }
            else if (totalSpeed > 4f)
            {
                trail.startWidth = 0.35f;
                trail.startColor = new Color(0f, 1f, 1f, 0.8f);
            }
            else
            {
                trail.startWidth = 0.2f;
                trail.startColor = new Color(1f, 1f, 0f, 0.5f);
            }
        }
    }
    
    // ==================== PUBLIC METHODS ====================
    public void ResetVelocity()
    {
        rb.linearVelocity = Vector2.zero;
        hasAppliedSwipe = false;
        isFalling = false;
    }
    
    public void SetVelocity(Vector2 velocity)
    {
        rb.linearVelocity = velocity;
    }
    
    public void SetAutoRiseForce(float force)
    {
        autoRiseForce = force;
        Debug.Log($"⬆️ Lực tự bay: {force}");
    }
    
    public void AddImpulse(Vector2 force)
    {
        rb.AddForce(force, ForceMode2D.Impulse);
    }
    
    public void EnablePhysics(bool enable)
    {
        rb.simulated = enable;
    }
    
    // ⭐ MỚI: Dừng rớt (gọi từ bên ngoài nếu cần)
    public void StopFalling()
    {
        isFalling = false;
        if (enableColorChange && spriteRenderer != null)
            spriteRenderer.color = originalColor;
        
        // ⭐ XOAY ĐẦU LÊN KHI DỪNG RỚT
        if (enableRotation)
        {
            targetRotation = Quaternion.Euler(0, 0, 0);
        }
        
        Debug.Log("⏹️ DỪNG RỚT!");
    }
}