using System.Collections;
using UnityEngine;
using UnityEngine.Localization.Settings;

public class LocalizationBootstrap : MonoBehaviour
{
    [SerializeField] private string fallbackLanguage = "en";
    private const string LangKey = "pf_lang"; // тот же ключ, что в ProgressService.SaveLanguage

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        StartCoroutine(Init());
    }

    private IEnumerator Init()
    {
        yield return LocalizationSettings.InitializationOperation;

        // ✅ Если язык уже выбирали — просто применяем сохранённый
        if (PlayerPrefs.HasKey(LangKey))
        {
            string saved = ProgressService.LoadLanguage(fallbackLanguage);
            yield return ApplyLocale(saved);
            yield break;
        }

        // ✅ Первый запуск: авто-выбор по языку устройства
        string auto = GetAutoLanguageCode();
        ProgressService.SaveLanguage(auto);
        yield return ApplyLocale(auto);
    }

    private string GetAutoLanguageCode()
    {
        // Русский язык устройства → RU, иначе EN
        return Application.systemLanguage == SystemLanguage.Russian ? "ru" : "en";
    }

    private IEnumerator ApplyLocale(string code)
    {
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
}