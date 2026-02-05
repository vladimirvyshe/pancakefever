using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class GameFlowController : MonoBehaviour
{

    private string IngredientToText(IngredientId id)
    {
        switch (id)
        {
            case IngredientId.Jam: return "Джем";
            case IngredientId.SourCream: return "Крем";
            case IngredientId.Chocolate: return "Шоколад";
            case IngredientId.Honey: return "Мёд";
            case IngredientId.MapleSyrup: return "Кленовый сироп";
            case IngredientId.PeanutButter: return "Арахисовая паста";
            default: return "—";
        }
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

    [Header("StoveShop")]
    [SerializeField] private StoveShopController stoveShop;

    [Header("DecorShop")]
    [SerializeField] private DecorShopController decorShop;

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

    private int _dayIndex = 1;
    private int _ordersThisDay = 0;
    private int _earnedThisDay = 0;

    private bool _dayChoiceMade = false;
    private bool _dayDoubleChosen = false;
    private bool _dayDoubleClaimed = false;
    private bool _shopOpenedFromDayEnd = false;

    // unlocks / upgrades
    private readonly HashSet<IngredientId> _unlocked = new();
    [SerializeField] private int stoveLevel = 1; // пока просто число






    private readonly Dictionary<Button, Coroutine> _btnAnim = new();

    private Coroutine _dayPulseRoutine;


    private void Awake()
    {

        _progress = ProgressService.Load();
        coins = _progress.coins;
        _dayIndex = _progress.dayIndex;
        stoveLevel = _progress.stoveLevel;

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
            decorShop.Init(this);



        if (cooking != null)
            cooking.ApplyStoveLevel(stoveLevel);

        OpenShopTab(ShopTab.Main);

    }

    private void OnEnable()
    {
        cooking.Cooked += OnCooked;
        cooking.Burned += OnBurned;
    }

    private void OnDisable()
    {
        cooking.Cooked -= OnCooked;
        cooking.Burned -= OnBurned;
    }

    private void Start()
    {
        UpdateComboUI();
        UpdateDayProgressUI();
        UpdateMoneyUI();
        ApplyUnlocksForCurrentDay(out _);
        NewOrder();
        EnterCookingPhase();
    }

    private void NewOrder()
    {
        ClearAllToppingOverlays();
        _order = GenerateOrder();
        _added.Clear();
        _selected = null;
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
        int count = Random.Range(1, 3);

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

    private void OnCooked()
    {
        // СРАЗУ после приготовления: включаем начинку и меняем кнопку на "Упаковать"
        EnterFillingPhase();
    }

    private void OnBurned()
    {
        ClearAllToppingOverlays();
        // Пока просто: новый заказ
        orderText.text = "Блин сгорел ❌ Новый заказ...";
        // позже добавим штраф/спасти рекламой
        //StartCoroutine(RestartAfterDelay(0.6f));
        ResetCombo();
    }

     //IEnumerator

    private IEnumerator RestartAfterDelay(float sec)
    {
        _phase = Phase.Animating;
        yield return new WaitForSeconds(sec);

        ClearAllToppingOverlays();

        ResetPancakeVisual();
        NewOrder();
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
        return s == Score.Perfect ? "ИДЕАЛЬНО!" :
               s == Score.Good ? "ХОРОШО" :
               "ПЛОХО!";
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

        actionButtonText.text = "Упаковать";
        StartCoroutine(PulseButton(actionButton.transform));
        UpdateOrderText();
    }

    private void OnActionButtonClicked()
    {
        // 1) Фаза начинки / упаковки
        if (_phase == Phase.Filling)
        {
            StartCoroutine(PackServeReturn());
            return;
        }

        // 2) Фаза готовки
        if (_phase == Phase.Cooking)
        {
            var prev = cooking.CurrentState;

            // сначала отдаём клик контроллеру готовки
            cooking.HandleActionButton();

            // 1) Если было Burned и нажали "Новый блин" → вернулись в WaitingPour
            // значит блин НЕ показываем, а прячем и ждём "Залить тесто"
            if (prev == CookingController.CookState.Burned &&
                cooking.CurrentState == CookingController.CookState.WaitingPour)
            {
                NewOrder();
                HidePancake();
                return;
            }

            // 2) Если было WaitingPour и после клика началась готовка
            // значит это было "Залить тесто" → показываем новый блин
            if (prev == CookingController.CookState.WaitingPour &&
                cooking.CurrentState != CookingController.CookState.WaitingPour)
            {
                ShowFreshPancake();
            }
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

        coins += finalReward;
        UpdateMoneyUI();
        _ordersThisDay++;
        _earnedThisDay += finalReward;
        UpdateDayProgressUI();

        if (sfxSource != null)
        {
            var clip = score == Score.Perfect ? perfectClip :
                       score == Score.Good ? goodClip :
                       badClip;

            if (clip != null) sfxSource.PlayOneShot(clip);
        }

        ApplyScoreStyle(score);

        string title = ScoreTitle(score);
        string rewardLine = $"+{finalReward} монет";

        StartCoroutine(ShowResultPopup(title, rewardLine, 0.6f));

        // orderText можно оставить для отладки или укоротить:
        orderText.text = $"Новый заказ...";
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

            reward = 30 + requiredTotalExact * 5;
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
            reward = 10 + Mathf.RoundToInt((30 + requiredTotal * 4) * accuracy) - extra * 2;
            reward = Mathf.Max(5, reward);
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
        s == Score.Good ? "ХОРОШО" :
        "ПЛОХО";

    private void UpdateOrderText()
    {
        var sb = new StringBuilder();

        if (_order.items.Count == 0)
        {
            sb.Append("Заказ: без начинки");
        }
        else
        {
            sb.Append("Заказ:\n");
            for (int i = 0; i < _order.items.Count; i++)
            {
                var it = _order.items[i];
                sb.Append($"{IngredientToText(it.id)} ×{it.count}");
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
            comboText.text = $"x{combo:0.0}";
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
        // (позже тут будет реклама)
        if (_dayDoubleClaimed) return;

        _dayDoubleClaimed = true;

        // начисляем сразу (как заглушка)
        coins += _earnedThisDay;
        UpdateMoneyUI();

        // обновляем текст на карточке
        if (dayEndMoneyText != null)
        {
            int doubled = _earnedThisDay * 2;
            dayEndMoneyText.text = $"День {_dayIndex}\nЗаработано: {doubled}";
        }

        // делаем кнопку неактивной и приглушенной
        DisableDayDoubleButtonVisual();
    }

    private void OnDayContinueClicked()
    {
        _dayDoubleChosen = false;
        _dayChoiceMade = true;
    }

    private IEnumerator ShowDayEndAndWait()
    {
        _dayDoubleClaimed = false;

        // вернуть кнопку в активное состояние на новом дне
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

        if (dayEndMoneyText != null)
            dayEndMoneyText.text = $"День {_dayIndex}\nЗаработано: {_earnedThisDay}";

        if (dayEndPopup != null)
            yield return AnimateDayEndPopup(true);

        while (!_dayChoiceMade)
            yield return null;

        if (dayEndPopup != null)
            yield return AnimateDayEndPopup(false);

        // следующий день
        _dayIndex++;
        _ordersThisDay = 0;
        _earnedThisDay = 0;
        SaveProgress();
        UpdateDayProgressUI();

        _phase = Phase.Cooking; // вернём обратно в конце PackServeReturn тоже, но пусть будет
    }

    private void UpdateDayProgressUI()
    {
        if (dayProgressText == null) return;

        int total = Mathf.Max(1, ordersPerDay);
        int done = Mathf.Clamp(_ordersThisDay, 0, total);

        dayProgressText.text = $"День {_dayIndex} • {done}/{total}";

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
            shopTitleText.text = tab switch
            {
                ShopTab.Main => "Магазин",
                ShopTab.Ingredients => "Ингредиенты",
                ShopTab.Stove => "Плита",
                ShopTab.Decor => "Декор",
                _ => "Магазин"
            };
        }

        if (tab == ShopTab.Ingredients)
        {
            RefreshShopIngredientSlots();

            var def = GetDefaultIngredientSelection();
            if (def != null) OnShopIngredientSelected(def);
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
        _progress.coins = coins;
        _progress.dayIndex = _dayIndex;
        _progress.stoveLevel = stoveLevel;
        // _progress.decorId = ... (позже)
        // _progress.ingredientMask = ... (мы будем менять при покупках)

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
        _selectedItem = item;

        foreach (var it in ingredientItems)
            if (it != null)
                it.SetSelected(it == item);

        bool bought = IsIngredientBought(item.ingredientId);
        bool dayOk = _dayIndex >= Mathf.Max(1, item.requiredDay);
        bool enoughMoney = coins >= item.price;

        if (ingNameText != null)
            ingNameText.text = item.title;

        if (bought)
        {
            // ✅ оставляем цену, как ты хочешь
            if (ingPriceText != null)
                ingPriceText.text = item.price.ToString();

            if (ingBuyButton != null)
                ingBuyButton.interactable = false;

            if (ingBuyButtonText != null)
                ingBuyButtonText.text = "Куплено";

            return;
        }

        // Если день ещё не достигнут — показываем блокировку по дню
        if (!dayOk)
        {
            if (ingNameText != null)
                ingNameText.text = $"Доступно с дня {item.requiredDay}";

            if (ingPriceText != null)
                ingPriceText.text = item.price.ToString();

            ingBuyButton.interactable = false;
            ingBuyButtonText.text = "Закрыто";
            return;
        }

        // День ок — проверяем деньги
        ingPriceText.text = item.price.ToString();
        ingBuyButton.interactable = enoughMoney;
        ingBuyButtonText.text = enoughMoney ? "Купить" : "Не хватает";
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
        if (coins < _selectedItem.price)
        {
            Debug.Log("NOT ENOUGH MONEY");
            return;
        }

        // 1️⃣ списываем деньги
        coins -= _selectedItem.price;
        UpdateMoneyUI();

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
        if (coins < amount) return false;
        coins -= amount;
        UpdateMoneyUI();
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
}
