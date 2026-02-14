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

        if (Advertisement.isInitialized)
        {
            AdsManager.Instance.ShowBanner(bannerPosition);
        }
        else
        {
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