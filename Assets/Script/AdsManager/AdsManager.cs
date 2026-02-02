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
    [SerializeField] private string androidGameId = "YOUR_ANDROID_GAME_ID";
    [SerializeField] private string iosGameId = "YOUR_IOS_GAME_ID";

    [Header("Placement/Unit IDs")]
    // Lưu ý: tên mặc định Unity hay dùng:
    // - Interstitial: "Interstitial_Android" / "Interstitial_iOS"
    // - Rewarded:     "Rewarded_Android" / "Rewarded_iOS"
    // - Banner:       "Banner_Android" / "Banner_iOS"
    [SerializeField] private string interstitialUnitIdAndroid = "Interstitial_Android";
    [SerializeField] private string interstitialUnitIdIOS = "Interstitial_iOS";
    [SerializeField] private string rewardedUnitIdAndroid = "Rewarded_Android";
    [SerializeField] private string rewardedUnitIdIOS = "Rewarded_iOS";
    [SerializeField] private string bannerUnitIdAndroid = "Banner_Android";
    [SerializeField] private string bannerUnitIdIOS = "Banner_iOS";

    [Header("Interstitial Config")]
    [Tooltip("Thời gian tối thiểu giữa 2 lần show interstitial (giây)")]
    [SerializeField] private float interstitialCooldownSeconds = 30f;

    // Events cho game bắt
    public event Action OnInitialized;
    public event Action<string> OnInitFailed;

    public event Action<string> OnAdLoaded;
    public event Action<string, string> OnAdFailedToLoad; // (unitId, message)

    public event Action<string> OnAdShowStart;
    public event Action<string> OnAdShowClick;
    public event Action<string> OnAdShowComplete;
    public event Action<string, string> OnAdShowFailed; // (unitId, message)

    public event Action OnRewardedEarned; // Khi rewarded thành công

    private bool _initialized;
    private bool _loadingInterstitial;
    private bool _loadingRewarded;

    private bool _interstitialReady;
    private bool _rewardedReady;

    private float _lastInterstitialShownTime = -9999f;

    // Dùng để gọi callback riêng cho mỗi lần show rewarded
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
        if (_initialized) return;

        if (string.IsNullOrEmpty(GameId) || GameId.Contains("YOUR_"))
        {
            Debug.LogError("[AdsManager] GameId chưa được set. Hãy nhập androidGameId/iosGameId trong Inspector.");
            OnInitFailed?.Invoke("Missing GameId");
            return;
        }

        Advertisement.Initialize(GameId, testMode, this);
    }

    public void OnInitializationComplete()
    {
        _initialized = true;
        Debug.Log("[AdsManager] Initialization complete.");

        // Auto load ads
        LoadInterstitial();
        LoadRewarded();

        OnInitialized?.Invoke();
    }

    public void OnInitializationFailed(UnityAdsInitializationError error, string message)
    {
        _initialized = false;
        Debug.LogError($"[AdsManager] Initialization failed: {error} - {message}");
        OnInitFailed?.Invoke($"{error} - {message}");
    }

    #endregion

    #region Load

    public void LoadInterstitial()
    {
        if (!_initialized) return;
        if (_loadingInterstitial) return;

        _loadingInterstitial = true;
        Advertisement.Load(InterstitialUnitId, this);
    }

    public void LoadRewarded()
    {
        if (!_initialized) return;
        if (_loadingRewarded) return;

        _loadingRewarded = true;
        Advertisement.Load(RewardedUnitId, this);
    }

    // Banner (Unity Ads banner thường load/show qua Banner API)
    public void ShowBanner(BannerPosition position = BannerPosition.BOTTOM_CENTER)
    {
        if (!_initialized) return;

        Advertisement.Banner.SetPosition(position);

        // Load banner rồi show
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
        }
        else if (unitId == RewardedUnitId)
        {
            _rewardedReady = true;
            _loadingRewarded = false;
        }

        Debug.Log($"[AdsManager] Ad loaded: {unitId}");
        OnAdLoaded?.Invoke(unitId);
    }

    public void OnUnityAdsFailedToLoad(string unitId, UnityAdsLoadError error, string message)
    {
        if (unitId == InterstitialUnitId) _loadingInterstitial = false;
        if (unitId == RewardedUnitId) _loadingRewarded = false;

        Debug.LogWarning($"[AdsManager] Failed to load: {unitId} - {error} - {message}");
        OnAdFailedToLoad?.Invoke(unitId, $"{error} - {message}");
    }

    #endregion

    #region Show

    public bool CanShowInterstitial()
    {
        if (!_initialized) return false;
        if (!_interstitialReady) return false;

        // cooldown
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
        if (!CanShowInterstitial())
        {
            // nếu chưa sẵn thì thử load lại
            if (!_loadingInterstitial) LoadInterstitial();
            return false;
        }

        _interstitialReady = false;
        _lastInterstitialShownTime = Time.unscaledTime;

        Advertisement.Show(InterstitialUnitId, this);
        return true;
    }

    /// <summary>
    /// Show rewarded.
    /// onReward: gọi khi user nhận reward (Completed)
    /// onClosedOrFailed: gọi khi user đóng hoặc fail (không reward)
    /// </summary>
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
        Debug.LogWarning($"[AdsManager] Show failed: {unitId} - {error} - {message}");
        OnAdShowFailed?.Invoke(unitId, $"{error} - {message}");

        // Nếu fail thì load lại
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
        Debug.Log($"[AdsManager] Show start: {unitId}");
        OnAdShowStart?.Invoke(unitId);
    }

    public void OnUnityAdsShowClick(string unitId)
    {
        Debug.Log($"[AdsManager] Show click: {unitId}");
        OnAdShowClick?.Invoke(unitId);
    }

    public void OnUnityAdsShowComplete(string unitId, UnityAdsShowCompletionState showCompletionState)
    {
        Debug.Log($"[AdsManager] Show complete: {unitId} - {showCompletionState}");
        OnAdShowComplete?.Invoke(unitId);

        if (unitId == InterstitialUnitId)
        {
            // Load lại để lần sau dùng
            LoadInterstitial();
            return;
        }

        if (unitId == RewardedUnitId)
        {
            // Reward chỉ khi Completed
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

            // Load lại rewarded
            LoadRewarded();
        }
    }

    #endregion
}
