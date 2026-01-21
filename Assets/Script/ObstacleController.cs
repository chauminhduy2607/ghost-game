using UnityEngine;

/// <summary>
/// ⭐ VẬT CẢN DI CHUYỂN RANDOM CÓ QUY LUẬT
/// - Di chuyển qua lại với tốc độ ngẫu nhiên
/// - Không bị tràn ra ngoài màn hình
/// - Mỗi vật cản có pattern riêng (không đồng bộ)
/// </summary>
public class ObstacleController : MonoBehaviour
{
    [Header("=== DI CHUYỂN NGANG ===")]
    [SerializeField] private float moveSpeed = 2f;           // Tốc độ di chuyển ngang
    [SerializeField] private float minSpeed = 1f;            // Tốc độ tối thiểu
    [SerializeField] private float maxSpeed = 4f;            // Tốc độ tối đa
    
    [Header("=== GIỚI HẠN MÀN HÌNH ===")]
    [SerializeField] private float screenPadding = 0.5f;     // Khoảng cách từ mép màn hình
    
    [Header("=== SMOOTH ===")]
    [SerializeField] private float smoothTime = 0.1f;        // Độ mượt
    [SerializeField] private float maxSmoothSpeed = 999f;
    
    [Header("=== PARALLAX (Di Chuyển Theo Camera) ===")]
    [Tooltip("Tốc độ di chuyển xuống khi camera di chuyển lên")]
    [SerializeField] private float parallaxSpeed = 1f;       // 1 = di chuyển cùng tốc camera
    
    // Private
    private Camera mainCamera;
    private float leftBound;
    private float rightBound;
    private float currentTargetX;
    private float velocityX = 0f;
    private Vector3 startLocalPosition;  // Vị trí ban đầu (local)
    private float randomPhaseOffset;      // Offset random để không đồng bộ
    private float randomSpeedMultiplier;  // Tốc độ random
    private int direction = 1;            // Hướng di chuyển: 1 = phải, -1 = trái
    
    // Public properties
    public float CurrentX => transform.position.x;
    public Vector3 StartLocalPosition => startLocalPosition;
    
    void Awake()
    {
        mainCamera = Camera.main;
        
        // Lưu vị trí ban đầu (local - so với parent)
        startLocalPosition = transform.localPosition;
        
        // Random các thông số để mỗi vật cản khác nhau
        RandomizeMovement();
        
        // Tính giới hạn màn hình
        CalculateScreenBounds();
        
        // Đặt vị trí ban đầu ngẫu nhiên trong màn hình
        SetRandomStartPosition();
    }
    
    void Update()
    {
        UpdateTargetPosition();
        SmoothMove();
    }
    
    // ==================== RANDOM HÓA CHUYỂN ĐỘNG ====================
    void RandomizeMovement()
    {
        // Random phase offset (0 - 2π) để mỗi vật cản khác nhau
        randomPhaseOffset = Random.Range(0f, Mathf.PI * 2f);
        
        // Random tốc độ (trong khoảng minSpeed - maxSpeed)
        randomSpeedMultiplier = Random.Range(minSpeed, maxSpeed) / moveSpeed;
        
        // Random hướng ban đầu
        direction = Random.value > 0.5f ? 1 : -1;
        
        Debug.Log($"🎲 {gameObject.name}: Phase={randomPhaseOffset:F2}, Speed={randomSpeedMultiplier:F2}, Dir={direction}");
    }
    
    // ==================== TÍNH GIỚI HẠN MÀN HÌNH ====================
    void CalculateScreenBounds()
    {
        if (mainCamera == null)
        {
            Debug.LogError("❌ Không tìm thấy Main Camera!");
            return;
        }
        
        // Lấy kích thước vật cản
        SpriteRenderer sprite = GetComponent<SpriteRenderer>();
        float objectWidth = sprite != null ? sprite.bounds.extents.x : 0.5f;
        
        // Tính giới hạn màn hình
        float cameraHalfWidth = mainCamera.orthographicSize * mainCamera.aspect;
        leftBound = -cameraHalfWidth + objectWidth + screenPadding;
        rightBound = cameraHalfWidth - objectWidth - screenPadding;
        
        Debug.Log($"📐 Screen bounds: [{leftBound:F1}, {rightBound:F1}]");
    }
    
    // ==================== ĐẶT VỊ TRÍ BAN ĐẦU NGẪU NHIÊN ====================
    void SetRandomStartPosition()
    {
        // Random vị trí X trong khoảng màn hình
        float randomX = Random.Range(leftBound, rightBound);
        currentTargetX = randomX;
        
        Vector3 pos = transform.position;
        pos.x = randomX;
        transform.position = pos;
    }
    
    // ==================== CẬP NHẬT VỊ TRÍ MỤC TIÊU ====================
    void UpdateTargetPosition()
    {
        // ⭐ DI CHUYỂN THEO SINE WAVE (mượt, có quy luật)
        float time = Time.time * randomSpeedMultiplier + randomPhaseOffset;
        float sineWave = Mathf.Sin(time * moveSpeed);
        
        // Tính vị trí mục tiêu
        float targetX = Mathf.Lerp(leftBound, rightBound, (sineWave + 1f) * 0.5f);
        
        // ⭐ GIỚI HẠN TRONG MÀN HÌNH
        targetX = Mathf.Clamp(targetX, leftBound, rightBound);
        
        currentTargetX = targetX;
    }
    
    // ==================== DI CHUYỂN MƯỢT ====================
    void SmoothMove()
    {
        Vector3 pos = transform.position;
        
        // ⭐ SMOOTH DAMP cho chuyển động X
        pos.x = Mathf.SmoothDamp(
            pos.x, 
            currentTargetX, 
            ref velocityX, 
            smoothTime, 
            maxSmoothSpeed, 
            Time.deltaTime
        );
        
        transform.position = pos;
    }
    
    // ==================== PUBLIC METHODS ====================
    
    /// <summary>
    /// Reset vật cản về vị trí ban đầu và random lại chuyển động
    /// </summary>
    public void ResetObstacle()
    {
        transform.localPosition = startLocalPosition;
        RandomizeMovement();
        SetRandomStartPosition();
        velocityX = 0f;
        
        Debug.Log($"🔄 Reset: {gameObject.name}");
    }
    
    /// <summary>
    /// Đặt tốc độ di chuyển mới
    /// </summary>
    public void SetSpeed(float speed)
    {
        moveSpeed = Mathf.Clamp(speed, minSpeed, maxSpeed);
    }
    
    /// <summary>
    /// Random lại chuyển động (không reset vị trí)
    /// </summary>
    public void RandomizeOnly()
    {
        RandomizeMovement();
    }
    
    /// <summary>
    /// Di chuyển vật cản theo camera (parallax)
    /// </summary>
    public void MoveWithCamera(float cameraDeltaY)
    {
        Vector3 pos = transform.position;
        pos.y += cameraDeltaY * parallaxSpeed;
        transform.position = pos;
    }
    
    /// <summary>
    /// Đặt vị trí Y mới
    /// </summary>
    public void SetPositionY(float y)
    {
        Vector3 pos = transform.position;
        pos.y = y;
        transform.position = pos;
    }
}