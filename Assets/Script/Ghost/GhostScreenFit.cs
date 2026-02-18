using UnityEngine;

/// <summary>
/// Scale con ma theo màn hình, đồng thời thông báo
/// cho FloatingAnimation cập nhật lại startScale.
/// Attach vào cùng GameObject với FloatingAnimation.
/// </summary>
public class GhostScreenFit : MonoBehaviour
{
    [Header("=== SCREEN FIT ===")]
    [Tooltip("Aspect ratio device bạn thiết kế game (iPhone 12 Pro Max ≈ 0.462)")]
    [SerializeField] private float referenceAspectRatio = 0.462f;
    [Tooltip("Tinh chỉnh thêm nếu thấy to/nhỏ quá")]
    [SerializeField] [Range(0.5f, 1.5f)] private float scaleMultiplier = 0.7f;

    private Camera mainCamera;

    void Awake()
    {
        // Awake chạy trước Start của FloatingAnimation
        // nên scale xong trước khi FloatingAnimation lưu startScale
        mainCamera = Camera.main;
        ApplyScale();
    }

    void ApplyScale()
    {
        if (mainCamera == null) return;

        float scaleFactor = (mainCamera.aspect / referenceAspectRatio) * scaleMultiplier;
        transform.localScale = new Vector3(
            transform.localScale.x * scaleFactor,
            transform.localScale.y * scaleFactor,
            transform.localScale.z
        );
    }
}