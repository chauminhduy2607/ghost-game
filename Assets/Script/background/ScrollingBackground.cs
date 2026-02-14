using UnityEngine;

public class ScrollingBackground : MonoBehaviour
{
    [Header("=== CÀI ĐẶT CUỘN ===")]
    [SerializeField] private float scrollSpeed = 2f;
    [SerializeField] private bool autoScroll = true;
    
    [Header("=== DEBUG ===")]
    [SerializeField] private bool showDebug = false;
    
    private Material material;
    private Vector2 offset;
    private bool isWorking = false;
    private SpriteRenderer spriteRenderer;
    
    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        if (spriteRenderer == null || spriteRenderer.sprite == null)
        {
            return;
        }
        
        Texture2D texture = spriteRenderer.sprite.texture;
        
        if (texture == null)
        {
            return;
        }
        
        if (texture.wrapMode != TextureWrapMode.Repeat)
        {
            texture.wrapMode = TextureWrapMode.Repeat;
        }
        
        material = new Material(Shader.Find("Sprites/Default"));
        
        if (material == null)
        {
            return;
        }
        
        material.mainTexture = texture;
        spriteRenderer.material = material;
        material.mainTextureScale = new Vector2(1, 3);
        
        isWorking = true;
    }
    
    void Update()
    {
        if (!isWorking || material == null || !autoScroll)
        {
            return;
        }
        
        offset.y -= scrollSpeed * Time.deltaTime;
        material.mainTextureOffset = offset;
    }
    
    void OnGUI()
    {
        if (!showDebug || !Application.isPlaying) return;
        
        GUIStyle style = new GUIStyle();
        style.fontSize = 18;
        style.fontStyle = FontStyle.Bold;
        style.normal.textColor = isWorking ? Color.cyan : Color.red;
        
        string status = isWorking ? "✅ SCROLL OK" : "❌ SCROLL ERROR";
        string info = $"{status}\n";
        
        if (isWorking)
        {
            info += $"Offset Y: {offset.y:F2}\n";
            info += $"Speed: {scrollSpeed:F1}";
        }
        
        float x = Screen.width - 200;
        GUI.Box(new Rect(x, 5, 190, 80), "");
        GUI.Label(new Rect(x + 5, 10, 180, 70), info, style);
    }
    
    public void SetScrollSpeed(float speed) => scrollSpeed = speed;
    public void StopScrolling() => autoScroll = false;
    public void StartScrolling() => autoScroll = true;
    public bool IsScrolling() => isWorking && autoScroll;
}