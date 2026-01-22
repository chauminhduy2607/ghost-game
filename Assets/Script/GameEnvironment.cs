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
        // ⭐⭐ KIỂM TRA INPUT RESTART
        if (ghost != null && ghost.IsGameOver && Input.GetKeyDown(KeyCode.R))
        {
            RestartGame();
        }
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
    
    void OnGUI()
    {
        if (!showDebugUI || !Application.isPlaying) return;
        
        // ⭐⭐ GAME OVER UI
        if (ghost.IsGameOver)
        {
            ShowGameOverUI();
            return;
        }
        
        GUIStyle style = new GUIStyle();
        style.fontSize = fontSize;
        style.fontStyle = FontStyle.Bold;
        style.alignment = TextAnchor.UpperLeft;
        style.normal.textColor = Color.white;
        
        Vector2 vel = ghost.Velocity;
        string status = "⬆️ TỰ BAY CHẬM";
        
        // ⭐⭐ TRẠNG THÁI CHẠM VẬT CẢN
        if (ghost.HitObstacle)
        {
            status = "💥💥 CHẠM VẬT CẢN - ĐANG RỚT!!!";
            style.normal.textColor = Color.red;
        }
        else if (ghost.IsFalling)
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
        
        string info = status + "\n";
        info += "━━━━━━━━━━━━━━━━━━━━\n";
        
        // ⭐⭐ HIỂN THỊ ĐẾM NGƯỢC NẾU CHẠM VẬT CẢN
        if (ghost.HitObstacle)
        {
            float timeLeft = 3f - ghost.ObstacleTimer;
            info += "⏱️ THỜI GIAN CÒN: " + timeLeft.ToString("F1") + "s\n";
            info += "━━━━━━━━━━━━━━━━━━━━\n";
        }
        
        info += "Velocity Y: " + vel.y.ToString("F2") + "\n";
        info += "Height: " + ghost.transform.position.y.ToString("F1") + "\n";
        info += "Max Height: " + maxHeightReached.ToString("F1") + " 🏆\n";
        info += "Position: (" + ghost.transform.position.x.ToString("F1") + ", " + ghost.transform.position.y.ToString("F1") + ")";
        
        GUI.Label(new Rect(10, 10, 400, 320), info, style);
        
        // Hướng dẫn
        GUIStyle tipStyle = new GUIStyle();
        tipStyle.fontSize = 20;
        tipStyle.normal.textColor = Color.yellow;
        tipStyle.alignment = TextAnchor.LowerCenter;
        tipStyle.fontStyle = FontStyle.Bold;
        
        string tip = "";
        
        // ⭐⭐ CẢNH BÁO VẬT CẢN
        if (ghost.HitObstacle)
        {
            tip = "💥 ĐÃ CHẠM VẬT CẢN!\n";
            tip += "⚠️ KHÔNG THỂ VUỐT LÊN NỮA!\n";
            float timeLeft = 3f - ghost.ObstacleTimer;
            tip += "⏱️ THUA SAU " + timeLeft.ToString("F1") + "s...";
            tipStyle.normal.textColor = Color.red;
        }
        else
        {
            tip = "👻 GHOST TỰ BAY LÊN CỰC CHẬM\n";
            tip += "🖐️ VUỐT LÊN = BAY SIÊU NHANH!\n";
            tip += "⬆️ KHÔNG GIỚI HẠN ĐỘ CAO! 🚀\n";
            tip += "⚠️ TRÁNH VẬT CẢN!";
        }
        
        GUI.Label(new Rect(0, Screen.height - 120, Screen.width, 120), tip, tipStyle);
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
        result += "Bạn đã chạm vật cản!\n";
        result += "Nhấn R để chơi lại";
        
        GUI.Label(
            new Rect(0, Screen.height / 2 - 20, Screen.width, 200),
            result,
            infoStyle
        );
    }
    
    // ⭐⭐ RESTART GAME
    void RestartGame()
    {
        // Reset ghost
        ghost.ResetGame();
        
        // Reset camera position nếu cần
        ghost.transform.position = new Vector3(0, groundY + objectHeight + 1f, 0);
        
        // Reset max height
        maxHeightReached = 0f;
        
        Debug.Log("🔄 RESTART GAME!");
    }
    
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