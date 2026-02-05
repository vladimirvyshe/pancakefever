using UnityEngine;
using UnityEngine.UI;
using TMPro;

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

    public void Init(GameFlowController game)
    {
        _game = game;

        if (buyButton != null)
            buyButton.onClick.AddListener(OnBuyClicked);

        Refresh();
    }

    public void Refresh()
    {
        if (_game == null) return;

        int lvl = _game.GetStoveLevel();
        int coins = _game.GetCoins();

        if (levelText != null)
            levelText.text = $"Уровень плиты: {lvl}";

        // если упёрлись в максимум
        if (lvl >= maxLevel)
        {
            if (priceText != null) priceText.text = "МАКС";
            if (buyButton != null) buyButton.interactable = false;
            if (buyButtonText != null) buyButtonText.text = "Макс";
            if (speedBonusText != null) speedBonusText.text = "Макс";
            if (windowBonusText != null) windowBonusText.text = "Макс";
            return;
        }

        int nextLevel = lvl + 1;
        int day = _game.GetDayIndex();
        int reqDay = GetRequiredDay(nextLevel);
        bool dayOk = day >= reqDay;

        int speedPct = Mathf.RoundToInt(18f * (nextLevel - 1));
        int windowPct = Mathf.RoundToInt(15f * (nextLevel - 1));

        if (speedBonusText != null)
            speedBonusText.text = $"+{speedPct}% скорость";

        if (windowBonusText != null)
            windowBonusText.text = $"+{windowPct}% время перегорания";

        int price = GetPrice(nextLevel);

        if (priceText != null)
            priceText.text = price.ToString();

        bool enough = coins >= price;

        if (buyButton != null)
            buyButton.interactable = dayOk && enough;

        if (buyButtonText != null)
        {
            if (!dayOk) buyButtonText.text = $"Доступно с дня {reqDay}";
            else buyButtonText.text = enough ? "Улучшить" : "Не хватает";
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
