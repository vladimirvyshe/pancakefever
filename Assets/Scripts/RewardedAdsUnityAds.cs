using System;
using UnityEngine;

public class RewardedAdsUnityAds : RewardedAdsStub
{
    [SerializeField] private RewardedAd rewardedAd;

    private void Awake()
    {
        if (rewardedAd == null)
            rewardedAd = GetComponent<RewardedAd>();
    }

    public override void Show(string placement, Action onReward)
    {
        if (rewardedAd == null)
        {
            Debug.LogWarning("[ADS] RewardedAd component missing");
            return;
        }

        Debug.Log("[ADS] Show rewarded (Unity Ads): " + placement);
        rewardedAd.ShowAd(onReward);
    }
}