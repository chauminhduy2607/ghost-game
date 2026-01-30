using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// ⭐ BACKGROUND LOOP VÔ HẠN + DI CHUYỂN MƯỢT - ĐÃ SỬA LỖI
/// - Background di chuyển theo camera với parallax (mượt hơn)
/// - Tự động tạo background mới khi camera gần đến cuối
/// - Background mới xuất hiện theo thứ tự đã sắp xếp (LUÂN PHIÊN 5 BACKGROUND)
/// - Xóa background cũ khi đã đi qua
/// - Mỗi background có scale riêng biệt
/// 
/// ⭐⭐ SỬA LỖI:
/// - Background đầu tiên được tạo đúng cách
/// - Tính toán chiều cao chính xác cho mỗi background
/// - Spawn vị trí chính xác hơn
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

    [Header("=== BACKGROUNDS (5 CÁI KHÁC NHAU) ===")]
    [Tooltip("Danh sách các background sẽ được spawn theo thứ tự (LUÂN PHIÊN)")]
    [SerializeField] private List<Sprite> backgroundSprites = new List<Sprite>();

    [Tooltip("Danh sách các scale tương ứng cho từng background")]
    [SerializeField] private List<Vector3> backgroundScales = new List<Vector3>();
    
    [Tooltip("Layer để render background (nên để -1 hoặc 0)")]
    [SerializeField] private int sortingOrder = -1;

    [Header("=== DEBUG ===")]
    [SerializeField] private bool showDebugInfo = true;
    [SerializeField] private bool showGizmos = true;

    // Private
    private List<GameObject> backgrounds = new List<GameObject>();
    private Vector3 lastCameraPosition;
    private int currentBackgroundIndex = 0;  // Theo dõi background tiếp theo sẽ spawn

    void Start()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        // Kiểm tra danh sách background
        if (backgroundSprites.Count == 0)
        {
            Debug.LogError("❌ Không có background nào được chỉ định!");
            enabled = false;
            return;
        }

        // Kiểm tra nếu số lượng sprite và scale không khớp
        if (backgroundSprites.Count != backgroundScales.Count)
        {
            Debug.LogError("❌ Số lượng background sprites và scales không khớp!");
            Debug.LogError($"   Sprites: {backgroundSprites.Count} | Scales: {backgroundScales.Count}");
            enabled = false;
            return;
        }

        // ⭐ TẠO BACKGROUND ĐẦU TIÊN (từ GameObject hiện tại)
        CreateInitialBackground();

        // Lưu vị trí camera ban đầu
        lastCameraPosition = mainCamera.transform.position;

        Debug.Log("🖼️ InfiniteBackground: Sẵn sàng!");
        Debug.Log($"📊 Số lượng background: {backgroundSprites.Count}");
        Debug.Log($"📊 Parallax Speed: {parallaxSpeed}");
        Debug.Log($"📊 Spawn Threshold: {spawnThreshold * 100}%");
    }

    /// <summary>
    /// Tạo background đầu tiên từ GameObject hiện tại
    /// </summary>
    void CreateInitialBackground()
    {
        // Kiểm tra xem GameObject này đã có SpriteRenderer chưa
        SpriteRenderer existingRenderer = GetComponent<SpriteRenderer>();
        
        if (existingRenderer == null)
        {
            // Nếu chưa có, tạo mới
            existingRenderer = gameObject.AddComponent<SpriteRenderer>();
        }

        // Gán sprite và scale đầu tiên
        existingRenderer.sprite = backgroundSprites[0];
        existingRenderer.sortingOrder = sortingOrder;
        transform.localScale = backgroundScales[0];

        // Thêm GameObject này vào danh sách
        backgrounds.Add(gameObject);
        gameObject.name = "Background_0";

        // Background tiếp theo sẽ là index 1
        currentBackgroundIndex = 1 % backgroundSprites.Count;

        Debug.Log($"✅ Background đầu tiên: {backgroundSprites[0].name} | Scale: {backgroundScales[0]}");
    }

    void LateUpdate()
    {
        MoveBackgroundWithParallax();
        CheckAndSpawnNewBackground();

        if (removeOldBackgrounds)
        {
            RemoveOldBackgrounds();
        }

        lastCameraPosition = mainCamera.transform.position;
    }

    // ==================== PARALLAX - DI CHUYỂN MƯỢT ====================
    void MoveBackgroundWithParallax()
    {
        float cameraDeltaY = mainCamera.transform.position.y - lastCameraPosition.y;

        // Di chuyển tất cả các background với tốc độ parallax
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

        SpriteRenderer topSpriteRenderer = topBackground.GetComponent<SpriteRenderer>();
        if (topSpriteRenderer == null) return;

        float topBgHeight = topSpriteRenderer.bounds.size.y;
        float topBgTopEdge = topBackground.transform.position.y + (topBgHeight / 2f);
        float cameraTopEdge = mainCamera.transform.position.y + (mainCamera.orthographicSize);

        float distanceToTop = topBgTopEdge - cameraTopEdge;
        float thresholdDistance = topBgHeight * spawnThreshold;

        if (distanceToTop < thresholdDistance)
        {
            SpawnNewBackground(topBackground, topBgHeight);
        }
    }

    void SpawnNewBackground(GameObject previousBackground, float previousHeight)
    {
        // Lấy background tiếp theo trong danh sách
        if (backgroundSprites.Count == 0) return;

        // Tạo background mới từ sprite kế tiếp trong danh sách
        Sprite nextSprite = backgroundSprites[currentBackgroundIndex];
        Vector3 nextScale = backgroundScales[currentBackgroundIndex];

        // ⭐ VỊ TRÍ Y: Đặt sát phía trên background trước đó
        float newY = previousBackground.transform.position.y + previousHeight;

        Vector3 newPosition = new Vector3(
            previousBackground.transform.position.x, 
            newY, 
            previousBackground.transform.position.z
        );

        // Tạo GameObject mới cho background
        GameObject newBackground = new GameObject($"Background_{backgrounds.Count}");
        newBackground.transform.position = newPosition;

        // Gán SpriteRenderer cho background mới
        SpriteRenderer newSpriteRenderer = newBackground.AddComponent<SpriteRenderer>();
        newSpriteRenderer.sprite = nextSprite;
        newSpriteRenderer.sortingOrder = sortingOrder;

        // Áp dụng scale cho background mới
        newBackground.transform.localScale = nextScale;

        // Thêm vào danh sách backgrounds
        backgrounds.Add(newBackground);

        if (showDebugInfo)
        {
            Debug.Log($"🆕 Background #{backgrounds.Count}: {nextSprite.name}");
            Debug.Log($"   Vị trí: Y = {newY:F1} | Scale: {nextScale}");
            Debug.Log($"   Index: {currentBackgroundIndex}/{backgroundSprites.Count}");
        }

        // ⭐ LUÂN PHIÊN: Tiến tới background tiếp theo (quay vòng)
        currentBackgroundIndex = (currentBackgroundIndex + 1) % backgroundSprites.Count;
    }

    // ==================== XÓA BACKGROUND CŨ ====================
    void RemoveOldBackgrounds()
    {
        if (backgrounds.Count <= 2) return;  // Giữ ít nhất 2 background (1 hiện tại + 1 dự phòng)

        float cameraBottomEdge = mainCamera.transform.position.y - mainCamera.orthographicSize;

        GameObject oldestBackground = backgrounds[0];
        if (oldestBackground == null)
        {
            backgrounds.RemoveAt(0);
            return;
        }

        SpriteRenderer sr = oldestBackground.GetComponent<SpriteRenderer>();
        if (sr == null)
        {
            backgrounds.RemoveAt(0);
            Destroy(oldestBackground);
            return;
        }

        float bgHeight = sr.bounds.size.y;
        float bgBottomEdge = oldestBackground.transform.position.y - (bgHeight / 2f);

        // Xóa nếu background đã ra khỏi màn hình hoàn toàn
        if (bgBottomEdge > cameraBottomEdge + bgHeight)
        {
            if (showDebugInfo)
            {
                Debug.Log($"🗑️ Xóa background cũ: {oldestBackground.name}");
            }

            backgrounds.RemoveAt(0);
            Destroy(oldestBackground);
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

        // Vẽ đường spawn line
        if (backgrounds.Count > 0 && backgrounds[backgrounds.Count - 1] != null)
        {
            GameObject topBg = backgrounds[backgrounds.Count - 1];
            SpriteRenderer sr = topBg.GetComponent<SpriteRenderer>();
            
            if (sr != null)
            {
                float bgHeight = sr.bounds.size.y;
                float spawnLineY = topBg.transform.position.y + (bgHeight / 2f) - (bgHeight * spawnThreshold);

                Gizmos.color = Color.red;
                Gizmos.DrawLine(
                    new Vector3(-100, spawnLineY, 0),
                    new Vector3(100, spawnLineY, 0)
                );
            }
        }
    }

    // ==================== PUBLIC METHODS ====================

    public void SetParallaxSpeed(float speed)
    {
        parallaxSpeed = Mathf.Clamp01(speed);
        Debug.Log($"🖼️ Đổi parallax speed: {parallaxSpeed}");
    }

    public void SetSpawnThreshold(float threshold)
    {
        spawnThreshold = Mathf.Clamp(threshold, 0.3f, 0.8f);
        Debug.Log($"🖼️ Đổi spawn threshold: {spawnThreshold * 100}%");
    }

    public void ResetBackgrounds()
    {
        // Xóa tất cả background trừ GameObject gốc
        for (int i = backgrounds.Count - 1; i > 0; i--)
        {
            if (backgrounds[i] != null && backgrounds[i] != gameObject)
            {
                Destroy(backgrounds[i]);
            }
        }

        backgrounds.Clear();
        
        // Tạo lại background đầu tiên
        CreateInitialBackground();

        Debug.Log("🔄 Reset tất cả backgrounds!");
    }

    /// <summary>
    /// Thêm background mới vào danh sách (có thể gọi từ code khác)
    /// </summary>
    public void AddBackgroundSprite(Sprite sprite, Vector3 scale)
    {
        backgroundSprites.Add(sprite);
        backgroundScales.Add(scale);
        Debug.Log($"➕ Thêm background: {sprite.name} | Scale: {scale}");
    }

    /// <summary>
    /// Lấy thông tin background hiện tại
    /// </summary>
    public int GetCurrentBackgroundIndex()
    {
        return currentBackgroundIndex;
    }

    public int GetTotalBackgrounds()
    {
        return backgrounds.Count;
    }
}