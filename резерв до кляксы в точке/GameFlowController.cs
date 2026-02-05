using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameFlowController : MonoBehaviour
{
    public enum IngredientId
    {
        None,
        Jam,
        SourCream,
        Chocolate,
        Honey,
        MapleSyrup,
        PeanutButter
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


    [Header("Result Popup")]
    [SerializeField] private GameObject resultPopup;
    [SerializeField] private TMP_Text resultText;
    [SerializeField] private TMP_Text rewardText; // можно null

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

    private Phase _phase = Phase.Cooking;

    private Order _order;
    private Dictionary<IngredientId, int> _added = new();
    private IngredientId? _selected = null;

    private Vector2 _pancakeStartPos;
    private Vector3 _pancakeStartScale;

    private void Awake()
    {
        _pancakeStartPos = pancakeRect.anchoredPosition;
        _pancakeStartScale = pancakeRect.localScale;

        // ингредиенты
        jamButton.onClick.AddListener(() => SelectIngredient(IngredientId.Jam));
        sourCreamButton.onClick.AddListener(() => SelectIngredient(IngredientId.SourCream));
        chocolateButton.onClick.AddListener(() => SelectIngredient(IngredientId.Chocolate));
        honeyButton.onClick.AddListener(() => SelectIngredient(IngredientId.Honey));
        mapleSyrupButton.onClick.AddListener(() => SelectIngredient(IngredientId.MapleSyrup));
        peanutButterButton.onClick.AddListener(() => SelectIngredient(IngredientId.PeanutButter));


        // клик по блину (добавление порции)
        pancakeButton.onClick.AddListener(OnPancakeClicked);

        // ActionButton всегда слушаем, но поведение зависит от фазы
        actionButton.onClick.AddListener(OnActionButtonClicked);
        ClearAllToppingOverlays();

        SetFillingUI(false);
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
        NewOrder();
        EnterCookingPhase();
        UpdateComboUI();
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

        if (Random.value < noFillingChance)
            return o; // без начинки

        // 1–2 ингредиента, порции 1–3
        var pool = new List<IngredientId>
{
    IngredientId.Jam,
    IngredientId.SourCream,
    IngredientId.Chocolate,
    IngredientId.Honey,
    IngredientId.MapleSyrup,
    IngredientId.PeanutButter
};

        int count = Random.Range(1, 3);

        for (int i = 0; i < count; i++)
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
        return s == Score.Perfect ? "✅ ИДЕАЛЬНО!" :
               s == Score.Good ? "🟡 НОРМ!" :
               "❌ ПЛОХО!";
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
        if (score == Score.Perfect || score == Score.Good)
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
        // БАЗОВАЯ логика оценки:
        // Perfect: совпало полностью
        // Good: есть ошибки, но "похоже" (не критично)
        // Bad: сильно не совпало
        //
        // Ты хотел: можно ошибиться — тогда просто меньше оценка/награда

        int requiredTotal = 0;
        foreach (var it in _order.items) requiredTotal += it.count;

        int addedTotal = 0;
        foreach (var kv in _added) addedTotal += kv.Value;

        // Проверяем точное совпадение
        bool exact = CheckExactMatch();

        // Награды (пока простые цифры — потом отбалансируем)
        if (_order.items.Count == 0)
        {
            // заказ без начинки: если не добавил — идеально
            if (_added.Count == 0)
            {
                reward = 20;
                return Score.Perfect;
            }
            // добавил лишнее — плохо
            reward = 5;
            return Score.Bad;
        }

        if (exact)
        {
            reward = 30 + requiredTotal * 5;
            return Score.Perfect;
        }

        // "Good": если добавил хоть что-то из нужного и не сделал дикого перебора
        int matched = CountMatchedPortions();
        bool notTooMuch = addedTotal <= requiredTotal + 1; // мягко
        if (matched > 0 && notTooMuch)
        {
            reward = 15 + matched * 4;
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
        s == Score.Perfect ? "✅ ИДЕАЛЬНО" :
        s == Score.Good ? "🟡 НОРМ" :
        "❌ ПЛОХО";

    private void UpdateOrderText()
    {
        var sb = new StringBuilder();

        //if (_phase == Phase.Cooking)
        //    sb.Append("Готовка ");
        //else if (_phase == Phase.Filling)
        //    sb.Append("Начинка: выбери ингредиент и нажимай на блин. ");

        if (_order.items.Count == 0)
        {
            sb.Append("\nЗаказ: без начинки");
        }
        else
        {
            sb.Append("\nЗаказ: ");
            for (int i = 0; i < _order.items.Count; i++)
            {
                var it = _order.items[i];
                sb.Append($"{it.id} x{it.count}");
                if (i < _order.items.Count - 1) sb.Append(", ");
            }
        }

        sb.Append("\nДобавлено: ");
        if (_added.Count == 0) sb.Append("ничего");
        else
        {
            bool first = true;
            foreach (var kv in _added)
            {
                if (!first) sb.Append(", ");
                sb.Append($"{kv.Key} x{kv.Value}");
                first = false;
            }
        }

        if (_phase == Phase.Filling)
        {
            sb.Append("\nВыбрано: ");
            sb.Append(_selected == null ? "—" : _selected.ToString());
        }

        orderText.text = sb.ToString();
    }

    private void SetFillingUI(bool enabled)
    {
        SetIngredientButtonState(jamButton, enabled);
        SetIngredientButtonState(sourCreamButton, enabled);
        SetIngredientButtonState(chocolateButton, enabled);

        SetIngredientButtonState(honeyButton, enabled);
        SetIngredientButtonState(mapleSyrupButton, enabled);
        SetIngredientButtonState(peanutButterButton, enabled);

        if (!enabled)
        {
            // чтобы игрок не оставлял “выбранную” начинку до готовки
            _selected = IngredientId.None; // или IngredientId.None если добавишь None
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
        var img = btn.GetComponent<Image>();
        if (img == null) return;
        img.color = selected ? new Color(1f, 0.9f, 0.6f) : Color.white;
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
    }



}
