using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Advertisements;

/// <summary>
/// Unity Ads manager (Banner / Interstitial / Rewarded)
/// - Initialize ASAP (Awake) + Preload (Start)
/// - Safe: init only once at a time (no spam)
/// - Load/Show with callbacks/events
/// - Optional interstitial cooldown
/// - Optional retry (exponential backoff)
/// - Logs use a single searchable prefix: [ADS]
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

    [Header("Retry Config")]
    [Tooltip("Bật retry load khi fail/no-fill")]
    [SerializeField] private bool enableRetry = true;

    [Tooltip("Delay retry lần đầu (giây)")]
    [SerializeField] private float retryBaseDelay = 2f;

    [Tooltip("Delay retry tối đa (giây)")]
    [SerializeField] private float retryMaxDelay = 30f;

    // ===== Events =====
    public event Action OnInitialized;
    public event Action<string> OnInitFailed;

    public event Action<string> OnAdLoaded;
    public event Action<string, string> OnAdFailedToLoad;

    public event Action<string> OnAdShowStart;
    public event Action<string> OnAdShowClick;
    public event Action<string> OnAdShowComplete;
    public event Action<string, string> OnAdShowFailed;

    public event Action OnRewardedEarned;

    // ===== internal state =====
    private bool _initialized;
    private bool _initializing;

    private bool _loadingInterstitial;
    private bool _loadingRewarded;

    private bool _interstitialReady;
    private bool _rewardedReady;

    private float _lastInterstitialShownTime = -9999f;

    private Action _pendingRewardSuccess;
    private Action _pendingRewardClosedOrFailed;

    // pending load requests before init completes
    private bool _pendingLoadInterstitialAfterInit;
    private bool _pendingLoadRewardedAfterInit;

    // retry state
    private int _interstitialRetryCount;
    private int _rewardedRetryCount;
    private Coroutine _retryInterstitialCo;
    private Coroutine _retryRewardedCo;

    // timing
    private float _initStartRealtime = -1f;
    private float _loadInterstitialStartRealtime = -1f;
    private float _loadRewardedStartRealtime = -1f;

    // search-friendly prefix
    private const string LOG = "[ADS]";

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

        Debug.Log($"{LOG} Awake -> init asap");
        InitializeAds();
    }

    private void Start()
    {
        // Preload ngay khi start game (nếu init đã xong sẽ load luôn, chưa xong thì pending)
        Debug.Log($"{LOG} Start -> preload interstitial/rewarded");
        PreloadAds();
    }

    /// <summary>
    /// Call this when entering the game to ensure ads are warmed up.
    /// </summary>
    public void PreloadAds()
    {
        InitializeAds();
        LoadInterstitial();
        LoadRewarded();

        // status log để bạn search
        DebugStatus();
    }

    #region Initialization

    public void InitializeAds()
    {
        // SDK already initialized
        if (Advertisement.isInitialized)
        {
            if (!_initialized)
            {
                _initialized = true;
                _initializing = false;
                Debug.Log($"{LOG} SDK already initialized (Advertisement.isInitialized=true)");
            }
            return;
        }

        if (_initialized)
        {
            Debug.Log($"{LOG} InitializeAds skipped (already initialized flag)");
            return;
        }

        if (_initializing)
        {
            Debug.Log($"{LOG} InitializeAds skipped (already initializing)");
            return;
        }

        if (string.IsNullOrEmpty(GameId))
        {
            Debug.LogError($"{LOG} GameId is EMPTY! Check Inspector.");
            OnInitFailed?.Invoke("Missing GameId");
            return;
        }

        _initializing = true;
        _initStartRealtime = Time.realtimeSinceStartup;

        Debug.Log($"{LOG} Initializing... gameId={GameId} testMode={testMode}");
        Advertisement.Initialize(GameId, testMode, this);
    }

    public void OnInitializationComplete()
    {
        _initialized = true;
        _initializing = false;

        float dt = (_initStartRealtime > 0f) ? (Time.realtimeSinceStartup - _initStartRealtime) : -1f;
        Debug.Log(dt >= 0f
            ? $"{LOG} Init COMPLETE (took {dt:0.00}s)"
            : $"{LOG} Init COMPLETE");

        // Auto load ads after init
        if (_pendingLoadInterstitialAfterInit) _pendingLoadInterstitialAfterInit = false;
        if (_pendingLoadRewardedAfterInit) _pendingLoadRewardedAfterInit = false;

        LoadInterstitial();
        LoadRewarded();

        DebugStatus();
        OnInitialized?.Invoke();
    }

    public void OnInitializationFailed(UnityAdsInitializationError error, string message)
    {
        _initialized = false;
        _initializing = false;

        float dt = (_initStartRealtime > 0f) ? (Time.realtimeSinceStartup - _initStartRealtime) : -1f;
        Debug.LogError(dt >= 0f
            ? $"{LOG} Init FAILED (after {dt:0.00}s): {error} - {message}"
            : $"{LOG} Init FAILED: {error} - {message}");

        OnInitFailed?.Invoke($"{error} - {message}");
        DebugStatus();
    }

    #endregion

    #region Load

    public void LoadInterstitial()
    {
        if (!Advertisement.isInitialized || !_initialized)
        {
            Debug.LogWarning($"{LOG} LoadInterstitial pending (not initialized yet)");
            _pendingLoadInterstitialAfterInit = true;
            InitializeAds();
            return;
        }

        if (_loadingInterstitial)
        {
            Debug.Log($"{LOG} LoadInterstitial skipped (already loading)");
            return;
        }

        if (_retryInterstitialCo != null)
        {
            StopCoroutine(_retryInterstitialCo);
            _retryInterstitialCo = null;
        }

        _loadingInterstitial = true;
        _loadInterstitialStartRealtime = Time.realtimeSinceStartup;

        Debug.Log($"{LOG} Loading interstitial... unitId={InterstitialUnitId}");
        Advertisement.Load(InterstitialUnitId, this);
    }

    public void LoadRewarded()
    {
        if (!Advertisement.isInitialized || !_initialized)
        {
            Debug.LogWarning($"{LOG} LoadRewarded pending (not initialized yet)");
            _pendingLoadRewardedAfterInit = true;
            InitializeAds();
            return;
        }

        if (_loadingRewarded)
        {
            Debug.Log($"{LOG} LoadRewarded skipped (already loading)");
            return;
        }

        if (_retryRewardedCo != null)
        {
            StopCoroutine(_retryRewardedCo);
            _retryRewardedCo = null;
        }

        _loadingRewarded = true;
        _loadRewardedStartRealtime = Time.realtimeSinceStartup;

        Debug.Log($"{LOG} Loading rewarded... unitId={RewardedUnitId}");
        Advertisement.Load(RewardedUnitId, this);
    }

    public void OnUnityAdsAdLoaded(string unitId)
    {
        if (unitId == InterstitialUnitId)
        {
            _interstitialReady = true;
            _loadingInterstitial = false;
            _interstitialRetryCount = 0;

            float dt = (_loadInterstitialStartRealtime > 0f) ? (Time.realtimeSinceStartup - _loadInterstitialStartRealtime) : -1f;
            Debug.Log(dt >= 0f
                ? $"{LOG} Interstitial LOADED (took {dt:0.00}s) ready={_interstitialReady}"
                : $"{LOG} Interstitial LOADED ready={_interstitialReady}");
        }
        else if (unitId == RewardedUnitId)
        {
            _rewardedReady = true;
            _loadingRewarded = false;
            _rewardedRetryCount = 0;

            float dt = (_loadRewardedStartRealtime > 0f) ? (Time.realtimeSinceStartup - _loadRewardedStartRealtime) : -1f;
            Debug.Log(dt >= 0f
                ? $"{LOG} Rewarded LOADED (took {dt:0.00}s) ready={_rewardedReady}"
                : $"{LOG} Rewarded LOADED ready={_rewardedReady}");
        }

        OnAdLoaded?.Invoke(unitId);
    }

    public void OnUnityAdsFailedToLoad(string unitId, UnityAdsLoadError error, string message)
    {
        if (unitId == InterstitialUnitId)
        {
            _loadingInterstitial = false;
            _interstitialReady = false;

            float dt = (_loadInterstitialStartRealtime > 0f) ? (Time.realtimeSinceStartup - _loadInterstitialStartRealtime) : -1f;
            Debug.LogError(dt >= 0f
                ? $"{LOG} Interstitial FAILED TO LOAD (after {dt:0.00}s): {error} - {message}"
                : $"{LOG} Interstitial FAILED TO LOAD: {error} - {message}");

            if (enableRetry) ScheduleRetryInterstitial();
        }

        if (unitId == RewardedUnitId)
        {
            _loadingRewarded = false;
            _rewardedReady = false;

            float dt = (_loadRewardedStartRealtime > 0f) ? (Time.realtimeSinceStartup - _loadRewardedStartRealtime) : -1f;
            Debug.LogError(dt >= 0f
                ? $"{LOG} Rewarded FAILED TO LOAD (after {dt:0.00}s): {error} - {message}"
                : $"{LOG} Rewarded FAILED TO LOAD: {error} - {message}");

            if (enableRetry) ScheduleRetryRewarded();
        }

        OnAdFailedToLoad?.Invoke(unitId, $"{error} - {message}");
    }

    private void ScheduleRetryInterstitial()
    {
        _interstitialRetryCount++;
        float delay = Mathf.Min(retryBaseDelay * Mathf.Pow(2f, _interstitialRetryCount - 1), retryMaxDelay);

        if (_retryInterstitialCo != null) StopCoroutine(_retryInterstitialCo);
        _retryInterstitialCo = StartCoroutine(RetryLoadInterstitial(delay));

        Debug.LogWarning($"{LOG} Retry interstitial in {delay:0.0}s (count={_interstitialRetryCount})");
    }

    private IEnumerator RetryLoadInterstitial(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        _retryInterstitialCo = null;
        LoadInterstitial();
    }

    private void ScheduleRetryRewarded()
    {
        _rewardedRetryCount++;
        float delay = Mathf.Min(retryBaseDelay * Mathf.Pow(2f, _rewardedRetryCount - 1), retryMaxDelay);

        if (_retryRewardedCo != null) StopCoroutine(_retryRewardedCo);
        _retryRewardedCo = StartCoroutine(RetryLoadRewarded(delay));

        Debug.LogWarning($"{LOG} Retry rewarded in {delay:0.0}s (count={_rewardedRetryCount})");
    }

    private IEnumerator RetryLoadRewarded(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        _retryRewardedCo = null;
        LoadRewarded();
    }

    #endregion

    #region Show

    public bool CanShowInterstitial()
    {
        if (!Advertisement.isInitialized || !_initialized) return false;
        if (!_interstitialReady) return false;

        if (Time.unscaledTime - _lastInterstitialShownTime < interstitialCooldownSeconds)
            return false;

        return true;
    }

    public bool CanShowRewarded()
    {
        if (!Advertisement.isInitialized || !_initialized) return false;
        if (!_rewardedReady) return false;
        return true;
    }

    public bool ShowInterstitial()
    {
        Debug.Log($"{LOG} ShowInterstitial called");

        if (!CanShowInterstitial())
        {
            Debug.LogWarning($"{LOG} Interstitial NOT READY -> skip show, try load");
            if (!Advertisement.isInitialized || !_initialized) InitializeAds();
            else if (!_loadingInterstitial) LoadInterstitial();
            return false;
        }

        _interstitialReady = false;
        _lastInterstitialShownTime = Time.unscaledTime;

        Debug.Log($"{LOG} SHOW interstitial unitId={InterstitialUnitId}");
        Advertisement.Show(InterstitialUnitId, this);
        return true;
    }

    public bool ShowRewarded(Action onReward, Action onClosedOrFailed = null)
    {
        Debug.Log($"{LOG} ShowRewarded called");

        if (!CanShowRewarded())
        {
            Debug.LogWarning($"{LOG} Rewarded NOT READY -> skip show, try load");
            if (!Advertisement.isInitialized || !_initialized) InitializeAds();
            else if (!_loadingRewarded) LoadRewarded();

            onClosedOrFailed?.Invoke();
            return false;
        }

        _rewardedReady = false;
        _pendingRewardSuccess = onReward;
        _pendingRewardClosedOrFailed = onClosedOrFailed;

        Debug.Log($"{LOG} SHOW rewarded unitId={RewardedUnitId}");
        Advertisement.Show(RewardedUnitId, this);
        return true;
    }

    public void OnUnityAdsShowFailure(string unitId, UnityAdsShowError error, string message)
    {
        Debug.LogError($"{LOG} Show FAILED: unitId={unitId} | {error} - {message}");
        OnAdShowFailed?.Invoke(unitId, $"{error} - {message}");

        if (unitId == InterstitialUnitId)
        {
            LoadInterstitial();
        }
        else if (unitId == RewardedUnitId)
        {
            _pendingRewardClosedOrFailed?.Invoke();
            _pendingRewardSuccess = null;
            _pendingRewardClosedOrFailed = null;

            LoadRewarded();
        }
    }

    public void OnUnityAdsShowStart(string unitId)
    {
        Debug.Log($"{LOG} Show START: unitId={unitId}");
        OnAdShowStart?.Invoke(unitId);
    }

    public void OnUnityAdsShowClick(string unitId)
    {
        Debug.Log($"{LOG} Show CLICK: unitId={unitId}");
        OnAdShowClick?.Invoke(unitId);
    }

    public void OnUnityAdsShowComplete(string unitId, UnityAdsShowCompletionState showCompletionState)
    {
        Debug.Log($"{LOG} Show COMPLETE: unitId={unitId} | state={showCompletionState}");
        OnAdShowComplete?.Invoke(unitId);

        if (unitId == InterstitialUnitId)
        {
            LoadInterstitial();
            return;
        }

        if (unitId == RewardedUnitId)
        {
            if (showCompletionState == UnityAdsShowCompletionState.COMPLETED)
            {
                Debug.Log($"{LOG} Reward EARNED");
                OnRewardedEarned?.Invoke();
                _pendingRewardSuccess?.Invoke();
            }
            else
            {
                Debug.LogWarning($"{LOG} Rewarded closed/skipped -> no reward");
                _pendingRewardClosedOrFailed?.Invoke();
            }

            _pendingRewardSuccess = null;
            _pendingRewardClosedOrFailed = null;

            LoadRewarded();
        }
    }

    #endregion

    #region Banner

    public void ShowBanner(BannerPosition position = BannerPosition.BOTTOM_CENTER)
    {
        if (!Advertisement.isInitialized || !_initialized)
        {
            Debug.LogWarning($"{LOG} ShowBanner skipped (not initialized yet)");
            InitializeAds();
            return;
        }

        Debug.Log($"{LOG} Banner load+show unitId={BannerUnitId} pos={position}");
        Advertisement.Banner.SetPosition(position);

        Advertisement.Banner.Load(
            BannerUnitId,
            new BannerLoadOptions
            {
                loadCallback = () =>
                {
                    Debug.Log($"{LOG} Banner LOADED -> showing");
                    Advertisement.Banner.Show(BannerUnitId, new BannerOptions
                    {
                        showCallback = () => Debug.Log($"{LOG} Banner SHOWN"),
                        clickCallback = () => Debug.Log($"{LOG} Banner CLICK"),
                        hideCallback = () => Debug.Log($"{LOG} Banner HIDE")
                    });
                },
                errorCallback = (msg) => Debug.LogWarning($"{LOG} Banner FAILED TO LOAD: {msg}")
            }
        );
    }

    public void HideBanner()
    {
        Debug.Log($"{LOG} Banner HIDE request");
        Advertisement.Banner.Hide();
    }

    #endregion

    #region Utilities

    public void ForceLoadInterstitial()
    {
        _interstitialReady = false;
        _loadingInterstitial = false;
        _interstitialRetryCount = 0;

        Debug.Log($"{LOG} ForceLoadInterstitial");
        LoadInterstitial();
    }

    public void DebugStatus()
    {
        Debug.Log($"{LOG} ===== STATUS =====");
        Debug.Log($"{LOG} flags: initialized={_initialized} initializing={_initializing} sdkInitialized={Advertisement.isInitialized}");
        Debug.Log($"{LOG} ids: gameId={GameId} testMode={testMode}");
        Debug.Log($"{LOG} interstitial: unitId={InterstitialUnitId} ready={_interstitialReady} loading={_loadingInterstitial} cooldown={interstitialCooldownSeconds:0.##}s");
        Debug.Log($"{LOG} rewarded: unitId={RewardedUnitId} ready={_rewardedReady} loading={_loadingRewarded}");
        Debug.Log($"{LOG} banner: unitId={BannerUnitId}");
        Debug.Log($"{LOG} ================");
    }

    [ContextMenu("ADS / Print Status")]
    private void DebugStatusMenu() => DebugStatus();

    #endregion
}
