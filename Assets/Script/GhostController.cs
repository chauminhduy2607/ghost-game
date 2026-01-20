using UnityEngine;

/// <summary>
/// ⭐ VẬT LÝ THỰC TẾ: Dùng Force thay vì set Velocity!
/// - Trọng lực âm (-0.5) = đẩy nhẹ lên trên
/// - Lực tự bay (1.5) = bay lơ lửng tự nhiên
/// - Kháng không khí (1.5) = giảm tốc tự nhiên
/// - Vuốt = áp lực theo hướng vuốt
/// - Thả = ngưng áp lực, vật lý tự xử lý
/// - Có quán tính, gia tốc, giảm tốc tự nhiên
/// </summary>
public class GhostController : MonoBehaviour
{
    [Header("=== VẬT LÝ GHOST ===")]
    [SerializeField] private float mass = 0.1f;               // ⭐ CỰC NHẸ: 0.1
    [SerializeField] private float linearDrag = 5.0f;         // ⭐ KHÁNG KHÍ RẤT CAO: 5.0 (tăng từ 3.5)
    [SerializeField] private float angularDrag = 1f;
    [SerializeField] private float gravityScale = 0f;         // ⭐ TẮT TRỌNG LỰC: 0 (từ -0.15)
    
    [Header("=== TỰ BAY LÊN ===")]
    [SerializeField] private float autoRiseForce = 0.3f;      // ⭐ LỰC TỰ BAY CỰC NHẸ: 0.3 (giảm từ 0.8)
    [SerializeField] private float maxAutoRiseSpeed = 0.5f;   // ⭐ TỐC ĐỘ TỰ BAY TỐI ĐA: 0.5
    
    [Header("=== ĐIỀU KHIỂN VUỐT ===")]
    [SerializeField] private float swipeForceMultiplier = 100f; // ⭐ Hệ số lực vuốt: 100 (giảm từ 150)
    [SerializeField] private float maxSwipeForce = 15f;       // ⭐ GIỚI HẠN: lực vuốt tối đa 15 (giảm từ 20)
    
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
    private Vector2 lastMousePos;
    private bool isApplyingSwipeForce = false;  // ⭐ Đang áp dụng lực vuốt
    
    // Squash & Stretch
    private Vector3 originalScale;
    private Vector3 targetScale;
    private bool isGroundSquashing = false;
    private float groundSquashTimer = 0f;
    
    // Public properties
    public Rigidbody2D Rigidbody => rb;
    public bool IsGroundSquashing => isGroundSquashing;
    public Vector2 Velocity => rb.linearVelocity;
    public bool IsDragging => isDragging;
    
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
        rb.gravityScale = gravityScale;  // ⭐ TRỌNG LỰC ÂM = đẩy nhẹ lên trên
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        
        // Lưu scale gốc
        originalScale = transform.localScale;
        targetScale = originalScale;
        
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
        
        Debug.Log("👻 GhostController: VẬT LÝ THỰC TẾ!");
        Debug.Log($"📊 Lực tự bay: {autoRiseForce}");
        Debug.Log($"📊 Trọng lực: {gravityScale} (âm = đẩy lên)");
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
            lastMousePos = mouseWorldPos;
            
            if (enableColorChange && spriteRenderer != null)
                spriteRenderer.color = Color.cyan;
            
            Debug.Log("🖐️ BẮT ĐẦU VUỐT!");
        }
        
        if (Input.GetMouseButton(0) && isDragging)
        {
            // Tính khoảng cách vuốt
            Vector2 delta = mouseWorldPos - lastMousePos;
            
            // ⭐ CHỈ TÍNH VUỐT LÊN (Y > 0)
            if (delta.y > 0.001f)
            {
                // ⭐ VUỐT MẠNH = delta.y lớn → force lớn
                // ⭐ VUỐT NHẸ = delta.y nhỏ → force nhỏ
                float swipeForce = delta.y * swipeForceMultiplier;
                
                // Giới hạn lực tối đa
                swipeForce = Mathf.Min(swipeForce, maxSwipeForce);
                
                // ⭐ ÁP DỤNG LỰC (không phải set velocity)
                rb.AddForce(Vector2.up * swipeForce, ForceMode2D.Force);
                isApplyingSwipeForce = true;
                
                // Đổi màu trail khi vuốt
                if (enableTrail && trail != null)
                {
                    float intensity = swipeForce / maxSwipeForce;
                    trail.startColor = Color.Lerp(
                        new Color(1f, 1f, 0f, 0.5f),  // Vàng nhạt (vuốt nhẹ)
                        new Color(0f, 1f, 1f, 0.9f),  // Cyan sáng (vuốt mạnh)
                        intensity
                    );
                    trail.startWidth = 0.2f + (0.2f * intensity);
                }
                
                Debug.Log($"⬆️ VUỐT! Delta: {delta.y:F3} | Force: {swipeForce:F2}");
            }
            
            lastMousePos = mouseWorldPos;
        }
        
        if (Input.GetMouseButtonUp(0) && isDragging)
        {
            isDragging = false;
            isApplyingSwipeForce = false;
            
            if (enableColorChange && spriteRenderer != null)
                spriteRenderer.color = originalColor;
            
            Debug.Log($"✋ THẢ! Ngưng áp lực vuốt");
        }
    }
    
    // ==================== CHUYỂN ĐỘNG ====================
    void ApplyMovement()
    {
        // ⭐ CHỈ ÁP DỤNG LỰC TỰ BAY NẾU CHƯA ĐẠT TỐC ĐỘ TỐI ĐA
        if (rb.linearVelocity.y < maxAutoRiseSpeed && !isDragging)
        {
            rb.AddForce(Vector2.up * autoRiseForce, ForceMode2D.Force);
        }
        
        // ⭐ GIỚI HẠN TỐC ĐỘ TỐI ĐA KHI VUỐT
        if (rb.linearVelocity.y > maxRiseSpeed)
        {
            Vector2 vel = rb.linearVelocity;
            vel.y = maxRiseSpeed;
            rb.linearVelocity = vel;
        }
        
        // Debug
        if (Time.frameCount % 30 == 0)
        {
            string status = isDragging ? "ĐANG VUỐT" : "TỰ BAY";
            Debug.Log($"🎯 {status} | Velocity Y: {rb.linearVelocity.y:F2} | Max Auto: {maxAutoRiseSpeed}");
        }
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
        
        if (isDragging)
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
            if (totalSpeed > 4f)
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
        isApplyingSwipeForce = false;
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
}