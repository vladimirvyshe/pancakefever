using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using Random = UnityEngine.Random;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

public class GameFlowController : MonoBehaviour
{

    private string IngredientToKey(IngredientId id)
    {
        return id switch
        {
            IngredientId.Jam => "ing_jam",
            IngredientId.SourCream => "ing_sour_cream",
            IngredientId.Chocolate => "ing_chocolate",
            IngredientId.Honey => "ing_honey",
            IngredientId.MapleSyrup => "ing_maple_syrup",
            IngredientId.PeanutButter => "ing_peanut_butter",
            _ => "ing_unknown"
        };
    }


    private const string ING_TABLE = "Ingredients";

    private string GetLoc(string table, string key)
    {
        if (string.IsNullOrEmpty(key)) return "";
        try
        {
            // If localization isn't initialized yet, return key to avoid null/empty.
            if (!LocalizationSettings.InitializationOperation.IsDone)
                return key;

            return LocalizationSettings.StringDatabase.GetLocalizedString(table, key);
        }
        catch
        {
            return key;
        }
    }

    // For places where we need a localized ingredient name synchronously (e.g., building strings).
    private string IngredientToText(IngredientId id)
    {
        return GetLoc(ING_TABLE, IngredientToKey(id));
    }



    [System.Serializable]
    public class OrderItem
    {
        public IngredientId id;
        public int count;
    }

    [System.Serializable]
    public class Order
    {
        public List<OrderItem> items = new();
    }

    private enum Phase
    {
        Cooking,
        Filling,
        Animating
    }

    public enum ShopTab
    {
        Main,
        Ingredients,
        Stove,
        Decor
    }

    private ShopTab _currentShopTab = ShopTab.Main;

    [ContextMenu("DEV BUY JAM")]
    private void DevBuyJam()
    {
        _progress.ingredientMask = ProgressService.AddIngredient(_progress.ingredientMask, ProgressService.IngredientBit.Jam);
        SaveProgress();
        ApplyUnlocksForCurrentDay(out _);
    }

    [SerializeField] private RectTransform toppingsLayer;


    [Header("Tutorial")]
    [SerializeField] private TutorialController tutorial;

    [Header("Result Popup")]
    [SerializeField] private GameObject resultPopup;
    [SerializeField] private TMP_Text resultText;
    [SerializeField] private TMP_Text rewardText; // можно null



    [Header("Days/Levels")]
    [SerializeField] private int ordersPerDay = 10;
    [SerializeField] private TMP_Text dayProgressText; // вместо LevelText

    [Header("Day End Popup")]
    [SerializeField] private GameObject dayEndPopup;     // твой DayEndPopup (под Canvas)
    [SerializeField] private TMP_Text dayEndMoneyText;    // текст "Заработано: ..."
    [SerializeField] private Button dayDoubleButton;      // BtnDouble
    [SerializeField] private Button dayContinueButton;    // BtnContinue

    [SerializeField] private RectTransform dayEndCard;      // тот самый Card внутри popup
    [SerializeField] private CanvasGroup dayEndCanvasGroup; // CanvasGroup на root DayEndPopup

    [SerializeField] private float dayEndFadeSeconds = 0.15f;
    [SerializeField] private float dayEndPopSeconds = 0.18f;
    [SerializeField] private float dayEndStartScale = 0.92f;
    [SerializeField] private float dayEndOvershoot = 1.02f;

    [Header("Shop Popup Anim")]
    [SerializeField] private CanvasGroup shopCanvasGroup;
    [SerializeField] private RectTransform shopCard;

    [SerializeField] private float shopFadeSeconds = 0.18f;
    [SerializeField] private float shopPopSeconds = 0.16f;
    [SerializeField] private float shopOvershoot = 1.04f;
    [SerializeField] private float shopStartScale = 0.92f;


    [Header("Combo")]
    [SerializeField] private TMP_Text comboText; // можно null, если нет UI
    [SerializeField] private float combo = 1f;
    [SerializeField] private float comboStep = 0.1f;
    [SerializeField] private float comboMax = 1.5f;

    [Header("Burn penalty")]
    [SerializeField] private int burnPenaltyCoins = 5;      // сколько списываем
    [SerializeField] private bool burnCountsAsOrder = true; // сгоревший блин засчитывается в 10 блинов дня


    [Header("Audio Manager")]
    [SerializeField] private AudioManager audioManager;


