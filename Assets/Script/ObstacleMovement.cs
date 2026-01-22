using UnityEngine;

/// <summary>
/// ⭐ SCRIPT DI CHUYỂN VẬT CẢN - CHỈ LO VIỆC MOVE NGANG
/// - Di chuyển qua lại với pattern sin wave
/// - Giới hạn trong màn hình
/// - Mỗi vật cản có chuyển động độc lập
/// </summary>
public class ObstacleMovement : MonoBehaviour
{
    [Header("=== DI CHUYỂN NGANG ===")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float minSpeed = 1f;
    [SerializeField] private float maxSpeed = 4f;
    
    [Header("=== GIỚI HẠN MÀN HÌNH ===")]
    [SerializeField] private float screenPadding = 0.5f;
    
    [Header("=== SMOOTH ===")]
    [SerializeField] private float smoothTime = 0.1f;
    [SerializeField] private float maxSmoothSpeed = 999f;
    
    // Private
    private Camera mainCamera;
    private float leftBound;
    private float rightBound;
    private float currentTargetX;
    private float velocityX = 0f;
    private float randomPhaseOffset;
    private float randomSpeedMultiplier;
    
    // Public
    public float CurrentX => transform.position.x;
    public float TargetX => currentTargetX;
    
    void Awake()
    {
        mainCamera = Camera.main;
        CalculateScreenBounds();
        RandomizeMovement();
        SetRandomStartPosition();
    }
    
    void Update()
    {
        UpdateTargetPosition();
        SmoothMove();
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
    }
    
    // ==================== RANDOM HÓA CHUYỂN ĐỘNG ====================
    void RandomizeMovement()
    {
        // Random phase offset để mỗi vật cản khác nhau
        randomPhaseOffset = Random.Range(0f, Mathf.PI * 2f);
        
        // Random tốc độ
        randomSpeedMultiplier = Random.Range(minSpeed, maxSpeed) / moveSpeed;
    }
    
    // ==================== VỊ TRÍ BAN ĐẦU NGẪU NHIÊN ====================
    void SetRandomStartPosition()
    {
        float randomX = Random.Range(leftBound, rightBound);
        currentTargetX = randomX;
        
        Vector3 pos = transform.position;
        pos.x = randomX;
        transform.position = pos;
    }
    
    // ==================== CẬP NHẬT VỊ TRÍ MỤC TIÊU ====================
    void UpdateTargetPosition()
    {
        // ⭐ DI CHUYỂN THEO SINE WAVE (smooth)
        float time = Time.time * randomSpeedMultiplier + randomPhaseOffset;
        float sineWave = Mathf.Sin(time * moveSpeed);
        
        // Tính vị trí mục tiêu
        float targetX = Mathf.Lerp(leftBound, rightBound, (sineWave + 1f) * 0.5f);
        
        // Giới hạn trong màn hình
        targetX = Mathf.Clamp(targetX, leftBound, rightBound);
        
        currentTargetX = targetX;
    }
    
    // ==================== DI CHUYỂN MƯỢT ====================
    void SmoothMove()
    {
        Vector3 pos = transform.position;
        
        // SmoothDamp cho chuyển động mượt
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
    /// Random lại chuyển động
    /// </summary>
    public void Randomize()
    {
        RandomizeMovement();
    }
    
    /// <summary>
    /// Đặt tốc độ mới
    /// </summary>
    public void SetSpeed(float speed)
    {
        moveSpeed = Mathf.Clamp(speed, minSpeed, maxSpeed);
    }
    
    /// <summary>
    /// Reset về vị trí ban đầu
    /// </summary>
    public void ResetPosition()
    {
        SetRandomStartPosition();
        velocityX = 0f;
    }
    
    /// <summary>
    /// Dừng di chuyển
    /// </summary>
    public void Stop()
    {
        enabled = false;
        velocityX = 0f;
    }
    
    /// <summary>
    /// Tiếp tục di chuyển
    /// </summary>
    public void Resume()
    {
        enabled = true;
    }
}