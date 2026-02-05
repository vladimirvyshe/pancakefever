using System;
using UnityEngine;

public class RewardedAdsStub : MonoBehaviour
{
    public virtual void Show(string placement, Action onReward)
    {
        Debug.Log($"[ADS] Show rewarded: {placement} (stub gives reward instantly)");
        onReward?.Invoke();
    }
}