    [Header("SFX pan")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioClip addClip;
    [SerializeField] private AudioClip packClip;

    [Header("Result SFX")]
    [SerializeField] private AudioClip perfectClip;
    [SerializeField] private AudioClip goodClip;
    [SerializeField] private AudioClip badClip;

    [Header("UI SFX")]
    [SerializeField] private AudioSource uiSfxSource; // можно тот же, что sfxSource, но лучше отдельный
    [SerializeField] private AudioClip uiClickClip;
    [SerializeField] private AudioClip uiBuySuccessClip;

    [Header("Shop FX")]
    [SerializeField] private UITextShake ingPriceShake;
    [SerializeField] private UIShineFx ingRightShine;
    [SerializeField] private UIShineFx decRightShine;


    [Header("References")]
    [SerializeField] private CookingController cooking;

    [SerializeField] private Button actionButton;
    [SerializeField] private TMP_Text actionButtonText;

    [SerializeField] private TMP_Text orderText;

    [SerializeField] private Button pancakeButton;      // Button на Pancake
    [SerializeField] private RectTransform pancakeRect; // RectTransform Pancake

    [Header("Ingredient Buttons")]
    [SerializeField] private Button jamButton;
    [SerializeField] private Button sourCreamButton;
    [SerializeField] private Button chocolateButton;
    [SerializeField] private Button honeyButton;
    [SerializeField] private Button mapleSyrupButton;
    [SerializeField] private Button peanutButterButton;

    [Header("Toppings Overlays")]
    [SerializeField] private GameObject jamOverlay;
    [SerializeField] private GameObject sourCreamOverlay;
    [SerializeField] private GameObject chocolateOverlay;
    [SerializeField] private GameObject honeyOverlay;
    [SerializeField] private GameObject mapleSyrupOverlay;
    [SerializeField] private GameObject peanutButterOverlay;


    [Header("Animation Targets")]
    [SerializeField] private RectTransform plateTarget; // куда летит
    [SerializeField] private float packAnimSeconds = 0.35f;
    [SerializeField] private float flyAnimSeconds = 0.6f;
    [SerializeField] private float returnAnimSeconds = 0.5f;

    [Header("Economy (temp)")]
    [SerializeField] private int coins = 0;
    [SerializeField] private TMP_Text moneyText; // опционально

    [Header("Order Gen")]
    [Range(0f, 1f)][SerializeField] private float noFillingChance = 0.20f;

    [Header("Shop Popup")]
    [SerializeField] private GameObject shopPopup;

    [Header("Ads")]
    [SerializeField] private RewardedAdsStub rewardedAds;

    [Header("Income x2 (5 min) UI")]
    [SerializeField] private Button incomeX2Button;
    [SerializeField] private TMP_Text incomeX2ButtonText;

    [Header("Burned Popup")]
    [SerializeField] private GameObject burnedPopup;
    [SerializeField] private Button savePancakeButton;
    [SerializeField] private Button newPancakeButton;

    [Header("Shop")]

    // входы
    [SerializeField] private Button shopButton;        // из DayEndPopup (у тебя уже есть)
    [SerializeField] private Button mainShopButton;    // NEW: из главного экрана (ты уже добавил кнопку)

    // шапка
    [SerializeField] private TMP_Text shopTitleText;   // TitleText в магазине
    [SerializeField] private Button shopBackButton;    // BtnBack
    [SerializeField] private Button shopCloseButton;   // BtnClose

    // табы (GameObject контейнеры)
    [SerializeField] private GameObject tabMain;
    [SerializeField] private GameObject tabIngredients;
    [SerializeField] private GameObject tabStove;
    [SerializeField] private GameObject tabDecor;

    // кнопки внутри Tab_Main
    [SerializeField] private Button tabIngredientsButton;  // "Покупка ингредиентов"
    [SerializeField] private Button tabStoveButton;        // "Улучшение плиты"
    [SerializeField] private Button tabDecorButton;        // "Улучшение декора"

    [Header("Shop Ingredients")]
    [SerializeField] private ShopIngredientItem[] ingredientItems;

    [SerializeField] private TMP_Text ingNameText;
    [SerializeField] private TMP_Text ingPriceText;
    [SerializeField] private Button ingBuyButton;
    [SerializeField] private TMP_Text ingBuyButtonText; // текст внутри кнопки "Купить"

    [SerializeField] private PopupBackdropController backdrop;

    [Header("Shop Ingredients Right Card FX")]
    [SerializeField] private CanvasGroup ingRightCardCg;
    [SerializeField] private RectTransform ingRightCardRect;
    [SerializeField] private float ingRightFade = 0.10f;
    [SerializeField] private float ingRightSlidePx = 10f;
    private Coroutine _ingRightFx;


    [Header("StoveShop")]
    [SerializeField] private StoveShopController stoveShop;

    [Header("StoveLevel")]
    [SerializeField] private int stoveLevel = 1; // пока просто число

    [Header("DecorShop")]
    [SerializeField] private DecorShopController decorShop;


    [Header("icomeX2Binder")]
    [SerializeField] private RewardedButtonBinder incomeX2Binder;

    [Header("DiamondConroller")]
    [SerializeField] private DiamondsController diamonds;
    [SerializeField] private Button savePancakeForDiamondButton;
    [SerializeField] private GameObject perfectDayRow;

    [Header("SkipOrder")]
    [SerializeField] private Button skipOrderForDiamondButton;
    [SerializeField] private int skipOrderDiamondCost = 1;

    [Header("Skip Order Button FX")]
    [SerializeField] private CanvasGroup skipOrderCanvasGroup; // можно не задавать — добавим автоматически
    [SerializeField] private float skipShowHideDuration = 0.18f;
    [SerializeField] private float skipHiddenScale = 0.92f;

    [Header("BurnedPopupAnimator")]
    [SerializeField] private BurnedPopupAnimator burnedPopupAnimator;



    private string _actionButtonKey;

    private bool _skipVisible;
    private float _skipUiTick;
    private Coroutine _skipAnim;


    private ShopIngredientItem _selectedItem;


    private Coroutine _shopAnim;

    private Phase _phase = Phase.Cooking;

    private Order _order;
    private Dictionary<IngredientId, int> _added = new();
    private IngredientId? _selected = null;

    private ProgressService.Data _progress;

    private Vector2 _pancakeStartPos;
    private Vector3 _pancakeStartScale;

    private int _lastMatched;
    private int _lastMissing;
    private int _lastExtra;

    private int _endedDayIndex;
    private int _endedDayEarned;
    private bool _endedPerfectDay;

    private int _dayIndex = 1;
    private int _ordersThisDay = 0;
    private int _earnedThisDay = 0;

    private int _burnPenaltyCoins = 30;
    private int _coinsBeforeBurn;
    private float _comboBeforeBurn;
    private int _earnedBeforeBurn;

    private bool _skipBlockedByBurnedPopup;


    private bool _burnedPopupWasActive;
    private bool _dayChoiceMade = false;
    private bool _dayDoubleChosen = false;
    private bool _dayDoubleClaimed = false;
    private bool _shopOpenedFromDayEnd = false;
    private bool _forceJamOrderOnce;

    private bool _dayAllPerfect = true; // пока день "идеальный"
    private bool _dayHadBurn = false;   // был ли хоть один Burned

    // unlocks / upgrades
    private readonly HashSet<IngredientId> _unlocked = new();







    private readonly Dictionary<Button, Coroutine> _btnAnim = new();

    private Coroutine _dayPulseRoutine;


    private void Awake()
    {

        _progress = ProgressService.Load();
        coins = _progress.coins;
        _dayIndex = _progress.dayIndex;
        stoveLevel = _progress.stoveLevel;
        _ordersThisDay = _progress.ordersThisDay;
        _earnedThisDay = _progress.earnedThisDay;

        cooking.ApplyStoveLevel(stoveLevel);

        if (shopPopup != null) shopPopup.SetActive(false);
        if (dayEndPopup != null) dayEndPopup.SetActive(false);

        _pancakeStartPos = pancakeRect.anchoredPosition;
        _pancakeStartScale = pancakeRect.localScale;

        // ингредиенты
        jamButton.onClick.AddListener(() => { PlayUIClick(); SelectIngredient(IngredientId.Jam); });
        sourCreamButton.onClick.AddListener(() => { PlayUIClick(); SelectIngredient(IngredientId.SourCream); });
        chocolateButton.onClick.AddListener(() => { PlayUIClick(); SelectIngredient(IngredientId.Chocolate); });
        honeyButton.onClick.AddListener(() => { PlayUIClick(); SelectIngredient(IngredientId.Honey); });
        mapleSyrupButton.onClick.AddListener(() => { PlayUIClick(); SelectIngredient(IngredientId.MapleSyrup); });
        peanutButterButton.onClick.AddListener(() => { PlayUIClick(); SelectIngredient(IngredientId.PeanutButter); });


        if (shopCloseButton != null)
            shopCloseButton.onClick.AddListener(() => { PlayUIClick(); CloseShop(); });

        if (shopBackButton != null)
            shopBackButton.onClick.AddListener(() => { PlayUIClick(); OpenShopTab(ShopTab.Main); });

        if (tabIngredientsButton != null)
            tabIngredientsButton.onClick.AddListener(() => { PlayUIClick(); OpenShopTab(ShopTab.Ingredients); });

        if (tabStoveButton != null)
            tabStoveButton.onClick.AddListener(() => { PlayUIClick(); OpenShopTab(ShopTab.Stove); });

        if (tabDecorButton != null)
            tabDecorButton.onClick.AddListener(() => { PlayUIClick(); OpenShopTab(ShopTab.Decor); });

        if (shopButton != null)
            shopButton.onClick.AddListener(() => { PlayUIClick(); OpenShopFromDayEnd(); });

        if (mainShopButton != null)
            mainShopButton.onClick.AddListener(() => { PlayUIClick(); OpenShopFromMain(); });



        // клик по блину (добавление порции)

        // ActionButton всегда слушаем, но поведение зависит от фазы
        actionButton.onClick.AddListener(() => { PlayUIClick(); OnActionButtonClicked(); });

        ClearAllToppingOverlays();
        SetFillingUI(false);

        // Day End popup init
        if (dayEndPopup != null)
            dayEndPopup.SetActive(false);

        if (dayDoubleButton != null)
            dayDoubleButton.onClick.AddListener(OnDayDoubleClicked);

        if (dayContinueButton != null)
            dayContinueButton.onClick.AddListener(OnDayContinueClicked);

        if (shopPopup != null)
            shopPopup.SetActive(false);


        if (shopButton != null)
            shopButton.onClick.AddListener(OpenShopFromDayEnd);

        if (mainShopButton != null)
            mainShopButton.onClick.AddListener(OpenShopFromMain);

        // кнопки шапки
        if (shopCloseButton != null)
            shopCloseButton.onClick.AddListener(CloseShop);

        if (shopBackButton != null)
            shopBackButton.onClick.AddListener(() => OpenShopTab(ShopTab.Main));

        // кнопки главного таба
        if (tabIngredientsButton != null)
            tabIngredientsButton.onClick.AddListener(() => OpenShopTab(ShopTab.Ingredients));

        if (tabStoveButton != null)
            tabStoveButton.onClick.AddListener(() => OpenShopTab(ShopTab.Stove));

        if (tabDecorButton != null)
            tabDecorButton.onClick.AddListener(() => OpenShopTab(ShopTab.Decor));

        if (burnedPopup != null)
            burnedPopup.SetActive(false);

        _burnedPopupWasActive = false;
        SetSkipBlockedByBurnedPopup(false);   // чтобы сразу пересчитать skip
        RefreshSkipOrderButtonFx(force: true);

        // начальное состояние магазина
        if (shopPopup != null)
            shopPopup.SetActive(false);

        if (ingredientItems != null)
        {
            foreach (var item in ingredientItems)
            {
                if (item != null)
                    item.Clicked += OnShopIngredientSelected;
            }
        }

        if (ingBuyButton != null)
            ingBuyButton.onClick.AddListener(BuySelectedIngredient_DevOnly);

        if (stoveShop != null)
            stoveShop.Init(this);

        if (decorShop != null)
        {
            decorShop.Init(this);
            ApplyDecorToSceneMulti(decorShop.GetDefs());
        }




        if (cooking != null)
            cooking.ApplyStoveLevel(stoveLevel);

        OpenShopTab(ShopTab.Main);

    }

    private void OnEnable()
    {
        cooking.Cooked += OnCooked;
        cooking.Burned += OnBurned;
        LocalizationSettings.SelectedLocaleChanged += HandleLocaleChanged;
    }

    private void OnDisable()
    {
        cooking.Cooked -= OnCooked;
        cooking.Burned -= OnBurned;
        LocalizationSettings.SelectedLocaleChanged -= HandleLocaleChanged;
    }

    private void Start()
    {

        UpdateIncomeX2UI();

        if (incomeX2Button != null)
            incomeX2Button.onClick.AddListener(OnIncomeX2Clicked);

        if (savePancakeButton != null)
            savePancakeButton.onClick.AddListener(OnSavePancakeClicked);

        if (newPancakeButton != null)
            newPancakeButton.onClick.AddListener(OnNewPancakeClicked);

        if (savePancakeForDiamondButton != null)
            savePancakeForDiamondButton.onClick.AddListener(OnSavePancakeForDiamondClicked);

        if (diamonds != null)
            diamonds.Init(_progress);

        if (skipOrderForDiamondButton != null)
            skipOrderForDiamondButton.onClick.AddListener(OnSkipOrderForDiamondClicked);

        InitSkipOrderButtonFx();
        RefreshSkipOrderButtonFx(force: true);
        _burnedPopupWasActive = (burnedPopup != null && burnedPopup.activeSelf);


        UpdateComboUI();
        UpdateDayProgressUI();
        UpdateMoneyUI();
        ApplyUnlocksForCurrentDay(out _);
        _dayAllPerfect = true;
        _dayHadBurn = false;
        NewOrder();
        EnterCookingPhase();

        if (tutorial != null)
            tutorial.TryStart();
    }

    private void NewOrder()
    {
        ClearAllToppingOverlays();
        var prev = _order;

        // пытаемся сгенерить другой (несколько попыток)
        Order next = null;
        for (int i = 0; i < 8; i++)
        {
            next = GenerateOrder();
            if (prev == null || !OrdersEqual(prev, next))
                break;
        }

        _order = next ?? GenerateOrder();
        _added.Clear();
        _selected = null;

        // ✅ если после покупки джема нужно гарантировать джем в заказе
        if (_forceJamOrderOnce)
        {
            EnsureJamInOrder(_order);
            _forceJamOrderOnce = false; // одноразово
        }

        UpdateOrderText();
        ResetPancakeVisual();              // цвет, начинка
        pancakeRect.localScale = _pancakeStartScale;
        pancakeRect.anchoredPosition = _pancakeStartPos;
        pancakeRect.gameObject.SetActive(true);
    }

    private Order GenerateOrder()
    {
        Order o = new Order();

        if (_unlocked.Count == 0)
            return o; // без начинки

        // пул только из доступных (купленных) ингредиентов
        var pool = new List<IngredientId>(_unlocked);
        pool.Remove(IngredientId.None);

        if (pool.Count == 0)
            return o;

        // 1–2 ингредиента
        int count = Random.Range(1, 5);

        // чтобы не повторять один и тот же ингредиент дважды в заказе
        for (int i = 0; i < count && pool.Count > 0; i++)
        {
            int idx = Random.Range(0, pool.Count);
            var id = pool[idx];
            pool.RemoveAt(idx);

            o.items.Add(new OrderItem
            {
                id = id,
                count = Random.Range(1, 4)
            });
        }

        return o;
    }

    private bool OrderHasJam(Order o)
    {
        if (o == null || o.items == null) return false;
        for (int i = 0; i < o.items.Count; i++)
            if (o.items[i].id == IngredientId.Jam)
                return true;
        return false;
    }

    private void EnsureJamInOrder(Order o)
    {
        if (o == null) return;

        // если уже есть джем — ок
        if (OrderHasJam(o)) return;

        // если заказ пустой — просто добавим джем
        if (o.items.Count == 0)
        {
            o.items.Add(new OrderItem { id = IngredientId.Jam, count = Random.Range(1, 4) });
            return;
        }

        // если уже 2 ингредиента — заменим первый на джем (чтобы сохранить 1–2 айтема как у тебя)
        if (o.items.Count >= 2)
        {
            o.items[0].id = IngredientId.Jam;
            o.items[0].count = Random.Range(1, 5);
            return;
        }

        // если 1 ингредиент — добавим вторым джем
        o.items.Add(new OrderItem { id = IngredientId.Jam, count = Random.Range(1, 4) });
    }

    private void OnCooked()
    {
        // СРАЗУ после приготовления: включаем начинку и меняем кнопку на "Упаковать"
        EnterFillingPhase();
    }

    private void OnBurned()
    {
        _dayHadBurn = true;
        if (actionButton != null)
            actionButton.gameObject.SetActive(false);
        ClearAllToppingOverlays();

        _coinsBeforeBurn = coins;
        _comboBeforeBurn = combo;
        _earnedBeforeBurn = _earnedThisDay;

        // штраф
        ResetCombo();
        coins = Mathf.Max(0, coins - burnPenaltyCoins);
        UpdateMoneyUI();

        int coinsAfter = coins;
        int lostCoins = _coinsBeforeBurn - coinsAfter; // это и есть X (учитывает clamp в 0)

        // ✅ Burn должен уменьшать "заработано за день", чтобы day-end и x2 дня были честными
        _earnedThisDay = Mathf.Max(0, _earnedThisDay - lostCoins);
        UpdateDayProgressUI();

        // ✅ Сохраняем сразу, чтобы штраф не терялся при выходе
        SaveProgress();

        string msg = string.Format(
            L("UI", "burn_penalty_coins"),
            lostCoins
        );

        StartCoroutine(ShowResultPopup(msg, "", 0.6f));

        // показать попап
        if (burnedPopup != null)
        {
            audioManager.PlayBurnedSfx();
            burnedPopupAnimator.Open();
        }
        SetSkipBlockedByBurnedPopup(true);

        if (savePancakeForDiamondButton != null)
        {
            bool can = (diamonds != null && diamonds.Diamonds >= 1);
            savePancakeForDiamondButton.interactable = can;
        }
        RefreshSkipOrderButtonFx(force: true);
    }



    //IEnumerator

    private IEnumerator RestartAfterDelay(float sec)
    {
        _phase = Phase.Animating;
        yield return new WaitForSeconds(sec);

        ClearAllToppingOverlays();
        ResetPancakeVisual();
        NewOrder();


        if (cooking != null)
            cooking.ResetForNewPancake();

        EnterCookingPhase();
        _phase = Phase.Cooking;
    }

    private IEnumerator PulseButton(Transform t)
    {
        Vector3 start = t.localScale;
        Vector3 up = start * 1.08f;

        float t1 = 0f;
        while (t1 < 1f)
        {
            t1 += Time.deltaTime * 6f;
            t.localScale = Vector3.Lerp(start, up, t1);
            yield return null;
        }

        t1 = 0f;
        while (t1 < 1f)
        {
            t1 += Time.deltaTime * 6f;
            t.localScale = Vector3.Lerp(up, start, t1);
            yield return null;
        }
    }

    private IEnumerator ShowResultPopup(string title, string rewardLine, float seconds)
    {
        if (resultPopup == null || resultText == null) yield break;

        resultPopup.SetActive(true);

        resultText.text = title;
        if (rewardText != null) rewardText.text = rewardLine;

        // Попробуем сделать fade через CanvasGroup (если нет — добавим автоматически)
        var cg = resultPopup.GetComponent<CanvasGroup>();
        if (cg == null) cg = resultPopup.AddComponent<CanvasGroup>();

        RectTransform rt = resultPopup.GetComponent<RectTransform>();
        Vector3 startScale = Vector3.one * 0.85f;
        Vector3 endScale = Vector3.one;

        cg.alpha = 0f;
        rt.localScale = startScale;

        // Появление
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * 10f;
            cg.alpha = Mathf.Lerp(0f, 1f, t);
            rt.localScale = Vector3.Lerp(startScale, endScale, Mathf.SmoothStep(0f, 1f, t));
            yield return null;
        }

        yield return new WaitForSeconds(seconds);

        // Исчезновение
        t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * 10f;
            cg.alpha = Mathf.Lerp(1f, 0f, t);
            rt.localScale = Vector3.Lerp(endScale, startScale, Mathf.SmoothStep(0f, 1f, t));
            yield return null;
        }

