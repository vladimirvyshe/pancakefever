using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

public class StoveShopController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text priceText;
    [SerializeField] private Button buyButton;
    [SerializeField] private TMP_Text buyButtonText;

    [Header("Tuning")]
    [SerializeField] private int maxLevel = 5;
    [SerializeField] private int[] priceByNextLevel = { 0, 250, 600, 1200, 2200, 4000 };
    [SerializeField] private int[] requiredDayByNextLevel = { 0, 1, 2, 4, 6, 9 };
    // индекс = уровень который покупаем (то есть nextLevel). priceByNextLevel[2] = цена апа до 2 уровня

    [SerializeField] private TMP_Text speedBonusText;
    [SerializeField] private TMP_Text windowBonusText;

    private GameFlowController _game;
    private bool _inited;

    public void Init(GameFlowController game)
    {
        _game = game;

        if (buyButton != null)
            buyButton.onClick.AddListener(OnBuyClicked);

        _inited = true;
        Refresh();
    }

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
        // если контроллер уже инициализирован — перерисуем тексты
        if (_inited)
            Refresh();
    }

    private string L(string table, string key)
    {
        return LocalizationSettings.StringDatabase.GetLocalizedString(table, key);
    }

    private void SetTextFormat(TMP_Text label, string table, string key, params object[] args)
    {
        if (label == null) return;
        label.text = string.Format(L(table, key), args);
    }

    private void SetTextKey(TMP_Text label, string table, string key)
    {
        if (label == null) return;
        label.text = L(table, key);
    }

    public void Refresh()
    {
        if (_game == null) return;

        int lvl = _game.GetStoveLevel();
        int coins = _game.GetCoins();

        // UI/stove_level : "Уровень плиты: {0}" / "Stove level: {0}"
        SetTextFormat(levelText, "UI", "stove_level", lvl);

        // если упёрлись в максимум
        if (lvl >= maxLevel)
        {
            // UI/stove_max : "МАКС" / "MAX"
            SetTextKey(priceText, "UI", "stove_max");

            if (buyButton != null) buyButton.interactable = false;

            // UI/stove_max_short : "Макс" / "Max"
            SetTextKey(buyButtonText, "UI", "stove_max_short");
            SetTextKey(speedBonusText, "UI", "stove_max_short");
            SetTextKey(windowBonusText, "UI", "stove_max_short");
            return;
        }

        int nextLevel = lvl + 1;
        int day = _game.GetDayIndex();
        int reqDay = GetRequiredDay(nextLevel);
        bool dayOk = day >= reqDay;

        int speedPct = Mathf.RoundToInt(18f * (nextLevel - 1));
        int windowPct = Mathf.RoundToInt(15f * (nextLevel - 1));

        // UI/stove_bonus_speed : "+{0}% скорость" / "+{0}% speed"
        SetTextFormat(speedBonusText, "UI", "stove_bonus_speed", speedPct);

        // UI/stove_bonus_burn : "+{0}% время перегорания" / "+{0}% burn time"
        SetTextFormat(windowBonusText, "UI", "stove_bonus_burn", windowPct);

        int price = GetPrice(nextLevel);

        if (priceText != null)
            priceText.text = price.ToString();

        bool enough = coins >= price;

        if (buyButton != null)
            buyButton.interactable = dayOk && enough;

        if (buyButtonText != null)
        {
            if (!dayOk)
            {
                // UI/shop_available_from_day : "Доступно с дня {0}" / "Available on day {0}"
                buyButtonText.text = string.Format(L("UI", "shop_available_from_day"), reqDay);
            }
            else
            {
                // UI/stove_upgrade : "Улучшить" / "Upgrade"
                // UI/shop_not_enough : "Не хватает" / "Not enough"
                SetTextKey(buyButtonText, "UI", enough ? "stove_upgrade" : "shop_not_enough");
            }
        }
    }

    private int GetPrice(int nextLevel)
    {
        if (priceByNextLevel == null || priceByNextLevel.Length == 0)
            return 999;

        if (nextLevel >= 0 && nextLevel < priceByNextLevel.Length)
            return priceByNextLevel[nextLevel];

        // если массив короче, чем maxLevel — простая формула
        return 500 * nextLevel;
    }

    private void OnBuyClicked()
    {
        if (_game == null) return;

        int lvl = _game.GetStoveLevel();
        if (lvl >= maxLevel) return;

        int nextLevel = lvl + 1;
        int day = _game.GetDayIndex();
        int reqDay = GetRequiredDay(nextLevel);
        if (day < reqDay)
        {
            Refresh();
            return;
        }

        int price = GetPrice(nextLevel);

        if (!_game.TrySpendCoins(price))
        {
            Refresh();
            return;
        }

        _game.SetStoveLevel(nextLevel);
        _game.SaveProgressPublic();
        _game.ApplyStoveToCooking();

        Refresh();
    }

    private int GetRequiredDay(int nextLevel)
    {
        if (requiredDayByNextLevel == null || requiredDayByNextLevel.Length == 0)
            return 1;

        if (nextLevel >= 0 && nextLevel < requiredDayByNextLevel.Length)
            return Mathf.Max(1, requiredDayByNextLevel[nextLevel]);

        return 1;
    }
}
