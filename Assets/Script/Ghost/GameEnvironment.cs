using UnityEngine;

/// <summary>
/// ⭐ BỎ GIỚI HẠN TRẦN - MA BAY LÊN VÔ HẠN!
/// - Chỉ giữ giới hạn đáy (không cho rơi xuống)
/// - Camera sẽ theo ma lên trên
/// 
/// ⭐⭐ BỔ SUNG MỚI:
/// - Hiển thị trạng thái chạm vật cản
/// - Đếm ngược thời gian trước khi Game Over
/// - UI Game Over
/// </summary>
public class GameEnvironment : MonoBehaviour
{
    [Header("=== REFERENCE ===")]
    [SerializeField] private GhostController ghost;
    
    [Header("=== GIỚI HẠN ĐÁY (Chỉ Dưới) ===")]
    [SerializeField] private float groundY = -4f;
    [SerializeField] private bool constrainX = true;
    
    [Header("=== UI ===")]
    [SerializeField] private bool showDebugUI = true;
    [SerializeField] private int fontSize = 22;
    
    // Private variables
    private Camera mainCamera;
    private float objectHeight;
    private float maxHeightReached = 0f;

    void Start()
    {
        mainCamera = Camera.main;
        
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
        
        SpriteRenderer spriteRenderer = ghost.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            objectHeight = spriteRenderer.bounds.extents.y;
        }
        
        if (constrainX)
        {
            ghost.Rigidbody.constraints = RigidbodyConstraints2D.FreezePositionX;
        }
        
        Debug.Log("🌍 GameEnvironment: BỎ GIỚI HẠN TRẦN!");
        Debug.Log($"📐 Mặt đất: Y = {groundY}");
        Debug.Log("⬆️ Ma có thể bay lên vô hạn!");
    }
    
    void Update()
    {
        // Không cần restart nữa
    }
    
    void FixedUpdate()
    {
        CheckGround();
        TrackMaxHeight();
    }
    
    void CheckGround()
    {
        Vector3 pos = ghost.transform.position;
        Vector2 vel = ghost.Velocity;
        
        float minY = groundY + objectHeight;
        
        if (pos.y <= minY)
        {
            pos.y = minY;
            
            if (vel.y < -0.1f && !ghost.IsGroundSquashing)
            {
                ghost.TriggerGroundSquash();
                Debug.Log("💥 CHẠM ĐẤT!");
            }
            
            vel.y = Mathf.Max(vel.y, 0);
            
            ghost.transform.position = pos;
            ghost.SetVelocity(vel);
        }
    }
    
    void TrackMaxHeight()
    {
        float currentHeight = ghost.transform.position.y;
        
        if (currentHeight > maxHeightReached)
        {
            maxHeightReached = currentHeight;
        }
    }
    
    
    // ⭐⭐ HIỂN THỊ GAME OVER
    void ShowGameOverUI()
    {
        // Background đen mờ
        GUI.color = new Color(0, 0, 0, 0.8f);
        GUI.Box(new Rect(0, 0, Screen.width, Screen.height), "");
        GUI.color = Color.white;
        
        // Tiêu đề GAME OVER
        GUIStyle titleStyle = new GUIStyle();
        titleStyle.fontSize = 80;
        titleStyle.fontStyle = FontStyle.Bold;
        titleStyle.alignment = TextAnchor.MiddleCenter;
        titleStyle.normal.textColor = Color.red;
        
        GUI.Label(
            new Rect(0, Screen.height / 2 - 150, Screen.width, 100),
            "☠️ GAME OVER ☠️",
            titleStyle
        );
        
        // Thông tin kết quả
        GUIStyle infoStyle = new GUIStyle();
        infoStyle.fontSize = 30;
        infoStyle.fontStyle = FontStyle.Bold;
        infoStyle.alignment = TextAnchor.MiddleCenter;
        infoStyle.normal.textColor = Color.yellow;
        
        string result = "Độ cao đạt được: " + maxHeightReached.ToString("F1") + "m 🏆\n\n";
        result += "Bạn đã chạm vật cản!";
        
        GUI.Label(
            new Rect(0, Screen.height / 2 - 20, Screen.width, 200),
            result,
            infoStyle
        );
    }
    
    // Không cần hàm RestartGame nữa
    
    public void SetGroundY(float newGroundY)
    {
        groundY = newGroundY;
        Debug.Log("🌍 Đổi mặt đất: Y = " + groundY);
    }
    
    public void ResetMaxHeight()
    {
        maxHeightReached = ghost.transform.position.y;
        Debug.Log("🔄 Reset độ cao tối đa!");
    }
    
    public float GetMaxHeight()
    {
        return maxHeightReached;
    }
}