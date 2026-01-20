using UnityEngine;

public class HeavyBalloonController : MonoBehaviour
{
    [Header("=== BẮT ĐẦU GAME ===")]
    [SerializeField] private float initialRiseForce = 20f;
    [SerializeField] private float targetHeightPercent = 0.6f;
    [SerializeField] private float riseSpeed = 3f;
    
    [Header("=== VẬT LÝ BÓNG BAY (NẶNG + MA SÁT CAO) ===")]
    [SerializeField] private float mass = 0.5f;                    
    [SerializeField] private float linearDrag = 4f;                // TĂNG TỪ 2 → 4 (MA SÁT GẤP ĐÔI!)
    [SerializeField] private float angularDrag = 1f;               
    [SerializeField] private float gravityScale = 1.5f;
    
    [Header("=== KÉO ĐỂ BAY LÊN (GIỚI HẠN 1/3 MÀN HÌNH) ===")]
    [SerializeField] private float dragForceMultiplier = 50f;      // Tăng lại lên 50
    [SerializeField] private float maxDragForce = 80f;             // Tăng lại lên 80
    [SerializeField] private float upwardBoost = 1.2f;             // Tăng lại lên 1.2
    [SerializeField] private float dragDamping = 0.85f;            // TĂNG damping mạnh (0.96 → 0.85)
    [SerializeField] private float minDragDistance = 0.1f;
    
    [Header("=== RƠI XUỐNG (ĐÃ TĂNG LỰC) ===")]
    [SerializeField] private float passiveDownwardForce = 0.5f;    
    [SerializeField] private float maxFallSpeed = 3f;              
    [SerializeField] private float maxRiseSpeed = 1.5f;            // GIẢM TỪ 3 → 1.5 (CHỈ LÊN CHẬM THÔI!)
    
    [Header("=== GIÓ NHẸ ===")]
    [SerializeField] private float windStrength = 0.3f;
    [SerializeField] private float swayFrequency = 1f;
    
    [Header("=== GIỚI HẠN MÀN HÌNH ===")]
    [SerializeField] private float screenPadding = 0.3f;
    [SerializeField] private bool enableBounce = true;
    [SerializeField] private float bounceForce = 0.5f;
    
    // ========== THÊM PHẦN NÀY: HIỆU ỨNG MÓP ==========
    [Header("=== HIỆU ỨNG MÓP (SQUASH & STRETCH) ===")]
    [SerializeField] private float squashAmount = 0.15f;  // Độ móp khi bay (15%)
    [SerializeField] private float squashSpeed = 8f;      // Tốc độ móp/phục hồi
    [SerializeField] private float minVelocityForSquash = 2f; // Vận tốc tối thiểu để móp
    
    [Header("=== HIỆU ỨNG RƠI TỪ TRÊN CAO ===")]
    [SerializeField] private float fallStretchAmount = 0.25f; // Kéo dài khi rơi (25%)
    [SerializeField] private float minFallVelocity = 3f;      // Vận tốc rơi để kích hoạt
    
    [Header("=== HIỆU ỨNG CHẠM ĐẤT ===")]
    [SerializeField] private float groundSquashAmount = 0.6f; // Bẹt khi chạm đất (60% - TĂNG!)
    [SerializeField] private float groundSquashDuration = 0.35f; // Thời gian bẹt (giây - DÀI HƠN!)
    
    private Vector3 originalScale;  // Lưu scale gốc
    private Vector3 targetScale;    // Scale mục tiêu
    private bool isGroundSquashing = false;  // Đang bẹt vì chạm đất
    private float groundSquashTimer = 0f;    // Đếm thời gian bẹt
    // ==================================================
    
    private Rigidbody2D rb;
    private Camera mainCamera;
    private float noiseSeed;
    private float objectWidth, objectHeight;
    
    private bool isDragging = false;
    private Vector2 dragStartPos;
    private Vector2 lastDragPos;
    private float dragStartTime;
    private int consecutiveDrags = 0;
    
    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private Vector2 screenBounds;
    
    private TrailRenderer trail;
    
    private enum GameState { WaitingToStart, Rising, Playing }
    private GameState currentState = GameState.WaitingToStart;
    private Vector3 startPosition;
    private float targetHeight;
    
