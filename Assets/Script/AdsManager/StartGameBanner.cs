using UnityEngine;
using UnityEngine.Advertisements;

/// <summary>
/// Hiện Banner Ads trên màn hình Start Game
/// </summary>
public class StartGameBanner : MonoBehaviour
{
    [Header("=== VỊ TRÍ BANNER ===")]
    [SerializeField] private BannerPosition bannerPosition = BannerPosition.BOTTOM_CENTER;

    void Start()
    {
        ShowBanner();
    }

    void ShowBanner()
    {
        if (AdsManager.Instance != null)
        {
            AdsManager.Instance.ShowBanner(bannerPosition);
        }
        else
        {
            // AdsManager chưa sẵn sàng, thử lại sau 1 giây
            Invoke(nameof(ShowBanner), 1f);
        }
    }

    void OnDestroy()
    {
        // Ẩn banner khi rời màn hình start
        if (AdsManager.Instance != null)
        {
            AdsManager.Instance.HideBanner();
        }
    }
}