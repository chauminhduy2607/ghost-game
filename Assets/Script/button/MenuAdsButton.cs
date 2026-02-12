using UnityEngine;

public class MenuAdsButton : MonoBehaviour
{
    public void OnAds()
    {
        if (AdsManager.Instance == null)
        {
            Debug.LogError("📺 AdsManager not found!");
            return;
        }

        Debug.Log("📺 Showing Interstitial Ad (Full Screen)");
        
        bool shown = AdsManager.Instance.ShowInterstitial();
        
        if (!shown)
        {
            Debug.LogWarning("📺 Interstitial ad not ready yet!");
        }
    }
}