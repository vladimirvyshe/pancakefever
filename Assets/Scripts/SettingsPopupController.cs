using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class SettingsPopupController : MonoBehaviour
{
    // PlayerPrefs keys
    private const string KEY_SFX = "SET_SFX";
    private const string KEY_VIBRO = "SET_VIBRO";
    private const string KEY_MUSIC = "SET_MUSIC";
    private const string KEY_LANG = "SET_LANG"; // "ru" / "en"

    [Header("Root")]
    [SerializeField] private GameObject root;          // SettingsPopup (сам объект попапа)
    [SerializeField] private CanvasGroup canvasGroup;  // на root или на Card (как сделаешь)
    [SerializeField] private RectTransform card;       // окно

    [Header("Controls")]
    [SerializeField] private Toggle sfxToggle;
    [SerializeField] private Toggle vibroToggle;

    // Вариант A: TMP_Dropdown (самый простой)
    [SerializeField] private TMP_Dropdown languageDropdown; // 0=RU, 1=EN

    [Header("Buttons")]
    [SerializeField] private Button closeButton;
    [SerializeField] private Button exitButton;

    [Header("Optional UI")]
    [SerializeField] private TMP_Text versionText;

    [Header("Music Toggle")]
    [SerializeField] private Toggle musicToggle;


    [Header("PopupAnimator")]
    [SerializeField] private PopupAnimator popupAnim;

    [Header("ConfirmExitPopup")]
    [SerializeField] private ConfirmExitPopup confirmExitPopup;


    public static event Action<string> OnLanguageChanged; // на будущее (локализация)

    private bool _isOpen;

    private void Awake()
    {

        // 1) считаем сохранённые значения
        bool musicOn = PlayerPrefs.GetInt("SET_MUSIC", 1) == 1;
        bool sfxOn = PlayerPrefs.GetInt("SET_SFX", 1) == 1;
        bool vibOn = PlayerPrefs.GetInt("SET_VIBRO", 1) == 1;

        // 2) выставляем тумблеры БЕЗ вызова OnValueChanged
        musicToggle.SetIsOnWithoutNotify(musicOn);
        sfxToggle.SetIsOnWithoutNotify(sfxOn);
        vibroToggle.SetIsOnWithoutNotify(vibOn);

        // 3) синхронизируем иконки (если ToggleIconSwap стоит на том же объекте)
        musicToggle.onValueChanged.Invoke(musicOn);
        sfxToggle.onValueChanged.Invoke(sfxOn);
        vibroToggle.onValueChanged.Invoke(vibOn);

        // 4) теперь подписываемся на изменения
        musicToggle.onValueChanged.AddListener(OnMusicToggle);
        sfxToggle.onValueChanged.AddListener(OnSfxToggle);
        vibroToggle.onValueChanged.AddListener(OnVibroToggle);

        // дефолты
        if (!PlayerPrefs.HasKey(KEY_SFX)) PlayerPrefs.SetInt(KEY_SFX, 1);
        if (!PlayerPrefs.HasKey(KEY_VIBRO)) PlayerPrefs.SetInt(KEY_VIBRO, 1);
        if (!PlayerPrefs.HasKey(KEY_MUSIC)) PlayerPrefs.SetInt(KEY_MUSIC, 1);
        if (!PlayerPrefs.HasKey(KEY_LANG)) PlayerPrefs.SetString(KEY_LANG, "ru");

        // подписки кнопок
        if (closeButton != null) closeButton.onClick.AddListener(Close);
        if (exitButton != null) exitButton.onClick.AddListener(ExitGame);
        if (musicToggle != null) musicToggle.onValueChanged.AddListener(OnMusicToggle);

        // применяем значения в UI
        ApplyToUI();

        // применяем поведение сразу
        ApplySfx(PlayerPrefs.GetInt(KEY_SFX, 1) == 1);
        ApplyVibro(PlayerPrefs.GetInt(KEY_VIBRO, 1) == 1);

        // слушатели (после ApplyToUI)
        if (sfxToggle != null) sfxToggle.onValueChanged.AddListener(OnSfxToggle);
        if (vibroToggle != null) vibroToggle.onValueChanged.AddListener(OnVibroToggle);
        if (languageDropdown != null) languageDropdown.onValueChanged.AddListener(OnLanguageChangedDropdown);

        // скрываем по умолчанию
        if (root == null) root = gameObject;
        root.SetActive(false);
        _isOpen = false;
    }

    private void ApplyToUI()
    {
        if (musicToggle != null)
            musicToggle.SetIsOnWithoutNotify(PlayerPrefs.GetInt(KEY_MUSIC, 1) == 1);

        if (versionText != null)
            versionText.text = "v" + Application.version;

        if (sfxToggle != null)
            sfxToggle.SetIsOnWithoutNotify(PlayerPrefs.GetInt(KEY_SFX, 1) == 1);

        if (vibroToggle != null)
            vibroToggle.SetIsOnWithoutNotify(PlayerPrefs.GetInt(KEY_VIBRO, 1) == 1);

        if (languageDropdown != null)
        {
            string lang = PlayerPrefs.GetString(KEY_LANG, "ru");
            languageDropdown.SetValueWithoutNotify(lang == "en" ? 1 : 0);
        }
    }

    // ---------- Open / Close ----------
    public void Open()
    {
        if (_isOpen) return;
        _isOpen = true;

        ApplyToUI();

        root.SetActive(true);
        popupAnim.Open();

        // ✅ гарантируем что попап сверху всех
        //transform.SetAsLastSibling();

        if (canvasGroup != null)
        {
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true; // ✅
        }
    }

    public void Close()
    {
        if (!_isOpen) return;
        _isOpen = false;

        if (canvasGroup != null)
        {
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false; // ✅
        }
        popupAnim.Close();
    }

    // ---------- Toggles ----------
    private void OnSfxToggle(bool on)
    {
        PlayerPrefs.SetInt(KEY_SFX, on ? 1 : 0);
        PlayerPrefs.Save();
        ApplySfx(on);
    }

    private void OnVibroToggle(bool on)
    {
        PlayerPrefs.SetInt(KEY_VIBRO, on ? 1 : 0);
        PlayerPrefs.Save();
        ApplyVibro(on);
    }

    private void OnLanguageChangedDropdown(int index)
    {
        string lang = (index == 1) ? "en" : "ru";
        PlayerPrefs.SetString(KEY_LANG, lang);
        PlayerPrefs.Save();

        OnLanguageChanged?.Invoke(lang);
        // пока локализации нет — просто сохраняем. Позже подключим таблицу/CSV.
    }

    // ---------- Apply behavior ----------
    private void ApplySfx(bool enabled)
    {
        if (AudioManager.I != null)
            AudioManager.I.SetSfxEnabled(enabled);
    }

    private void ApplyVibro(bool enabled)
    {
        // Ничего не вибрируем тут. Просто сохраняем настройку.
        // Потом при кликах/успехах дергай: Haptics.VibrateIfEnabled();
    }

    private void ExitGame()
    {
        confirmExitPopup.Open();
    }

    // ---------- Helpers for other scripts ----------
    public static bool IsSfxEnabled() => PlayerPrefs.GetInt(KEY_SFX, 1) == 1;
    public static bool IsVibroEnabled() => PlayerPrefs.GetInt(KEY_VIBRO, 1) == 1;
    public static string GetLang() => PlayerPrefs.GetString(KEY_LANG, "ru");

    private void OnMusicToggle(bool on)
    {
        PlayerPrefs.SetInt(KEY_MUSIC, on ? 1 : 0);
        PlayerPrefs.Save();

        if (AudioManager.I != null)
            AudioManager.I.SetMusicEnabled(on);
    }
}
