using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// ⭐ BACKGROUND LOOP VÔ HẠN + DI CHUYỂN MƯỢT
/// - Background di chuyển theo camera với parallax (mượt hơn)
/// - Tự động tạo background mới khi camera gần đến cuối
/// - Background mới xuất hiện từ GIỮA background cũ (50%)
/// - Xóa background cũ khi đã đi qua
/// </summary>
public class InfiniteBackground : MonoBehaviour
{
    [Header("=== CAMERA ===")]
    [SerializeField] private Camera mainCamera;
    
    [Header("=== PARALLAX (Di Chuyển Mượt) ===")]
    [Tooltip("Tốc độ di chuyển của background so với camera. 0 = đứng yên, 1 = theo camera 100%")]
    [SerializeField] private float parallaxSpeed = 0.5f;  // 0.5 = di chuyển chậm hơn camera 50%
    
    [Header("=== INFINITE SCROLL ===")]
    [Tooltip("Khoảng cách từ camera đến cuối background để tạo background mới (%)")]
    [SerializeField] [Range(0.3f, 0.8f)] private float spawnThreshold = 0.5f;  // Tạo mới ở 50%
    
    [Tooltip("Có xóa background cũ không?")]
    [SerializeField] private bool removeOldBackgrounds = true;
    
    [Header("=== DEBUG ===")]
    [SerializeField] private bool showDebugInfo = true;
    [SerializeField] private bool showGizmos = true;
    
    // Private
    private List<GameObject> backgrounds = new List<GameObject>();
    private float backgroundHeight;  // Chiều cao 1 background
    private Vector3 lastCameraPosition;
    private SpriteRenderer backgroundSprite;
    
    void Start()
    {
        // Tự động tìm camera
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
        
        // Lấy chiều cao của background hiện tại
        backgroundSprite = GetComponent<SpriteRenderer>();
        if (backgroundSprite == null)
        {
            Debug.LogError("❌ GameObject này phải có SpriteRenderer!");
            enabled = false;
            return;
        }
        
        backgroundHeight = backgroundSprite.bounds.size.y;
        
        // Thêm background hiện tại vào danh sách
        backgrounds.Add(gameObject);
        
        // Lưu vị trí camera ban đầu
        lastCameraPosition = mainCamera.transform.position;
        
        Debug.Log("🖼️ InfiniteBackground: Sẵn sàng!");
        Debug.Log($"📐 Background Height: {backgroundHeight}");
        Debug.Log($"📊 Parallax Speed: {parallaxSpeed}");
        Debug.Log($"📊 Spawn Threshold: {spawnThreshold * 100}%");
    }
    
    void LateUpdate()
    {
        MoveBackgroundWithParallax();
        CheckAndSpawnNewBackground();
        
        if (removeOldBackgrounds)
        {
            RemoveOldBackgrounds();
        }
        
        // Cập nhật vị trí camera
        lastCameraPosition = mainCamera.transform.position;
    }
    
    // ==================== PARALLAX - DI CHUYỂN MƯỢT ====================
    void MoveBackgroundWithParallax()
    {
        // Tính khoảng cách camera di chuyển
        float cameraDeltaY = mainCamera.transform.position.y - lastCameraPosition.y;
        
        // Di chuyển TẤT CẢ background với tốc độ parallax
        foreach (GameObject bg in backgrounds)
        {
            if (bg != null)
            {
                Vector3 pos = bg.transform.position;
                pos.y += cameraDeltaY * parallaxSpeed;
                bg.transform.position = pos;
            }
        }
    }
    
    // ==================== TẠO BACKGROUND MỚI ====================
    void CheckAndSpawnNewBackground()
    {
        if (backgrounds.Count == 0) return;
        
        // Lấy background cao nhất (mới nhất)
        GameObject topBackground = backgrounds[backgrounds.Count - 1];
        if (topBackground == null) return;
        
        float topBgTopEdge = topBackground.transform.position.y + (backgroundHeight / 2f);
        float cameraTopEdge = mainCamera.transform.position.y + (mainCamera.orthographicSize);
        
        // Tính khoảng cách còn lại
        float distanceToTop = topBgTopEdge - cameraTopEdge;
        float thresholdDistance = backgroundHeight * spawnThreshold;
        
        // ⭐ KHI CAMERA GẦN ĐẾN CUỐI (Ở 50%) → TẠO MỚI
        if (distanceToTop < thresholdDistance)
        {
            SpawnNewBackground(topBackground);
        }
    }
    
