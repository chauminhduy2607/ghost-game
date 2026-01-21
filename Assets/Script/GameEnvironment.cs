using UnityEngine;

/// <summary>
/// ⭐ BỎ GIỚI HẠN TRẦN - MA BAY LÊN VÔ HẠN!
/// - Chỉ giữ giới hạn đáy (không cho rơi xuống)
/// - Camera sẽ theo ma lên trên
/// </summary>
public class GameEnvironment : MonoBehaviour
{
    [Header("=== REFERENCE ===")]
    [SerializeField] private GhostController ghost;
    
    [Header("=== GIỚI HẠN ĐÁY (Chỉ Dưới) ===")]
    [SerializeField] private float groundY = -4f;  // Vị trí mặt đất
    [SerializeField] private bool constrainX = true;
    
    [Header("=== UI ===")]
    [SerializeField] private bool showDebugUI = true;
    [SerializeField] private int fontSize = 22;
    
    // Private variables
    private Camera mainCamera;
    private float objectHeight;
    private float maxHeightReached = 0f;  // Độ cao tối đa ma đã đạt được

    void Start()
    {
        mainCamera = Camera.main;
        
        // Tự động tìm GhostController
        if (ghost == null)
        {
            ghost = FindObjectOfType<GhostController>();
            if (ghost == null)
            {
                Debug.LogError("❌ Không tìm thấy GhostController!");
                enabled = false;
                return;
            }
        }
        
        // Tính chiều cao của ma
        SpriteRenderer spriteRenderer = ghost.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            objectHeight = spriteRenderer.bounds.extents.y;
        }
        
        // Constraint X (không cho di chuyển ngang)
        if (constrainX)
        {
            ghost.Rigidbody.constraints = RigidbodyConstraints2D.FreezePositionX;
        }
        
        Debug.Log("🌍 GameEnvironment: BỎ GIỚI HẠN TRẦN!");
        Debug.Log($"📐 Mặt đất: Y = {groundY}");
        Debug.Log("⬆️ Ma có thể bay lên vô hạn!");
    }
    
    void FixedUpdate()
    {
        CheckGround();
        TrackMaxHeight();
    }
    
    // ==================== CHỈ GIỚI HẠN ĐÁY ====================
    void CheckGround()
    {
        Vector3 pos = ghost.transform.position;
        Vector2 vel = ghost.Velocity;
        
        // Tính vị trí mặt đất (có tính chiều cao của ma)
        float minY = groundY + objectHeight;
        
        // ⭐ CHỈ GIỚI HẠN DƯỚI - Không giới hạn trên!
        if (pos.y <= minY)
        {
            pos.y = minY;
            
            // Trigger hiệu ứng móp khi chạm đất
            if (vel.y < -0.1f && !ghost.IsGroundSquashing)
            {
                ghost.TriggerGroundSquash();
                Debug.Log("💥 CHẠM ĐẤT!");
            }
            
            // Dừng velocity âm (không cho rơi xuống nữa)
            vel.y = Mathf.Max(vel.y, 0);
            
            ghost.transform.position = pos;
            ghost.SetVelocity(vel);
        }
    }
    
    // ==================== THEO DÕI ĐỘ CAO ====================
    void TrackMaxHeight()
    {
        float currentHeight = ghost.transform.position.y;
        
        if (currentHeight > maxHeightReached)
        {
            maxHeightReached = currentHeight;
        }
    }
    
    // ==================== UI ====================
    void OnGUI()
    {
        if (!showDebugUI || !Application.isPlaying) return;
        
        GUIStyle style = new GUIStyle();
        style.fontSize = fontSize;
        style.fontStyle = FontStyle.Bold;
        style.alignment = TextAnchor.UpperLeft;
        style.normal.textColor = Color.white;
        
        Vector2 vel = ghost.Velocity;
        string status = "⬆️ TỰ BAY CHẬM";
        
        if (ghost.IsFalling)
        {
            status = "⬇️ ĐANG RỚT!";
            style.normal.textColor = Color.red;
        }
        else if (ghost.IsDragging)
        {
            status = "🚀 ĐANG VUỐT - BAY NHANH!";
            style.normal.textColor = Color.cyan;
        }
        else if (vel.y > 3f)
        {
            status = "⬆️⬆️ BAY SIÊU NHANH!";
            style.normal.textColor = Color.green;
        }
        else if (ghost.transform.position.y < groundY + 2f)
        {
            status = "⚠️ GẦN ĐẤT!";
            style.normal.textColor = Color.yellow;
        }
        
        string info = $"{status}\n";
        info += $"━━━━━━━━━━━━━━━━━━━━\n";
        info += $"Velocity Y: {vel.y:F2}\n";
        info += $"Height: {ghost.transform.position.y:F1}\n";
        info += $"Max Height: {maxHeightReached:F1} 🏆\n";
        info += $"Position: ({ghost.transform.position.x:F1}, {ghost.transform.position.y:F1})";
        
        GUI.Label(new Rect(10, 10, 400, 280), info, style);
        
        // Hướng dẫn
        GUIStyle tipStyle = new GUIStyle();
        tipStyle.fontSize = 20;
        tipStyle.normal.textColor = Color.yellow;
        tipStyle.alignment = TextAnchor.LowerCenter;
        tipStyle.fontStyle = FontStyle.Bold;
        
        string tip = "👻 GHOST TỰ BAY LÊN CỰC CHẬM\n";
        tip += "🖐️ VUỐT LÊN = BAY SIÊU NHANH!\n";
        tip += "⬆️ KHÔNG GIỚI HẠN ĐỘ CAO! 🚀";
        
        GUI.Label(new Rect(0, Screen.height - 100, Screen.width, 100), tip, tipStyle);
    }
    
    // ==================== PUBLIC METHODS ====================
    
    /// <summary>
    /// Đặt vị trí mặt đất mới
    /// </summary>
    public void SetGroundY(float newGroundY)
    {
        groundY = newGroundY;
        Debug.Log($"🌍 Đổi mặt đất: Y = {groundY}");
    }
    
    /// <summary>
    /// Reset độ cao tối đa
    /// </summary>
    public void ResetMaxHeight()
    {
        maxHeightReached = ghost.transform.position.y;
        Debug.Log("🔄 Reset độ cao tối đa!");
    }
    
    /// <summary>
    /// Lấy độ cao tối đa
    /// </summary>
    public float GetMaxHeight()
    {
        return maxHeightReached;
    }
}