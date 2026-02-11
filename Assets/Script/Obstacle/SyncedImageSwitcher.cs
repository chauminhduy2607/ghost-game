using UnityEngine;

/// <summary>
/// Đổi ảnh theo 2 phase:
/// Phase 1 (Normal): Chạy qua list ảnh bình thường
/// Phase 2 (Sync): Cả 2 vật cản cùng hiện 1 ảnh đặc biệt
/// </summary>
public class SyncedImageSwitcher : MonoBehaviour
{
    [System.Serializable]
    public class ImageData
    {
        public Sprite sprite;
        public Vector3 scale = new Vector3(0.06741104f, 0.06156023f, 1f);
    }

    [Header("=== PARTNER ===")]
    [Tooltip("Kéo vật cản còn lại vào đây")]
    [SerializeField] private SyncedImageSwitcher partner;

    [Header("=== PHASE 1: LIST ẢNH THƯỜNG ===")]
    [SerializeField] private ImageData[] normalImages;
    [SerializeField] private float normalSwitchTime = 0.5f;
    [Tooltip("Thời gian chạy phase 1 trước khi chuyển sang phase 2")]
    [SerializeField] private float normalPhaseDuration = 3f;

    [Header("=== PHASE 2: ẢNH SYNC ===")]
    [SerializeField] private ImageData syncImage;
    [Tooltip("Thời gian giữ ảnh sync trước khi quay về phase 1")]
    [SerializeField] private float syncPhaseDuration = 1f;

    private SpriteRenderer spriteRenderer;
    private int currentIndex = 0;
    private float switchTimer = 0f;
    private float phaseTimer = 0f;
    private bool isSyncPhase = false;
    private bool isMaster = false; // Chỉ 1 object điều khiển timing

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        // Object nào không có partner tự nhận là master
        // Hoặc nếu có partner, object đứng trước trong scene là master
        if (partner == null)
        {
            isMaster = true;
        }
        else
        {
            // So sánh sibling index để quyết định master
            isMaster = transform.GetSiblingIndex() < partner.transform.GetSiblingIndex();
        }

        if (normalImages.Length > 0)
            ShowNormalImage(0);
    }

    void Update()
    {
        // Chỉ master mới tự chạy timer
        // Slave được master gọi trực tiếp
        if (!isMaster) return;

        phaseTimer += Time.deltaTime;

        if (!isSyncPhase)
        {
            // Phase 1: Chạy list ảnh
            RunNormalPhase();

            if (phaseTimer >= normalPhaseDuration)
            {
                EnterSyncPhase();
            }
        }
        else
        {
            // Phase 2: Giữ ảnh sync
            if (phaseTimer >= syncPhaseDuration)
            {
                EnterNormalPhase();
            }
        }
    }

    void RunNormalPhase()
    {
        if (normalImages.Length == 0) return;

        switchTimer += Time.deltaTime;

        if (switchTimer >= normalSwitchTime)
        {
            switchTimer = 0f;
            currentIndex = (currentIndex + 1) % normalImages.Length;
            ShowNormalImage(currentIndex);

            // Đồng bộ partner cùng index
            if (partner != null)
                partner.ShowNormalImage(currentIndex);
        }
    }

    void EnterSyncPhase()
    {
        isSyncPhase = true;
        phaseTimer = 0f;
        switchTimer = 0f;

        ShowSyncImage();

        if (partner != null)
            partner.ShowSyncImage();
    }

    void EnterNormalPhase()
    {
        isSyncPhase = false;
        phaseTimer = 0f;
        switchTimer = 0f;
        currentIndex = 0;

        if (normalImages.Length > 0)
        {
            ShowNormalImage(currentIndex);

            if (partner != null)
                partner.ShowNormalImage(currentIndex);
        }
    }

    // ========== PUBLIC (được master gọi) ==========

    public void ShowNormalImage(int index)
    {
        if (normalImages.Length == 0 || spriteRenderer == null) return;
        if (index < 0 || index >= normalImages.Length) return;

        spriteRenderer.sprite = normalImages[index].sprite;
        transform.localScale = normalImages[index].scale;
    }

    public void ShowSyncImage()
    {
        if (syncImage == null || syncImage.sprite == null || spriteRenderer == null) return;

        spriteRenderer.sprite = syncImage.sprite;
        transform.localScale = syncImage.scale;
    }

    // ========== PUBLIC UTILS ==========

    public void SetPartner(SyncedImageSwitcher newPartner) => partner = newPartner;
    public void SetNormalSwitchTime(float t) => normalSwitchTime = Mathf.Max(0.1f, t);
    public void SetNormalPhaseDuration(float t) => normalPhaseDuration = Mathf.Max(0.1f, t);
    public void SetSyncPhaseDuration(float t) => syncPhaseDuration = Mathf.Max(0.1f, t);
    public bool IsSyncPhase => isSyncPhase;
}