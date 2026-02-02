using System;
using UnityEngine;
using UnityEngine.Advertisements;

/// <summary>
/// Unity Ads manager (Banner / Interstitial / Rewarded)
/// - Auto initialize on Awake
/// - Load + Show with callbacks/events
/// - Optional interstitial cooldown
/// 
/// Requirements:
/// - Install package "Advertisement" (Unity Monetization / Ads)
/// - Project Settings > Services > Ads enabled (hoặc qua Dashboard)
/// </summary>
public class AdsManager : MonoBehaviour,
    IUnityAdsInitializationListener,
    IUnityAdsLoadListener,
    IUnityAdsShowListener
{
    public static AdsManager Instance { get; private set; }

    [Header("Debug")]
    [SerializeField] private bool testMode = true;

    [Header("Game IDs")]
    [SerializeField] private string androidGameId = "6037561";
    [SerializeField] private string iosGameId = "6037560";

    [Header("Placement/Unit IDs")]
    [SerializeField] private string interstitialUnitIdAndroid = "Interstitial_Android";
    [SerializeField] private string interstitialUnitIdIOS = "Interstitial_iOS";
    [SerializeField] private string rewardedUnitIdAndroid = "Rewarded_Android";
    [SerializeField] private string rewardedUnitIdIOS = "Rewarded_iOS";
    [SerializeField] private string bannerUnitIdAndroid = "Banner_Android";
    [SerializeField] private string bannerUnitIdIOS = "Banner_iOS";

    [Header("Interstitial Config")]
    [Tooltip("Thời gian tối thiểu giữa 2 lần show interstitial (giây)")]
    [SerializeField] private float interstitialCooldownSeconds = 0f;

    // Events cho game bắt
    public event Action OnInitialized;
    public event Action<string> OnInitFailed;

    public event Action<string> OnAdLoaded;
    public event Action<string, string> OnAdFailedToLoad;

    public event Action<string> OnAdShowStart;
    public event Action<string> OnAdShowClick;
    public event Action<string> OnAdShowComplete;
    public event Action<string, string> OnAdShowFailed;

    public event Action OnRewardedEarned;

    private bool _initialized;
    private bool _loadingInterstitial;
    private bool _loadingRewarded;

    private bool _interstitialReady;
    private bool _rewardedReady;

    private float _lastInterstitialShownTime = -9999f;

    private Action _pendingRewardSuccess;
    private Action _pendingRewardClosedOrFailed;

    private string GameId
    {
        get
        {
#if UNITY_IOS
            return iosGameId;
#else
            return androidGameId;
#endif
        }
    }

    private string InterstitialUnitId
    {
        get
        {
#if UNITY_IOS
            return interstitialUnitIdIOS;
#else
            return interstitialUnitIdAndroid;
#endif
        }
    }

    private string RewardedUnitId
    {
        get
        {
#if UNITY_IOS
            return rewardedUnitIdIOS;
#else
            return rewardedUnitIdAndroid;
#endif
        }
    }

    private string BannerUnitId
    {
        get
        {
#if UNITY_IOS
            return bannerUnitIdIOS;
#else
            return bannerUnitIdAndroid;
#endif
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        InitializeAds();
    }

    #region Initialization

    public void InitializeAds()
    {
        if (_initialized)
        {
            Debug.Log("[AdsManager] ⚠️ Đã initialize rồi, bỏ qua.");
            return;
        }

        if (string.IsNullOrEmpty(GameId))
        {
            Debug.LogError("[AdsManager] ❌ GameId TRỐNG! Hãy nhập Game ID trong Inspector.");
            OnInitFailed?.Invoke("Missing GameId");
            return;
        }

        Debug.Log($"[AdsManager] 🎬 Bắt đầu initialize với Game ID: {GameId}, Test Mode: {testMode}");
        Advertisement.Initialize(GameId, testMode, this);
    }

    public void OnInitializationComplete()
    {
        _initialized = true;
        Debug.Log("[AdsManager] ✅ Initialization complete!");

        // Auto load ads
        LoadInterstitial();
        LoadRewarded();

        OnInitialized?.Invoke();
    }

    public void OnInitializationFailed(UnityAdsInitializationError error, string message)
    {
        _initialized = false;
        Debug.LogError($"[AdsManager] ❌ Initialization failed: {error} - {message}");
        OnInitFailed?.Invoke($"{error} - {message}");
    }

    #endregion

    #region Load

    public void LoadInterstitial()
    {
        if (!_initialized)
        {
            Debug.LogWarning("[AdsManager] ⚠️ Chưa initialize, thử initialize lại...");
            InitializeAds(); // ⭐ TỰ ĐỘNG INITIALIZE LẠI
            return; // Chờ OnInitializationComplete() sẽ tự động gọi LoadInterstitial()
        }
        
        if (_loadingInterstitial)
        {
            Debug.Log("[AdsManager] ⏳ Đang load Interstitial, bỏ qua request mới.");
            return;
        }

        _loadingInterstitial = true;
        Debug.Log($"[AdsManager] 📥 Bắt đầu load Interstitial: {InterstitialUnitId}");
        Advertisement.Load(InterstitialUnitId, this);
    }

    public void LoadRewarded()
    {
        if (!_initialized) return;
        if (_loadingRewarded) return;

        _loadingRewarded = true;
        Debug.Log($"[AdsManager] 📥 Bắt đầu load Rewarded: {RewardedUnitId}");
        Advertisement.Load(RewardedUnitId, this);
    }

    public void ShowBanner(BannerPosition position = BannerPosition.BOTTOM_CENTER)
    {
        if (!_initialized) return;

        Advertisement.Banner.SetPosition(position);

        Advertisement.Banner.Load(
            BannerUnitId,
            new BannerLoadOptions
            {
                loadCallback = () =>
                {
                    Advertisement.Banner.Show(BannerUnitId, new BannerOptions
                    {
                        showCallback = () => Debug.Log("[AdsManager] Banner shown"),
                        clickCallback = () => Debug.Log("[AdsManager] Banner clicked"),
                        hideCallback = () => Debug.Log("[AdsManager] Banner hidden")
                    });
                },
                errorCallback = (msg) => Debug.LogWarning("[AdsManager] Banner load failed: " + msg)
            }
        );
    }

    public void HideBanner()
    {
        Advertisement.Banner.Hide();
    }

    public void OnUnityAdsAdLoaded(string unitId)
    {
        if (unitId == InterstitialUnitId)
        {
            _interstitialReady = true;
            _loadingInterstitial = false;
            Debug.Log($"[AdsManager] ✅ Interstitial đã load xong! READY = {_interstitialReady}");
        }
        else if (unitId == RewardedUnitId)
        {
            _rewardedReady = true;
            _loadingRewarded = false;
            Debug.Log($"[AdsManager] ✅ Rewarded đã load xong!");
        }

        OnAdLoaded?.Invoke(unitId);
    }

    public void OnUnityAdsFailedToLoad(string unitId, UnityAdsLoadError error, string message)
    {
        if (unitId == InterstitialUnitId)
        {
            _loadingInterstitial = false;
            Debug.LogError($"[AdsManager] ❌ Interstitial FAILED TO LOAD: {error} - {message}");
        }
        
        if (unitId == RewardedUnitId)
        {
            _loadingRewarded = false;
            Debug.LogError($"[AdsManager] ❌ Rewarded FAILED TO LOAD: {error} - {message}");
        }

        OnAdFailedToLoad?.Invoke(unitId, $"{error} - {message}");
    }

    #endregion

    #region Show

    public bool CanShowInterstitial()
    {
        Debug.Log($"[AdsManager] 🔍 Kiểm tra CanShowInterstitial:");
        Debug.Log($"  - Initialized: {_initialized}");
        Debug.Log($"  - Interstitial Ready: {_interstitialReady}");
        Debug.Log($"  - Advertisement.isInitialized: {Advertisement.isInitialized}");
        Debug.Log($"  - Cooldown OK: {Time.unscaledTime - _lastInterstitialShownTime >= interstitialCooldownSeconds}");
        
        if (!_initialized) return false;
        if (!_interstitialReady) return false;

        if (Time.unscaledTime - _lastInterstitialShownTime < interstitialCooldownSeconds)
            return false;

        return Advertisement.isInitialized;
    }

    public bool CanShowRewarded()
    {
        if (!_initialized) return false;
        if (!_rewardedReady) return false;
        return Advertisement.isInitialized;
    }

    public bool ShowInterstitial()
    {
        Debug.Log("[AdsManager] 🎬 ShowInterstitial() được gọi!");
        
        if (!CanShowInterstitial())
        {
            Debug.LogWarning("[AdsManager] ⚠️ KHÔNG THỂ SHOW INTERSTITIAL!");
            if (!_loadingInterstitial)
            {
                Debug.Log("[AdsManager] 🔄 Thử load lại Interstitial...");
                LoadInterstitial();
            }
            return false;
        }

        _interstitialReady = false;
        _lastInterstitialShownTime = Time.unscaledTime;

        Debug.Log($"[AdsManager] 📺 Đang SHOW Interstitial: {InterstitialUnitId}");
        Advertisement.Show(InterstitialUnitId, this);
        return true;
    }

    public bool ShowRewarded(Action onReward, Action onClosedOrFailed = null)
    {
        if (!CanShowRewarded())
        {
            if (!_loadingRewarded) LoadRewarded();
            onClosedOrFailed?.Invoke();
            return false;
        }

        _rewardedReady = false;
        _pendingRewardSuccess = onReward;
        _pendingRewardClosedOrFailed = onClosedOrFailed;

        Advertisement.Show(RewardedUnitId, this);
        return true;
    }

    public void OnUnityAdsShowFailure(string unitId, UnityAdsShowError error, string message)
    {
        Debug.LogError($"[AdsManager] ❌ Show failed: {unitId} - {error} - {message}");
        OnAdShowFailed?.Invoke(unitId, $"{error} - {message}");

        if (unitId == InterstitialUnitId) LoadInterstitial();
        if (unitId == RewardedUnitId)
        {
            _pendingRewardClosedOrFailed?.Invoke();
            _pendingRewardSuccess = null;
            _pendingRewardClosedOrFailed = null;
            LoadRewarded();
        }
    }

    public void OnUnityAdsShowStart(string unitId)
    {
        Debug.Log($"[AdsManager] ▶️ Show start: {unitId}");
        OnAdShowStart?.Invoke(unitId);
    }

    public void OnUnityAdsShowClick(string unitId)
    {
        Debug.Log($"[AdsManager] 👆 Show click: {unitId}");
        OnAdShowClick?.Invoke(unitId);
    }

    public void OnUnityAdsShowComplete(string unitId, UnityAdsShowCompletionState showCompletionState)
    {
        Debug.Log($"[AdsManager] ✅ Show complete: {unitId} - {showCompletionState}");
        OnAdShowComplete?.Invoke(unitId);

        if (unitId == InterstitialUnitId)
        {
            Debug.Log("[AdsManager] 🔄 Load lại Interstitial cho lần sau...");
            LoadInterstitial();
            return;
        }

        if (unitId == RewardedUnitId)
        {
            if (showCompletionState == UnityAdsShowCompletionState.COMPLETED)
            {
                OnRewardedEarned?.Invoke();
                _pendingRewardSuccess?.Invoke();
            }
            else
            {
                _pendingRewardClosedOrFailed?.Invoke();
            }

            _pendingRewardSuccess = null;
            _pendingRewardClosedOrFailed = null;

            LoadRewarded();
        }
    }

    #endregion
    
    public void ForceLoadInterstitial()
    {
        _interstitialReady = false;
        _loadingInterstitial = false;
        LoadInterstitial();
        Debug.Log("[AdsManager] 🔄 Force reload Interstitial!");
    }
    
    public void DebugStatus()
    {
        Debug.Log("========== ADS MANAGER STATUS ==========");
        Debug.Log($"Initialized: {_initialized}");
        Debug.Log($"Game ID: {GameId}");
        Debug.Log($"Test Mode: {testMode}");
        Debug.Log($"Interstitial Ready: {_interstitialReady}");
        Debug.Log($"Rewarded Ready: {_rewardedReady}");
        Debug.Log($"Advertisement.isInitialized: {Advertisement.isInitialized}");
        Debug.Log("========================================");
    }
}