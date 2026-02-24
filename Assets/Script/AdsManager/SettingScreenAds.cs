using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.Advertisements;

/// <summary>
/// Quản lý hiển thị Banner Ad trên SettingScreen.
/// 
/// HƯỚNG DẪN SETUP TRONG UNITY:
/// 1. Gắn script này vào GameObject "ads" trong Canvas
/// 2. Kéo "AdPlaceholder" vào field Ad Placeholder
/// 3. Không cần gán Ad Container (dùng chính GameObject này)
/// 4. Banner Position mặc định: BOTTOM_CENTER
///
/// LƯU Ý VỀ UNITY ADS:
/// - Banner ad do Unity Ads SDK tự vẽ overlay lên màn hình
/// - AdPlaceholder chỉ là ảnh tạm hiển thị trong lúc banner đang load
/// - Không cần lo responsive cho banner vì SDK tự handle
/// </summary>
public class SettingScreenAds : MonoBehaviour
{
    [Header("=== AD CONTAINER ===")]
    [Tooltip("Placeholder hiển thị khi đang load ad (con chim xanh hoặc loading image)")]
    [SerializeField] private GameObject adPlaceholder;

    [Tooltip("Text hiển thị trạng thái ad (có thể để trống)")]
    [SerializeField] private TMP_Text adStatusText;

    [Header("=== AD CONFIG ===")]
    [Tooltip("Vị trí banner (Unity Ads chỉ hỗ trợ TOP/BOTTOM)")]
    [SerializeField] private BannerPosition bannerPosition = BannerPosition.BOTTOM_CENTER;

    [Tooltip("Thời gian chờ trước khi ẩn placeholder sau khi banner load (giây)")]
    [SerializeField] private float placeholderHideDelay = 0.5f;

    [Tooltip("Có tự động load banner khi mở Setting không?")]
    [SerializeField] private bool autoLoadOnEnable = true;

    [Header("=== DEBUG ===")]
    [SerializeField] private bool showDebugLogs = true;

    private bool isLoadingBanner = false;

    // ---------------------------------------------------------------
    // Unity Lifecycle
    // ---------------------------------------------------------------

    void OnEnable()
    {
        if (autoLoadOnEnable)
            ShowBannerAd();
    }

    void OnDisable()
    {
        HideBannerAd();
    }

    // ---------------------------------------------------------------
    // Public API
    // ---------------------------------------------------------------

    /// <summary>
    /// Hiển thị banner ad. Gọi từ bên ngoài nếu autoLoadOnEnable = false.
    /// </summary>
    public void ShowBannerAd()
    {
        if (isLoadingBanner)
        {
            DebugLog("Banner đang load, bỏ qua lệnh gọi trùng.");
            return;
        }

        if (AdsManager.Instance == null)
        {
            DebugLog("AdsManager không tồn tại!");
            UpdateAdStatus("No Ads Available", Color.red);
            return;
        }

        if (!Advertisement.isInitialized)
        {
            DebugLog("Unity Ads chưa init, đang thử khởi tạo lại...");
            UpdateAdStatus("Initializing Ads...", Color.yellow);
            AdsManager.Instance.InitializeAds();
            Invoke(nameof(ShowBannerAd), 1f);
            return;
        }

        // Hiện placeholder trong khi banner load
        SetPlaceholderActive(true);
        UpdateAdStatus("Loading Ad...", Color.yellow);
        isLoadingBanner = true;

        DebugLog("Đang hiển thị Unity Ads Banner...");
        AdsManager.Instance.ShowBanner(bannerPosition);

        // Ẩn placeholder sau delay
        StartCoroutine(HidePlaceholderAfterDelay());
    }

    /// <summary>
    /// Ẩn banner ad và reset UI.
    /// </summary>
    public void HideBannerAd()
    {
        if (AdsManager.Instance != null)
        {
            AdsManager.Instance.HideBanner();
            DebugLog("Banner đã ẩn.");
        }

        isLoadingBanner = false;
        SetPlaceholderActive(false);
        UpdateAdStatus("", Color.clear);
    }

    /// <summary>
    /// Force reload banner (có thể gắn vào Button nếu cần).
    /// </summary>
    public void ForceReloadBanner()
    {
        HideBannerAd();
        ShowBannerAd();
    }

    // ---------------------------------------------------------------
    // Private Helpers
    // ---------------------------------------------------------------

    private void SetPlaceholderActive(bool active)
    {
        if (adPlaceholder != null)
            adPlaceholder.SetActive(active);
    }

    private IEnumerator HidePlaceholderAfterDelay()
    {
        yield return new WaitForSeconds(placeholderHideDelay);
        SetPlaceholderActive(false);
        UpdateAdStatus("", Color.clear);
        isLoadingBanner = false;
        DebugLog("Placeholder ẩn - Banner đã load xong.");
    }

    private void UpdateAdStatus(string message, Color color)
    {
        if (adStatusText == null) return;
        adStatusText.text = message;
        adStatusText.color = color;
    }

    private void DebugLog(string message)
    {
        if (showDebugLogs)
            Debug.Log($"[SettingScreenAds] {message}");
    }
}