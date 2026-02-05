using System;
using UnityEngine;
using UnityEngine.UI;

public class RewardedButtonBinder : MonoBehaviour
{
    [SerializeField] private RewardedAd rewardedAd;
    [SerializeField] private Button button;

    [Header("Optional visuals")]
    [SerializeField] private CanvasGroup canvasGroup; // можно не ставить
    [SerializeField, Range(0f, 1f)] private float disabledAlpha = 0.45f;

    private bool _isLoaded;
    private bool _lockedByGameplay;

    private void OnEnable()
    {
        if (rewardedAd == null || button == null) return;

        _isLoaded = rewardedAd.IsLoaded;
        Apply();

        rewardedAd.OnLoadedChanged += OnLoadedChanged;
    }

    private void OnDisable()
    {
        if (rewardedAd == null) return;
        rewardedAd.OnLoadedChanged -= OnLoadedChanged;
    }

    public void SetGameplayLock(bool locked)
    {
        _lockedByGameplay = locked;
        Apply();
    }

    private void OnLoadedChanged(bool isLoaded)
    {
        _isLoaded = isLoaded;
        Apply();
    }

    private void Apply()
    {
        bool interactable = _isLoaded && !_lockedByGameplay;

        if (button != null) button.interactable = interactable;

        if (canvasGroup != null)
            canvasGroup.alpha = interactable ? 1f : disabledAlpha;

        Debug.Log($"[ADS][Binder] isLoaded={_isLoaded} lockedByGameplay={_lockedByGameplay} => interactable={interactable}");
    }
}