        resultPopup.SetActive(false);
    }

    private string ScoreTitle(Score s)
    {
        return s == Score.Perfect ? L("UI", "score_perfect") :
               s == Score.Good ? L("UI", "score_good") :
                                    L("UI", "score_bad");
    }

    private void ApplyScoreStyle(Score s)
    {
        if (resultText == null) return;
        if (s == Score.Perfect) resultText.color = new Color(0.56f, 0.78f, 0.60f);
        else if (s == Score.Good) resultText.color = new Color(0.96f, 0.78f, 0.42f);
        else resultText.color = new Color(0.62f, 0.60f, 0.56f);
    }



    private void EnterCookingPhase()
    {
        HidePancake();
        _phase = Phase.Cooking;

        SetFillingUI(false);
        _selected = null;

        // Важно: CookingController сам управляет подписью кнопки в готовке
        // Мы тут ничего не ставим, чтобы не мешать
        UpdateOrderText();
    }

    private void EnterFillingPhase()
    {
        _phase = Phase.Filling;

        SetFillingUI(true);
        _selected = null;

        UpdateIngredientSelectionUI();

        SetActionButtonKey("action_pack");
        StartCoroutine(PulseButton(actionButton.transform));
        UpdateOrderText();
    }

    private void OnActionButtonClicked()
    {
        // 1) Фаза начинки / упаковки
        if (_phase == Phase.Filling)
        {
            if (tutorial != null)
                tutorial.NotifyAction(TutorialAction.Pack);

            StartCoroutine(PackServeReturn());
            return;
        }

        // 2) Фаза готовки
        if (_phase == Phase.Cooking)
        {
            if (cooking == null) return;

            var prev = cooking.CurrentState;

            // сначала отдаём клик контроллеру готовки
            cooking.HandleActionButton();

            // если состояние НЕ изменилось — значит была "..." / действие недоступно
            // => туториал не трогаем
            if (cooking.CurrentState == prev)
                return;

            // Burned -> WaitingPour (Новый блин) туториал тоже не должен двигать
            if (prev == CookingController.CookState.Burned &&
                cooking.CurrentState == CookingController.CookState.WaitingPour)
            {
                NewOrder();
                HidePancake();
                return;
            }

            // определяем, что реально сделали: Pour или Flip
            TutorialAction action;
            if (prev == CookingController.CookState.WaitingPour &&
                cooking.CurrentState != CookingController.CookState.WaitingPour)
            {
                action = TutorialAction.Pour;
                ShowFreshPancake(); // это твой текущий хук "после заливки появился блин"
            }
            else
            {
                action = TutorialAction.Flip;
            }

            if (tutorial != null)
                tutorial.NotifyAction(action);

            return;
        }
    }


    private void SelectIngredient(IngredientId id)
    {
        if (_phase != Phase.Filling) return;
        _selected = id;
        UpdateOrderText();
        UpdateIngredientSelectionUI();
    }

    private void OnPancakeClicked()
    {


        if (_phase != Phase.Filling) return;
        if (_selected == null) return;

        var id = _selected.Value;
        if (!_added.ContainsKey(id)) _added[id] = 0;
        _added[id]++;

        sfxSource.PlayOneShot(addClip);

        ShowToppingOverlay(id, true);

        UpdateOrderText();
    }

    private IEnumerator PackServeReturn()
    {
        sfxSource.PlayOneShot(packClip);

        _phase = Phase.Animating;
        SetFillingUI(false);

        // 1) "сворачивание" (простая анимация скейла)
        yield return ScaleTo(pancakeRect, _pancakeStartScale * 1.15f, packAnimSeconds);

        // 2) "перелет" к тарелке
        Vector2 from = pancakeRect.anchoredPosition;
        Vector2 to = plateTarget.anchoredPosition;
        yield return MoveTo(pancakeRect, from, to, flyAnimSeconds);
        pancakeRect.gameObject.SetActive(false);

        HidePancake();

        // 3) оценка + награда
        var score = EvaluateOrder(out int baseReward);
        if (score != Score.Perfect)
            _dayAllPerfect = false;

        // комбо применяется только к хорошим исходам
        int finalReward = baseReward;
        if (score == Score.Perfect)
        {
            finalReward = Mathf.RoundToInt(baseReward * combo);
            IncreaseCombo();
        }
        else
        {
            ResetCombo();
        }

        // ✅ Decor income bonus (как сейчас: по owned, не по active)
        float decorMult = 1f;
        if (decorShop != null)
            decorMult = GetDecorIncomeMultiplier(decorShop.GetDefs());

        finalReward = Mathf.RoundToInt(finalReward * decorMult);

        finalReward = ApplyIncomeBoost(finalReward);

        coins += finalReward;
        UpdateMoneyUI();

        if (tutorial != null)
            tutorial.OnOrderCompleted(coins);

        _ordersThisDay++;
        _earnedThisDay += finalReward;
        UpdateDayProgressUI();

        // ✅ ВАЖНО: сохраняем сразу, чтобы при выходе не терялось
        SaveProgress();

        if (sfxSource != null)
        {
            var clip = score == Score.Perfect ? perfectClip :
                       score == Score.Good ? goodClip :
                       badClip;

            if (clip != null) sfxSource.PlayOneShot(clip);
        }

        ApplyScoreStyle(score);

        string title = ScoreTitle(score);
        string rewardLine = string.Format(L("UI", "reward_plus_coins"), finalReward);

        StartCoroutine(ShowResultPopup(title, rewardLine, 0.6f));


        // orderText можно оставить для отладки или укоротить:
        orderText.text = L("UI", "order_new");
        UpdateMoneyUI();


        yield return new WaitForSeconds(0.7f);

        // 4) возвращаем блин на плиту
        yield return MoveTo(pancakeRect, pancakeRect.anchoredPosition, _pancakeStartPos, returnAnimSeconds);
        yield return ScaleTo(pancakeRect, _pancakeStartScale, 0.2f);

        // ✅ End of day check (после результата и перед новым заказом)
        if (_ordersThisDay >= ordersPerDay)
        {
            yield return ShowDayEndAndWait();
        }

        // новый цикл
        ResetPancakeVisual();
        NewOrder();
        cooking.ResetForNewPancake();
        EnterCookingPhase();
        _phase = Phase.Cooking;
    }

    private enum Score { Perfect, Good, Bad }

    private Score EvaluateOrder(out int reward)
    {
        reward = 0;

        if (_order.items.Count == 0)
        {
            if (_added.Count == 0)
            {
                reward = 20;
                return Score.Perfect;
            }

            reward = 5;
            return Score.Bad;
        }

        if (CheckExactMatch())
        {
            int requiredTotalExact = 0;
            foreach (var it in _order.items) requiredTotalExact += it.count;

            reward = 20 + requiredTotalExact * 4;
            return Score.Perfect;
        }

        int requiredTotal = 0;
        foreach (var it in _order.items) requiredTotal += it.count;

        int matched = CountMatchedPortions();

        int addedTotal = 0;
        foreach (var v in _added.Values) addedTotal += v;

        int extra = Mathf.Max(0, addedTotal - matched);
        int missing = Mathf.Max(0, requiredTotal - matched);

        _lastMatched = matched;
        _lastMissing = missing;
        _lastExtra = extra;

        float accuracy = requiredTotal == 0 ? 1f : (float)matched / requiredTotal;

        bool goodEnough = accuracy >= 0.5f && extra <= 2;

        if (goodEnough)
        {
            reward = 8 + Mathf.RoundToInt((22 + requiredTotal * 3) * accuracy) - extra * 3;
            reward = Mathf.Max(3, reward);
            return Score.Good;
        }

        reward = 5;
        return Score.Bad;
    }



    private bool CheckExactMatch()
    {
        // все требуемые совпали
        foreach (var it in _order.items)
        {
            _added.TryGetValue(it.id, out int got);
            if (got != it.count) return false;
        }
        // нет лишних ингредиентов
        foreach (var kv in _added)
        {
            bool exists = false;
            foreach (var it in _order.items)
            {
                if (it.id == kv.Key) { exists = true; break; }
            }
            if (!exists) return false;
        }
        return true;
    }

    private int CountMatchedPortions()
    {
        int matched = 0;
        foreach (var it in _order.items)
        {
            _added.TryGetValue(it.id, out int got);
            matched += Mathf.Min(got, it.count);
        }
        return matched;
    }

    private string ScoreToText(Score s) =>
        s == Score.Perfect ? "ИДЕАЛЬНО" :
        s == Score.Good ? "НОРМАЛЬНО" :
        "ПЛОХО";

    private void UpdateOrderText()
    {
        var sb = new StringBuilder();

        if (_order.items.Count == 0)
        {
            sb.Append(L("UI", "order_empty"));
        }
        else
        {
            sb.Append(L("UI", "order_title"));
            sb.Append("\n");

            for (int i = 0; i < _order.items.Count; i++)
            {
                var it = _order.items[i];

                // локализованное имя ингредиента
                string ingName = L("Ingredients", IngredientToKey(it.id));

                sb.Append($"{ingName} x{it.count}");

                if (i < _order.items.Count - 1)
                    sb.Append(",\n");
            }
        }

        orderText.text = sb.ToString();
    }



    private void SetFillingUI(bool isFillingPhase)
    {
        // базовые/премиум включаем только если: (фаза начинки) И (ингредиент куплен)
        SetIngredientButtonAvailability(jamButton, IngredientId.Jam, isFillingPhase);
        SetIngredientButtonAvailability(sourCreamButton, IngredientId.SourCream, isFillingPhase);
        SetIngredientButtonAvailability(chocolateButton, IngredientId.Chocolate, isFillingPhase);
        SetIngredientButtonAvailability(honeyButton, IngredientId.Honey, isFillingPhase);
        SetIngredientButtonAvailability(mapleSyrupButton, IngredientId.MapleSyrup, isFillingPhase);
        SetIngredientButtonAvailability(peanutButterButton, IngredientId.PeanutButter, isFillingPhase);

        if (!isFillingPhase)
        {
            _selected = IngredientId.None;
            UpdateIngredientSelectionUI();
        }
    }

    private void SetIngredientButtonState(Button btn, bool enabled)
    {
        if (btn == null) return;

        btn.interactable = enabled;

        // Визуально “приглушаем”, но не скрываем
        var cg = btn.GetComponent<CanvasGroup>();
        if (cg == null) cg = btn.gameObject.AddComponent<CanvasGroup>();
        cg.alpha = enabled ? 1f : 0.8f;
    }

    private void UpdateMoneyUI()
    {
        if (moneyText != null)
            moneyText.text = coins.ToString();
    }

    private void ResetPancakeVisual()
    {
        // возвращаем позицию/скейл уже в корутине, тут просто сброс данных начинки
        _added.Clear();
        _selected = null;

        ClearAllToppingOverlays();
    }

    private static IEnumerator MoveTo(RectTransform rt, Vector2 from, Vector2 to, float sec)
    {
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.0001f, sec);
            rt.anchoredPosition = Vector2.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t));
            yield return null;
        }
        rt.anchoredPosition = to;
    }

    private static IEnumerator ScaleTo(RectTransform rt, Vector3 to, float sec)
    {
        Vector3 from = rt.localScale;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.0001f, sec);
            rt.localScale = Vector3.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t));
            yield return null;
        }
        rt.localScale = to;
    }
    private void UpdateIngredientSelectionUI()
    {
        SetBtn(jamButton, _selected == IngredientId.Jam);
        SetBtn(sourCreamButton, _selected == IngredientId.SourCream);
        SetBtn(chocolateButton, _selected == IngredientId.Chocolate);
        SetBtn(honeyButton, _selected == IngredientId.Honey);
        SetBtn(mapleSyrupButton, _selected == IngredientId.MapleSyrup);
        SetBtn(peanutButterButton, _selected == IngredientId.PeanutButter);

    }

    private void SetBtn(Button btn, bool selected)
    {
        if (btn == null) return;

        // стопаем прошлую анимацию на этой кнопке
        if (_btnAnim.TryGetValue(btn, out var c) && c != null)
            StopCoroutine(c);

        _btnAnim[btn] = StartCoroutine(AnimateBtn(btn, selected));
    }

    private IEnumerator AnimateBtn(Button btn, bool selected)
    {
        var rt = btn.GetComponent<RectTransform>();
        var img = btn.GetComponent<Image>();

        Vector3 fromScale = rt != null ? rt.localScale : Vector3.one;
        Vector3 toScale = selected ? Vector3.one * 1.06f : Vector3.one;

        Color fromColor = img != null ? img.color : Color.white;
        Color toColor = selected ? new Color(1f, 0.92f, 0.78f, 1f) : Color.white;

        float dur = 0.08f; // очень быстро, но плавно
        float t = 0f;

        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / dur);
            float s = Mathf.SmoothStep(0f, 1f, k);

            if (rt != null) rt.localScale = Vector3.Lerp(fromScale, toScale, s);
            if (img != null) img.color = Color.Lerp(fromColor, toColor, s);

            yield return null;
        }

        if (rt != null) rt.localScale = toScale;
        if (img != null) img.color = toColor;

        _btnAnim[btn] = null;
    }

    private void IncreaseCombo()
    {
        combo = Mathf.Min(comboMax, combo + comboStep);
        UpdateComboUI();
    }

    private void ResetCombo()
    {
        combo = 1f;
        UpdateComboUI();
    }

    private void UpdateComboUI()
    {
        if (comboText != null)
            comboText.text = $"{combo:0.0}";
    }


    private void HidePancake()
    {
        if (pancakeRect != null)
            pancakeRect.gameObject.SetActive(false);
    }

    private void ShowFreshPancake()
    {
        if (pancakeRect == null) return;

        pancakeRect.gameObject.SetActive(true);
        pancakeRect.anchoredPosition = _pancakeStartPos;
        pancakeRect.localScale = _pancakeStartScale;

        ClearAllToppingOverlays();
        ResetPancakeVisual(); // очистка цвета/начинок — у тебя уже есть
    }

    private void ShowToppingOverlay(IngredientId id, bool show)
    {
        GameObject target = null;

        switch (id)
        {
            case IngredientId.Jam: target = jamOverlay; break;
            case IngredientId.SourCream: target = sourCreamOverlay; break;
            case IngredientId.Chocolate: target = chocolateOverlay; break;
            case IngredientId.Honey: target = honeyOverlay; break;
            case IngredientId.MapleSyrup: target = mapleSyrupOverlay; break;
            case IngredientId.PeanutButter: target = peanutButterOverlay; break;
            default: return;
        }

        if (target != null)
            target.SetActive(show);
    }

    private void ClearAllToppingOverlays()
    {
        if (jamOverlay) jamOverlay.SetActive(false);
        if (sourCreamOverlay) sourCreamOverlay.SetActive(false);
        if (chocolateOverlay) chocolateOverlay.SetActive(false);
        if (honeyOverlay) honeyOverlay.SetActive(false);
        if (mapleSyrupOverlay) mapleSyrupOverlay.SetActive(false);
        if (peanutButterOverlay) peanutButterOverlay.SetActive(false);
        for (int i = toppingsLayer.childCount - 1; i >= 0; i--)
        {
            var child = toppingsLayer.GetChild(i).gameObject;

            // НЕ удаляем шаблоны (те самые overlay-объекты)
            if (child == jamOverlay || child == sourCreamOverlay || child == chocolateOverlay ||
                child == honeyOverlay || child == mapleSyrupOverlay || child == peanutButterOverlay)
                continue;

            Destroy(child);
        }
    }


    public void OnPancakePointerClick(PointerEventData eventData)
    {
        Debug.Log("Pancake pointer click!");
        if (_phase != Phase.Filling) return;
        if (_selected == null || _selected == IngredientId.None) return;
        if (toppingsLayer == null) return;

        // экран -> локальная позиция в ToppingsLayer
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                toppingsLayer, eventData.position, eventData.pressEventCamera, out var local))
            return;

        // ограничение кругом (можно без него, но лучше так)
        float radius = Mathf.Min(toppingsLayer.rect.width, toppingsLayer.rect.height) * 0.5f * 0.95f;
        if (local.sqrMagnitude > radius * radius) return;

        var id = _selected.Value;

        // ТВОЯ СУЩЕСТВУЮЩАЯ ЛОГИКА УЧЁТА:
        if (!_added.ContainsKey(id)) _added[id] = 0;
        _added[id]++;

        if (sfxSource && addClip) sfxSource.PlayOneShot(addClip);

        // ВОТ ОНО: ставим "кляксу" именно в точку клика
        SpawnBlobAt(id, local);

        UpdateOrderText();
    }
    private void SpawnBlobAt(IngredientId id, Vector2 localPos)
    {
        var template = GetOverlayTemplate(id);
        var blob = Instantiate(template, toppingsLayer);

        var rt = blob.GetComponent<RectTransform>();

        // 1️⃣ СЛУЧАЙНЫЙ БАЗОВЫЙ РАЗМЕР
        float randomScale = Random.Range(0.9f, 1f);
        rt.localScale = Vector3.one * randomScale;

        // 2️⃣ ПЕРЕДАЁМ БАЗОВЫЙ РАЗМЕР В BlobPop
        var pop = blob.GetComponent<BlobPop>();
        if (pop != null)
        {
            pop.baseScale = randomScale;
        }

        // позиция и поворот
        rt.anchoredPosition = localPos;
        rt.localRotation = Quaternion.Euler(0, 0, Random.Range(0f, 360f));

        tutorial.OnToppingApplied();
        blob.SetActive(true);

    }

    private GameObject GetOverlayTemplate(IngredientId id)
    {
        switch (id)
        {
            case IngredientId.Jam: return jamOverlay;
            case IngredientId.SourCream: return sourCreamOverlay;
            case IngredientId.Chocolate: return chocolateOverlay;
            case IngredientId.Honey: return honeyOverlay;
            case IngredientId.MapleSyrup: return mapleSyrupOverlay;
            case IngredientId.PeanutButter: return peanutButterOverlay;
            default: return null;
        }
    }

    private void PlayUIClick()
    {
        if (uiSfxSource != null && uiClickClip != null)
            uiSfxSource.PlayOneShot(uiClickClip);
    }

    private void OnDayDoubleClicked()
    {
        if (_dayDoubleClaimed) return;

        if (rewardedAds == null)
        {
            Debug.LogWarning("RewardedAds not assigned");
            return;
        }

        rewardedAds.Show("day_end_x2", () =>
        {
            ApplyDayDoubleEnded();
        });
    }


    private void OnDayContinueClicked()
    {
        _dayDoubleChosen = false;
        _dayChoiceMade = true;
    }

    private IEnumerator ShowDayEndAndWait()
    {
        _dayDoubleClaimed = false;

        // вернуть кнопку x2 в активное состояние на новом дне
        if (dayDoubleButton != null)
        {
            dayDoubleButton.interactable = true;
            var cg = dayDoubleButton.GetComponent<CanvasGroup>();
            if (cg == null) cg = dayDoubleButton.gameObject.AddComponent<CanvasGroup>();
            cg.alpha = 1f;
        }

        _phase = Phase.Animating; // блокируем обычные клики
        _dayChoiceMade = false;
        _dayDoubleChosen = false;

        // ---------- 1) фиксируем итоги текущего дня ДЛЯ ПОПАПА ----------
        int endedDayIndex = _dayIndex;
        int endedEarned = _earnedThisDay;

        bool perfectDay = _dayAllPerfect && !_dayHadBurn;
        if (perfectDayRow != null)
            perfectDayRow.SetActive(perfectDay);

        if (perfectDay && diamonds != null)
        {
            diamonds.Add(1);
            RefreshSkipOrderButtonFx(force: true); // важно для сохранения
            _progress.diamonds = diamonds.Diamonds;
        }

        _endedDayIndex = _dayIndex;
        _endedDayEarned = _earnedThisDay;
        _endedPerfectDay = _dayAllPerfect && !_dayHadBurn;

        if (perfectDayRow != null)
            perfectDayRow.SetActive(_endedPerfectDay);


        // текст попапа — про завершённый день
        if (dayEndMoneyText != null)
            dayEndMoneyText.text = string.Format(L("UI", "day_end_earned"), endedDayIndex, endedEarned);

        // ---------- 2) СРАЗУ ПЕРЕВОДИМ ИГРУ В НОВЫЙ ДЕНЬ + СОХРАНЯЕМ ----------

        _dayIndex++;
        _ordersThisDay = 0;
        _earnedThisDay = 0;
        _dayAllPerfect = true;
        _dayHadBurn = false;

        SaveProgress();
        UpdateDayProgressUI();

        // ---------- 3) показываем попап и ждём выбора ----------
        if (dayEndPopup != null)
            yield return AnimateDayEndPopup(true);

        while (!_dayChoiceMade)
            yield return null;

        if (dayEndPopup != null)
            yield return AnimateDayEndPopup(false);

        // ---------- 4) продолжаем игру (день уже новый) ----------
        NewOrder();
        EnterCookingPhase();
        _phase = Phase.Cooking;
    }

    private void ApplyDayDoubleEnded()
    {
        // добавляем ещё столько же, сколько заработали в законченном дне
        coins += _endedDayEarned;
        UpdateMoneyUI();

        _dayDoubleClaimed = true;
        DisableDayDoubleButtonVisual();
        SaveProgress();

        // обновляем ТЕКСТ попапа (теперь уже x2)
        if (dayEndMoneyText != null)
            dayEndMoneyText.text = string.Format(L("UI", "day_end_earned_x2"), _endedDayIndex, _endedDayEarned * 2);
    }

    private void UpdateDayProgressUI()
    {
        if (dayProgressText == null) return;

        int total = Mathf.Max(1, ordersPerDay);
        int done = Mathf.Clamp(_ordersThisDay, 0, total);

        dayProgressText.text = string.Format(L("UI", "day_progress"), _dayIndex, done, total);

        // 🔔 Пульс
        if (_dayPulseRoutine != null)
            StopCoroutine(_dayPulseRoutine);

        _dayPulseRoutine = StartCoroutine(
            PulseText(dayProgressText.rectTransform)
        );
    }


    private IEnumerator PulseText(RectTransform rt, float scale = 1.15f, float duration = 0.12f)
    {
        if (rt == null) yield break;

        Vector3 start = Vector3.one;
        Vector3 peak = Vector3.one * scale;

        float t = 0f;

        // Вверх
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);
            float s = Mathf.SmoothStep(0f, 1f, k);
            rt.localScale = Vector3.Lerp(start, peak, s);
            yield return null;
        }

        t = 0f;

        // Назад
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);
            float s = Mathf.SmoothStep(0f, 1f, k);
            rt.localScale = Vector3.Lerp(peak, start, s);
            yield return null;
        }

        rt.localScale = start;
    }

    private IEnumerator AnimateDayEndPopup(bool show)
    {
        if (dayEndPopup == null) yield break;

        if (dayEndCanvasGroup == null)
            dayEndCanvasGroup = dayEndPopup.GetComponent<CanvasGroup>() ?? dayEndPopup.AddComponent<CanvasGroup>();

        if (show)
        {
            dayEndPopup.SetActive(true); // ✅ тут включаем один раз
            dayEndCanvasGroup.blocksRaycasts = true;
            dayEndCanvasGroup.interactable = true;

            dayEndCanvasGroup.alpha = 0f;
            if (dayEndCard != null) dayEndCard.localScale = Vector3.one * dayEndStartScale;

            // fade in
            float t = 0f;
            while (t < dayEndFadeSeconds)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / Mathf.Max(0.0001f, dayEndFadeSeconds));
                dayEndCanvasGroup.alpha = Mathf.SmoothStep(0f, 1f, k);
                yield return null;
            }
            dayEndCanvasGroup.alpha = 1f;

            // pop
            if (dayEndCard != null)
            {
                t = 0f;
                Vector3 a = Vector3.one * dayEndStartScale;
                Vector3 b = Vector3.one * dayEndOvershoot;

                while (t < dayEndPopSeconds)
                {
                    t += Time.unscaledDeltaTime;
                    float k = Mathf.Clamp01(t / Mathf.Max(0.0001f, dayEndPopSeconds));
                    dayEndCard.localScale = Vector3.Lerp(a, b, Mathf.SmoothStep(0f, 1f, k));
                    yield return null;
                }

                t = 0f;
                float back = dayEndPopSeconds * 0.8f;
                Vector3 c = Vector3.one;

                while (t < back)
                {
                    t += Time.unscaledDeltaTime;
                    float k = Mathf.Clamp01(t / Mathf.Max(0.0001f, back));
                    dayEndCard.localScale = Vector3.Lerp(b, c, Mathf.SmoothStep(0f, 1f, k));
                    yield return null;
                }
                dayEndCard.localScale = Vector3.one;
            }

            yield break;
        }
        else
        {
            float t = 0f;
            float sec = Mathf.Max(0.0001f, dayEndFadeSeconds);
            float startAlpha = dayEndCanvasGroup.alpha;

            Vector3 startScale = dayEndCard != null ? dayEndCard.localScale : Vector3.one;
            Vector3 endScale = Vector3.one * 0.96f;

            dayEndCanvasGroup.blocksRaycasts = false;
            dayEndCanvasGroup.interactable = false;

            while (t < sec)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / sec);
                float s = Mathf.SmoothStep(0f, 1f, k);

                dayEndCanvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, s);
                if (dayEndCard != null) dayEndCard.localScale = Vector3.Lerp(startScale, endScale, s);

                yield return null;
            }

            dayEndCanvasGroup.alpha = 0f;
            if (dayEndCard != null) dayEndCard.localScale = Vector3.one;

            dayEndPopup.SetActive(false); // ✅ тут выключаем один раз
        }
    }

    private void OpenShopFromMain()
    {
        _shopOpenedFromDayEnd = false;
        OpenShopTab(ShopTab.Main);
        if (_shopAnim != null) StopCoroutine(_shopAnim);
        _shopAnim = StartCoroutine(AnimateShop(true));
    }

    private void OpenShopFromDayEnd()
    {
        _shopOpenedFromDayEnd = true;

        OpenShopTab(ShopTab.Main);
        if (_shopAnim != null) StopCoroutine(_shopAnim);
        _shopAnim = StartCoroutine(TransitionDayEndToShop());
    }

    private void CloseShop()
    {
        if (_shopAnim != null) StopCoroutine(_shopAnim);
        _shopAnim = StartCoroutine(CloseShopRoutine());
    }
    private IEnumerator CloseShopRoutine()
    {
        if (_shopOpenedFromDayEnd)
            yield return TransitionShopToDayEnd();
        else
            yield return AnimateShop(false);

        // ✅ после закрытия магазина обновляем замочки/иконки слева
        SetFillingUI(_phase == Phase.Filling);

        // ✅ если только что купили джем в туториале — меняем заказ на "с джемом"
        if (_forceJamOrderOnce)
        {
            NewOrder(); // NewOrder сам "съест" флаг (см. ниже)
        }

        // если у тебя туториал ждёт закрытия магазина:
        if (tutorial != null)
            tutorial.OnShopClosed();
    }

    private void OpenShopTab(ShopTab tab)
    {
        _currentShopTab = tab;

        if (tabMain != null) tabMain.SetActive(tab == ShopTab.Main);
        if (tabIngredients != null) tabIngredients.SetActive(tab == ShopTab.Ingredients);
        if (tabStove != null) tabStove.SetActive(tab == ShopTab.Stove);
        if (tabDecor != null) tabDecor.SetActive(tab == ShopTab.Decor);

        if (shopBackButton != null)
            shopBackButton.gameObject.SetActive(tab != ShopTab.Main);

        if (tab == ShopTab.Ingredients && tabIngredients != null)
        {
            var fx = tabIngredients.GetComponent<UITabSlideFx>();
            if (fx != null) fx.PlayInFromRight();
        }

        if (tab == ShopTab.Decor && tabDecor != null)
        {
            var fx = tabDecor.GetComponent<UITabSlideFx>();
            if (fx != null) fx.PlayInFromRight();
        }

        if (tab == ShopTab.Stove && tabStove != null)
        {
            var fx = tabStove.GetComponent<UITabSlideFx>();
            if (fx != null) fx.PlayInFromRight();
        }

        if (tab == ShopTab.Main && tabMain != null)
        {
            var fx = tabMain.GetComponent<UITabSlideFx>();
            if (fx != null) fx.PlayInFromLeft();
        }

        if (tab == ShopTab.Stove)
        {
            if (stoveShop != null) stoveShop.Refresh();
        }

        if (tab == ShopTab.Decor)
        {
            if (decorShop != null) decorShop.Refresh();
        }

        if (shopTitleText != null)
        {
            string key = tab switch
            {
                ShopTab.Main => "shop_tab_main",
                ShopTab.Ingredients => "shop_tab_ingredients",
                ShopTab.Stove => "shop_tab_stove",
                ShopTab.Decor => "shop_tab_decor",
                _ => "shop_tab_main"
            };

            SetLocText(shopTitleText, key);
        }

        if (tab == ShopTab.Ingredients)
        {
            RefreshShopIngredientSlots();

            var def = GetDefaultIngredientSelection();
            if (def != null)
            {
                _selectedItem = def;

                foreach (var it in ingredientItems)
                    if (it != null) it.SetSelectedSilent(it == def);

                ApplyIngredientRightCard(def);
                // и правую карточку заполни напрямую, без корутины/анимации:
            }
        }

    }


    private void ApplyUnlocksForCurrentDay(out IngredientId? newlyUnlocked)
    {
        newlyUnlocked = null;

        _unlocked.Clear();

        // добавляем ТОЛЬКО купленные
        if (IsBought(ProgressService.IngredientBit.Jam)) _unlocked.Add(IngredientId.Jam);
        if (IsBought(ProgressService.IngredientBit.SourCream)) _unlocked.Add(IngredientId.SourCream);
        if (IsBought(ProgressService.IngredientBit.Chocolate)) _unlocked.Add(IngredientId.Chocolate);

        if (IsBought(ProgressService.IngredientBit.Honey)) _unlocked.Add(IngredientId.Honey);
        if (IsBought(ProgressService.IngredientBit.MapleSyrup)) _unlocked.Add(IngredientId.MapleSyrup);
        if (IsBought(ProgressService.IngredientBit.PeanutButter)) _unlocked.Add(IngredientId.PeanutButter);

        // Если выбран заблокированный — сброс
        if (_selected.HasValue && _selected.Value != IngredientId.None && !_unlocked.Contains(_selected.Value))
        {
            _selected = IngredientId.None;
            UpdateIngredientSelectionUI();
        }
    }




    private void SaveProgress()
    {
        // ✅ Пока туториал НЕ пройден — не сохраняем прогресс вообще
        if (PlayerPrefs.GetInt("tutorial_done", 0) == 0)
            return;

        if (TutorialBlocksSaving())
            return;
        _progress.coins = coins;
        _progress.dayIndex = _dayIndex;
        _progress.stoveLevel = stoveLevel;
        _progress.ordersThisDay = _ordersThisDay;
        _progress.earnedThisDay = _earnedThisDay;



        if (diamonds != null)
            _progress.diamonds = diamonds.Diamonds;   // ✅ СИНХРОН

        ProgressService.Save(_progress);
    }

    [ContextMenu("RESET PROGRESS")]
    private void ResetProgress()
    {
        ProgressService.ResetAll();
    }

    private bool IsBought(ProgressService.IngredientBit bit)
    {
        return ProgressService.HasIngredient(_progress.ingredientMask, bit);
    }

    private bool IsBoughtHoney()
    => ProgressService.HasIngredient(_progress.ingredientMask, ProgressService.IngredientBit.Honey);

    private bool IsBoughtMaple()
        => ProgressService.HasIngredient(_progress.ingredientMask, ProgressService.IngredientBit.MapleSyrup);

    private bool IsBoughtPeanut()
        => ProgressService.HasIngredient(_progress.ingredientMask, ProgressService.IngredientBit.PeanutButter);

    private void SetIngredientButtonAvailability(
    Button btn,
    IngredientId id,
    bool isFillingPhase)
    {
        if (btn == null) return;

        bool bought = _unlocked.Contains(id);

        // 🔒 замок / иконка — ТОЛЬКО от покупки
        var icon = btn.transform.Find("Icon");
        var lockObj = btn.transform.Find("Lock");

        if (icon != null) icon.gameObject.SetActive(bought);
        if (lockObj != null) lockObj.gameObject.SetActive(!bought);

        // 🖱 кликабельность — от фазы
        btn.interactable = bought && isFillingPhase;

        // 🎨 визуально приглушаем, если нельзя нажать
        var cg = btn.GetComponent<CanvasGroup>();
        if (cg == null) cg = btn.gameObject.AddComponent<CanvasGroup>();

        if (!bought)
            cg.alpha = 0.6f;          // не куплен
        else if (!isFillingPhase)
            cg.alpha = 0.8f;           // куплен, но пока нельзя
        else
            cg.alpha = 1f;             // можно использовать
    }

    private void DisableDayDoubleButtonVisual()
    {
        if (dayDoubleButton == null) return;

        dayDoubleButton.interactable = false;

        var cg = dayDoubleButton.GetComponent<CanvasGroup>();
        if (cg == null) cg = dayDoubleButton.gameObject.AddComponent<CanvasGroup>();
        cg.alpha = 0.55f;
    }

    private void OnShopIngredientSelected(ShopIngredientItem item)
    {
        if (item == null) return;

        // ✅ если уже выбран — не проигрываем анимацию и не трогаем панель
        if (_selectedItem == item)
            return;
        _selectedItem = item;

        foreach (var it in ingredientItems)
            if (it != null) it.SetSelected(it == item);

        if (_ingRightFx != null) StopCoroutine(_ingRightFx);
        _ingRightFx = StartCoroutine(RightSwapIng(() => ApplyIngredientRightCard(item)));
    }



    private bool IsIngredientBought(IngredientId id)
    {
        switch (id)
        {
            case IngredientId.Jam: return IsBought(ProgressService.IngredientBit.Jam);
            case IngredientId.SourCream: return IsBought(ProgressService.IngredientBit.SourCream);
            case IngredientId.Chocolate: return IsBought(ProgressService.IngredientBit.Chocolate);
            case IngredientId.Honey: return IsBought(ProgressService.IngredientBit.Honey);
            case IngredientId.MapleSyrup: return IsBought(ProgressService.IngredientBit.MapleSyrup);
            case IngredientId.PeanutButter: return IsBought(ProgressService.IngredientBit.PeanutButter);
            default: return false;
        }
    }




    private void RefreshShopIngredientSlots()
    {
        if (ingredientItems == null) return;

        foreach (var it in ingredientItems)
        {
            if (it == null) continue;

            bool bought = IsIngredientBought(it.ingredientId);
            bool dayOk = _dayIndex >= Mathf.Max(1, it.requiredDay);

            it.SetBought(bought);
            it.SetDayLocked(!dayOk && !bought, it.requiredDay);
        }
    }

    private void BuySelectedIngredient_DevOnly()
    {
        if (_selectedItem == null) return;

        if (IsIngredientBought(_selectedItem.ingredientId))
            return;

        // ✅ проверка дня
        int reqDay = Mathf.Max(1, _selectedItem.requiredDay);
        if (_dayIndex < reqDay)
        {
            Debug.Log($"LOCKED BY DAY: need day {reqDay}, current {_dayIndex}");
            OnShopIngredientSelected(_selectedItem); // обновим UI
            return;
        }

        // 💰 проверка денег (пока простая)
        if (!TrySpendCoins(_selectedItem.price))
        {
            Debug.Log("NOT ENOUGH MONEY");
            if (ingPriceShake != null) ingPriceShake.Play();
            OnShopIngredientSelected(_selectedItem); // чтобы справа текст обновился ("Не хватает")
            return;
        }

        if (ingRightShine != null) ingRightShine.Play();

        // 2️⃣ добавляем ингредиент в прогресс
        _progress.ingredientMask = AddIngredientToMask(
            _progress.ingredientMask,
            _selectedItem.ingredientId
        );


        // 3️⃣ сохраняем
        SaveProgress();

        // 4️⃣ обновляем unlocks в игре
        ApplyUnlocksForCurrentDay(out _);

        // 5️⃣ обновляем UI магазина
        RefreshShopIngredientSlots();
        OnShopIngredientSelected(_selectedItem);

        Debug.Log($"BOUGHT: {_selectedItem.ingredientId}");
    }

    private int AddIngredientToMask(int mask, IngredientId id)
    {
        switch (id)
        {
            case IngredientId.Jam:
                _forceJamOrderOnce = true; // ✅ после покупки следующая заявка будет с джемом

                if (tutorial != null)
                    tutorial.OnFirstIngredientBought();

                return ProgressService.AddIngredient(mask, ProgressService.IngredientBit.Jam);
            case IngredientId.SourCream:
                return ProgressService.AddIngredient(mask, ProgressService.IngredientBit.SourCream);
            case IngredientId.Chocolate:
                return ProgressService.AddIngredient(mask, ProgressService.IngredientBit.Chocolate);
            case IngredientId.Honey:
                return ProgressService.AddIngredient(mask, ProgressService.IngredientBit.Honey);
            case IngredientId.MapleSyrup:
                return ProgressService.AddIngredient(mask, ProgressService.IngredientBit.MapleSyrup);
            case IngredientId.PeanutButter:
                return ProgressService.AddIngredient(mask, ProgressService.IngredientBit.PeanutButter);
            default:
                return mask;
        }

    }

    private ShopIngredientItem GetDefaultIngredientSelection()
    {
        if (ingredientItems == null || ingredientItems.Length == 0) return null;

        foreach (var it in ingredientItems)
            if (it != null && !IsIngredientBought(it.ingredientId))
                return it;

        return ingredientItems[0];
    }

    private IEnumerator AnimateShop(bool show)
    {
        if (shopPopup == null) yield break;

        // страховки
        if (shopCanvasGroup == null) shopCanvasGroup = shopPopup.GetComponent<CanvasGroup>();
        if (shopCanvasGroup == null) shopCanvasGroup = shopPopup.AddComponent<CanvasGroup>();

        if (show)
        {
            shopPopup.SetActive(true);

            shopCanvasGroup.alpha = 0f;
            shopCanvasGroup.blocksRaycasts = false;
            shopCanvasGroup.interactable = false;

            if (shopCard != null) shopCard.localScale = Vector3.one * shopStartScale;

            // fade in
            float t = 0f;
            float sec = Mathf.Max(0.0001f, shopFadeSeconds);
            while (t < sec)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / sec);
                float s = Mathf.SmoothStep(0f, 1f, k);
                shopCanvasGroup.alpha = Mathf.Lerp(0f, 1f, s);
                yield return null;
            }
            shopCanvasGroup.alpha = 1f;

            if (tutorial != null)
                tutorial.OnShopOpened();

            // pop
            if (shopCard != null)
            {
                Vector3 a = Vector3.one * shopStartScale;
                Vector3 b = Vector3.one * shopOvershoot;

                t = 0f;
                sec = Mathf.Max(0.0001f, shopPopSeconds);
                while (t < sec)
                {
                    t += Time.unscaledDeltaTime;
                    float k = Mathf.Clamp01(t / sec);
                    shopCard.localScale = Vector3.Lerp(a, b, Mathf.SmoothStep(0f, 1f, k));
                    yield return null;
                }

                // back to 1
                t = 0f;
                float back = shopPopSeconds * 0.8f;
                Vector3 c = Vector3.one;
                while (t < back)
                {
                    t += Time.unscaledDeltaTime;
                    float k = Mathf.Clamp01(t / Mathf.Max(0.0001f, back));
                    shopCard.localScale = Vector3.Lerp(b, c, Mathf.SmoothStep(0f, 1f, k));
                    yield return null;
                }
                shopCard.localScale = Vector3.one;
            }

            shopCanvasGroup.blocksRaycasts = true;
            shopCanvasGroup.interactable = true;
        }
        else
        {
            // close anim
            shopCanvasGroup.blocksRaycasts = false;
            shopCanvasGroup.interactable = false;

            float t = 0f;
            float sec = Mathf.Max(0.0001f, shopFadeSeconds);
            float startAlpha = shopCanvasGroup.alpha;

            Vector3 startScale = shopCard != null ? shopCard.localScale : Vector3.one;
            Vector3 endScale = Vector3.one * 0.96f;

            while (t < sec)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / sec);
                float s = Mathf.SmoothStep(0f, 1f, k);

                shopCanvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, s);
                if (shopCard != null) shopCard.localScale = Vector3.Lerp(startScale, endScale, s);

                yield return null;
            }

            shopCanvasGroup.alpha = 0f;
            if (shopCard != null) shopCard.localScale = Vector3.one;

            shopPopup.SetActive(false);
            tutorial.OnShopClosed();
        }
    }

    private IEnumerator TransitionDayEndToShop()
    {
        // включаем Shop заранее, но пока невидим и без кликов
        shopPopup.SetActive(true);

        shopCanvasGroup.alpha = 0f;
        shopCanvasGroup.blocksRaycasts = false;
        shopCanvasGroup.interactable = false;
        if (shopCard != null) shopCard.localScale = Vector3.one * shopStartScale;

        // DayEnd пока виден и активен
        if (dayEndCanvasGroup != null)
        {
            dayEndCanvasGroup.blocksRaycasts = false;
            dayEndCanvasGroup.interactable = false;
        }

        float t = 0f;
        float sec = 0.18f;

        float dayStart = (dayEndCanvasGroup != null) ? dayEndCanvasGroup.alpha : 1f;

        while (t < sec)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / sec);
            float s = Mathf.SmoothStep(0f, 1f, k);

            // кроссфейд
            if (dayEndCanvasGroup != null) dayEndCanvasGroup.alpha = Mathf.Lerp(dayStart, 0f, s);
            shopCanvasGroup.alpha = Mathf.Lerp(0f, 1f, s);

            yield return null;
        }

        if (dayEndPopup != null) dayEndPopup.SetActive(false);
        if (dayEndCanvasGroup != null) dayEndCanvasGroup.alpha = 1f; // вернуть на будущее

        // pop магазина (твоя функция)
        yield return AnimateShopPopOnly(); // см. ниже

        shopCanvasGroup.blocksRaycasts = true;
        shopCanvasGroup.interactable = true;
    }

    private IEnumerator AnimateShopPopOnly()
    {
        if (shopCard == null) yield break;

        Vector3 a = Vector3.one * shopStartScale;
        Vector3 b = Vector3.one * shopOvershoot;

        float t = 0f;
        float sec = Mathf.Max(0.0001f, shopPopSeconds);

        while (t < sec)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / sec);
            shopCard.localScale = Vector3.Lerp(a, b, Mathf.SmoothStep(0f, 1f, k));
            yield return null;
        }

        t = 0f;
        float back = shopPopSeconds * 0.8f;
        while (t < back)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / Mathf.Max(0.0001f, back));
            shopCard.localScale = Vector3.Lerp(b, Vector3.one, Mathf.SmoothStep(0f, 1f, k));
            yield return null;
        }

        shopCard.localScale = Vector3.one;
    }

    private IEnumerator TransitionShopToDayEnd()
    {
        // включаем DayEnd заранее, но невидим
        if (dayEndPopup != null) dayEndPopup.SetActive(true);

        if (dayEndCanvasGroup != null)
        {
            dayEndCanvasGroup.alpha = 0f;
            dayEndCanvasGroup.blocksRaycasts = false;
            dayEndCanvasGroup.interactable = false;
        }

        // Shop выключаем клики
        shopCanvasGroup.blocksRaycasts = false;
        shopCanvasGroup.interactable = false;

        float t = 0f;
        float sec = 0.18f;
        float shopStart = shopCanvasGroup.alpha;

        while (t < sec)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / sec);
            float s = Mathf.SmoothStep(0f, 1f, k);

            shopCanvasGroup.alpha = Mathf.Lerp(shopStart, 0f, s);
            if (dayEndCanvasGroup != null) dayEndCanvasGroup.alpha = Mathf.Lerp(0f, 1f, s);

            yield return null;
        }

        shopCanvasGroup.alpha = 0f;
        shopPopup.SetActive(false);

        if (dayEndCanvasGroup != null)
        {
            dayEndCanvasGroup.alpha = 1f;
            dayEndCanvasGroup.blocksRaycasts = true;
            dayEndCanvasGroup.interactable = true;
        }
    }

    public int GetCoins() => coins;
    public int GetStoveLevel() => stoveLevel;
    public int GetDayIndex() => _dayIndex;

    public bool TrySpendCoins(int amount)
    {
        if (amount <= 0) return true;

        if (coins < amount)
        {
            // ❌ НИКАКОГО звука
            return false;
        }

        coins -= amount;
        UpdateMoneyUI();

        PlayUIBuySuccess(); // ✅ ТОЛЬКО успех
        return true;
    }

    public void SetStoveLevel(int level)
    {
        stoveLevel = Mathf.Max(1, level);
    }
    public void SaveProgressPublic()
    {
        SaveProgress();
    }

    public void ApplyStoveToCooking()
    {
        if (cooking != null)
            cooking.ApplyStoveLevel(stoveLevel);
    }

    public int GetDecorId() => _progress.decorId; // или твое поле decorId, если ты его держишь отдельно

    public bool IsDecorOwned(int id)
    {
        int mask = _progress.decorMask;
        return (mask & (1 << id)) != 0;
    }

    public void AddOwnedDecor(int id)
    {
        _progress.decorMask |= (1 << id);
    }

    public void SetDecorId(int id)
    {
        _progress.decorId = id;
    }
    public void ApplyDecorToScene(DecorShopController.DecorSetDef[] defs)
    {
        int activeId = _progress.decorId;

        foreach (var d in defs)
        {
            if (d == null || d.sceneSetRoot == null) continue;
            d.sceneSetRoot.SetActive(d.id == activeId);
        }
    }

    public bool IsDecorActive(int id)
    {
        return (_progress.activeDecorMask & (1 << id)) != 0;
    }

    public void SetDecorActive(int id, bool active)
    {
        if (active) _progress.activeDecorMask |= (1 << id);
        else _progress.activeDecorMask &= ~(1 << id);
    }

    public void ApplyDecorToSceneMulti(DecorShopController.DecorSetDef[] defs)
    {
        foreach (var d in defs)
        {
            if (d == null || d.sceneSetRoot == null) continue;
            bool on = (_progress.activeDecorMask & (1 << d.id)) != 0;
            d.sceneSetRoot.SetActive(on);
        }
    }

    public float GetDecorIncomeMultiplier(DecorShopController.DecorSetDef[] defs)
    {
        float bonus = 0f;

        foreach (var d in defs)
        {
            if (IsDecorOwned(d.id))
                bonus += d.incomeBonus;
        }

        return 1f + bonus;
    }

    private bool IsIncomeX2Active()
    {
        return DateTime.UtcNow.Ticks < _progress.incomeX2UntilUtcTicks;
    }

    private void ActivateIncomeX2Minutes(int minutes)
    {
        long now = DateTime.UtcNow.Ticks;
        long add = TimeSpan.FromMinutes(minutes).Ticks;

        long baseTicks = Math.Max(now, _progress.incomeX2UntilUtcTicks);
        _progress.incomeX2UntilUtcTicks = baseTicks + add;

        ProgressService.Save(_progress);
        UpdateIncomeX2UI();
    }

    private void UpdateIncomeX2UI()
    {
        if (incomeX2ButtonText == null) return;

        bool incomeActive = IsIncomeX2Active();

        // ВАЖНО: всегда обновляем лок (и когда активен, и когда НЕ активен)
        incomeX2Binder?.SetGameplayLock(incomeActive);

        if (!incomeActive)
        {
            SetLocText(incomeX2ButtonText, "income_x2_cooldown_5m");
            return;
        }

        TimeSpan left = new TimeSpan(_progress.incomeX2UntilUtcTicks - DateTime.UtcNow.Ticks);
        int mm = Mathf.Max(0, (int)left.TotalMinutes);
        int ss = Mathf.Max(0, left.Seconds);

        incomeX2ButtonText.text = $"{mm:00}:{ss:00}";
    }

    private int ApplyIncomeBoost(int value)
    {
        if (!IsIncomeX2Active()) return value;
        return value * 2;
    }

    private void OnNewPancakeClicked()
    {
        if (actionButton != null)
            actionButton.gameObject.SetActive(true);

        if (burnedPopup != null) burnedPopupAnimator.Close();
        SetSkipBlockedByBurnedPopup(false);

        // просто начать заново
        cooking.ResetForNewPancake();
        EnterCookingPhase();
        _phase = Phase.Cooking;
        RefreshSkipOrderButtonFx(force: true);
    }

    private void OnSavePancakeClicked()
    {
        rewardedAds.Show("save_pancake", () =>
        {
            SavePancake_ApplyRescue();
        });
    }


    private void SavePancake_ApplyRescue()
    {
        // вернуть деньги/комбо (которые были до штрафа)
        int beforeRescueCoins = coins;   // тут coins = уже после штраф
        coins = _coinsBeforeBurn;
        int gainedCoins = coins - beforeRescueCoins;   // X
        string msg = string.Format(L("UI", "rescue_gain_coins"), gainedCoins);
        StartCoroutine(ShowResultPopup(msg, "", 0.6f));
        sfxSource.PlayOneShot(goodClip);
        combo = _comboBeforeBurn;
        UpdateMoneyUI();
        UpdateComboUI();

        _earnedThisDay = _earnedBeforeBurn;
        UpdateDayProgressUI();
        SaveProgress();

        // закрыть попап
        if (burnedPopup != null)
            burnedPopupAnimator.Close();
        SetSkipBlockedByBurnedPopup(false);
        // вернуть главную кнопку
        if (actionButton != null)
            actionButton.gameObject.SetActive(true);

        // вернуть блин в состояние "готов, можно начинку"
        if (cooking != null)
            cooking.RestoreCookedPancake();

        EnterFillingPhase();
        RefreshSkipOrderButtonFx(force: true);
    }



    private void OnIncomeX2Clicked()
    {
        Debug.Log("[UI] OnIncomeX2Clicked fired");
        if (IsIncomeX2Active())
        {
            Debug.Log("[UI] IsIncomeX2Active=" + IsIncomeX2Active());
            return; // ⛔ защита
        }

        rewardedAds.Show("income_x2_5min", () =>
        {
            ActivateIncomeX2Minutes(5);
        });
    }

    private bool _incomeWasActive;
    private float _incomeUiTick;

    private void Update()
    {
        // ✅ 1) SkipOrder UI — всегда
        _skipUiTick += Time.deltaTime;
        if (_skipUiTick >= 0.25f)
        {
            _skipUiTick = 0f;
            RefreshSkipOrderButtonFx(force: false);
        }

        if (burnedPopup != null)
        {
            bool burnedActive = burnedPopup.activeSelf;

            if (burnedActive != _burnedPopupWasActive)
            {
                _burnedPopupWasActive = burnedActive;
                SetSkipBlockedByBurnedPopup(burnedActive);
            }
        }


        // ✅ 2) IncomeX2 UI — как было
        bool incomeActive = IsIncomeX2Active();

        if (_incomeWasActive && !incomeActive)
        {
            _incomeWasActive = false;
            _incomeUiTick = 0f;
            UpdateIncomeX2UI();
        }

        _incomeWasActive = incomeActive;
        if (!incomeActive) return;

        _incomeUiTick += Time.deltaTime;
        if (_incomeUiTick >= 0.25f)
        {
            _incomeUiTick = 0f;
            UpdateIncomeX2UI();
        }
    }


    private void OnSavePancakeForDiamondClicked()
    {
        if (diamonds == null) return;

        if (!diamonds.TrySpend(1))
        {
            Debug.Log("[DIAMONDS] Not enough diamonds");
            return;
        }

        // ТУТ вызывай ту же логику, что и после rewarded-спасения:
        // например: CloseBurnedPopup(); RestorePancake(); EnterCookingPhase(); и т.п.
        SavePancake_ApplyRescue();
    }

    private void UpdateDayEndText()
    {
        bool perfectDay = _dayAllPerfect && !_dayHadBurn;

    }

    private void ApplyDayDouble()
    {
        // удваиваем заработок дня: добавляем ещё столько же
        coins += _earnedThisDay;
        _earnedThisDay *= 2;

        UpdateMoneyUI();
        DisableDayDoubleButtonVisual();
        SaveProgress();
    }


    private void OnSkipOrderForDiamondClicked()
    {
        if (diamonds == null) return;

        // нельзя во время анимаций
        if (_phase == Phase.Animating) return;

        // нельзя, если открыт попап сгоревшего блина (иначе можно случайно слить 💎)
        if (burnedPopup != null && burnedPopup.activeSelf) return;

        // ✅ Разрешено и в Cooking, и в Filling (как ты и хотел)

        if (!diamonds.TrySpend(skipOrderDiamondCost))
        {
            Debug.Log("[DIAMONDS] Not enough diamonds to skip order");
            return;
        }

        RefreshSkipOrderButtonFx(force: true);

        SkipOrder_Apply();
    }


    private void SkipOrder_Apply()
    {
        // пропуск = не идеальный исход
        _dayAllPerfect = false;
        ResetCombo();

        // ✅ ВАЖНО: остановить текущую готовку + звук
        if (cooking != null)
            cooking.ResetForNewPancake();   // должен сбросить таймеры/состояние готовки

        // на всякий случай возвращаем UI в режим готовки
        NewOrder();
        ClearAllToppingOverlays();
        ResetPancakeVisual();
        cooking.StopCookingSfx();
        EnterCookingPhase();
        _phase = Phase.Cooking;

        // засчитываем как “заказ сделан”
        _ordersThisDay++;
        UpdateDayProgressUI();

        // если день закончился — показываем DayEnd как обычно
        if (_ordersThisDay >= ordersPerDay)
        {
            StartCoroutine(ShowDayEndAndWait());
            return;
        }

        // иначе — новый заказ
    }
    private void InitSkipOrderButtonFx()
    {
        if (skipOrderForDiamondButton == null) return;

        if (skipOrderCanvasGroup == null)
            skipOrderCanvasGroup = skipOrderForDiamondButton.GetComponent<CanvasGroup>();

        if (skipOrderCanvasGroup == null)
            skipOrderCanvasGroup = skipOrderForDiamondButton.gameObject.AddComponent<CanvasGroup>();

        // ⛔ ЖЁСТКО скрываем на старте
        skipOrderCanvasGroup.alpha = 0f;
        skipOrderCanvasGroup.interactable = false;
        skipOrderCanvasGroup.blocksRaycasts = false;

        skipOrderForDiamondButton.interactable = false;
        skipOrderForDiamondButton.gameObject.SetActive(true);

        // стартовое состояние
        _skipVisible = false;
    }

    private void RefreshSkipOrderButtonFx(bool force)
    {

        if (skipOrderForDiamondButton == null) return;
        if (diamonds == null) return;


        bool popupActive = (burnedPopup != null && burnedPopup.activeSelf);
        bool shouldShow = diamonds.Diamonds >= skipOrderDiamondCost
                          && !_skipBlockedByBurnedPopup
                          && !popupActive;
        if (!force && shouldShow == _skipVisible) return;

        _skipVisible = shouldShow;

        if (_skipAnim != null) StopCoroutine(_skipAnim);
        _skipAnim = StartCoroutine(AnimateSkipOrderButton(shouldShow));
    }

    private IEnumerator AnimateSkipOrderButton(bool show)
    {
        if (skipOrderForDiamondButton == null) yield break;
        if (skipOrderCanvasGroup == null) yield break;

        var tr = skipOrderForDiamondButton.transform;
        float dur = Mathf.Max(0.05f, skipShowHideDuration);

        // если показываем — сразу включаем объект и клики (но клики дадим только в конце анимации)
        skipOrderForDiamondButton.gameObject.SetActive(true);

        // блокируем клики во время анимации
        skipOrderCanvasGroup.blocksRaycasts = false;
        skipOrderCanvasGroup.interactable = false;

        float a0 = skipOrderCanvasGroup.alpha;
        float a1 = show ? 1f : 0f;

        Vector3 s0 = tr.localScale;
        Vector3 s1 = show ? Vector3.one : Vector3.one * skipHiddenScale;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / dur;
            float k = Mathf.SmoothStep(0f, 1f, t);

            skipOrderCanvasGroup.alpha = Mathf.Lerp(a0, a1, k);
            tr.localScale = Vector3.Lerp(s0, s1, k);

            yield return null;
        }

        skipOrderCanvasGroup.alpha = a1;
        tr.localScale = s1;

        if (show)
        {
            // теперь можно кликать
            skipOrderCanvasGroup.blocksRaycasts = true;
            skipOrderCanvasGroup.interactable = true;
            skipOrderForDiamondButton.interactable = true;
        }
        else
        {
            // полностью убрали
            skipOrderForDiamondButton.interactable = false;
            skipOrderForDiamondButton.gameObject.SetActive(false);
        }
    }
    private bool _skipForcedHidden = false;


    private void SetSkipBlockedByBurnedPopup(bool blocked)
    {
        _skipBlockedByBurnedPopup = blocked;
        RefreshSkipOrderButtonFx(force: true);
    }


    private IEnumerator RightSwapIng(System.Action applyData)
    {
        // если FX не задан — просто применяем данные
        if (ingRightCardCg == null || ingRightCardRect == null)
        {
            applyData?.Invoke();
            yield break;
        }

        float sec = Mathf.Max(0.0001f, ingRightFade);

        Vector2 basePos = ingRightCardRect.anchoredPosition;
        Vector2 outPos = basePos + Vector2.right * ingRightSlidePx;
        Vector2 inPos = basePos - Vector2.right * ingRightSlidePx;

        // fade out + slide out
        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / sec;
            float s = Mathf.SmoothStep(0f, 1f, t);
            ingRightCardCg.alpha = 1f - s;
            ingRightCardRect.anchoredPosition = Vector2.Lerp(basePos, outPos, s);
            yield return null;
        }

        ingRightCardCg.alpha = 0f;

        // применяем новые данные "в темноте"
        ingRightCardRect.anchoredPosition = inPos;
        applyData?.Invoke();

        // fade in + slide in
        t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / sec;
            float s = Mathf.SmoothStep(0f, 1f, t);
            ingRightCardCg.alpha = s;
            ingRightCardRect.anchoredPosition = Vector2.Lerp(inPos, basePos, s);
            yield return null;
        }

        ingRightCardCg.alpha = 1f;
        ingRightCardRect.anchoredPosition = basePos;
    }

    private void ApplyIngredientRightCard(ShopIngredientItem item)
    {
        if (item == null) return;

        bool bought = IsIngredientBought(item.ingredientId);
        bool dayOk = _dayIndex >= Mathf.Max(1, item.requiredDay);
        bool enoughMoney = coins >= item.price;

        // Название ингредиента (локализованное)
        if (ingNameText != null)
            SetLocText(ingNameText, "Ingredients", IngredientToKey(item.ingredientId));

        // Цена всегда числом
        if (ingPriceText != null)
            ingPriceText.text = item.price.ToString();

        if (bought)
        {
            if (ingBuyButton != null) ingBuyButton.interactable = false;
            if (ingBuyButtonText != null) SetLocText(ingBuyButtonText, "shop_bought");
            return;
        }

        if (!dayOk)
        {
            // Доступно с дня {0}
            if (ingNameText != null)
                SetLocTextFormat(ingNameText, "UI", "shop_available_from_day", item.requiredDay);

            if (ingBuyButton != null) ingBuyButton.interactable = false;
            if (ingBuyButtonText != null) SetLocText(ingBuyButtonText, "shop_closed");
            return;
        }

        if (ingBuyButton != null) ingBuyButton.interactable = enoughMoney;

        if (ingBuyButtonText != null)
            SetLocText(ingBuyButtonText, enoughMoney ? "shop_buy" : "shop_not_enough");
    }

    private void PlayUIBuySuccess()
    {
        if (uiSfxSource != null && uiBuySuccessClip != null)
            uiSfxSource.PlayOneShot(uiBuySuccessClip);
    }

    private bool OrdersEqual(Order a, Order b)
    {
        if (a == null || b == null) return false;
        if (a.items.Count != b.items.Count) return false;

        for (int i = 0; i < a.items.Count; i++)
        {
            if (a.items[i].id != b.items[i].id) return false;
            if (a.items[i].count != b.items[i].count) return false;
        }
        return true;
    }

    private void OnApplicationPause(bool pause)
    {
        if (pause && !TutorialBlocksSaving())
            SaveProgress();
    }

    private void OnApplicationQuit()
    {
        if (!TutorialBlocksSaving())
            SaveProgress();
    }
    private bool TutorialBlocksSaving()
    {
        // пока туториал не пройден — не сохраняем
        if (PlayerPrefs.GetInt("tutorial_done", 0) == 0)
            return true;

        return false;
    }

    private void SetLocText(TMP_Text label, string key)
    {
        SetLocText(label, "UI", key);
    }

    private void SetLocText(TMP_Text label, string table, string key)
    {
        if (label == null) return;
        StartCoroutine(SetLocTextCo(label, table, key));
    }

    private IEnumerator SetLocTextCo(TMP_Text label, string table, string key)
    {
        yield return LocalizationSettings.InitializationOperation;

        var ls = new LocalizedString(table, key);
        var handle = ls.GetLocalizedStringAsync();
        yield return handle;

        label.text = handle.Result;
    }

    private void SetLocTextFormat(TMP_Text label, string key, params object[] args)
    {
        SetLocTextFormat(label, "UI", key, args);
    }

    private void SetLocTextFormat(TMP_Text label, string table, string key, params object[] args)
    {
        if (label == null) return;
        StartCoroutine(SetLocTextFormatCo(label, table, key, args));
    }

    private IEnumerator SetLocTextFormatCo(TMP_Text label, string table, string key, object[] args)
    {
        yield return LocalizationSettings.InitializationOperation;

        var ls = new LocalizedString(table, key);
        var handle = ls.GetLocalizedStringAsync();
        yield return handle;

        label.text = string.Format(handle.Result, args);
    }

    private string L(string table, string key)
    {
        return LocalizationSettings.StringDatabase.GetLocalizedString(table, key);
    }

    private void HandleLocaleChanged(Locale _)
    {
        var def = GetDefaultIngredientSelection();
        UpdateOrderText(); // пересобираем строку "Заказ" + имена
        ApplyIngredientRightCard(def);
        UpdateDayProgressUI();
        UpdateComboUI();
        UpdateMoneyUI();
        UpdateIncomeX2UI();

        // ВАЖНО: если сейчас готовка — НЕ трогаем кнопку действия (ею рулит CookingController)
        if (cooking != null && cooking.OwnsActionButton)
            return;

        // обновить главный action button
        if (!string.IsNullOrEmpty(_actionButtonKey))
            SetActionButtonKey(_actionButtonKey);
    }

    private void SetLocTextInstant(TMP_Text label, string key)
    {
        SetLocTextInstant(label, "UI", key);
    }

    private void SetLocTextInstant(TMP_Text label, string table, string key)
    {
        if (label == null) return;
        label.text = LocalizationSettings.StringDatabase.GetLocalizedString(table, key);
    }

    private void SetActionButtonKey(string key)
    {
        _actionButtonKey = key;

        if (actionButtonText != null)
            SetLocTextInstant(actionButtonText, "UI", key);
    }

}
