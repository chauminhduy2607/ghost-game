// using UnityEngine;
// using System;
// using UnityEngine.Advertisements;

// /// <summary>
// /// ⭐⭐⭐ ADS MANAGER - ĐÃ SỬA LỖI RESTART
// /// 
// /// LỖI CŨ:
// /// - Khi scene reload, listener OnAdClosed vẫn giữ reference cũ
// /// - Dẫn đến callback gọi đến object đã bị destroy
// /// 
// /// CÁCH SỬA:
// /// - KHÔNG dùng SceneManager.sceneLoaded (vì SimpleAdsManager là DontDestroyOnLoad)
// /// - Dùng Action callback trực tiếp từ GameManager
// /// - Clear callback SAU KHI gọi để tránh duplicate
// /// - Không cần reset state vì object không bị destroy
// /// 
// /// CÁCH DÙNG:
// /// 1. Attach script này vào GameObject "AdsManager" trong scene đầu tiên
// /// 2. Trong GameManager, gọi: SimpleAdsManager.Instance.ShowInterstitialAd(OnAdsComplete);
// /// 3. Khi ads đóng, callback OnAdsComplete sẽ được gọi
// /// </summary>
// public class SimpleAdsManager : MonoBehaviour, IUnityAdsInitializationListener, IUnityAdsLoadListener, IUnityAdsShowListener
// {
//     public static SimpleAdsManager Instance;

//     [Header("=== UNITY ADS SETTINGS ===")]
//     [SerializeField] private string androidGameId = "4374881";  // Test ID
//     [SerializeField] private string iOSGameId = "4374880";      // Test ID
//     [SerializeField] private bool testMode = true; // BẬT TEST MODE khi dev
    
//     [Header("=== AD UNIT IDS ===")]
//     [SerializeField] private string androidAdUnitId = "Interstitial_Android";
//     [SerializeField] private string iOSAdUnitId = "Interstitial_iOS";

//     private string gameId;
//     private string adUnitId;
//     private bool isAdsInitialized = false;
//     private bool isShowingAd = false;

//     // ⭐⭐⭐ THAY ĐỔI: Dùng Action callback trực tiếp thay vì event
//     private Action currentAdCallback;

//     void Awake()
//     {
//         // Singleton pattern
//         if (Instance == null)
//         {
//             Instance = this;
//             DontDestroyOnLoad(gameObject);
//             InitializeAds();
//         }
//         else
//         {
//             Destroy(gameObject);
//         }
//     }

//     /// <summary>
//     /// Khởi tạo Unity Ads
//     /// </summary>
//     void InitializeAds()
//     {
//         // Chọn Game ID theo platform
//         #if UNITY_ANDROID
//             gameId = androidGameId;
//             adUnitId = androidAdUnitId;
//         #elif UNITY_IOS
//             gameId = iOSGameId;
//             adUnitId = iOSAdUnitId;
//         #else
//             gameId = androidGameId; // Fallback cho Editor
//             adUnitId = androidAdUnitId;
//         #endif

//         Debug.Log($"🎮 Khởi tạo Unity Ads - Game ID: {gameId} | Test Mode: {testMode}");
        
//         // Khởi tạo Unity Ads
//         if (!Advertisement.isInitialized)
//         {
//             Advertisement.Initialize(gameId, testMode, this);
//         }
//         else
//         {
//             Debug.Log("✅ Unity Ads đã được khởi tạo trước đó!");
//             isAdsInitialized = true;
//             LoadAd();
//         }
//     }

//     /// <summary>
//     /// Load quảng cáo trước
//     /// </summary>
//     void LoadAd()
//     {
//         Debug.Log($"📺 Đang load ads: {adUnitId}...");
//         Advertisement.Load(adUnitId, this);
//     }

//     /// <summary>
//     /// ⭐⭐⭐ HIỂN THỊ QUẢNG CÁO - CÁCH MỚI
//     /// Nhận callback trực tiếp thay vì dùng event
//     /// </summary>
//     public void ShowInterstitialAd(Action onComplete)
//     {
//         if (isShowingAd)
//         {
//             Debug.LogWarning("⚠️ Ads đang hiển thị rồi!");
//             onComplete?.Invoke();
//             return;
//         }

//         if (!isAdsInitialized)
//         {
//             Debug.LogWarning("⚠️ Unity Ads chưa khởi tạo xong! Bỏ qua ads...");
//             onComplete?.Invoke();
//             return;
//         }

//         // Lưu callback để gọi sau khi ads đóng
//         currentAdCallback = onComplete;

//         Debug.Log("👻 MA CHẾT - BẮT ĐẦU HIỂN THỊ QUẢNG CÁO!");
        
//         // Hiển thị ads
//         Advertisement.Show(adUnitId, this);
//         isShowingAd = true;
//     }

//     // ==================== UNITY ADS CALLBACKS ====================

//     // 1. Callback khi khởi tạo Unity Ads
//     public void OnInitializationComplete()
//     {
//         Debug.Log("✅ Unity Ads khởi tạo thành công!");
//         isAdsInitialized = true;
//         LoadAd(); // Load ads trước để sẵn sàng hiển thị
//     }

//     public void OnInitializationFailed(UnityAdsInitializationError error, string message)
//     {
//         Debug.LogError($"❌ Unity Ads khởi tạo thất bại: {error} - {message}");
//         isAdsInitialized = false;
//     }

//     // 2. Callback khi load ads
//     public void OnUnityAdsAdLoaded(string placementId)
//     {
//         Debug.Log($"✅ Ads loaded thành công: {placementId}");
//     }

//     public void OnUnityAdsFailedToLoad(string placementId, UnityAdsLoadError error, string message)
//     {
//         Debug.LogError($"❌ Ads load thất bại: {placementId} - {error} - {message}");
        
//         // Retry load sau 5 giây
//         Invoke(nameof(LoadAd), 5f);
//     }

//     // 3. Callback khi hiển thị ads
//     public void OnUnityAdsShowComplete(string placementId, UnityAdsShowCompletionState showCompletionState)
//     {
//         Debug.Log($"✅ Ads hiển thị xong: {placementId} - {showCompletionState}");
        
//         isShowingAd = false;
        
//         // ⭐⭐⭐ Gọi callback rồi CLEAR để tránh duplicate
//         Action callback = currentAdCallback;
//         currentAdCallback = null; // Clear trước khi gọi
//         callback?.Invoke();
        
//         // Load ads mới cho lần sau
//         LoadAd();
//     }

//     public void OnUnityAdsShowFailure(string placementId, UnityAdsShowError error, string message)
//     {
//         Debug.LogError($"❌ Ads hiển thị thất bại: {placementId} - {error} - {message}");
        
//         isShowingAd = false;
        
//         // ⭐⭐⭐ Vẫn gọi callback để Game Over, không để user bị kẹt
//         Action callback = currentAdCallback;
//         currentAdCallback = null; // Clear trước khi gọi
//         callback?.Invoke();
        
//         // Load lại ads
//         LoadAd();
//     }

//     public void OnUnityAdsShowStart(string placementId)
//     {
//         Debug.Log($"📺 Ads bắt đầu hiển thị: {placementId}");
//     }

//     public void OnUnityAdsShowClick(string placementId)
//     {
//         Debug.Log($"👆 User click vào ads: {placementId}");
//     }

//     // ==================== HELPER METHODS ====================

//     public bool IsShowingAd()
//     {
//         return isShowingAd;
//     }

//     public bool IsAdsReady()
//     {
//         return isAdsInitialized && Advertisement.isInitialized;
//     }
// }