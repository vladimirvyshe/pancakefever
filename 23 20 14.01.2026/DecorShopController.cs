using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DecorShopController : MonoBehaviour
{

    [System.Serializable]
    public class DecorSetDef
    {
        public int id;
        public string title;
        public int price;
        public int requiredDay = 1;
        public Sprite icon;              // ⬅️ ВАЖНО
        public GameObject sceneSetRoot;
    }

    [Header("Data")]
    [SerializeField] private DecorSetDef[] sets;

    [Header("UI - right card")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text priceText;
    [SerializeField] private TMP_Text dayText;
    [SerializeField] private Button actionButton;
    [SerializeField] private TMP_Text actionButtonText;

    [Header("UI - left list")]
    [SerializeField] private Transform listRoot;              // Content
    [SerializeField] private DecorSetListItem listItemPrefab;

    private GameFlowController _game;
    private int _selectedId = 0;

    public void Init(GameFlowController game)
    {
        _game = game;

        if (actionButton != null)
            actionButton.onClick.AddListener(OnActionClicked);

        // выберем текущий сохраненный декор
        _selectedId = _game.GetDecorId();

        BuildList();
        Refresh();
    }

    // вызови это из кнопок списка слева
    public void SelectSet(int id)
    {
        _selectedId = id;
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
    }

    private void OnActionClicked()
    {
        if (_game == null) return;

        var def = FindDef(_selectedId);
        if (def == null) return;

        int day = _game.GetDayIndex();
        bool dayOk = day >= Mathf.Max(1, def.requiredDay);
        bool owned = _game.IsDecorOwned(def.id);

        // если не куплено — покупаем
        if (!owned)
        {
            if (!dayOk) { Refresh(); return; }

            if (!_game.TrySpendCoins(def.price))
            {
                Refresh();
                return;
            }

            _game.AddOwnedDecor(def.id);
        }

        // toggle on/off
        bool activeNow = _game.IsDecorActive(def.id);
        _game.SetDecorActive(def.id, !activeNow);

        _game.ApplyDecorToSceneMulti(sets);
        _game.SaveProgressPublic();

        Refresh();
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
            bool active = _game.IsDecorActive(def.id);
            bool isActive = _game.IsDecorActive(def.id);
            bool isSelected = def.id == _selectedId;
            bool dayLocked = day < Mathf.Max(1, def.requiredDay);

            it.SetState(owned, isSelected, isActive, dayLocked);
        }
    }
    public DecorSetDef[] GetDefs() => sets;

    public void OnItemClicked(int id)
    {
        _selectedId = id;   // справа показываем инфу по выбранному элементу
        Refresh();
    }

}
