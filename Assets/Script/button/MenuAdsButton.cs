using UnityEngine;

public class MenuAdsButton : MonoBehaviour
{
    public void OnAds()
    {
        if (AdsManager.Instance == null)
        {
            return;
        }

        AdsManager.Instance.ShowInterstitial();
    }
}