using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// ⭐ VẬT LÝ THỰC TẾ: Dùng Force thay vì set Velocity!
/// ⭐⭐⭐⭐ CẬP NHẬT QUẢNG CÁO:
/// - Khi Ghost chết → Hiện quảng cáo Interstitial
/// - Skip/Đóng quảng cáo → Mới hiện màn hình Game Over
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
    
    [Header("=== RỚT XUỐNG KHI NHẤN (KHÔNG VUỐT) ===")]
    [SerializeField] private float fallForceMultiplier = 30.0f;
    
    [Header("=== ⭐⭐ VẬT CẢN - GAME OVER ===")]
    [SerializeField] private float obstacleKnockbackForce = 15f;
    [SerializeField] private float timeBeforeGameOver = 3f;
    [SerializeField] private string obstacleTag = "Obstacle";
    
    [Header("=== XOAY ĐẦU KHI RỚT ===")]
    [SerializeField] private bool enableRotation = true;
    [SerializeField] private float rotationSpeed = 4f;
    
    [Header("=== ĐIỀU KHIỂN VUỐT ===")]
    [SerializeField] private float swipeForceMultiplier = 300f;
    [SerializeField] private float maxSwipeForce = 100f;
    [SerializeField] private float minSwipeDistance = 0.01f;
    
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
    
    [Header("=== ⭐ GAME OVER UI ===")]
    [SerializeField] private string gameOverPanelName = "GameOverPanel";
    
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
    private bool isFalling = false;
    
    // ⭐⭐ VẬT CẢN - TRẠNG THÁI MỚI
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
    public bool IsFalling => isFalling;
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
        
        Debug.Log("👻 GhostController: VẬT LÝ THỰC TẾ! (BỎ TAP=RỚT, CHỈ VUỐT LÊN)");
    }
    
    void Start()
    {
        // ⭐ LOAD SẴN QUẢNG CÁO KHI BẮT ĐẦU GAME
        if (AdsManager.Instance != null)
        {
            AdsManager.Instance.LoadInterstitial();
            Debug.Log("🎬 Đã yêu cầu load quảng cáo Interstitial");
        }
        else
        {
            Debug.LogWarning("⚠️ AdsManager không tồn tại! Hãy tạo GameObject AdsManager trong scene.");
        }
    }
    
    void Update()
    {
        if (isGameOver)
        {
            return;
        }
        
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
    
    void OnObstacleHit(Collision2D collision)
    {
        hitObstacle = true;
        isFalling = true;
        obstacleTimer = 0f;
        
        PlayerPrefs.SetFloat("RespawnX", transform.position.x);
        PlayerPrefs.SetFloat("RespawnY", transform.position.y);
        PlayerPrefs.SetFloat("RespawnVelocityY", rb.linearVelocity.y);
        PlayerPrefs.Save();
        
        Debug.Log("💾 SAVED RESPAWN POSITION: " + transform.position);
        
        Vector2 knockbackDirection = (transform.position - collision.transform.position).normalized;
        
        rb.linearVelocity = Vector2.zero;
        rb.AddForce(knockbackDirection * obstacleKnockbackForce, ForceMode2D.Impulse);
        
        if (enableColorChange && spriteRenderer != null)
        {
            spriteRenderer.color = new Color(0.8f, 0f, 0f, 1f);
        }
        
        if (enableRotation)
        {
            targetRotation = Quaternion.Euler(0, 0, 180);
        }
        
        Debug.Log("💥💥💥 CHẠM VẬT CẢN! Bắt đầu đếm ngược " + timeBeforeGameOver + "s...");
    }
    
    // ⭐⭐⭐⭐ HIỆN QUẢNG CÁO TRƯỚC KHI GAME OVER
    void TriggerGameOver()
    {
        if (isGameOver) return; // Tránh gọi nhiều lần
        
        isGameOver = true;
        rb.linearVelocity = Vector2.zero;
        rb.simulated = false;
        
        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.black;
        }
        
        Debug.Log("☠️☠️☠️ GAME OVER - Chuẩn bị hiện quảng cáo...");
        
        // ⭐ GỌI QUẢNG CÁO TRƯỚC KHI GAME OVER
        ShowAdThenGameOver();
    }
    
    // ⭐ HÀM MỚI: Hiện quảng cáo rồi mới Game Over
    void ShowAdThenGameOver()
    {
        if (AdsManager.Instance != null)
        {
            // ⭐ Kiểm tra xem quảng cáo có sẵn sàng không
            if (AdsManager.Instance.CanShowInterstitial())
            {
                Debug.Log("📺 ĐANG HIỆN QUẢNG CÁO...");
                
                // ⭐ Đăng ký event TRƯỚC KHI show (QUAN TRỌNG!)
                AdsManager.Instance.OnAdShowComplete += HandleAdComplete;
                AdsManager.Instance.OnAdShowFailed += HandleAdFailed;
                
                // Show quảng cáo
                bool adShown = AdsManager.Instance.ShowInterstitial();
                
                if (!adShown)
                {
                    // Nếu show thất bại, hủy event và hiện Game Over luôn
                    Debug.LogWarning("⚠️ ShowInterstitial() trả về false!");
                    AdsManager.Instance.OnAdShowComplete -= HandleAdComplete;
                    AdsManager.Instance.OnAdShowFailed -= HandleAdFailed;
                    ShowGameOverScreen();
                }
            }
            else
            {
                Debug.Log("⚠️ Quảng cáo chưa sẵn sàng, vào Game Over luôn");
                ShowGameOverScreen();
            }
        }
        else
        {
            Debug.LogWarning("⚠️ AdsManager không tồn tại!");
            ShowGameOverScreen();
        }
    }
    
    // ⭐ XỬ LÝ KHI QUẢNG CÁO ĐÓNG/SKIP/HOÀN THÀNH
    void HandleAdComplete(string unitId)
    {
        Debug.Log("✅✅✅ Quảng cáo đã đóng/skip: " + unitId + " - BÂY GIỜ MỚI HIỆN GAME OVER!");
        
        // ⭐ Hủy đăng ký event (QUAN TRỌNG để tránh memory leak)
        if (AdsManager.Instance != null)
        {
            AdsManager.Instance.OnAdShowComplete -= HandleAdComplete;
            AdsManager.Instance.OnAdShowFailed -= HandleAdFailed;
        }
        
        // ⭐ HIỆN MÀN HÌNH GAME OVER SAU KHI ĐÓNG QUẢNG CÁO
        ShowGameOverScreen();
    }
    
    // ⭐ XỬ LÝ KHI QUẢNG CÁO BỊ LỖI
    void HandleAdFailed(string unitId, string message)
    {
        Debug.LogWarning("❌ Quảng cáo lỗi: " + message + " - HIỆN GAME OVER!");
        
        // ⭐ Hủy đăng ký event
        if (AdsManager.Instance != null)
        {
            AdsManager.Instance.OnAdShowComplete -= HandleAdComplete;
            AdsManager.Instance.OnAdShowFailed -= HandleAdFailed;
        }
        
        // ⭐ Vẫn hiện Game Over dù quảng cáo lỗi
        ShowGameOverScreen();
    }
    
    // ⭐ HIỆN MÀN HÌNH GAME OVER (CHỈ GỌI SAU KHI ĐÓNG QUẢNG CÁO)
    void ShowGameOverScreen()
    {
        Debug.Log("🎮🎮🎮 BÂY GIỜ MỚI HIỆN MÀN HÌNH GAME OVER!");
        
        // Tìm GameObject Game Over Panel
        GameObject gameOverPanel = GameObject.Find(gameOverPanelName);
        
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            Debug.Log("✅ Đã bật " + gameOverPanelName);
        }
        else
        {
            // Thử tìm trong Canvas
            GameObject canvas = GameObject.Find("Canvas");
            if (canvas != null)
            {
                Transform panelTransform = canvas.transform.Find(gameOverPanelName);
                if (panelTransform != null)
                {
                    panelTransform.gameObject.SetActive(true);
                    Debug.Log("✅ Đã bật " + gameOverPanelName + " trong Canvas");
                }
                else
                {
                    Debug.LogWarning("⚠️⚠️⚠️ KHÔNG TÌM THẤY '" + gameOverPanelName + "' trong Canvas!");
                    Debug.LogWarning("👉 Hãy tạo Panel tên '" + gameOverPanelName + "' trong Canvas");
                }
            }
            else
            {
                Debug.LogWarning("⚠️⚠️⚠️ KHÔNG TÌM THẤY Canvas!");
                Debug.LogWarning("👉 Tạo UI > Panel trong Canvas, đặt tên '" + gameOverPanelName + "'");
            }
        }
        
        // ⭐ Load lại quảng cáo cho lần chết tiếp theo
        if (AdsManager.Instance != null)
        {
            AdsManager.Instance.LoadInterstitial();
            Debug.Log("🔄 Đã load lại quảng cáo cho lần sau");
        }
    }
    
    // ==================== INPUT: VUỐT ====================
    void HandleSwipeInput()
    {
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
            
            if (swipeDistance >= minSwipeDistance)
            {
                float swipeForce = swipeDistance * swipeForceMultiplier;
                swipeForce = Mathf.Min(swipeForce, maxSwipeForce);
                
                rb.AddForce(Vector2.up * swipeForce, ForceMode2D.Impulse);
                isFalling = false;
                
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
            }
        }
    }
    
    // ==================== CHUYỂN ĐỘNG ====================
    void ApplyMovement()
    {
        if (isFalling)
        {
            float fallForce = autoRiseForce * fallForceMultiplier;
            rb.AddForce(Vector2.down * fallForce, ForceMode2D.Force);
        }
        else if (!isDragging)
        {
            if (rb.linearVelocity.y < maxAutoRiseSpeed)
            {
                rb.AddForce(Vector2.up * autoRiseForce, ForceMode2D.Force);
            }
        }
        
        if (rb.linearVelocity.y > maxRiseSpeed)
        {
            Vector2 vel = rb.linearVelocity;
            vel.y = maxRiseSpeed;
            rb.linearVelocity = vel;
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
    }
    
    // ==================== VISUAL ====================
    void UpdateVisuals()
    {
        if (!enableColorChange || spriteRenderer == null) return;
        
        if (hitObstacle)
        {
            float blinkSpeed = 5f;
            float blink = Mathf.PingPong(Time.time * blinkSpeed, 1f);
            spriteRenderer.color = Color.Lerp(
                new Color(0.5f, 0f, 0f, 1f),
                new Color(1f, 0f, 0f, 1f),
                blink
            );
            return;
        }
        
        float totalSpeed = rb.linearVelocity.y;
        
        if (isFalling)
        {
            spriteRenderer.color = Color.Lerp(spriteRenderer.color, Color.red, Time.deltaTime * 5f);
        }
        else if (isDragging)
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
            if (isFalling)
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
        isFalling = false;
    }
    
    public void SetVelocity(Vector2 velocity)
    {
        rb.linearVelocity = velocity;
    }
    
    public void SetAutoRiseForce(float force)
    {
        autoRiseForce = force;
    }
    
    public void AddImpulse(Vector2 force)
    {
        rb.AddForce(force, ForceMode2D.Impulse);
    }
    
    public void EnablePhysics(bool enable)
    {
        rb.simulated = enable;
    }
    
    public void StopFalling()
    {
        isFalling = false;
        if (enableColorChange && spriteRenderer != null)
            spriteRenderer.color = originalColor;
        
        if (enableRotation)
        {
            targetRotation = Quaternion.Euler(0, 0, 0);
        }
    }
    
    public void ResetGame()
    {
        hitObstacle = false;
        isGameOver = false;
        obstacleTimer = 0f;
        isFalling = false;
        isDragging = false;
        
        rb.simulated = true;
        rb.linearVelocity = Vector2.zero;
        
        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
        }
        
        transform.rotation = Quaternion.Euler(0, 0, 0);
        targetRotation = Quaternion.Euler(0, 0, 0);
    }
    
    // ⭐ HỦY ĐĂNG KÝ EVENT KHI DESTROY (QUAN TRỌNG - TRÁNH MEMORY LEAK!)
    void OnDestroy()
    {
        if (AdsManager.Instance != null)
        {
            AdsManager.Instance.OnAdShowComplete -= HandleAdComplete;
            AdsManager.Instance.OnAdShowFailed -= HandleAdFailed;
        }
        
        Debug.Log("👻 GhostController đã bị destroy - đã hủy event callbacks");
    }
}