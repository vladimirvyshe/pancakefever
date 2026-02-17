using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

public class BurnResultPopup : MonoBehaviour
{
    [SerializeField] private TMP_Text resultText;

    private string _currentKey;
    private int _currentValue;

    private void OnEnable()
    {
        LocalizationSettings.SelectedLocaleChanged += HandleLocaleChanged;
    }

    private void OnDisable()
    {
        LocalizationSettings.SelectedLocaleChanged -= HandleLocaleChanged;
    }

    private void HandleLocaleChanged(Locale _)
    {
        // если попап открыт — пересобираем текст на новом языке
        if (!string.IsNullOrEmpty(_currentKey))
            ApplyText(_currentKey, _currentValue);
    }

    public void ShowBurned(int lostCoins)
    {
        _currentKey = "burn_result_lost";
        _currentValue = lostCoins;
        ApplyText(_currentKey, _currentValue);
        gameObject.SetActive(true);
    }

    public void ShowSaved(int earnedCoins)
    {
        _currentKey = "burn_result_saved";
        _currentValue = earnedCoins;
        ApplyText(_currentKey, _currentValue);
        gameObject.SetActive(true);
    }

    private void ApplyText(string key, int value)
    {
        if (resultText == null) return;

        string template = LocalizationSettings.StringDatabase.GetLocalizedString("UI", key);
        resultText.text = string.Format(template, value);
    }
}
