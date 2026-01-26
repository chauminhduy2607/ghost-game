using UnityEngine;

/// <summary>
/// ⭐ SPRITE ANIMATOR - ĐỔI HÌNH TỰ ĐỘNG
/// - Không cần Animation Controller
/// - Chỉ cần kéo thả các sprite vào array
/// - Tự động đổi hình theo thời gian
/// - Hỗ trợ random hoặc theo thứ tự
/// </summary>
public class SpriteAnimator : MonoBehaviour
{
    [Header("=== DANH SÁCH SPRITE ===")]
    [SerializeField] private Sprite[] sprites;  // Kéo thả tất cả frame vào đây
    
    [Header("=== CÀI ĐẶT ===")]
    [SerializeField] private float frameRate = 10f;  // Tốc độ đổi hình (frame/giây)
    [SerializeField] private bool randomOrder = false;  // Đổi ngẫu nhiên hay theo thứ tự?
    [SerializeField] private bool playOnStart = true;   // Tự động chạy khi bắt đầu?
    
    [Header("=== THÔNG TIN (CHỈ ĐỌC) ===")]
    [SerializeField] private int currentFrameIndex = 0;
    [SerializeField] private bool isPlaying = false;
    
    // Private
    private SpriteRenderer spriteRenderer;
    private float timer = 0f;
    private float frameTime;  // Thời gian mỗi frame
    
    void Start()
    {
        // Lấy SpriteRenderer
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        if (spriteRenderer == null)
        {
            Debug.LogError("❌ Không tìm thấy SpriteRenderer! Hãy thêm SpriteRenderer vào GameObject này.");
            enabled = false;
            return;
        }
        
        // Kiểm tra có sprite không
        if (sprites == null || sprites.Length == 0)
        {
            Debug.LogWarning("⚠️ Chưa có sprite nào! Hãy kéo thả sprite vào array 'Sprites' trong Inspector.");
            enabled = false;
            return;
        }
        
        // Tính thời gian mỗi frame
        frameTime = 1f / frameRate;
        
        // Set sprite đầu tiên
        if (sprites.Length > 0)
        {
            spriteRenderer.sprite = sprites[0];
        }
        
        // Tự động play?
        if (playOnStart)
        {
            Play();
        }
        
        Debug.Log("🎬 SpriteAnimator: Đã load " + sprites.Length + " sprites!");
        Debug.Log("⏱️ Frame Rate: " + frameRate + " fps (" + frameTime + "s/frame)");
        Debug.Log("🎲 Random: " + (randomOrder ? "BẬT" : "TẮT"));
    }
    
    void Update()
    {
        if (!isPlaying || sprites.Length == 0) return;
        
        // Đếm thời gian
        timer += Time.deltaTime;
        
        // Đã đủ thời gian để đổi frame?
        if (timer >= frameTime)
        {
            timer = 0f;
            NextFrame();
        }
    }
    
    // Chuyển sang frame tiếp theo
    void NextFrame()
    {
        if (randomOrder)
        {
            // Đổi random
            int newIndex = Random.Range(0, sprites.Length);
            
            // Đảm bảo không trùng với frame hiện tại (nếu có > 1 sprite)
            if (sprites.Length > 1)
            {
                while (newIndex == currentFrameIndex)
                {
                    newIndex = Random.Range(0, sprites.Length);
                }
            }
            
            currentFrameIndex = newIndex;
        }
        else
        {
            // Đổi theo thứ tự
            currentFrameIndex++;
            
            // Quay lại đầu nếu hết
            if (currentFrameIndex >= sprites.Length)
            {
                currentFrameIndex = 0;
            }
        }
        
        // Đổi sprite
        spriteRenderer.sprite = sprites[currentFrameIndex];
    }
    
    // ==================== PUBLIC METHODS ====================
    
    /// <summary>
    /// Bắt đầu chạy animation
    /// </summary>
    public void Play()
    {
        isPlaying = true;
        timer = 0f;
        Debug.Log("▶️ Play animation!");
    }
    
    /// <summary>
    /// Dừng animation
    /// </summary>
    public void Stop()
    {
        isPlaying = false;
        Debug.Log("⏸️ Stop animation!");
    }
    
    /// <summary>
    /// Pause animation (giữ nguyên frame hiện tại)
    /// </summary>
    public void Pause()
    {
        isPlaying = false;
        Debug.Log("⏸️ Pause animation!");
    }
    
    /// <summary>
    /// Resume animation
    /// </summary>
    public void Resume()
    {
        isPlaying = true;
        Debug.Log("▶️ Resume animation!");
    }
    
    /// <summary>
    /// Reset về frame đầu tiên
    /// </summary>
    public void Reset()
    {
        currentFrameIndex = 0;
        timer = 0f;
        
        if (sprites.Length > 0)
        {
            spriteRenderer.sprite = sprites[0];
        }
        
        Debug.Log("🔄 Reset animation!");
    }
    
    /// <summary>
    /// Đổi tốc độ animation
    /// </summary>
    public void SetFrameRate(float newFrameRate)
    {
        frameRate = Mathf.Max(1f, newFrameRate);  // Tối thiểu 1 fps
        frameTime = 1f / frameRate;
        Debug.Log("⏱️ Frame Rate: " + frameRate + " fps");
    }
    
    /// <summary>
    /// Bật/tắt chế độ random
    /// </summary>
    public void SetRandomOrder(bool random)
    {
        randomOrder = random;
        Debug.Log("🎲 Random: " + (randomOrder ? "BẬT" : "TẮT"));
    }
    
    /// <summary>
    /// Đổi sang sprite cụ thể
    /// </summary>
    public void SetFrame(int frameIndex)
    {
        if (frameIndex >= 0 && frameIndex < sprites.Length)
        {
            currentFrameIndex = frameIndex;
            spriteRenderer.sprite = sprites[frameIndex];
            Debug.Log("🖼️ Đổi sang frame " + frameIndex);
        }
        else
        {
            Debug.LogWarning("⚠️ Frame index không hợp lệ: " + frameIndex);
        }
    }
    
    /// <summary>
    /// Thêm sprite mới vào danh sách
    /// </summary>
    public void AddSprite(Sprite newSprite)
    {
        if (newSprite == null) return;
        
        // Tạo array mới lớn hơn
        Sprite[] newSprites = new Sprite[sprites.Length + 1];
        
        // Copy sprites cũ
        for (int i = 0; i < sprites.Length; i++)
        {
            newSprites[i] = sprites[i];
        }
        
        // Thêm sprite mới
        newSprites[sprites.Length] = newSprite;
        sprites = newSprites;
        
        Debug.Log("➕ Đã thêm sprite! Tổng: " + sprites.Length);
    }
    
    // ==================== GETTERS ====================
    
    public bool IsPlaying() => isPlaying;
    public int GetCurrentFrame() => currentFrameIndex;
    public int GetTotalFrames() => sprites.Length;
    public float GetFrameRate() => frameRate;
}