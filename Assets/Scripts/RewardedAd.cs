using System;
using UnityEngine;
using UnityEngine.Advertisements;
using System.Collections;

public class RewardedAd : MonoBehaviour, IUnityAdsLoadListener, IUnityAdsShowListener
{
    [SerializeField] private string androidAdUnitId = "Rewarded_Android";
    [SerializeField] private string iOSAdUnitId = "Rewarded_iOS";

    private string _adUnitId;
    private Action _pendingReward;
    private bool _isLoaded;
    public bool IsLoaded => _isLoaded;


    private void Awake()
    {
#if UNITY_IOS
        _adUnitId = iOSAdUnitId;
#else
        _adUnitId = androidAdUnitId;
#endif
    }

    private IEnumerator Start()
    {
        while (!Advertisement.isInitialized)
            yield return null;

        LoadAd();
    }

    public void LoadAd()
    {
        SetLoaded(false);
        Debug.Log("[ADS] Loading rewarded: " + _adUnitId);
        Advertisement.Load(_adUnitId, this);
    }

    public void ShowAd(Action onReward)
    {
        Debug.Log($"[ADS] ShowAd requested. init={Advertisement.isInitialized} loaded={_isLoaded} unit={_adUnitId}");

        if (!Advertisement.isInitialized)
        {
            Debug.LogWarning("[ADS] Not initialized yet.");
            return;
        }

        if (!_isLoaded)
        {
            Debug.LogWarning("[ADS] Rewarded not loaded yet. Reloading...");
            LoadAd();
            return;
        }

        _pendingReward = onReward;
        Advertisement.Show(_adUnitId, this);
    }

    // Load callbacks
    public void OnUnityAdsAdLoaded(string adUnitId)
    {
        SetLoaded(true);
        Debug.Log("[ADS] Rewarded loaded: " + adUnitId);
    }

    public void OnUnityAdsFailedToLoad(string adUnitId, UnityAdsLoadError error, string message)
    {
        SetLoaded(false);
        Debug.LogWarning($"[ADS] Rewarded failed to load ({adUnitId}): {error} - {message}");
        // простой ретрай
        Invoke(nameof(LoadAd), 2f);
    }

    // Show callbacks
    public void OnUnityAdsShowStart(string adUnitId)
    {
        Debug.Log("[ADS] Rewarded show START: " + adUnitId);
    }

    public void OnUnityAdsShowClick(string adUnitId)
    {
        Debug.Log("[ADS] Rewarded show CLICK: " + adUnitId);
    }

    public void OnUnityAdsShowFailure(string adUnitId, UnityAdsShowError error, string message)
    {
        SetLoaded(false);
        Debug.LogWarning($"[ADS] Rewarded show FAILED ({adUnitId}): {error} - {message}");
        _pendingReward = null;
        LoadAd();
    }

    public void OnUnityAdsShowComplete(string adUnitId, UnityAdsShowCompletionState state)
    {
        Debug.Log("[ADS] Rewarded show COMPLETE: " + state);

        if (state == UnityAdsShowCompletionState.COMPLETED)
            _pendingReward?.Invoke();

        _pendingReward = null;
        LoadAd();
    }

    public event Action<bool> OnLoadedChanged;
    private void SetLoaded(bool value)
    {
        if (_isLoaded == value) return;
        _isLoaded = value;
        OnLoadedChanged?.Invoke(_isLoaded);
    }
}
