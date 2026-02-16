using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.Advertisements;

public class SettingScreenAds : MonoBehaviour
{
    [Header("=== AD CONTAINER ===")]
    [Tooltip("Khung chứa quảng cáo (Image object)")]
    [SerializeField] private GameObject adContainer;
    
    [Tooltip("Placeholder hiển thị khi đang load ad")]
    [SerializeField] private GameObject adPlaceholder;
    
    [Tooltip("Text hiển thị trạng thái ad")]
    [SerializeField] private TMP_Text adStatusText;
    
    [Header("=== AD CONFIG ===")]
    [Tooltip("Vị trí banner (Unity Ads chỉ hỗ trợ TOP/BOTTOM)")]
    [SerializeField] private BannerPosition bannerPosition = BannerPosition.BOTTOM_CENTER;
    
    [Tooltip("Thời gian chờ trước khi ẩn placeholder (giây)")]
    [SerializeField] private float placeholderHideDelay = 0.5f;
    
    [Tooltip("Có tự động load banner khi mở Setting không?")]
    [SerializeField] private bool autoLoadOnEnable = true;
    
    [Header("=== DEBUG ===")]
    [SerializeField] private bool showDebugLogs = true;
    
    private bool isLoadingBanner = false;
    
    void OnEnable()
    {
        if (autoLoadOnEnable)
        {
            ShowBannerAd();
        }
    }
    
    void OnDisable()
    {
        HideBannerAd();
    }
    
    /// <summary>
    /// Hiển thị banner ad trong khung
    /// </summary>
    public void ShowBannerAd()
    {
        if (isLoadingBanner)
        {
            DebugLog("⏳ Banner đang được load...");
            return;
        }
        
        if (AdsManager.Instance == null)
        {
            DebugLog("❌ AdsManager không tồn tại!");
            UpdateAdStatus("No Ads Available", Color.red);
            return;
        }
        
        // Kiểm tra Unity Ads đã init chưa
        if (!UnityEngine.Advertisements.Advertisement.isInitialized)
        {
            DebugLog("⚠️ Unity Ads chưa khởi tạo, đang khởi tạo...");
            UpdateAdStatus("Initializing Ads...", Color.yellow);
            AdsManager.Instance.InitializeAds();
            
            // Retry sau 1s
            Invoke(nameof(ShowBannerAd), 1f);
            return;
        }
        
        // Hiển thị placeholder
        ShowPlaceholder();
        UpdateAdStatus("Loading Ad...", Color.yellow);
        
        isLoadingBanner = true;
        
        // Show banner
        DebugLog("📺 Đang hiển thị Unity Ads Banner...");
        AdsManager.Instance.ShowBanner(bannerPosition);
        
        // Ẩn placeholder sau delay
        StartCoroutine(HidePlaceholderAfterDelay());
    }
    
    /// <summary>
    /// Ẩn banner ad
    /// </summary>
    public void HideBannerAd()
    {
        if (AdsManager.Instance != null)
        {
            AdsManager.Instance.HideBanner();
            DebugLog("🙈 Banner đã ẩn");
        }
        
        isLoadingBanner = false;
        
        // Reset UI
        if (adPlaceholder != null)
            adPlaceholder.SetActive(false);
    }
    
    /// <summary>
    /// Hiển thị placeholder
    /// </summary>
    private void ShowPlaceholder()
    {
        if (adPlaceholder != null)
        {
            adPlaceholder.SetActive(true);
            DebugLog("📦 Hiển thị placeholder");
        }
    }
    
    /// <summary>
    /// Ẩn placeholder sau một khoảng thời gian
    /// </summary>
    private IEnumerator HidePlaceholderAfterDelay()
    {
        yield return new WaitForSeconds(placeholderHideDelay);
        
        if (adPlaceholder != null)
        {
            adPlaceholder.SetActive(false);
            DebugLog("✅ Ẩn placeholder - Banner đã load");
        }
        
        UpdateAdStatus("", Color.clear);
        
        isLoadingBanner = false;
    }
    
    /// <summary>
    /// Cập nhật text trạng thái ad
    /// </summary>
    private void UpdateAdStatus(string message, Color color)
    {
        if (adStatusText != null)
        {
            adStatusText.text = message;
            adStatusText.color = color;
        }
    }
    
    /// <summary>
    /// Debug log có điều kiện
    /// </summary>
    private void DebugLog(string message)
    {
        if (showDebugLogs)
        {
            Debug.Log($"[SettingScreenAds] {message}");
        }
    }
    
    /// <summary>
    /// Force reload banner (gọi từ button nếu cần)
    /// </summary>
    public void ForceReloadBanner()
    {
        HideBannerAd();
        ShowBannerAd();
    }
}