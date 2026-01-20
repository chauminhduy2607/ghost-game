using UnityEngine;

public class ScrollingBackground : MonoBehaviour
{
    [Header("=== CÀI ĐẶT CUỘN ===")]
    [SerializeField] private float scrollSpeed = 2f;
    [SerializeField] private bool autoScroll = true;
    
    [Header("=== DEBUG ===")]
    [SerializeField] private bool showDebug = true;
    
    private Material material;
    private Vector2 offset;
    private bool isWorking = false;
    private SpriteRenderer spriteRenderer;
    
    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        // Kiểm tra SpriteRenderer
        if (spriteRenderer == null)
        {
            Debug.LogError("❌ BACKGROUND KHÔNG CÓ SpriteRenderer! Hãy thêm component này vào!");
            return;
        }
        
        // Kiểm tra Sprite
        if (spriteRenderer.sprite == null)
        {
            Debug.LogError("❌ BACKGROUND KHÔNG CÓ SPRITE! Kéo hình vào ô Sprite trong Inspector!");
            return;
        }
        
        // Lấy texture
        Texture2D texture = spriteRenderer.sprite.texture;
        
        if (texture == null)
        {
            Debug.LogError("❌ Sprite không có texture!");
            return;
        }
        
        // ⭐ QUAN TRỌNG: Setup Wrap Mode
        if (texture.wrapMode != TextureWrapMode.Repeat)
        {
            Debug.LogWarning("⚠️ Texture Wrap Mode không phải Repeat! Đang tự động sửa...");
            texture.wrapMode = TextureWrapMode.Repeat;
        }
        
        // Tạo material mới (clone để không ảnh hưởng asset gốc)
        material = new Material(Shader.Find("Sprites/Default"));
        
        if (material == null)
        {
            Debug.LogError("❌ Không tìm thấy Shader 'Sprites/Default'!");
            return;
        }
        
        material.mainTexture = texture;
        spriteRenderer.material = material;
        
        // Setup tiling (lặp lại texture)
        material.mainTextureScale = new Vector2(1, 3);
        
        isWorking = true;
        
        Debug.Log("✅✅✅ BACKGROUND SẴN SÀNG SCROLL! ✅✅✅");
        Debug.Log($"📊 Texture: {texture.name} ({texture.width}x{texture.height})");
        Debug.Log($"📊 Wrap Mode: {texture.wrapMode}");
        Debug.Log($"📊 Scroll Speed: {scrollSpeed}");
        Debug.Log($"📊 Material: {material.name}");
    }
    
    void Update()
    {
        if (!isWorking || material == null || !autoScroll)
        {
            if (!isWorking)
                Debug.LogWarning("⚠️ Background chưa khởi động được! Kiểm tra Console log!");
            return;
        }
        
        // ⭐ SCROLL TỪ TRÊN XUỐNG (offset.y giảm dần)
        offset.y -= scrollSpeed * Time.deltaTime;
        
        // Áp dụng offset vào material
        material.mainTextureOffset = offset;
    }
    
    void OnGUI()
    {
        if (!showDebug || !Application.isPlaying) return;
        
        GUIStyle style = new GUIStyle();
        style.fontSize = 24;
        style.fontStyle = FontStyle.Bold;
        style.normal.textColor = isWorking ? Color.cyan : Color.red;
        
        string status = isWorking ? "✅ SCROLL HOẠT ĐỘNG" : "❌ SCROLL CHƯA HOẠT ĐỘNG";
        string info = $"{status}\n";
        info += "━━━━━━━━━━━━━━━━\n";
        
        if (isWorking)
        {
            info += $"Offset Y: {offset.y:F2}\n";
            info += $"Speed: {scrollSpeed:F1}\n";
            info += $"Material: OK\n";
        }
        else
        {
            info += "KIỂM TRA:\n";
            info += $"SpriteRenderer: {(spriteRenderer != null ? "✅" : "❌")}\n";
            info += $"Material: {(material != null ? "✅" : "❌")}\n";
        }
        
        info += $"Frame: {Time.frameCount}";
        
        // Vẽ ở góc phải trên
        float x = Screen.width - 300;
        GUI.Box(new Rect(x, 5, 290, 170), "");
        GUI.Label(new Rect(x + 5, 10, 280, 160), info, style);
    }
    
    // ==================== PUBLIC METHODS ====================
    
    public void SetScrollSpeed(float speed)
    {
        scrollSpeed = speed;
        Debug.Log($"📊 Scroll Speed đổi thành: {speed}");
    }
    
    public void StopScrolling()
    {
        autoScroll = false;
        Debug.Log("⏸️ DỪNG SCROLL!");
    }
    
    public void StartScrolling()
    {
        autoScroll = true;
        Debug.Log("▶️ BẮT ĐẦU SCROLL!");
    }
    
    // Kiểm tra trạng thái
    public bool IsScrolling()
    {
        return isWorking && autoScroll;
    }
}       