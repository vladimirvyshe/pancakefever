using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

public class TutorialController : MonoBehaviour
{
    private const string PP_TUTORIAL_DONE = "tutorial_done";

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
    [SerializeField] private bool forceTutorialEveryPlay = false; // ✅ в релизе false

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
    private string _msgKey;
    private object[] _msgArgs;


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
        // DEV: всегда показывать
        if (forceTutorialEveryPlay)
        {
            StartWelcome();
            return;
        }

        // если туториал уже пройден
        if (PlayerPrefs.GetInt(PP_TUTORIAL_DONE, 0) == 1)
        {
            SetOverlayVisible(false);
            FadeDim(false);
            _step = Step.Finished;
            return;
        }

        // ✅ если джем уже разблокирован — считаем туториал пройденным
        var data = ProgressService.Load(); // struct
        bool hasJam = ProgressService.HasIngredient(
            data.ingredientMask,
            ProgressService.IngredientBit.Jam
        );

        if (hasJam)
        {
            PlayerPrefs.SetInt(PP_TUTORIAL_DONE, 1);
            PlayerPrefs.Save();

            if (overlayRoot != null) overlayRoot.SetActive(false);
            _step = Step.Finished;
            return;
        }

        StartWelcome();
    }

    // -----------------------------
    // Public hooks from GameFlow
    // -----------------------------

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

    public void OnOrderCompleted(int currentCoins)
    {
        if (_step != Step.RepeatOrders) return;

        if (currentCoins >= moneyGoalForFirstIngredient)
            StartBuyIngredient();
    }

    public void OnShopOpened()
    {
        if (_step != Step.BuyIngredient) return;

        SetOverlayVisible(true);
        SetOverlayBlocking(true);
        ApplyLayout(layoutBuyJam);

        SetMessage("tut_buy_jam");
        AllowOnly(firstIngredientButton);

        SetActionHardBlocked(true);
    }

    public void OnFirstIngredientBought()
    {
        if (_step != Step.BuyIngredient) return;
        StartExitShop();
    }

    public void OnShopClosed()
    {
        if (_step != Step.ExitShop) return;
        StartToppingHint();
    }

    public void OnToppingApplied()
    {
        if (_step != Step.ToppingHint) return;
        StartFinalMessage();
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
        // Перерисовать текущий текст туториала на новом языке
        ReapplyMessage();
    }

    private void SetMessage(string key, params object[] args)
    {
        _msgKey = key;
        _msgArgs = args;

        if (messageText == null) return;

        string template = LocalizationSettings.StringDatabase.GetLocalizedString("Tutorial", key);
        messageText.text = (args == null || args.Length == 0) ? template : string.Format(template, args);
    }

    private void ReapplyMessage()
    {
        if (string.IsNullOrEmpty(_msgKey)) return;
        SetMessage(_msgKey, _msgArgs);
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

        SetAdsHardBlocked(true);
        SetActionHardBlocked(true);

        SetOverlayVisible(true);
        SetOverlayBlocking(true);
        SetContinueVisible(true);

        ApplyLayout(layoutWelcome);

        messageText.alignment = TMPro.TextAlignmentOptions.Center;
        if (messageText != null) messageText.alignment = TextAlignmentOptions.Center;
        SetMessage("tut_welcome");
    }

    private void StartPour()
    {
        _step = Step.Pour;


        if (messageText != null) messageText.alignment = TextAlignmentOptions.TopLeft;

        FadeDim(false);
        SetOverlayVisible(true);
        SetOverlayBlocking(false);
        SetContinueVisible(false);

        ApplyLayout(layoutCooking);

        SetActionHardBlocked(false);
        SetAdsHardBlocked(true);

        AllowOnly(actionButton);
        messageText.alignment = TMPro.TextAlignmentOptions.Center;
        SetMessage("tut_step1_pour");
    }

    private void StartFlip()
    {
        _step = Step.Flip;


        if (messageText != null) messageText.alignment = TextAlignmentOptions.TopLeft;

        ApplyLayout(layoutCooking);

        SetActionHardBlocked(false);
        SetAdsHardBlocked(true);
        messageText.alignment = TMPro.TextAlignmentOptions.Center;
        AllowOnly(actionButton);
        SetMessage("tut_step2_flip");
    }

    private void StartPack()
    {
        _step = Step.Pack;


        if (messageText != null) messageText.alignment = TextAlignmentOptions.TopLeft;

        ApplyLayout(layoutCooking);

        SetActionHardBlocked(false);
        SetAdsHardBlocked(true);

        AllowOnly(actionButton);
        messageText.alignment = TMPro.TextAlignmentOptions.Center;
        SetMessage("tut_step3_pack");
    }

    private void StartRepeatOrders()
    {
        _step = Step.RepeatOrders;


        if (messageText != null) messageText.alignment = TextAlignmentOptions.TopLeft;

        ApplyLayout(layoutRepeat);

        SetOverlayVisible(true);
        SetOverlayBlocking(false);
        SetContinueVisible(false);

        SetActionHardBlocked(false);
        SetAdsHardBlocked(true);

        AllowOnly(actionButton, pancakeButton);
        messageText.alignment = TMPro.TextAlignmentOptions.Center;
        SetMessage("tut_repeat_orders_goal", moneyGoalForFirstIngredient);
    }

    private void StartBuyIngredient()
    {
        _step = Step.BuyIngredient;


        if (messageText != null) messageText.alignment = TextAlignmentOptions.TopLeft;

        StopSoftHideOverlay();

        SetOverlayVisible(true);
        SetOverlayBlocking(true);
        SetContinueVisible(false);

        ApplyLayout(layoutShop);

        SetActionHardBlocked(true);
        SetAdsHardBlocked(true);
        messageText.alignment = TMPro.TextAlignmentOptions.Center;
        SetMessage("tut_open_shop");

        AllowOnly(shopButton);
    }

    private void StartExitShop()
    {
        _step = Step.ExitShop;


        if (messageText != null) messageText.alignment = TextAlignmentOptions.TopLeft;

        SetOverlayVisible(true);
        SetOverlayBlocking(true);
        SetContinueVisible(false);

        ApplyLayout(layoutExitShop);

        SetActionHardBlocked(true);
        SetAdsHardBlocked(true);
        messageText.alignment = TMPro.TextAlignmentOptions.Center;
        SetMessage("tut_close_shop");

        AllowOnly(shopCloseButton);
    }

    private void StartToppingHint()
    {
        _step = Step.ToppingHint;


        if (messageText != null) messageText.alignment = TextAlignmentOptions.TopLeft;

        SetOverlayVisible(true);
        SetOverlayBlocking(false);
        SetContinueVisible(false);

        ApplyLayout(layoutTopping);

        SetActionHardBlocked(false);
        SetAdsHardBlocked(true);

        AllowOnly(firstIngredientButton, pancakeButton, actionButton);
        messageText.alignment = TMPro.TextAlignmentOptions.Center;
        SetMessage("tut_topping_hint");
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
        messageText.alignment = TMPro.TextAlignmentOptions.Center;
        if (messageText != null) messageText.alignment = TextAlignmentOptions.Center;
        SetMessage("tut_final");
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

        PlayerPrefs.SetInt(PP_TUTORIAL_DONE, 1);
        PlayerPrefs.Save();

        var gf = FindObjectOfType<GameFlowController>();
        if (gf != null)
            gf.SaveProgressPublic();
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
    // Panel layout (pos + size)
    // -----------------------------

    private void ApplyLayout(PanelLayout layout, bool instant = false)
    {
        if (panelRect == null) return;


        // если мы не в иерархии — просто применим мгновенно
        if (!isActiveAndEnabled || !gameObject.activeInHierarchy)
            instant = true;

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
            case Step.BuyIngredient: return layoutBuyJam;
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

    private void AllowOnly(params Button[] allowed)
    {
        if (actionButton != null) actionButton.interactable = false;
        if (pancakeButton != null) pancakeButton.interactable = false;
        if (shopButton != null) shopButton.interactable = false;
        if (firstIngredientButton != null) firstIngredientButton.interactable = false;
        if (shopCloseButton != null) shopCloseButton.interactable = false;

        if (buttonsToBlock != null)
            foreach (var b in buttonsToBlock)
                if (b != null) b.interactable = false;

        if (allowed != null)
            foreach (var a in allowed)
                if (a != null) a.interactable = true;
    }

    // -----------------------------
    // Hard locks
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

    // -----------------------------
    // Dim background
    // -----------------------------

    private void FadeDim(bool show)
    {
        if (dimImage == null) return;

        // ✅ если объект туториала не активен — просто выставляем состояние без корутин
        if (!isActiveAndEnabled || !gameObject.activeInHierarchy)
        {
            var c = dimImage.color;
            c.a = show ? dimMaxAlpha : 0f;
            dimImage.color = c;
            dimImage.gameObject.SetActive(show);
            return;
        }

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

        if (!show)
            dimImage.gameObject.SetActive(false);

        _dimFadeCo = null;
    }


    // -----------------------------
    // DEV: reset tutorial progress
    // -----------------------------



    public void ResetTutorialProgress()
    {
        PlayerPrefs.SetInt(PP_TUTORIAL_DONE, 0);
        PlayerPrefs.Save();

        // Если туториал сейчас был активен — убираем и возвращаем управление
        FadeDim(false);
        StopSoftHideOverlay();
        StopLayoutAnim();

        SetGlobalBlocked(false);
        SetAdsHardBlocked(false);
        SetActionHardBlocked(false);

        SetOverlayVisible(false);
        _step = Step.None;
    }

#if UNITY_EDITOR
    [ContextMenu("DEV: Reset Tutorial Progress")]
    private void DevResetTutorialProgress()
    {
        ResetTutorialProgress();
        Debug.Log("[TUTORIAL] tutorial_done reset.");
    }

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
