using UnityEngine;
using TMPro;
using UnityEngine.Localization.Settings;
using System.Collections;

public class LanguageSwitcher : MonoBehaviour
{
    [SerializeField] private TMP_Dropdown dropdown;

    private void Start()
    {
        if (dropdown == null) return;

        dropdown.onValueChanged.AddListener(OnDropdownChanged);
        SyncDropdownWithCurrentLocale();
    }

    private void OnDestroy()
    {
        if (dropdown != null)
            dropdown.onValueChanged.RemoveListener(OnDropdownChanged);
    }

    private void OnDropdownChanged(int index)
    {
        switch (index)
        {
            case 0: StartCoroutine(SetLocale("en")); break;
            case 1: StartCoroutine(SetLocale("ru")); break;
        }
    }

    private IEnumerator SetLocale(string code)
    {
        yield return LocalizationSettings.InitializationOperation;

        foreach (var locale in LocalizationSettings.AvailableLocales.Locales)
        {
            if (locale.Identifier.Code == code)
            {
                LocalizationSettings.SelectedLocale = locale;
                yield break;
            }
        }

        Debug.LogWarning("[LOC] Locale not found: " + code);
    }

    private void SyncDropdownWithCurrentLocale()
    {
        var current = LocalizationSettings.SelectedLocale;
        if (current == null) return;

        switch (current.Identifier.Code)
        {
            case "en": dropdown.SetValueWithoutNotify(0); break;
            case "ru": dropdown.SetValueWithoutNotify(1); break;
        }
    }
}
