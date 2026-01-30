using UnityEngine;

/// <summary>
/// ⭐ GIỚI HẠN MA TRONG BACKGROUND
/// - Con ma không thể bay ra ngoài background hiện tại
/// - Tự động tính toán giới hạn dựa trên background đang hiển thị
/// - Khi chạm vật cản và bị bắn ra, ma sẽ bị giữ lại trong màn hình
/// </summary>
public class GhostBoundaryConstraint : MonoBehaviour
{
    [Header("=== REFERENCES ===")]
    [SerializeField] private GhostController ghost;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private InfiniteBackground backgroundManager;
    
    [Header("=== GIỚI HẠN X (Trái Phải) ===")]
    [Tooltip("Khoảng cách tối thiểu từ mép màn hình (đơn vị)")]
    [SerializeField] private float horizontalPadding = 0.5f;
    
    [Tooltip("Có giới hạn trục X không?")]
    [SerializeField] private bool constrainX = true;
    
    [Header("=== GIỚI HẠN Y (Trên Dưới) ===")]
    [Tooltip("Có giới hạn trục Y không?")]
    [SerializeField] private bool constrainY = true;
    
    [Tooltip("Khoảng cách padding từ mép trên/dưới màn hình")]
    [SerializeField] private float verticalPadding = 1f;
    
    [Header("=== ĐẨY MƯỢT VỀ TRONG ===")]
    [Tooltip("Tốc độ đẩy ma về trong khi ma ở ngoài")]
    [SerializeField] private float pushBackForce = 5f;
    
    [Tooltip("Giảm vận tốc khi chạm mép (0-1, 1 = không giảm)")]
    [SerializeField] [Range(0f, 1f)] private float velocityDampening = 0.7f;
    
    [Header("=== DEBUG ===")]
    [SerializeField] private bool showDebugInfo = false;
    [SerializeField] private bool showBoundaryGizmos = true;
    
    // Private
    private float ghostRadius;
    private Vector2 currentMinBounds;
    private Vector2 currentMaxBounds;
    
    void Start()
    {
        // Tự động tìm references
        if (ghost == null)
        {
            ghost = FindObjectOfType<GhostController>();
        }
        
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
        
        if (backgroundManager == null)
        {
            backgroundManager = FindObjectOfType<InfiniteBackground>();
        }
        
        if (ghost == null)
        {
            Debug.LogError("❌ Không tìm thấy GhostController!");
            enabled = false;
            return;
        }
        
        // Tính bán kính của ghost
        SpriteRenderer sr = ghost.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            ghostRadius = Mathf.Max(sr.bounds.extents.x, sr.bounds.extents.y);
        }
        else
        {
            ghostRadius = 0.5f;
        }
        
