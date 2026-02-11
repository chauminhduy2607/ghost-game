using UnityEngine;
using UnityEngine.Advertisements;

public class StartGameBanner : MonoBehaviour
{
    [Header("=== VỊ TRÍ BANNER ===")]
    [SerializeField] private BannerPosition bannerPosition = BannerPosition.BOTTOM_CENTER;

    void Start()
    {
        if (AdsManager.Instance == null)
        {
            Invoke(nameof(Start), 0.5f);
            return;
        }

        // Nếu đã init rồi → show luôn
        if (Advertisement.isInitialized)
        {
            AdsManager.Instance.ShowBanner(bannerPosition);
        }
        else
        {
            // Chờ init xong mới show
            AdsManager.Instance.OnInitialized += OnAdsReady;
        }
    }

    void OnAdsReady()
    {
        AdsManager.Instance.OnInitialized -= OnAdsReady;
        AdsManager.Instance.ShowBanner(bannerPosition);
    }

    void OnDestroy()
    {
        if (AdsManager.Instance != null)
        {
            AdsManager.Instance.OnInitialized -= OnAdsReady;
            AdsManager.Instance.HideBanner();
        }
    }
}