    void SpawnNewBackground(GameObject referenceBackground)
    {
        // ⭐ VỊ TRÍ MỚI: Từ GIỮA background cũ trở lên
        // Nếu spawnThreshold = 0.5 → background mới bắt đầu từ 50% background cũ
        float newY = referenceBackground.transform.position.y + (backgroundHeight * (1f - spawnThreshold));
        
        Vector3 newPosition = new Vector3(
            referenceBackground.transform.position.x,
            newY,
            referenceBackground.transform.position.z
        );
        
        // Tạo background mới
        GameObject newBackground = Instantiate(referenceBackground, newPosition, Quaternion.identity);
        newBackground.name = $"Background_{backgrounds.Count}";
        
        // Xóa script InfiniteBackground khỏi bản sao (chỉ giữ 1 script chính)
        InfiniteBackground duplicateScript = newBackground.GetComponent<InfiniteBackground>();
        if (duplicateScript != null && duplicateScript != this)
        {
            Destroy(duplicateScript);
        }
        
        backgrounds.Add(newBackground);
        
        Debug.Log($"🆕 Tạo background mới! Tổng: {backgrounds.Count} | Vị trí: Y = {newY:F1}");
    }
    
    // ==================== XÓA BACKGROUND CŨ ====================
    void RemoveOldBackgrounds()
    {
        if (backgrounds.Count <= 1) return;  // Giữ ít nhất 1 background
        
        float cameraBottomEdge = mainCamera.transform.position.y - mainCamera.orthographicSize;
        
        // Kiểm tra background đầu tiên (cũ nhất)
        GameObject oldestBackground = backgrounds[0];
        if (oldestBackground == null)
        {
            backgrounds.RemoveAt(0);
            return;
        }
        
        float bgBottomEdge = oldestBackground.transform.position.y - (backgroundHeight / 2f);
        
        // ⭐ XÓA NẾU BACKGROUND ĐÃ RA KHỎI CAMERA (Ở dưới)
        if (bgBottomEdge > cameraBottomEdge + backgroundHeight)
        {
            Debug.Log($"🗑️ Xóa background cũ: {oldestBackground.name}");
            
            // Không xóa background gốc (gameObject hiện tại)
            if (oldestBackground != gameObject)
            {
                backgrounds.RemoveAt(0);
                Destroy(oldestBackground);
            }
            else
            {
                backgrounds.RemoveAt(0);
            }
        }
    }
    
    
    void OnDrawGizmos()
    {
        if (!showGizmos || !Application.isPlaying) return;
        
        // Vẽ viền các background
        Gizmos.color = Color.yellow;
        foreach (GameObject bg in backgrounds)
        {
            if (bg != null)
            {
                SpriteRenderer sr = bg.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    Gizmos.DrawWireCube(bg.transform.position, sr.bounds.size);
                }
            }
        }
        
        // Vẽ vùng spawn threshold
        if (backgrounds.Count > 0 && backgrounds[backgrounds.Count - 1] != null)
        {
            GameObject topBg = backgrounds[backgrounds.Count - 1];
            float spawnLineY = topBg.transform.position.y + (backgroundHeight / 2f) - (backgroundHeight * spawnThreshold);
            
            Gizmos.color = Color.red;
            Gizmos.DrawLine(
                new Vector3(-100, spawnLineY, 0),
                new Vector3(100, spawnLineY, 0)
            );
        }
    }
    
    // ==================== PUBLIC METHODS ====================
    
    /// <summary>
    /// Đặt tốc độ parallax
    /// </summary>
    public void SetParallaxSpeed(float speed)
    {
        parallaxSpeed = Mathf.Clamp01(speed);
        Debug.Log($"🖼️ Đổi parallax speed: {parallaxSpeed}");
    }
    
    /// <summary>
    /// Đặt ngưỡng spawn
    /// </summary>
    public void SetSpawnThreshold(float threshold)
    {
        spawnThreshold = Mathf.Clamp(threshold, 0.3f, 0.8f);
        Debug.Log($"🖼️ Đổi spawn threshold: {spawnThreshold * 100}%");
    }
    
    /// <summary>
    /// Reset tất cả background
    /// </summary>
    public void ResetBackgrounds()
    {
        // Xóa tất cả background clone
        for (int i = backgrounds.Count - 1; i > 0; i--)
        {
            if (backgrounds[i] != null && backgrounds[i] != gameObject)
            {
                Destroy(backgrounds[i]);
            }
        }
        
        backgrounds.Clear();
        backgrounds.Add(gameObject);
        
        Debug.Log("🔄 Reset tất cả backgrounds!");
    }
}