        Debug.Log("🚧 GhostBoundaryConstraint: Sẵn sàng!");
        Debug.Log($"📐 Ghost Radius: {ghostRadius:F2}");
        Debug.Log($"🔒 Constrain X: {constrainX} | Constrain Y: {constrainY}");
    }
    
    void LateUpdate()
    {
        if (ghost == null) return;
        
        UpdateBounds();
        ApplyConstraints();
    }
    
    /// <summary>
    /// Cập nhật giới hạn dựa trên camera
    /// </summary>
    void UpdateBounds()
    {
        float camHeight = mainCamera.orthographicSize;
        float camWidth = camHeight * mainCamera.aspect;
        
        Vector3 camPos = mainCamera.transform.position;
        
        // Giới hạn X (trái phải)
        currentMinBounds.x = camPos.x - camWidth + horizontalPadding + ghostRadius;
        currentMaxBounds.x = camPos.x + camWidth - horizontalPadding - ghostRadius;
        
        // Giới hạn Y (trên dưới)
        currentMinBounds.y = camPos.y - camHeight + verticalPadding + ghostRadius;
        currentMaxBounds.y = camPos.y + camHeight - verticalPadding - ghostRadius;
        
        if (showDebugInfo && Time.frameCount % 60 == 0)
        {
            Debug.Log($"📊 Bounds - X: [{currentMinBounds.x:F1}, {currentMaxBounds.x:F1}] | Y: [{currentMinBounds.y:F1}, {currentMaxBounds.y:F1}]");
        }
    }
    
    /// <summary>
    /// Áp dụng giới hạn vị trí cho ma
    /// </summary>
    void ApplyConstraints()
    {
        Vector3 pos = ghost.transform.position;
        Vector2 velocity = ghost.Velocity;
        bool needsCorrection = false;
        
        // ⭐ GIỚI HẠN X (TRÁI PHẢI)
        if (constrainX)
        {
            if (pos.x < currentMinBounds.x)
            {
                pos.x = currentMinBounds.x;
                velocity.x = Mathf.Max(velocity.x, 0); // Chỉ cho phép đi sang phải
                velocity.x *= velocityDampening;
                needsCorrection = true;
                
                if (showDebugInfo)
                {
                    Debug.Log("⬅️ MA CHẠM MÉP TRÁI!");
                }
            }
            else if (pos.x > currentMaxBounds.x)
            {
                pos.x = currentMaxBounds.x;
                velocity.x = Mathf.Min(velocity.x, 0); // Chỉ cho phép đi sang trái
                velocity.x *= velocityDampening;
                needsCorrection = true;
                
                if (showDebugInfo)
                {
                    Debug.Log("➡️ MA CHẠM MÉP PHẢI!");
                }
            }
        }
        
        // ⭐ GIỚI HẠN Y (TRÊN DƯỚI)
        if (constrainY)
        {
            if (pos.y < currentMinBounds.y)
            {
                pos.y = currentMinBounds.y;
                velocity.y = Mathf.Max(velocity.y, 0); // Chỉ cho phép đi lên
                velocity.y *= velocityDampening;
                needsCorrection = true;
                
                if (showDebugInfo)
                {
                    Debug.Log("⬇️ MA CHẠM MÉP DƯỚI!");
                }
            }
            else if (pos.y > currentMaxBounds.y)
            {
                pos.y = currentMaxBounds.y;
                velocity.y = Mathf.Min(velocity.y, 0); // Chỉ cho phép đi xuống
                velocity.y *= velocityDampening;
                needsCorrection = true;
                
                if (showDebugInfo)
                {
                    Debug.Log("⬆️ MA CHẠM MÉP TRÊN!");
                }
            }
        }
        
        // Áp dụng nếu có thay đổi
        if (needsCorrection)
        {
            ghost.transform.position = pos;
            ghost.SetVelocity(velocity);
        }
    }
    
    void OnDrawGizmos()
    {
        if (!showBoundaryGizmos || !Application.isPlaying) return;
        if (mainCamera == null) return;
        
        // Vẽ khung giới hạn
        Gizmos.color = Color.cyan;
        
        Vector3 center = new Vector3(
            (currentMinBounds.x + currentMaxBounds.x) / 2f,
            (currentMinBounds.y + currentMaxBounds.y) / 2f,
            0
        );
        
        Vector3 size = new Vector3(
            currentMaxBounds.x - currentMinBounds.x,
            currentMaxBounds.y - currentMinBounds.y,
            0
        );
        
        Gizmos.DrawWireCube(center, size);
        
        // Vẽ vị trí ma
        if (ghost != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(ghost.transform.position, ghostRadius);
        }
    }
    
    // ==================== PUBLIC METHODS ====================
    
    /// <summary>
    /// Bật/tắt giới hạn X
    /// </summary>
    public void SetConstrainX(bool value)
    {
        constrainX = value;
        Debug.Log($"🔒 Giới hạn X: {(value ? "BẬT" : "TẮT")}");
    }
    
    /// <summary>
    /// Bật/tắt giới hạn Y
    /// </summary>
    public void SetConstrainY(bool value)
    {
        constrainY = value;
        Debug.Log($"🔒 Giới hạn Y: {(value ? "BẬT" : "TẮT")}");
    }
    
    /// <summary>
    /// Đặt padding ngang
    /// </summary>
    public void SetHorizontalPadding(float padding)
    {
        horizontalPadding = padding;
        Debug.Log($"🔒 Horizontal Padding: {padding}");
    }
    
    /// <summary>
    /// Đặt padding dọc
    /// </summary>
    public void SetVerticalPadding(float padding)
    {
        verticalPadding = padding;
        Debug.Log($"🔒 Vertical Padding: {padding}");
    }
    
    /// <summary>
    /// Kiểm tra ma có nằm trong giới hạn không
    /// </summary>
    public bool IsGhostInBounds()
    {
        if (ghost == null) return true;
        
        Vector3 pos = ghost.transform.position;
        
        bool inX = pos.x >= currentMinBounds.x && pos.x <= currentMaxBounds.x;
        bool inY = pos.y >= currentMinBounds.y && pos.y <= currentMaxBounds.y;
        
        return inX && inY;
    }
    
    /// <summary>
    /// Lấy thông tin giới hạn hiện tại
    /// </summary>
    public void GetCurrentBounds(out Vector2 min, out Vector2 max)
    {
        min = currentMinBounds;
        max = currentMaxBounds;
    }
}