using UnityEngine;

/// <summary>
/// ⭐ ĐƠN GIẢN HÓA: Chỉ giới hạn màn hình, không can thiệp vào physics
/// </summary>
public class GameEnvironment : MonoBehaviour
{
    [Header("=== REFERENCE ===")]
    [SerializeField] private GhostController ghost;
    
    [Header("=== GIỚI HẠN MÀN HÌNH ===")]
    [SerializeField] private float screenPadding = 0.3f;
    [SerializeField] private bool constrainX = true;
    
    [Header("=== UI ===")]
    [SerializeField] private bool showDebugUI = true;
    [SerializeField] private int fontSize = 22;
    
    // Private variables
    private Camera mainCamera;
    private Vector2 screenBounds;
    private float objectWidth, objectHeight;
    
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
        
        // Tính toán bounds
        SpriteRenderer spriteRenderer = ghost.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            objectWidth = spriteRenderer.bounds.extents.x;
            objectHeight = spriteRenderer.bounds.extents.y;
        }
        
        screenBounds = mainCamera.ScreenToWorldPoint(new Vector3(Screen.width, Screen.height, 0));
        
        // Constraint X
        if (constrainX)
        {
            ghost.Rigidbody.constraints = RigidbodyConstraints2D.FreezePositionX;
        }
        
        Debug.Log("🌍 GameEnvironment: Sẵn sàng!");
        Debug.Log($"📐 Screen: {screenBounds.x:F1}, {screenBounds.y:F1}");
    }
    
    void FixedUpdate()
    {
        ClampToScreen();
    }
    
    // ==================== GIỚI HẠN MÀN HÌNH ====================
    void ClampToScreen()
    {
        Vector3 pos = ghost.transform.position;
        Vector2 vel = ghost.Velocity;
        bool changed = false;
        
        float minY = -screenBounds.y + objectHeight + screenPadding;
        float maxY = screenBounds.y - objectHeight - screenPadding;
        
        // Chạm đất
        if (pos.y <= minY)
        {
            pos.y = minY;
            
            if (vel.y < -0.1f && !ghost.IsGroundSquashing)
            {
                ghost.TriggerGroundSquash();
                Debug.Log("💥 CHẠM ĐẤT!");
            }
            
            vel.y = Mathf.Max(vel.y, 0);  // Không cho velocity âm
            changed = true;
        }
        
        // Chạm trần
        if (pos.y >= maxY)
        {
            pos.y = maxY;
            vel.y = Mathf.Min(vel.y, 0);  // Không cho velocity dương
            changed = true;
            Debug.Log("⚠️ CHẠM TRẦN!");
        }
        
        ghost.transform.position = pos;
        
        if (changed)
        {
            ghost.SetVelocity(vel);
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
        
        if (ghost.IsDragging)
        {
            status = "🚀 ĐANG VUỐT - BAY NHANH!";
            style.normal.textColor = Color.cyan;
        }
        else if (vel.y > 3f)
        {
            status = "⬆️⬆️ BAY SIÊU NHANH!";
            style.normal.textColor = Color.green;
        }
        else if (ghost.transform.position.y < -screenBounds.y + 2f)
        {
            status = "⚠️ GẦN ĐẤT!";
            style.normal.textColor = Color.red;
        }
        
        string info = $"{status}\n";
        info += $"━━━━━━━━━━━━━━━━━━━━\n";
        info += $"Velocity Y: {vel.y:F2}\n";
        info += $"Height: {ghost.transform.position.y:F1}\n";
        info += $"Position: ({ghost.transform.position.x:F1}, {ghost.transform.position.y:F1})";
        
        GUI.Label(new Rect(10, 10, 400, 250), info, style);
        
        // Hướng dẫn
        GUIStyle tipStyle = new GUIStyle();
        tipStyle.fontSize = 20;
        tipStyle.normal.textColor = Color.yellow;
        tipStyle.alignment = TextAnchor.LowerCenter;
        tipStyle.fontStyle = FontStyle.Bold;
        
        string tip = "👻 GHOST TỰ BAY LÊN CỰC CHẬM (0.2)\n";
        tip += "🖐️ VUỐT LÊN = BAY SIÊU NHANH (x40)!";
        
        GUI.Label(new Rect(0, Screen.height - 80, Screen.width, 80), tip, tipStyle);
    }
}