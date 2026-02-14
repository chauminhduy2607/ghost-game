using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Advertisements;

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
    private bool _initializing;

    private bool _loadingInterstitial;
    private bool _loadingRewarded;

    private bool _interstitialReady;
    private bool _rewardedReady;

    private float _lastInterstitialShownTime = -9999f;

    private Action _pendingRewardSuccess;
    private Action _pendingRewardClosedOrFailed;

    private bool _pendingLoadInterstitialAfterInit;
    private bool _pendingLoadRewardedAfterInit;

    private int _interstitialRetryCount;
    private int _rewardedRetryCount;
    private Coroutine _retryInterstitialCo;
    private Coroutine _retryRewardedCo;

    private float _initStartRealtime = -1f;
    private float _loadInterstitialStartRealtime = -1f;
    private float _loadRewardedStartRealtime = -1f;

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

    private void Start()
    {
        PreloadAds();
    }

    public void PreloadAds()
    {
        InitializeAds();
        LoadInterstitial();
        LoadRewarded();
    }

    #region Initialization

    public void InitializeAds()
    {
        if (Advertisement.isInitialized)
        {
            if (!_initialized)
            {
                _initialized = true;
                _initializing = false;
            }
            return;
        }

        if (_initialized)
        {
            return;
        }

        if (_initializing)
        {
            return;
        }

        if (string.IsNullOrEmpty(GameId))
        {
            OnInitFailed?.Invoke("Missing GameId");
            return;
        }

        _initializing = true;
        _initStartRealtime = Time.realtimeSinceStartup;

        Advertisement.Initialize(GameId, testMode, this);
    }

    public void OnInitializationComplete()
    {
        _initialized = true;
        _initializing = false;

        if (_pendingLoadInterstitialAfterInit) _pendingLoadInterstitialAfterInit = false;
        if (_pendingLoadRewardedAfterInit) _pendingLoadRewardedAfterInit = false;

        LoadInterstitial();
        LoadRewarded();

        OnInitialized?.Invoke();
    }

    public void OnInitializationFailed(UnityAdsInitializationError error, string message)
    {
        _initialized = false;
        _initializing = false;

        OnInitFailed?.Invoke($"{error} - {message}");
    }

    #endregion

    #region Load

    public void LoadInterstitial()
    {
        if (!Advertisement.isInitialized || !_initialized)
        {
            _pendingLoadInterstitialAfterInit = true;
            InitializeAds();
            return;
        }

        if (_loadingInterstitial)
        {
            return;
        }

        if (_retryInterstitialCo != null)
        {
            StopCoroutine(_retryInterstitialCo);
            _retryInterstitialCo = null;
        }

        _loadingInterstitial = true;
        _loadInterstitialStartRealtime = Time.realtimeSinceStartup;

        Advertisement.Load(InterstitialUnitId, this);
    }

    public void LoadRewarded()
    {
        if (!Advertisement.isInitialized || !_initialized)
        {
            _pendingLoadRewardedAfterInit = true;
            InitializeAds();
            return;
        }

        if (_loadingRewarded)
        {
            return;
        }

        if (_retryRewardedCo != null)
        {
            StopCoroutine(_retryRewardedCo);
            _retryRewardedCo = null;
        }

        _loadingRewarded = true;
        _loadRewardedStartRealtime = Time.realtimeSinceStartup;

        Advertisement.Load(RewardedUnitId, this);
    }

    public void OnUnityAdsAdLoaded(string unitId)
    {
        if (unitId == InterstitialUnitId)
        {
            _interstitialReady = true;
            _loadingInterstitial = false;
            _interstitialRetryCount = 0;
        }
        else if (unitId == RewardedUnitId)
        {
            _rewardedReady = true;
            _loadingRewarded = false;
            _rewardedRetryCount = 0;
        }

        OnAdLoaded?.Invoke(unitId);
    }

    public void OnUnityAdsFailedToLoad(string unitId, UnityAdsLoadError error, string message)
    {
        if (unitId == InterstitialUnitId)
        {
            _loadingInterstitial = false;
            _interstitialReady = false;

            if (enableRetry) ScheduleRetryInterstitial();
        }

        if (unitId == RewardedUnitId)
        {
            _loadingRewarded = false;
            _rewardedReady = false;

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
        if (!CanShowInterstitial())
        {
            if (!Advertisement.isInitialized || !_initialized) InitializeAds();
            else if (!_loadingInterstitial) LoadInterstitial();
            return false;
        }

        _interstitialReady = false;
        _lastInterstitialShownTime = Time.unscaledTime;

        Advertisement.Show(InterstitialUnitId, this);
        return true;
    }

    public bool ShowRewarded(Action onReward, Action onClosedOrFailed = null)
    {
        if (!CanShowRewarded())
        {
            if (!Advertisement.isInitialized || !_initialized) InitializeAds();
            else if (!_loadingRewarded) LoadRewarded();

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
        OnAdShowStart?.Invoke(unitId);
    }

    public void OnUnityAdsShowClick(string unitId)
    {
        OnAdShowClick?.Invoke(unitId);
    }

    public void OnUnityAdsShowComplete(string unitId, UnityAdsShowCompletionState showCompletionState)
    {
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

    #region Banner

    public void ShowBanner(BannerPosition position = BannerPosition.BOTTOM_CENTER)
    {
        if (!Advertisement.isInitialized || !_initialized)
        {
            InitializeAds();
            return;
        }

        Advertisement.Banner.SetPosition(position);

        Advertisement.Banner.Load(
            BannerUnitId,
            new BannerLoadOptions
            {
                loadCallback = () =>
                {
                    Advertisement.Banner.Show(BannerUnitId, new BannerOptions
                    {
                        showCallback = () => { },
                        clickCallback = () => { },
                        hideCallback = () => { }
                    });
                },
                errorCallback = (msg) => { }
            }
        );
    }

    public void HideBanner()
    {
        Advertisement.Banner.Hide();
    }

    #endregion

    #region Utilities

    public void ForceLoadInterstitial()
    {
        _interstitialReady = false;
        _loadingInterstitial = false;
        _interstitialRetryCount = 0;

        LoadInterstitial();
    }

    #endregion
}