using UnityEngine;
using TMPro;
using UnityEngine.Localization.Settings;
using System.Collections;

public class LanguageSwitcher : MonoBehaviour
{
    [SerializeField] private TMP_Dropdown dropdown;

    // ✅ Укажи тут корневой объект настроек (панель), чтобы скрывать её пока локализация готовится
    // Если не хочешь — оставь null, просто не будет скрытия
    [SerializeField] private GameObject settingsRootToHide;
    [SerializeField] private CanvasGroup settingsCanvasGroup;

    private void Awake()
    {
        if (settingsCanvasGroup != null)
        {
            settingsCanvasGroup.alpha = 0f;
            settingsCanvasGroup.interactable = false;
            settingsCanvasGroup.blocksRaycasts = false;
        }
    }

    private void Start()
    {
        if (dropdown == null) return;

        dropdown.onValueChanged.AddListener(OnDropdownChanged);
        StartCoroutine(InitUI());
    }

    private void OnDestroy()
    {
        if (dropdown != null)
            dropdown.onValueChanged.RemoveListener(OnDropdownChanged);
    }

    private IEnumerator InitUI()
    {
        yield return LocalizationSettings.InitializationOperation;

        SyncDropdownWithCurrentLocale();

        if (settingsCanvasGroup != null)
        {
            settingsCanvasGroup.alpha = 1f;
            settingsCanvasGroup.interactable = true;
            settingsCanvasGroup.blocksRaycasts = true;
        }
    }

    private void OnDropdownChanged(int index)
    {
        string target = (index == 1) ? "ru" : "en";

        var current = LocalizationSettings.SelectedLocale;
        string currentCode = current != null ? current.Identifier.Code : "en";

        if (currentCode == target) return;

        StartCoroutine(SetLocale(target));
    }

    private IEnumerator SetLocale(string code)
    {
        yield return LocalizationSettings.InitializationOperation;

        foreach (var locale in LocalizationSettings.AvailableLocales.Locales)
        {
            if (locale.Identifier.Code == code)
            {
                LocalizationSettings.SelectedLocale = locale;
                ProgressService.SaveLanguage(code);
                yield break;
            }
        }

        Debug.LogWarning("[LOC] Locale not found: " + code);
    }

    private void SyncDropdownWithCurrentLocale()
    {
        var current = LocalizationSettings.SelectedLocale;
        if (current == null || dropdown == null) return;

        switch (current.Identifier.Code)
        {
            case "en": dropdown.SetValueWithoutNotify(0); break;
            case "ru": dropdown.SetValueWithoutNotify(1); break;
        }
    }
}