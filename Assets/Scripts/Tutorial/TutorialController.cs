using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TutorialController : MonoBehaviour
{
    // Локализация не влияет: туториал работает по действиям/хукам, а не по текстам.
    public enum Step
    {
        None,
        Welcome,
        Pour,
        Flip,
        Pack,
        RepeatOrders,
        BuyIngredient,
        ExitShop,        // ✅ новый подшаг: "выйди из магазина"
        ToppingHint,
        FinalMessage,
        Finished
    }

    [Serializable]
    public struct PanelLayout
    {
        public Vector2 anchoredPos; // позиция Panel (RectTransform.anchoredPosition)
        public Vector2 sizeDelta;   // размер Panel (RectTransform.sizeDelta)
    }

    [Header("Debug")]
    [SerializeField] private bool forceTutorialEveryPlay = true;

    [Header("UI - Overlay")]
    [SerializeField] private GameObject overlayRoot;              // TutorialOverlay
    [SerializeField] private CanvasGroup overlayCanvasGroup;       // CanvasGroup на TutorialOverlay
    [SerializeField] private RectTransform panelRect;              // TutorialOverlay/Panel
    [SerializeField] private TMP_Text messageText;                 // Panel/MessageText
    [SerializeField] private Button continueButton;                // Panel/ContinueButton

    [Header("Panel Layout Presets (pos + size)")]
    [SerializeField] private float layoutAnimSeconds = 0.25f;
    [SerializeField] private PanelLayout layoutWelcome;
    [SerializeField] private PanelLayout layoutCooking;     // pour/flip/pack
    [SerializeField] private PanelLayout layoutRepeat;      // "сделай ещё пару заказов"
    [SerializeField] private PanelLayout layoutShop;        // "открой магазин"
    [SerializeField] private PanelLayout layoutBuyJam;      // "магазин открыт -> купи джем"
    [SerializeField] private PanelLayout layoutExitShop;    // ✅ "после покупки -> выйди из магазина"
    [SerializeField] private PanelLayout layoutTopping;     // "джем + тап по блину"
    [SerializeField] private PanelLayout layoutFinal;

    [Header("Gameplay Buttons")]
    [SerializeField] private Button actionButton;          // главная кнопка действия
    [SerializeField] private Button pancakeButton;         // тап по блину

    [Header("Shop Buttons")]
    [SerializeField] private Button shopButton;             // ShopButton
    [SerializeField] private Button firstIngredientButton;  // Btn_Jam
    [SerializeField] private Button shopCloseButton;        // ✅ кнопка закрытия магазина (X / Close)

    [Header("Economy")]
    [SerializeField] private int moneyGoalForFirstIngredient = 150;

    [Header("Block During Tutorial (global)")]
    [SerializeField] private Button[] buttonsToBlock;        // всё лишнее (settings, skip, etc.) кроме action/pancake/shop/jam/close
    [SerializeField] private GameObject[] objectsToBlock;    // опционально: панели/объекты

    [Header("Hard Locks (CanvasGroup)")]
    [SerializeField] private CanvasGroup adsX2CanvasGroup;        // CanvasGroup на ADSX2Button root
    [SerializeField] private CanvasGroup actionButtonCanvasGroup; // CanvasGroup на ActionButton (или ActionButtonPanel)

    [Header("Debug Layout Tuning (Editor Only)")]
    [SerializeField] private bool debugLiveApplyPresetsInPlayMode = true; // менять пресеты в инспекторе -> сразу применится
    [SerializeField] private bool debugInstantApply = true;               // применять без анимации

    [Header("Overlay Dim Background")]
    [SerializeField] private Image dimImage;   // Image, растянутый на весь экран
    [SerializeField] private float dimFadeDuration = 0.25f;
    [SerializeField, Range(0f, 1f)] private float dimMaxAlpha = 0.6f;

    private Coroutine _dimFadeCo;
    private Step _step = Step.None;

    // restore state
    private bool[] _buttonsPrev;
    private bool[] _objectsPrev;
    private bool _cachedBlockState;

    // coroutines
    private Coroutine _layoutCo;
    private Coroutine _softHideCo;

    public bool IsActive => _step != Step.None && _step != Step.Finished;

    private void Awake()
    {
        if (continueButton != null)
            continueButton.onClick.AddListener(OnContinueClicked);
    }

    private void Start()
    {
        TryStart();
    }

    public void TryStart()
    {
        if (!forceTutorialEveryPlay && PlayerPrefs.GetInt("tutorial_done", 0) == 1)
        {
            SetOverlayVisible(false);
            _step = Step.Finished;
            return;
        }

        StartWelcome();
    }

    // -----------------------------
    // Public hooks from GameFlow
    // -----------------------------

    // Вызывай из GameFlowController ДО основной логики кнопки:
    // tutorial.NotifyAction(action);
    public void NotifyAction(TutorialAction action)
    {
        if (!IsActive) return;

        if (_step == Step.Pour && action == TutorialAction.Pour)
        {
            StartFlip();
            return;
        }

        if (_step == Step.Flip && action == TutorialAction.Flip)
        {
            StartPack();
            return;
        }

        if (_step == Step.Pack && action == TutorialAction.Pack)
        {
            StartRepeatOrders();
            return;
        }
    }

    // Вызывай после начисления монет за заказ
    public void OnOrderCompleted(int currentCoins)
    {
        if (_step != Step.RepeatOrders) return;

        if (currentCoins >= moneyGoalForFirstIngredient)
            StartBuyIngredient();
    }

    // Вызывай когда реально открылся магазин (после клика ShopButton / показ ShopPopup)
    public void OnShopOpened()
    {
        if (_step != Step.BuyIngredient) return;

        SetOverlayVisible(true);
        SetOverlayBlocking(true);
        ApplyLayout(layoutBuyJam);

        messageText.text = "Купи джем, чтобы добавлять начинку.";
        AllowOnly(firstIngredientButton);

        // ActionButton в этот момент должен оставаться заблокирован:
        SetActionHardBlocked(true);
    }

    // Вызывай после успешной покупки Btn_Jam
    public void OnFirstIngredientBought()
    {
        if (_step != Step.BuyIngredient) return;
        StartExitShop(); // ✅ теперь после покупки просим выйти из магазина
    }

    // Вызывай когда магазин реально закрылся (после нажатия X / Close или скрытия ShopPopup)
    public void OnShopClosed()
    {
        if (_step != Step.ExitShop) return;
        StartToppingHint();
    }

    // Вызывай только после реального нанесения начинки (выбрал джем + тап по блину)
    public void OnToppingApplied()
    {
        if (_step != Step.ToppingHint) return;
        StartFinalMessage();
    }

    // -----------------------------
    // Step Flow
    // -----------------------------

    private void StartWelcome()
    {
        _step = Step.Welcome;

        FadeDim(true);
        CacheBlockStateOnce();
        SetGlobalBlocked(true);

        // жёстко блокируем конкретные проблемные кнопки
        SetAdsHardBlocked(true);
        SetActionHardBlocked(true);

        SetOverlayVisible(true);
        SetOverlayBlocking(true);
        SetContinueVisible(true);

        ApplyLayout(layoutWelcome);

        messageText.text =
            "Добро пожаловать на кухню!\n" +
            "Сейчас быстро научу тебя выполнять заказы.\n\n" +
            "Нажми «Начать», когда будешь готов.";
    }

    private void StartPour()
    {
        _step = Step.Pour;

        FadeDim(false);
        SetOverlayVisible(true);
        SetOverlayBlocking(false);
        SetContinueVisible(false);

        ApplyLayout(layoutCooking);

        // ActionButton нужен
        SetActionHardBlocked(false);
        // ADS во время туториала блокируем всегда
        SetAdsHardBlocked(true);

        AllowOnly(actionButton);

        messageText.text = "Шаг 1/3\nНажми «Залить тесто».";
    }

    private void StartFlip()
    {
        _step = Step.Flip;

        ApplyLayout(layoutCooking);

        // ActionButton нужен
        SetActionHardBlocked(false);
        SetAdsHardBlocked(true);

        AllowOnly(actionButton);

        messageText.text = "Шаг 2/3\nПереверни вовремя — блин может сгореть!";
    }

    private void StartPack()
    {
        _step = Step.Pack;

        ApplyLayout(layoutCooking);

        SetActionHardBlocked(false);
        SetAdsHardBlocked(true);

        AllowOnly(actionButton);

        messageText.text = "Шаг 3/3\nТеперь упакуй заказ.";
    }

    private void StartRepeatOrders()
    {
        _step = Step.RepeatOrders;

        ApplyLayout(layoutRepeat);

        SetOverlayVisible(true);
        SetOverlayBlocking(false);
        SetContinueVisible(false);

        SetActionHardBlocked(false);
        SetAdsHardBlocked(true);

        AllowOnly(actionButton, pancakeButton);

        messageText.text = $"Сделай ещё пару заказов,\nчтобы накопить {moneyGoalForFirstIngredient} монет";
    }

    private void StartBuyIngredient()
    {
        _step = Step.BuyIngredient;

        StopSoftHideOverlay();

        SetOverlayVisible(true);
        SetOverlayBlocking(true);
        SetContinueVisible(false);

        ApplyLayout(layoutShop);

        // ВАЖНО: ActionButton тут должен быть железно заблокирован
        SetActionHardBlocked(true);
        SetAdsHardBlocked(true);

        messageText.text = "Отлично!\nОткрой магазин.";

        AllowOnly(shopButton);
    }

    private void StartExitShop()
    {
        _step = Step.ExitShop;

        SetOverlayVisible(true);
        SetOverlayBlocking(true);
        SetContinueVisible(false);

        ApplyLayout(layoutExitShop);

        // Пока магазин открыт — блокируем игру полностью
        SetActionHardBlocked(true);
        SetAdsHardBlocked(true);

        messageText.text = "Отлично!\nТеперь закрой магазин.";

        // Разрешаем только кнопку закрытия магазина
        AllowOnly(shopCloseButton);
    }

    private void StartToppingHint()
    {
        _step = Step.ToppingHint;

        SetOverlayVisible(true);
        SetOverlayBlocking(false);
        SetContinueVisible(false);

        ApplyLayout(layoutTopping);

        SetActionHardBlocked(false);
        SetAdsHardBlocked(true);

        AllowOnly(firstIngredientButton, pancakeButton, actionButton);

        messageText.text =
            "Когда приготовится блин:\n" +
            "1) Нажми на джем внизу\n" +
            "2) Тапни по блину, чтобы нанести";
    }

    private void StartFinalMessage()
    {
        FadeDim(true);
        _step = Step.FinalMessage;

        SetOverlayVisible(true);
        SetOverlayBlocking(true);
        SetContinueVisible(true);

        ApplyLayout(layoutFinal);

        SetActionHardBlocked(true);
        SetAdsHardBlocked(true);

        messageText.text =
            "Отлично!\n" +
            "Начинка увеличивает награду\n" +
            "Продолжай играть!";
    }

    private void Finish()
    {
        FadeDim(false);
        _step = Step.Finished;

        StopSoftHideOverlay();
        StopLayoutAnim();

        SetGlobalBlocked(false);

        SetAdsHardBlocked(false);
        SetActionHardBlocked(false);

        SetOverlayVisible(false);

        PlayerPrefs.SetInt("tutorial_done", 1);
        PlayerPrefs.Save();
    }

    // -----------------------------
    // UI Events
    // -----------------------------

    private void OnContinueClicked()
    {
        if (_step == Step.Welcome)
        {
            StartPour();
            return;
        }

        if (_step == Step.FinalMessage)
        {
            Finish();
            return;
        }
    }

    // -----------------------------
    // Overlay helpers
    // -----------------------------

    private void SetOverlayVisible(bool visible)
    {
        if (overlayRoot != null)
            overlayRoot.SetActive(visible);
    }

    private void SetContinueVisible(bool visible)
    {
        if (continueButton != null)
            continueButton.gameObject.SetActive(visible);
    }

    private void SetOverlayBlocking(bool blocks)
    {
        if (overlayCanvasGroup == null) return;
        overlayCanvasGroup.blocksRaycasts = blocks;
        overlayCanvasGroup.interactable = blocks;
    }

    // -----------------------------
    // Panel layout (pos + size) with safe coroutines
    // -----------------------------

    private void ApplyLayout(PanelLayout layout, bool instant = false)
    {
        if (panelRect == null) return;

        StopLayoutAnim();

        if (instant)
        {
            panelRect.anchoredPosition = layout.anchoredPos;
            panelRect.sizeDelta = layout.sizeDelta;
            return;
        }

        _layoutCo = StartCoroutine(AnimateLayout(layout));
    }

    private PanelLayout GetLayoutForStep(Step step)
    {
        switch (step)
        {
            case Step.Welcome: return layoutWelcome;
            case Step.Pour:
            case Step.Flip:
            case Step.Pack: return layoutCooking;
            case Step.RepeatOrders: return layoutRepeat;
            case Step.BuyIngredient: return layoutBuyJam; // базово: если хочешь, оставь layoutShop только в StartBuyIngredient()
            case Step.ExitShop: return layoutExitShop;
            case Step.ToppingHint: return layoutTopping;
            case Step.FinalMessage: return layoutFinal;
            default: return layoutCooking;
        }
    }

    private IEnumerator AnimateLayout(PanelLayout target)
    {
        Vector2 startPos = panelRect.anchoredPosition;
        Vector2 startSize = panelRect.sizeDelta;

        float dur = Mathf.Max(0.01f, layoutAnimSeconds);
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / dur;
            float k = Mathf.SmoothStep(0f, 1f, t);

            panelRect.anchoredPosition = Vector2.Lerp(startPos, target.anchoredPos, k);
            panelRect.sizeDelta = Vector2.Lerp(startSize, target.sizeDelta, k);

            yield return null;
        }

        panelRect.anchoredPosition = target.anchoredPos;
        panelRect.sizeDelta = target.sizeDelta;

        _layoutCo = null;
    }

    private void StopLayoutAnim()
    {
        if (_layoutCo != null)
        {
            StopCoroutine(_layoutCo);
            _layoutCo = null;
        }
    }

    private void StartSoftHideOverlay(float seconds)
    {
        StopSoftHideOverlay();
        _softHideCo = StartCoroutine(SoftHideRoutine(seconds));
    }

    private IEnumerator SoftHideRoutine(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        if (_step == Step.RepeatOrders)
            SetOverlayVisible(false);
        _softHideCo = null;
    }

    private void StopSoftHideOverlay()
    {
        if (_softHideCo != null)
        {
            StopCoroutine(_softHideCo);
            _softHideCo = null;
        }
    }

    // -----------------------------
    // Blocking system
    // -----------------------------

    private void CacheBlockStateOnce()
    {
        if (_cachedBlockState) return;
        _cachedBlockState = true;

        if (buttonsToBlock != null)
        {
            _buttonsPrev = new bool[buttonsToBlock.Length];
            for (int i = 0; i < buttonsToBlock.Length; i++)
                _buttonsPrev[i] = buttonsToBlock[i] != null && buttonsToBlock[i].interactable;
        }

        if (objectsToBlock != null)
        {
            _objectsPrev = new bool[objectsToBlock.Length];
            for (int i = 0; i < objectsToBlock.Length; i++)
                _objectsPrev[i] = objectsToBlock[i] != null && objectsToBlock[i].activeSelf;
        }
    }

    private void SetGlobalBlocked(bool blocked)
    {
        // Buttons
        if (buttonsToBlock != null)
        {
            for (int i = 0; i < buttonsToBlock.Length; i++)
            {
                var b = buttonsToBlock[i];
                if (b == null) continue;

                if (blocked) b.interactable = false;
                else b.interactable = (_buttonsPrev != null && i < _buttonsPrev.Length) ? _buttonsPrev[i] : true;
            }
        }

        // Objects
        if (objectsToBlock != null)
        {
            for (int i = 0; i < objectsToBlock.Length; i++)
            {
                var go = objectsToBlock[i];
                if (go == null) continue;

                if (blocked) go.SetActive(false);
                else go.SetActive((_objectsPrev != null && i < _objectsPrev.Length) ? _objectsPrev[i] : true);
            }
        }
    }

    // Важно: этот AllowOnly выключает ВСЁ, чем туториал управляет,
    // чтобы никакие "внешние" включения не мешали.
    private void AllowOnly(params Button[] allowed)
    {
        // 1) выключаем управляемые кнопки
        if (actionButton != null) actionButton.interactable = false;
        if (pancakeButton != null) pancakeButton.interactable = false;
        if (shopButton != null) shopButton.interactable = false;
        if (firstIngredientButton != null) firstIngredientButton.interactable = false;
        if (shopCloseButton != null) shopCloseButton.interactable = false;

        if (buttonsToBlock != null)
            foreach (var b in buttonsToBlock)
                if (b != null) b.interactable = false;

        // 2) включаем только разрешённые
        if (allowed != null)
            foreach (var a in allowed)
                if (a != null) a.interactable = true;
    }

    // -----------------------------
    // Hard locks (CanvasGroup)
    // -----------------------------

    private void SetAdsHardBlocked(bool blocked)
    {
        if (adsX2CanvasGroup == null) return;
        adsX2CanvasGroup.interactable = !blocked;
        adsX2CanvasGroup.blocksRaycasts = !blocked;
    }

    private void SetActionHardBlocked(bool blocked)
    {
        if (actionButtonCanvasGroup == null) return;
        actionButtonCanvasGroup.interactable = !blocked;
        actionButtonCanvasGroup.blocksRaycasts = !blocked;
    }

    private void FadeDim(bool show)
    {
        if (dimImage == null) return;

        if (show)
            dimImage.gameObject.SetActive(true);

        if (_dimFadeCo != null)
            StopCoroutine(_dimFadeCo);

        _dimFadeCo = StartCoroutine(FadeDimRoutine(show));
    }

    private IEnumerator FadeDimRoutine(bool show)
    {
        float startAlpha = dimImage.color.a;
        float targetAlpha = show ? dimMaxAlpha : 0f;

        float t = 0f;
        float dur = Mathf.Max(0.01f, dimFadeDuration);

        Color c = dimImage.color;

        while (t < 1f)
        {
            t += Time.deltaTime / dur;
            float k = Mathf.SmoothStep(0f, 1f, t);

            c.a = Mathf.Lerp(startAlpha, targetAlpha, k);
            dimImage.color = c;

            yield return null;
        }

        c.a = targetAlpha;
        dimImage.color = c;

        // если полностью выключили — можно отключить Image
        if (!show)
            dimImage.gameObject.SetActive(false);

        _dimFadeCo = null;
    }

#if UNITY_EDITOR
    [ContextMenu("DEBUG: Apply Layout For Current Step")]
    private void DebugApplyLayoutForCurrentStep()
    {
        ApplyLayout(GetLayoutForStep(_step), instant: true);
    }

    private void OnValidate()
    {
        if (!debugLiveApplyPresetsInPlayMode) return;
        if (!Application.isPlaying) return;
        if (panelRect == null) return;
        if (!IsActive) return;

        ApplyLayout(GetLayoutForStep(_step), instant: debugInstantApply);
    }
#endif
}
