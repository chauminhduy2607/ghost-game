using UnityEngine;

public class RealisticBalloonController : MonoBehaviour
{
    [Header("=== VẬT LÝ BÓNG BAY ===")]
    [SerializeField] private float mass = 0.1f;
    [SerializeField] private float linearDrag = 2f;
    [SerializeField] private float angularDrag = 1f;
    [SerializeField] private float gravityScale = 1f;
    
    [Header("=== LỰC NỔI ===")]
    [SerializeField] private float passiveBuoyancy = 0f;
    [SerializeField] private float maxFallSpeed = 5f;
    
    [Header("=== GIÓ ===")]
    [SerializeField] private float windStrength = 0.4f;
    [SerializeField] private float swayFrequency = 1.2f;
    
    [Header("=== VUỐT ===")]
    [SerializeField] private float swipeForceMultiplier = 0.2f;
    [SerializeField] private float minSwipeSpeed = 300f;
    [SerializeField] private float minSwipeDistance = 50f;
    [SerializeField] private float maxSwipeForce = 50f;
    
    [Header("=== DAMPING ===")]
    [SerializeField] private float velocityDamping = 0.98f;
    
    [Header("=== GIỚI HẠN ===")]
    [SerializeField] private float screenPadding = 0.5f;
    
    private Rigidbody2D rb;
    private Camera mainCamera;
    private float noiseSeed;
    private float objectWidth, objectHeight;
    private bool isSwiping = false;
    private Vector2 swipeStartPos;
    private float swipeStartTime;
    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    
    void Start()
    {
        Debug.Log("========================================");
        Debug.Log("🚀🚀🚀 SCRIPT MỚI BẮT ĐẦU! 🚀🚀🚀");
        Debug.Log("========================================");
        
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.mass = mass;
        rb.linearDamping = linearDrag;
        rb.angularDamping = angularDrag;
        rb.gravityScale = gravityScale;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        
        mainCamera = Camera.main;
        noiseSeed = Random.Range(0f, 100f);
        
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            objectWidth = spriteRenderer.bounds.extents.x;
            objectHeight = spriteRenderer.bounds.extents.y;
            originalColor = spriteRenderer.color;
        }
        
        Debug.Log("✅ Setup hoàn tất! Click và vuốt bất kỳ đâu!");
    }
    
    void Update()
    {
        // TEST: Nhấn Space để test nhanh
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("⌨️ NHẤN SPACE - BAY LÊN!");
            rb.AddForce(Vector2.up * 30f, ForceMode2D.Impulse);
        }
        
        HandleSwipeInput();
    }
    
    void FixedUpdate()
    {
        ApplyPhysics();
        ClampToScreen();
    }
    
    void HandleSwipeInput()
    {
        // BẮT ĐẦU VUỐT
        if (Input.GetMouseButtonDown(0))
        {
            isSwiping = true;
            swipeStartPos = Input.mousePosition;
            swipeStartTime = Time.time;
            
            if (spriteRenderer != null)
                spriteRenderer.color = Color.yellow;
            
            Debug.Log($"🖱️ CLICK! Vị trí: ({Input.mousePosition.x:F0}, {Input.mousePosition.y:F0})");
        }
        
        // ĐANG VUỐT
        if (Input.GetMouseButton(0) && isSwiping)
        {
            Vector2 currentPos = Input.mousePosition;
            float distance = Vector2.Distance(swipeStartPos, currentPos);
            Debug.Log($"📏 Đang vuốt... {distance:F0}px");
        }
        
        // THẢ CHUỘT
        if (Input.GetMouseButtonUp(0) && isSwiping)
        {
            ProcessSwipe();
            isSwiping = false;
            
            if (spriteRenderer != null)
                spriteRenderer.color = originalColor;
        }
    }
    
    void ProcessSwipe()
    {
        Vector2 swipeEndPos = Input.mousePosition;
        float swipeDuration = Time.time - swipeStartTime;
        Vector2 swipeVector = swipeEndPos - swipeStartPos;
        float swipeDistance = swipeVector.magnitude;
        float swipeSpeed = swipeDistance / Mathf.Max(swipeDuration, 0.01f);
        
        Debug.Log("═══════════════════════════════");
        Debug.Log($"📊 VUỐT: {swipeDistance:F0}px trong {swipeDuration:F2}s");
        Debug.Log($"⚡ Tốc độ: {swipeSpeed:F0} px/s");
        
        // Kiểm tra hợp lệ
        if (swipeDuration > 0.5f)
        {
            Debug.Log("❌ Quá chậm!");
            return;
        }
        
        if (swipeDistance < minSwipeDistance)
        {
            Debug.Log($"❌ Quá ngắn! Cần >{minSwipeDistance}px");
            return;
        }
        
        if (swipeSpeed < minSwipeSpeed)
        {
            Debug.Log($"❌ Tốc độ thấp! Cần >{minSwipeSpeed}px/s");
            return;
        }
        
        // ÁP LỰC
        float forceMagnitude = Mathf.Clamp(
            swipeSpeed * swipeForceMultiplier,
            5f,
            maxSwipeForce
        );
        
        Vector2 direction = new Vector2(
            swipeVector.normalized.x * 0.3f,
            Mathf.Max(Mathf.Abs(swipeVector.normalized.y), 0.5f)
        ).normalized;
        
        rb.AddForce(direction * forceMagnitude, ForceMode2D.Impulse);
        
        Debug.Log($"🚀 BAY LÊN! Lực: {forceMagnitude:F1}N");
        Debug.Log("═══════════════════════════════");
    }
    
    void ApplyPhysics()
    {
        if (passiveBuoyancy > 0)
        {
            rb.AddForce(Vector2.up * passiveBuoyancy);
        }
        
        // Gió đơn giản
        if (!isSwiping)
        {
            float sway = Mathf.Sin(Time.time * swayFrequency) * windStrength;
            rb.AddForce(new Vector2(sway, 0));
        }
        
        // Damping
        rb.linearVelocity *= velocityDamping;
        
        // Giới hạn tốc độ
        Vector2 vel = rb.linearVelocity;
        if (vel.y < 0) vel.y = Mathf.Max(vel.y, -maxFallSpeed);
        vel.x = Mathf.Clamp(vel.x, -8f, 8f);
        rb.linearVelocity = vel;
    }
    
    void ClampToScreen()
    {
        Vector3 pos = transform.position;
        Vector2 screenBounds = mainCamera.ScreenToWorldPoint(new Vector3(Screen.width, Screen.height, 0));
        
        float minX = -screenBounds.x + objectWidth + screenPadding;
        float maxX = screenBounds.x - objectWidth - screenPadding;
        float minY = -screenBounds.y + objectHeight + screenPadding;
        float maxY = screenBounds.y - objectHeight - screenPadding;
        
        pos.x = Mathf.Clamp(pos.x, minX, maxX);
        pos.y = Mathf.Clamp(pos.y, minY, maxY);
        
        if (transform.position.y <= minY)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, Mathf.Abs(rb.linearVelocity.y) * 0.3f);
        }
        
        transform.position = pos;
    }
}