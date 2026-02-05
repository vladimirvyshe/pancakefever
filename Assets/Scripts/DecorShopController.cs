using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DecorShopController : MonoBehaviour
{

    [SerializeField] private UIShineFx rightShine;

    [System.Serializable]
    public class DecorSetDef
    {
        public int id;
        public string title;
        public int price;
        public int requiredDay = 1;
        public Sprite icon;
        public GameObject sceneSetRoot;

        [Range(0f, 0.2f)]
        public float incomeBonus; // 0.02 = +2%
    }

    [Header("Data")]
    [SerializeField] private DecorSetDef[] sets;

    [Header("UI - right card")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text priceText;
    [SerializeField] private TMP_Text dayText;
    [SerializeField] private Button actionButton;
    [SerializeField] private TMP_Text actionButtonText;
    [SerializeField] private TMPro.TMP_Text buffText;      // справа над ценой
    [SerializeField] private TMPro.TMP_Text totalBuffText; // сверху суммарный
    [SerializeField] private CanvasGroup rightCardCanvasGroup;
    [SerializeField] private RectTransform rightCardRect;
    [SerializeField] private float rightCardFade = 0.10f;
    [SerializeField] private float rightCardSlidePx = 10f;

    private Coroutine _rightCardFx;



    [Header("UI - left list")]
    [SerializeField] private Transform listRoot;              // Content
    [SerializeField] private DecorSetListItem listItemPrefab;

    private GameFlowController _game;
    private int _selectedId = 0;

    public void Init(GameFlowController game)
    {
        _game = game;

        // Кнопку подключаем один раз
        if (actionButton != null)
        {
            actionButton.onClick.RemoveAllListeners();
            actionButton.onClick.AddListener(OnActionClicked);
        }

        // Сначала строим левый список
        BuildList();

        // Выбираем, что показывать справа при первом открытии:
        // 1) если уже был выбран какой-то id (например ты сохраняешь отдельно) — оставим его
        // 2) иначе ставим первый элемент из sets
        if (sets != null && sets.Length > 0)
        {
            bool found = false;
            foreach (var d in sets)
            {
                if (d != null && d.id == _selectedId) { found = true; break; }
            }

            if (!found)
                _selectedId = sets[0].id;
        }

        // Обновляем UI один раз
        Refresh();
    }

    public void Refresh()
    {
        if (_game == null || sets == null || sets.Length == 0)
            return;

        var def = FindDef(_selectedId);
        if (def == null) return;

        int day = _game.GetDayIndex();
        int coins = _game.GetCoins();

        bool owned = _game.IsDecorOwned(def.id);
        bool active = _game.IsDecorActive(def.id);
        bool dayOk = day >= Mathf.Max(1, def.requiredDay);
        bool enough = coins >= def.price;


        // 1) Баф выбранного (показываем всегда)
        if (buffText != null)
        {
            float pct = def.incomeBonus * 100f;
            buffText.text = pct > 0f ? $"Бонус к прибыли: +{pct:0.#}%" : "";
        }

        if (titleText != null)
        {
            if (!owned && !dayOk)
                titleText.text = $"Доступно с дня {def.requiredDay}";
            else
                titleText.text = def.title;
        }

        // цена всегда числом (как ты хотел для ингредиентов)
        if (priceText != null) priceText.text = def.price.ToString();


        if (actionButtonText != null)
        {
            if (!owned && !dayOk)
                actionButtonText.text = "Закрыто";
            else if (!owned)
                actionButtonText.text = enough ? "Купить" : "Не хватает";
            else if (active)
                actionButtonText.text = "Выключить";
            else
                actionButtonText.text = "Включить";
        }

        if (actionButton != null)
        {
            if (!owned && !dayOk)
                actionButton.interactable = false;          // закрыто по дню
            else if (!owned)
                actionButton.interactable = enough;         // можно купить только если хватает монет
            else
                actionButton.interactable = true;           // куплено -> можно включать/выключать всегда
        }

        RefreshListStates();

        // 2) Суммарный баф по купленным
        if (totalBuffText != null && _game != null)
        {
            float mul = _game.GetDecorIncomeMultiplier(sets); // sets = твой массив DecorSetDef[]
            float totalPct = (mul - 1f) * 100f;

            totalBuffText.text = totalPct > 0.01f ? $"Доход +{totalPct:0.#}%" : "Доход +0%";
        }
    }

    private void OnActionClicked()
    {
        if (_game == null) return;

        var def = FindDef(_selectedId);
        if (def == null) return;

        int day = _game.GetDayIndex();
        bool dayOk = day >= Mathf.Max(1, def.requiredDay);

        bool wasOwned = _game.IsDecorOwned(def.id);
        bool boughtNow = false;

        if (!wasOwned)
        {
            if (!dayOk) { Refresh(); return; }

            if (!_game.TrySpendCoins(def.price))
            {
                Refresh();
                return;
            }

            _game.AddOwnedDecor(def.id);
            boughtNow = true;
        }

        bool activeNow = _game.IsDecorActive(def.id);
        _game.SetDecorActive(def.id, !activeNow);

        _game.ApplyDecorToSceneMulti(sets);
        _game.SaveProgressPublic();

        Refresh();

        // ✅ если идет анимация правой карточки — глушим её, иначе alpha может быть 0
        if (_rightCardFx != null)
        {
            StopCoroutine(_rightCardFx);
            _rightCardFx = null;
        }
        if (rightCardCanvasGroup != null) rightCardCanvasGroup.alpha = 1f;


        Debug.Log($"[DECOR] boughtNow={boughtNow} rightShineNull={rightShine == null}");
        if (rightShine != null)
            Debug.Log($"[DECOR] rightShine obj={rightShine.name} active={rightShine.gameObject.activeInHierarchy}");

        // ✅ запускаем на следующий кадр, когда UI уже точно обновился
        if (boughtNow && rightShine != null)
            StartCoroutine(PlayShineNextFrame());
    }

    private System.Collections.IEnumerator PlayShineNextFrame()
    {
        yield return null; // следующий кадр
        if (rightShine != null) rightShine.Play();
    }



    private DecorSetDef FindDef(int id)
    {
        foreach (var s in sets)
            if (s != null && s.id == id)
                return s;
        return null;
    }

    private DecorSetListItem[] _items;

    private void BuildList()
    {
        Debug.Log($"[DECOR] BuildList root={(listRoot != null)} prefab={(listItemPrefab != null)} sets={(sets != null ? sets.Length : 0)}");

        if (listRoot == null || listItemPrefab == null) return;

        for (int i = listRoot.childCount - 1; i >= 0; i--)
            Destroy(listRoot.GetChild(i).gameObject);

        _items = new DecorSetListItem[sets.Length];

        for (int i = 0; i < sets.Length; i++)
        {
            var def = sets[i];
            var item = Instantiate(listItemPrefab, listRoot);
            item.Bind(this, def.id, def.title, def.requiredDay);
            item.SetIcon(def.icon);
            _items[i] = item;
        }

    }

    private void RefreshListStates()
    {
        if (_items == null) return;

        int day = _game.GetDayIndex();

        for (int i = 0; i < sets.Length; i++)
        {
            var def = sets[i];
            var it = _items[i];

            bool owned = _game.IsDecorOwned(def.id);
            bool isActive = _game.IsDecorActive(def.id);
            bool isSelected = def.id == _selectedId;
            bool dayLocked = day < Mathf.Max(1, def.requiredDay);

            it.SetState(owned, isSelected, isActive, dayLocked);
        }
    }
    public DecorSetDef[] GetDefs() => sets;

    public void OnItemClicked(int id)
    {
        if (_selectedId == id) return;

        _selectedId = id;

        if (_rightCardFx != null) StopCoroutine(_rightCardFx);
        _rightCardFx = StartCoroutine(RightCardSwapFx(() =>
        {
            // обновляем правую карточку и список
            Refresh();
        }));
    }

    private System.Collections.IEnumerator RightCardSwapFx(System.Action applyData)
    {
        if (rightCardCanvasGroup == null || rightCardRect == null)
        {
            applyData?.Invoke();
            yield break;
        }

        float t = 0f;
        float sec = Mathf.Max(0.0001f, rightCardFade);
        Vector2 basePos = rightCardRect.anchoredPosition;
        Vector2 outPos = basePos + Vector2.right * rightCardSlidePx;
        Vector2 inPos = basePos - Vector2.right * rightCardSlidePx;

        // fade out + slide out
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / sec;
            float s = Mathf.SmoothStep(0f, 1f, t);
            rightCardCanvasGroup.alpha = 1f - s;
            rightCardRect.anchoredPosition = Vector2.Lerp(basePos, outPos, s);
            yield return null;
        }

        rightCardCanvasGroup.alpha = 0f;

        // применяем новые данные в “пустоте”
        rightCardRect.anchoredPosition = inPos;
        applyData?.Invoke();

        // fade in + slide in
        t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / sec;
            float s = Mathf.SmoothStep(0f, 1f, t);
            rightCardCanvasGroup.alpha = s;
            rightCardRect.anchoredPosition = Vector2.Lerp(inPos, basePos, s);
            yield return null;
        }

        rightCardCanvasGroup.alpha = 1f;
        rightCardRect.anchoredPosition = basePos;
    }

}
