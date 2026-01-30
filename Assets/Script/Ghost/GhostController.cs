using UnityEngine;
using UnityEngine.SceneManagement;



/// <summary>
/// ⭐ VẬT LÝ THỰC TẾ: Dùng Force thay vì set Velocity!
/// - Trọng lực âm (-0.5) = đẩy nhẹ lên trên
/// - Lực tự bay (1.5) = bay lơ lửng tự nhiên
/// - Kháng không khí (1.5) = giảm tốc tự nhiên
/// - Vuốt lên = bay nhanh
/// - Thả = ngưng áp lực, vật lý tự xử lý
/// - Có quán tính, gia tốc, giảm tốc tự nhiên
/// 
/// ⭐⭐ BỔ SUNG:
/// - Chạm vật cản = tự động rớt + khóa vuốt lên
/// - Rớt 3 giây sau khi chạm vật cản = Game Over
/// 
/// ⭐⭐⭐ CẬP NHẬT MỚI NHẤT:
/// - Mass: 0.01 (cực nhẹ)
/// - Linear Drag: 0.5 (gần như không kháng)
/// - Swipe Force Multiplier: 300 (VUỐT CỰC MẠNH!)
/// - Max Swipe Force: 100 (KHÔNG GIỚI HẠN!)
/// - ❌ ĐÃ BỎ: Tính năng nhấn 1 cái = rớt xuống
/// → CHỈ VUỐT LÊN ĐỂ BAY!
/// </summary>
public class GhostController : MonoBehaviour
{
    [Header("=== VẬT LÝ GHOST ===")]
    [SerializeField] private float mass = 0.01f;
    [SerializeField] private float linearDrag = 0.5f;
    [SerializeField] private float angularDrag = 1f;
    [SerializeField] private float gravityScale = 0f;
    
    [Header("=== TỰ BAY LÊN ===")]
    [SerializeField] private float autoRiseForce = 0.25f;
    [SerializeField] private float maxAutoRiseSpeed = 0.5f;
    
    [Header("=== ⭐⭐ VẬT CẢN - GAME OVER ===")]
    [SerializeField] private float obstacleKnockbackForce = 15f;
    [SerializeField] private float timeBeforeGameOver = 3f;
    [SerializeField] private string obstacleTag = "Obstacle";
    [SerializeField] private float obstacleFallForceMultiplier = 30.0f; // Lực rớt sau khi chạm vật cản
    
    [Header("=== XOAY ĐẦU KHI RỚT ===")]
    [SerializeField] private bool enableRotation = true;
    [SerializeField] private float rotationSpeed = 4f;
    
    [Header("=== ĐIỀU KHIỂN VUỐT ===")]
    [SerializeField] private float swipeForceMultiplier = 300f;
    [SerializeField] private float maxSwipeForce = 100f;
    [SerializeField] private float swipeThreshold = 0.3f; // Khoảng cách tối thiểu để tính là vuốt
    
    [Header("=== TỐC ĐỘ ===")]
    [SerializeField] private float maxRiseSpeed = 20f;
    
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
    private Vector2 dragStartPos;
    private Vector2 lastMousePos;
    private bool hasAppliedSwipe = false;
    
    // ⭐⭐ VẬT CẢN - TRẠNG THÁI
    private bool hitObstacle = false;
    private bool isGameOver = false;
    private float obstacleTimer = 0f;
    
    // Squash & Stretch
    private Vector3 originalScale;
    private Vector3 targetScale;
    private bool isGroundSquashing = false;
    private float groundSquashTimer = 0f;
    
    // Rotation
    private Quaternion targetRotation;
    
    // Public properties
    public Rigidbody2D Rigidbody => rb;
    public bool IsGroundSquashing => isGroundSquashing;
    public Vector2 Velocity => rb.linearVelocity;
    public bool IsDragging => isDragging;
    public bool HitObstacle => hitObstacle;
    public bool IsGameOver => isGameOver;
    public float ObstacleTimer => obstacleTimer;
   
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
        rb.gravityScale = gravityScale;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        
        // Lưu scale gốc
        originalScale = transform.localScale;
        targetScale = originalScale;
        
        // Lưu góc xoay ban đầu
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
        
