using UnityEngine;

public static class ProgressService
{
    // ¬ерси€ сейва (на будущее, если формат помен€ешь)
    private const int SAVE_VERSION = 1;

    private const string PP_VERSION = "pf_ver";
    private const string PP_COINS = "pf_coins";
    private const string PP_DAY = "pf_day";
    private const string PP_ING_MASK = "pf_ing_mask";
    private const string PP_STOVE_LEVEL = "pf_stove_lvl";
    private const string PP_DECOR_ID = "pf_decor_id";
    private const string PP_DECOR_MASK = "pp_decor_mask";

    // Ѕиты покупок ингредиентов (базовые не храним Ч они всегда доступны)
    // ћожно расшир€ть без боли
    public enum IngredientBit
    {
        Jam = 0,
        SourCream = 1,
        Chocolate = 2,
        Honey = 3,
        MapleSyrup = 4,
        PeanutButter = 5,
        // дальше добавишь: Banana = 3, etc
    }

    public struct Data
    {
        public int coins;
        public int dayIndex;
        public int ingredientMask;
        public int stoveLevel;
        public int decorId;
        public int decorMask;
        public int activeDecorMask;
    }

    public static Data Load()
    {
        // если нет версии Ч считаем новый сейв
        int ver = PlayerPrefs.GetInt(PP_VERSION, 0);

        Data d = new Data
        {
            coins = PlayerPrefs.GetInt(PP_COINS, 0),
            dayIndex = PlayerPrefs.GetInt(PP_DAY, 1),
            ingredientMask = PlayerPrefs.GetInt(PP_ING_MASK, 0),
            stoveLevel = PlayerPrefs.GetInt(PP_STOVE_LEVEL, 1),
            decorId = PlayerPrefs.GetInt(PP_DECOR_ID, 0),
            decorMask = PlayerPrefs.GetInt(PP_DECOR_MASK, 1 << 0),
            activeDecorMask = PlayerPrefs.GetInt("pp_active_decor_mask", 0),
        };

        // если когда-то помен€ешь формат Ч тут будет миграци€ по ver
        if (ver == 0)
        {
            // первый запуск или старый сейв без версии
            PlayerPrefs.SetInt(PP_VERSION, SAVE_VERSION);
            PlayerPrefs.Save();
        }

        // защита
        if (d.dayIndex < 1) d.dayIndex = 1;
        if (d.stoveLevel < 1) d.stoveLevel = 1;
        if (d.coins < 0) d.coins = 0;

        return d;
    }

    public static void Save(Data d)
    {
        PlayerPrefs.SetInt(PP_VERSION, SAVE_VERSION);
        PlayerPrefs.SetInt(PP_COINS, d.coins);
        PlayerPrefs.SetInt(PP_DAY, d.dayIndex);
        PlayerPrefs.SetInt(PP_ING_MASK, d.ingredientMask);
        PlayerPrefs.SetInt(PP_STOVE_LEVEL, d.stoveLevel);
        PlayerPrefs.SetInt(PP_DECOR_ID, d.decorId);
        PlayerPrefs.SetInt(PP_DECOR_MASK, d.decorMask);
        PlayerPrefs.SetInt("pp_active_decor_mask", d.activeDecorMask);
        PlayerPrefs.Save();
    }

    public static void ResetAll()
    {
        PlayerPrefs.DeleteKey(PP_VERSION);
        PlayerPrefs.DeleteKey(PP_COINS);
        PlayerPrefs.DeleteKey(PP_DAY);
        PlayerPrefs.DeleteKey(PP_ING_MASK);
        PlayerPrefs.DeleteKey(PP_STOVE_LEVEL);
        PlayerPrefs.DeleteKey(PP_DECOR_ID);
        PlayerPrefs.DeleteKey(PP_DECOR_MASK);
        PlayerPrefs.DeleteKey("pp_active_decor_mask");
        PlayerPrefs.Save();
    }

    public static bool HasIngredient(int mask, IngredientBit bit)
        => (mask & (1 << (int)bit)) != 0;

    public static int AddIngredient(int mask, IngredientBit bit)
        => mask | (1 << (int)bit);

    public static bool HasDecor(int mask, int decorId)
    => (mask & (1 << decorId)) != 0;

    public static int AddDecor(int mask, int decorId)
        => mask | (1 << decorId);
}
