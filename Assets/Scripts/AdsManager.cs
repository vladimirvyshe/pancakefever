using UnityEngine;
using UnityEngine.Advertisements;

public class AdsManager : MonoBehaviour, IUnityAdsInitializationListener
{
#if UNITY_ANDROID
    private const string GAME_ID = "6032347"; // Android
#elif UNITY_IOS
    private const string GAME_ID = "6032346"; // iOS
#endif

    private const bool TEST_MODE = true;

    void Awake()
    {
        Debug.Log("[ADS] Awake AdsManager. supported=" 
            + Advertisement.isSupported 
            + " initialized=" 
            + Advertisement.isInitialized);

        if (!Advertisement.isInitialized && Advertisement.isSupported)
        {
            Advertisement.Initialize(GAME_ID, TEST_MODE, this);
        }
    }

    public void OnInitializationComplete()
    {
        Debug.Log("[ADS] Unity Ads initialized successfully");
    }

    public void OnInitializationFailed(UnityAdsInitializationError error, string message)
    {
        Debug.LogError($"[ADS] Init failed: {error} - {message}");
    }
}