        Debug.Log("👻 GhostController: VẬT LÝ THỰC TẾ - CHỈ VUỐT ĐỂ BAY!");
        Debug.Log("📊 Mass: " + mass + " (CỰC NHẸ: 0.01!)");
        Debug.Log("📊 Linear Drag: " + linearDrag + " (GẦN NHƯ KHÔNG KHÁNG: 0.5!)");
        Debug.Log("📊 Auto Rise Force: " + autoRiseForce + " (TỰ NỔI MẠNH: 0.25!)");
        Debug.Log("📊 Swipe Force: x" + swipeForceMultiplier + " (⭐⭐⭐ CỰC MẠNH: 300!)");
        Debug.Log("📊 Max Swipe Force: " + maxSwipeForce + " (⭐⭐⭐ KHÔNG GIỚI HẠN: 100!)");
        Debug.Log("⭐⭐ PHÁT HIỆN VẬT CẢN QUA TAG: '" + obstacleTag + "'");
        Debug.Log("✅ Chỉ vuốt lên để bay - KHÔNG CÓ nhấn = rớt!");
    }
    
    void Update()
    {
        // ⭐⭐ KIỂM TRA GAME OVER
        if (isGameOver)
        {
            return;
        }
        
        // ⭐⭐ ĐẾM THỜI GIAN SAU KHI CHẠM VẬT CẢN
        if (hitObstacle)
        {
            obstacleTimer += Time.deltaTime;
            
            if (obstacleTimer >= timeBeforeGameOver)
            {
                TriggerGameOver();
                return;
            }
        }
        
        HandleSwipeInput();
        UpdateVisuals();
        
        if (enableSquashStretch)
        {
            UpdateSquashStretch();
        }
        
        if (enableRotation)
        {
            UpdateRotation();
        }
    }
    
    void FixedUpdate()
    {
        if (isGameOver) return;
        
        ApplyMovement();
    }
    
    // ⭐⭐ PHÁT HIỆN VA CHẠM VỚI VẬT CẢN
    void OnCollisionEnter2D(Collision2D collision)
    {
        Debug.Log("💥 VA CHẠM VỚI: " + collision.gameObject.name + " (Tag: " + collision.gameObject.tag + ")");
        
        if (collision.gameObject.CompareTag(obstacleTag))
        {
            if (!hitObstacle)
            {
                OnObstacleHit(collision);
            }
        }
    }
    
    // ⭐⭐ XỬ LÝ KHI CHẠM VẬT CẢN
    void OnObstacleHit(Collision2D collision)
    {
        hitObstacle = true;
        obstacleTimer = 0f;
        
        // ⭐⭐ LƯU VỊ TRÍ NGAY KHI VA CHẠM (để Continue)
        PlayerPrefs.SetFloat("RespawnX", transform.position.x);
        PlayerPrefs.SetFloat("RespawnY", transform.position.y);
        PlayerPrefs.SetFloat("RespawnVelocityY", rb.linearVelocity.y);
        PlayerPrefs.Save();
        
        Debug.Log("💾 SAVED RESPAWN POSITION: " + transform.position);
        
        // Tính hướng đẩy lùi (ngược với hướng va chạm)
        Vector2 knockbackDirection = (transform.position - collision.transform.position).normalized;
        
        // Áp dụng lực đẩy lùi
        rb.linearVelocity = Vector2.zero;
        rb.AddForce(knockbackDirection * obstacleKnockbackForce, ForceMode2D.Impulse);
        
        // Đổi màu thành đỏ sẫm
        if (enableColorChange && spriteRenderer != null)
        {
            spriteRenderer.color = new Color(0.8f, 0f, 0f, 1f);
        }
        
        // Xoay đầu xuống
        if (enableRotation)
        {
            targetRotation = Quaternion.Euler(0, 0, 180);
        }
        
        Debug.Log("💥💥💥 CHẠM VẬT CẢN! Bắt đầu đếm ngược " + timeBeforeGameOver + "s...");
    }
    
    // ⭐⭐ GAME OVER
    void TriggerGameOver()
    {
        isGameOver = true;
        rb.linearVelocity = Vector2.zero;
        rb.simulated = false;
        
        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.black;
        }
        
        Debug.Log("☠️☠️☠️ GAME OVER! ☠️☠️☠️");
        
        // TODO: Gọi UI Game Over hoặc Scene Manager
        // GameManager.Instance.ShowGameOver();
    }
    
    // ==================== INPUT: VUỐT ====================
    void HandleSwipeInput()
    {
        // ⭐⭐ NẾU ĐÃ CHẠM VẬT CẢN = KHÔNG CHO VUỐT
        if (hitObstacle)
        {
            isDragging = false;
            return;
        }
        
        Vector2 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        
        if (Input.GetMouseButtonDown(0))
        {
            isDragging = true;
            dragStartPos = mouseWorldPos;
            lastMousePos = mouseWorldPos;
            hasAppliedSwipe = false;
            
            if (enableColorChange && spriteRenderer != null)
                spriteRenderer.color = Color.cyan;
            
            Debug.Log("🖐️ BẮT ĐẦU VUỐT!");
        }
        
        if (Input.GetMouseButton(0) && isDragging)
        {
            lastMousePos = mouseWorldPos;
        }
        
        if (Input.GetMouseButtonUp(0) && isDragging)
        {
            isDragging = false;
            
            Vector2 totalSwipe = mouseWorldPos - dragStartPos;
            float swipeDistance = totalSwipe.y;
            
            // CHỈ XỬ LÝ VUỐT LÊN (không có rớt xuống khi nhấn)
            if (swipeDistance >= swipeThreshold)
            {
                // VUỐT LÊN
                float swipeForce = swipeDistance * swipeForceMultiplier;
                swipeForce = Mathf.Min(swipeForce, maxSwipeForce);
                
                rb.AddForce(Vector2.up * swipeForce, ForceMode2D.Impulse);
                
                if (enableRotation)
                {
                    targetRotation = Quaternion.Euler(0, 0, 0);
                }
                
                if (enableTrail && trail != null)
                {
                    float intensity = swipeForce / maxSwipeForce;
                    trail.startColor = Color.Lerp(
                        new Color(1f, 1f, 0f, 0.5f),
                        new Color(0f, 1f, 1f, 0.9f),
                        intensity
                    );
                    trail.startWidth = 0.2f + (0.3f * intensity);
                }
                
                if (enableColorChange && spriteRenderer != null)
                    spriteRenderer.color = originalColor;
                
                Debug.Log("⬆️ VUỐT LÊN! Distance: " + swipeDistance.ToString("F2") + " | Force: " + swipeForce.ToString("F2"));
            }
            else
            {
                // Không đủ khoảng cách = không làm gì (chỉ reset màu)
                if (enableColorChange && spriteRenderer != null)
                    spriteRenderer.color = originalColor;
                
                Debug.Log("👆 Nhấn nhẹ (không đủ khoảng cách: " + swipeDistance.ToString("F2") + ")");
            }
        }
    }
    
    // ==================== CHUYỂN ĐỘNG ====================
    void ApplyMovement()
    {
        // ⭐⭐ NẾU CHẠM VẬT CẢN = RỚT NHANH
        if (hitObstacle)
        {
            float fallForce = autoRiseForce * obstacleFallForceMultiplier;
            rb.AddForce(Vector2.down * fallForce, ForceMode2D.Force);
            
            if (Time.frameCount % 30 == 0)
            {
                Debug.Log("⬇️ RỚT SAU KHI CHẠM VẬT CẢN! Force: " + fallForce.ToString("F3"));
            }
        }
        else if (!isDragging)
        {
            // Tự bay lên nhẹ khi không vuốt
            if (rb.linearVelocity.y < maxAutoRiseSpeed)
            {
                rb.AddForce(Vector2.up * autoRiseForce, ForceMode2D.Force);
            }
        }
        
        // Giới hạn tốc độ bay lên
        if (rb.linearVelocity.y > maxRiseSpeed)
        {
            Vector2 vel = rb.linearVelocity;
            vel.y = maxRiseSpeed;
            rb.linearVelocity = vel;
        }
        
        if (Time.frameCount % 30 == 0 && !hitObstacle)
        {
            string status = isDragging ? "ĐANG VUỐT" : "TỰ BAY";
            Debug.Log("🎯 " + status + " | Velocity Y: " + rb.linearVelocity.y.ToString("F2"));
        }
    }
    
    // ==================== XOAY ĐẦU ====================
    void UpdateRotation()
    {
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
        
        // ⭐⭐ NẾU CHẠM VẬT CẢN = GIỮ MÀU ĐỎ THẪM (NHẤP NHÁY)
        if (hitObstacle)
        {
            float blinkSpeed = 5f;
            float blink = Mathf.PingPong(Time.time * blinkSpeed, 1f);
            spriteRenderer.color = Color.Lerp(
                new Color(0.5f, 0f, 0f, 1f),  // Đỏ đậm
                new Color(1f, 0f, 0f, 1f),    // Đỏ sáng
                blink
            );
            return;
        }
        
        float totalSpeed = rb.linearVelocity.y;
        
        if (isDragging)
        {
            spriteRenderer.color = Color.Lerp(spriteRenderer.color, Color.cyan, Time.deltaTime * 5f);
        }
        else if (totalSpeed > 3f)
        {
            spriteRenderer.color = Color.Lerp(spriteRenderer.color, Color.green, Time.deltaTime * 3f);
        }
        else
        {
            spriteRenderer.color = Color.Lerp(spriteRenderer.color, originalColor, Time.deltaTime * 3f);
        }
        
        if (enableTrail && trail != null)
        {
            if (hitObstacle)
            {
                trail.startWidth = 0.15f;
                trail.startColor = new Color(1f, 0.3f, 0f, 0.6f);
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
    }
    
    public void SetVelocity(Vector2 velocity)
    {
        rb.linearVelocity = velocity;
    }
    
    public void SetAutoRiseForce(float force)
    {
        autoRiseForce = force;
        Debug.Log("⬆️ Lực tự bay: " + force);
    }
    
    public void AddImpulse(Vector2 force)
    {
        rb.AddForce(force, ForceMode2D.Impulse);
    }
    
    public void EnablePhysics(bool enable)
    {
        rb.simulated = enable;
    }
    
    // ⭐⭐ RESET GAME (để chơi lại)
    public void ResetGame()
    {
        hitObstacle = false;
        isGameOver = false;
        obstacleTimer = 0f;
        isDragging = false;
        
        rb.simulated = true;
        rb.linearVelocity = Vector2.zero;
        
        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
        }
        
        transform.rotation = Quaternion.Euler(0, 0, 0);
        targetRotation = Quaternion.Euler(0, 0, 0);
        
        Debug.Log("🔄 RESET GAME!");
    }
}