    void Start()
    {
        Debug.Log("========================================");
        Debug.Log("🎈 BÓNG BAY ĐANG RƠI!");
        Debug.Log("👆 TAP ĐỂ BẮT ĐẦU GAME!");
        Debug.Log("========================================");
        
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.mass = mass;
        rb.linearDamping = linearDrag;
        rb.angularDamping = angularDrag;
        rb.gravityScale = 0;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        
        rb.constraints = RigidbodyConstraints2D.FreezePositionX;
        
        startPosition = transform.position;
        
        // ========== LƯU SCALE GỐC ==========
        originalScale = transform.localScale;
        targetScale = originalScale;
        // ===================================
        
        mainCamera = Camera.main;
        noiseSeed = Random.Range(0f, 100f);
        
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            objectWidth = spriteRenderer.bounds.extents.x;
            objectHeight = spriteRenderer.bounds.extents.y;
            originalColor = spriteRenderer.color;
        }
        
        screenBounds = mainCamera.ScreenToWorldPoint(new Vector3(Screen.width, Screen.height, 0));
        targetHeight = -screenBounds.y + (screenBounds.y * 2 * targetHeightPercent);
        
        trail = GetComponent<TrailRenderer>();
        if (trail == null)
        {
            trail = gameObject.AddComponent<TrailRenderer>();
            trail.time = 0.3f;
            trail.startWidth = 0.2f;
            trail.endWidth = 0.05f;
            trail.material = new Material(Shader.Find("Sprites/Default"));
            trail.startColor = new Color(1f, 1f, 0f, 0.5f);
            trail.endColor = new Color(1f, 1f, 0f, 0f);
        }
        
        Debug.Log($"✅ Mass: {mass}kg | Gravity: {gravityScale}x");
        Debug.Log("🎮 GAMEPLAY: Kéo liên tục để giữ bóng bay!");
    }
    
    void Update()
    {
        HandleDragInput();
        UpdateVisuals();
        UpdateSquashStretch(); // ← THÊM DÒNG NÀY
    }
    
    void FixedUpdate()
    {
        if (currentState == GameState.Rising)
        {
            RiseToTarget();
            return;
        }
        
        if (currentState == GameState.Playing)
        {
            ApplyPhysics();
            ClampToScreenWithBounce();
        }
    }
    
    // ========== HÀM MỚI: CẬP NHẬT HIỆU ỨNG MÓP ==========
    void UpdateSquashStretch()
    {
        // Chỉ áp dụng khi đang chơi
        if (currentState != GameState.Playing)
        {
            transform.localScale = originalScale;
            isGroundSquashing = false;
            return;
        }
        
        // ===== 1. HIỆU ỨNG CHẠM ĐẤT (ƯU TIÊN CAO NHẤT) =====
        if (isGroundSquashing)
        {
            groundSquashTimer += Time.deltaTime;
            
            if (groundSquashTimer < groundSquashDuration)
            {
                // Đang bẹt - tính % thời gian đã trôi qua
                float progress = groundSquashTimer / groundSquashDuration;
                
                // Bẹt mạnh nhất ở đầu (0.0-0.3), sau đó từ từ phục hồi
                // Sử dụng curve để hiệu ứng rõ hơn
                float squashCurve = Mathf.Pow(1f - progress, 2f); // Exponential decay
                
                targetScale = new Vector3(
                    originalScale.x * (1f + groundSquashAmount * squashCurve), // GIÃN NGANG CỰC MẠNH
                    originalScale.y * (1f - groundSquashAmount * 0.8f * squashCurve), // CO ĐỨNG
                    originalScale.z
                );
                
                // Áp dụng NGAY LẬP TỨC
                transform.localScale = targetScale;
                
                // Debug để thấy rõ
                Debug.Log($"💥 BẸT! Scale X: {targetScale.x:F2}, Y: {targetScale.y:F2} (Progress: {progress:F2})");
                
                return;
            }
            else
            {
                // Hết thời gian bẹt
                isGroundSquashing = false;
                groundSquashTimer = 0f;
                Debug.Log("✅ Phục hồi hình dạng!");
            }
        }
        
        // ===== 2. HIỆU ỨNG RƠI TỪ TRÊN CAO =====
        float velocityY = rb.linearVelocity.y;
        
        if (velocityY < -minFallVelocity)  // Đang rơi nhanh
        {
            // Kéo dài như giọt nước rơi
            float fallIntensity = Mathf.Clamp(Mathf.Abs(velocityY) / 15f, 0f, 1f);
            float stretch = fallStretchAmount * fallIntensity;
            
            targetScale = new Vector3(
                originalScale.x * (1f - stretch * 0.7f), // Co ngang (ít hơn)
                originalScale.y * (1f + stretch),         // Kéo dài đứng (nhiều)
                originalScale.z
            );
        }
        // ===== 3. HIỆU ỨNG BAY LÊN (KÉO) =====
        else if (velocityY > minVelocityForSquash)
        {
            float squashFactor = Mathf.Clamp(velocityY / 10f, 0f, 1f);
            float squash = squashAmount * squashFactor;
            
            // BAY LÊN → Kéo dài theo chiều Y, co ngang
            targetScale = new Vector3(
                originalScale.x * (1f - squash),
                originalScale.y * (1f + squash),
                originalScale.z
            );
        }
        // ===== 4. BÌNH THƯỜNG =====
        else
        {
            targetScale = originalScale;
        }
        
        // Lerp mượt mà về targetScale
        transform.localScale = Vector3.Lerp(
            transform.localScale, 
            targetScale, 
            Time.deltaTime * squashSpeed
        );
    }
    
    // ========== HÀM MỚI: KÍCH HOẠT HIỆU ỨNG CHẠM ĐẤT ==========
    void TriggerGroundSquash()
    {
        isGroundSquashing = true;
        groundSquashTimer = 0f;
        Debug.Log("💥 BỊ BẸT!");
    }
    // =========================================================
    
    void RiseToTarget()
    {
        float currentY = transform.position.y;
        
        if (currentY < targetHeight)
        {
            rb.linearVelocity = new Vector2(0, riseSpeed);
            
            if (spriteRenderer != null)
            {
                float progress = (currentY - startPosition.y) / (targetHeight - startPosition.y);
                spriteRenderer.color = Color.Lerp(Color.green, Color.yellow, progress);
            }
        }
        else
        {
            currentState = GameState.Playing;
            rb.gravityScale = gravityScale;
            rb.linearVelocity = Vector2.zero;
            
            if (spriteRenderer != null)
                spriteRenderer.color = originalColor;
            
            Debug.Log("✅ ĐÃ ĐẾN GIỮA MÀN HÌNH!");
            Debug.Log("⬇️ BẮT ĐẦU RƠI - KÉO ĐỂ ĐIỀU KHIỂN!");
        }
    }
    
    void StartGame()
    {
        Debug.Log("🚀🚀🚀 TAP - BAY LÊN! 🚀🚀🚀");
        
        currentState = GameState.Rising;
        rb.gravityScale = 0;
        rb.linearVelocity = new Vector2(0, riseSpeed);
        
        if (spriteRenderer != null)
            spriteRenderer.color = Color.green;
        
        Debug.Log($"⬆️ BAY LÊN VỚI TỐC ĐỘ: {riseSpeed} units/s");
        Debug.Log($"🎯 Đích đến: Y = {targetHeight:F1}");
    }
    
    void HandleDragInput()
    {
        Vector2 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        
        if (currentState == GameState.WaitingToStart)
        {
            if (Input.GetMouseButtonDown(0))
            {
                StartGame();
                return;
            }
            return;
        }
        
        if (currentState != GameState.Playing)
            return;
        
        if (Input.GetMouseButtonDown(0))
        {
            isDragging = true;
            dragStartPos = mouseWorldPos;
            lastDragPos = mouseWorldPos;
            dragStartTime = Time.time;
            
            if (spriteRenderer != null)
                spriteRenderer.color = Color.cyan;
            
            if (rb.linearVelocity.y < 0)
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
            
            Debug.Log("🖐️ BẮT ĐẦU KÉO!");
        }
        
        if (Input.GetMouseButton(0) && isDragging)
        {
            Vector2 dragDelta = mouseWorldPos - lastDragPos;
            float dragDistance = dragDelta.magnitude;
            
            if (dragDistance > 0.0001f)
            {
                float forceMagnitude = Mathf.Clamp(
                    dragDistance * dragForceMultiplier,
                    0f,
                    maxDragForce
                );
                
                Vector2 forceDirection = dragDelta.normalized;
                forceDirection = new Vector2(0, forceDirection.y);
                
                if (forceDirection.y > 0)
                {
                    forceMagnitude *= upwardBoost;
                    
                    if (trail != null)
                        trail.startColor = new Color(0f, 1f, 1f, 0.7f);
                }
                else
                {
                    if (trail != null)
                        trail.startColor = new Color(1f, 1f, 0f, 0.5f);
                }
                
                rb.AddForce(forceDirection * forceMagnitude, ForceMode2D.Force);
            }
            
            lastDragPos = mouseWorldPos;
        }
        
        if (Input.GetMouseButtonUp(0) && isDragging)
        {
            isDragging = false;
            consecutiveDrags++;
            
            if (spriteRenderer != null)
                spriteRenderer.color = originalColor;
            
            float dragDuration = Time.time - dragStartTime;
            Vector2 totalDrag = mouseWorldPos - dragStartPos;
            
            Debug.Log($"✋ THẢ! Kéo {totalDrag.magnitude:F1} px trong {dragDuration:F2}s");
            
            if (totalDrag.y > 0)
                Debug.Log($"⬆️ Đẩy lên! Velocity: {rb.linearVelocity.y:F1}");
            else
                Debug.Log($"⬇️ Bóng đang rơi: {rb.linearVelocity.y:F1}");
        }
    }
    
    void ApplyPhysics()
    {
        if (!isDragging)
        {
            rb.AddForce(Vector2.down * passiveDownwardForce, ForceMode2D.Force);
        }
        
        rb.linearVelocity *= dragDamping;
        
        Vector2 vel = rb.linearVelocity;
        
        if (vel.y > 0)
        {
            vel.y = Mathf.Min(vel.y, maxRiseSpeed);
        }
        
        if (vel.y < 0)
        {
            vel.y = Mathf.Max(vel.y, -maxFallSpeed);
        }
        
        rb.linearVelocity = vel;
    }
    
    void ClampToScreenWithBounce()
    {
        Vector3 pos = transform.position;
        
        float minY = -screenBounds.y + objectHeight + screenPadding;
        float maxY = screenBounds.y - objectHeight - screenPadding;
        
        Vector2 vel = rb.linearVelocity;
        bool hitBoundary = false;
        
        if (pos.y <= minY)
        {
            pos.y = minY;
            
            // ===== KÍCH HOẠT HIỆU ỨNG CHẠM ĐẤT =====
            // Luôn kích hoạt khi chạm đất (để dễ test)
            if (vel.y < -0.1f && !isGroundSquashing)  // Giảm ngưỡng xuống 0.1
            {
                TriggerGroundSquash();
                Debug.Log($"💥💥💥 CHẠM ĐẤT VỚI VẬN TỐC: {vel.y:F2}");
            }
            // ========================================
            
            if (enableBounce && vel.y < -1f)
            {
                vel.y = -vel.y * bounceForce;
                Debug.Log("💥 CHẠM ĐẤT! Nảy lên!");
            }
            else
            {
                vel.y = 0;
                Debug.Log("🔴 CHẠM ĐẤT - GAME OVER?");
            }
            hitBoundary = true;
        }
        
        if (pos.y >= maxY)
        {
            pos.y = maxY;
            if (vel.y > 0)
            {
                vel.y = -vel.y * 0.3f;
                Debug.Log("⚠️ CHẠM TRẦN!");
            }
        }
        
        transform.position = pos;
        
        if (hitBoundary)
        {
            rb.linearVelocity = vel;
        }
    }
    
    void UpdateVisuals()
    {
        if (currentState == GameState.WaitingToStart)
        {
            if (spriteRenderer != null)
            {
                float pulse = (Mathf.Sin(Time.time * 3f) + 1f) / 2f;
                spriteRenderer.color = Color.Lerp(originalColor, Color.yellow, pulse * 0.3f);
            }
            return;
        }
        
        if (currentState == GameState.Rising)
        {
            return;
        }
        
        if (rb.linearVelocity.magnitude > 0.5f)
        {
            float angle = Mathf.Atan2(rb.linearVelocity.y, rb.linearVelocity.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Lerp(
                transform.rotation, 
                Quaternion.Euler(0, 0, angle - 90), 
                Time.deltaTime * 3f
            );
        }
        
        if (!isDragging && spriteRenderer != null)
        {
            if (transform.position.y < -screenBounds.y + 2f)
            {
                spriteRenderer.color = Color.Lerp(spriteRenderer.color, Color.red, Time.deltaTime * 5f);
            }
            else if (rb.linearVelocity.y > 0)
            {
                spriteRenderer.color = Color.Lerp(spriteRenderer.color, Color.green, Time.deltaTime * 3f);
            }
            else
            {
                spriteRenderer.color = Color.Lerp(spriteRenderer.color, originalColor, Time.deltaTime * 3f);
            }
        }
    }
    
    void OnGUI()
    {
        if (!Application.isPlaying) return;
        
        GUIStyle style = new GUIStyle();
        style.fontSize = 22;
        style.fontStyle = FontStyle.Bold;
        style.alignment = TextAnchor.UpperLeft;
        
        if (currentState == GameState.WaitingToStart)
        {
            style.normal.textColor = Color.yellow;
            style.fontSize = 28;
            style.alignment = TextAnchor.MiddleCenter;
            
            string message = "🎈 SẴN SÀNG!\n\n";
            message += "👆 TAP ĐỂ BAY LÊN!\n\n";
            message += "━━━━━━━━━━━━━━━━━━━━\n";
            message += "Bóng sẽ bay lên giữa màn hình\n";
            message += "rồi bắt đầu rơi!";
            
            GUI.Label(new Rect(0, Screen.height / 2 - 100, Screen.width, 200), message, style);
            return;
        }
        
        if (currentState == GameState.Rising)
        {
            style.normal.textColor = Color.green;
            style.fontSize = 32;
            style.alignment = TextAnchor.UpperCenter;
            
            float progress = (transform.position.y - startPosition.y) / (targetHeight - startPosition.y);
            progress = Mathf.Clamp01(progress);
            
            string message = "⬆️ ĐANG BAY LÊN...\n";
            message += $"{(progress * 100):F0}%";
            
            GUI.Label(new Rect(0, 50, Screen.width, 100), message, style);
            return;
        }
        
        style.alignment = TextAnchor.UpperLeft;
        style.fontSize = 22;
        style.normal.textColor = isDragging ? Color.cyan : Color.white;
        
        string status = isDragging ? "🖐️ ĐANG KÉO" : "⬇️ ĐANG RƠI";
        
        if (transform.position.y < -screenBounds.y + 2f)
        {
            style.normal.textColor = Color.red;
            status = "⚠️ GẦN ĐẤT!";
        }
        else if (rb.linearVelocity.y > 0 && !isDragging)
        {
            style.normal.textColor = Color.green;
            status = "⬆️ ĐANG BAY";
        }
        
        string info = $"{status}\n";
        info += $"━━━━━━━━━━━━━━━━━━━━\n";
        info += $"Velocity Y: {rb.linearVelocity.y:F2}\n";
        info += $"Height: {transform.position.y:F1}\n";
        info += $"Drags: {consecutiveDrags}\n";
        info += $"━━━━━━━━━━━━━━━━━━━━\n";
        info += $"Mass: {mass}kg | G: {gravityScale}x";
        
        GUI.Label(new Rect(10, 10, 400, 250), info, style);
        
        GUIStyle tipStyle = new GUIStyle();
        tipStyle.fontSize = 18;
        tipStyle.normal.textColor = Color.yellow;
        tipStyle.alignment = TextAnchor.LowerCenter;
        
        string tip = "💡 KÉO LÊN ĐỂ BAY! KÉO XUỐNG CHO CHẬM!\n";
        tip += "🎯 CLICK VÀ KÉO BẤT KỲ ĐÂU TRÊN MÀN HÌNH";
        
        GUI.Label(new Rect(0, Screen.height - 80, Screen.width, 80), tip, tipStyle);
    }
}