using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;


public class CookingController : MonoBehaviour
{
    private bool _fryLoopActive = false;
    private bool _inputLocked = false;

    public CookState CurrentState => _state;
    public event Action Cooked;
    public event Action Burned;
    public enum CookState
    {
        WaitingPour,     // ждём "Залить тесто"
        Pouring,         // Заливка теста
        CookingSideA,    // жарим сторону A
        WaitingFlip,     // ждём нажатие "Перевернуть" (окно)
        CookingSideB,    // жарим сторону B
        WaitingFinish,   // ждём нажатие "Приготовить" (окно)
        Burned,          // блин сгорел
        Done             // блин приготовлен
    }

    [Header("SFX")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioSource frySource;
    [SerializeField] private AudioClip flipClip;
    [SerializeField] private AudioClip fryLoopClip;      // петля жарки
    [SerializeField] private AudioClip side2ReadyClip;   // сигнал: 2-я сторона прожарилась

    [Header("UI References")]
    [SerializeField] private Button actionButton;
    [SerializeField] private TMP_Text actionButtonText;
    [SerializeField] private TMP_Text hintText;          // можно не подключать (необязательно)
    [SerializeField] private TMP_Text timerText;         // можно не подключать (необязательно)
    [SerializeField] private Image pancakeImage;

    [Header("Timing (seconds)")]
    [SerializeField] private float cookDuration = 5f;   // сколько жарится сторона
    [SerializeField] private float tapWindow = 3f;       // сколько секунд даётся на нажатие

    [Header("Base Timings (for upgrades)")]
    [SerializeField] private float baseCookTime = 0f;
    [SerializeField] private float baseBurnTime = 0f;

    [Header("Colors")]
    [SerializeField] private Color rawColor = new Color(1f, 0.95f, 0.85f, 1f);
    [SerializeField] private Color cookedColor = new Color(0.85f, 0.65f, 0.35f, 1f);
    [SerializeField] private Color burnedColor = new Color(0.1f, 0.1f, 0.1f, 1f);

    [Header("Flip Animation")]
    [SerializeField] private float flipAnimSeconds = 0.18f;
    [SerializeField] private float flipOvershoot = 1.08f;

    [Header("Flip Animation (Physical)")]
    [SerializeField] private float flipLift = 38f;
    [SerializeField] private float flipRotateDeg = 18f;
    [SerializeField] private float flipSquashX = 0.15f;
    [SerializeField] private float flipStretchY = 1.08f;

    [Header("Pour Animation")]
    [SerializeField] private float pourAnimSeconds = 0.25f;
    [SerializeField] private float pourStartScale = 0.15f;
    [SerializeField] private float pourOvershoot = 1.08f;

    [Header("Pancake Side Colors")]
    [SerializeField] private Color sideAColor = new Color(1f, 0.95f, 0.85f, 1f);
    [SerializeField] private Color sideBColor = new Color(0.96f, 0.83f, 0.70f, 1f);
    [SerializeField] private Color cookedColorSideB = new Color(0.78f, 0.55f, 0.28f, 1f); // чуть темнее





    // internal
    private CookState _state = CookState.WaitingPour;
    private float _stateTimer = 0f; // сколько прошло времени внутри текущего состояния
    private float _windowTimer = 0f;

    private void Awake()
    {
        if (actionButton == null)
            Debug.LogError("CookingController: actionButton is not assigned.");

        if (actionButtonText == null)
            Debug.LogError("CookingController: actionButtonText is not assigned.");

        if (pancakeImage == null)
            Debug.LogError("CookingController: pancakeImage is not assigned.");

        //actionButton.onClick.AddListener(OnActionButtonClicked);
    }

    private void Start()
    {
        if (baseCookTime <= 0f) baseCookTime = cookDuration;
        if (baseBurnTime <= 0f) baseBurnTime = tapWindow;
        ResetToWaitingPour();
    }

    private void Update()
    {
        switch (_state)
        {
            case CookState.CookingSideA:
                TickCooking(isSecondSide: false);
                break;

            case CookState.WaitingFlip:
                TickWindow(nextStateOnSuccess: CookState.CookingSideB, buttonLabel: "Перевернуть");
                break;

            case CookState.CookingSideB:
                TickCooking(isSecondSide: true);
                break;

            case CookState.WaitingFinish:
                TickWindow(nextStateOnSuccess: CookState.Done, buttonLabel: "Приготовить");
                break;

            case CookState.Burned:
            case CookState.Done:
            case CookState.WaitingPour:
            case CookState.Pouring:
                return;
            default:
                // ничего
                break;
        }
    }

    private void TickCooking(bool isSecondSide)
    {
        _stateTimer += Time.deltaTime;

        // прогресс прожарки 0..1
        float t = Mathf.Clamp01(_stateTimer / cookDuration);

        // визуал: сырой -> прожаренный
        if (pancakeImage != null)
        {
            Color start = isSecondSide ? sideBColor : sideAColor;
            Color end = isSecondSide ? cookedColorSideB : cookedColor;

            pancakeImage.color = Color.Lerp(start, end, t);
        }

        SetTimerUI(cookDuration - _stateTimer);

        if (_stateTimer >= cookDuration)
        {
            // переходим в окно нажатия
            _windowTimer = 0f;
            _state = isSecondSide ? CookState.WaitingFinish : CookState.WaitingFlip;


            if (isSecondSide)
            {
                SetUI("Приготовить", "Нажми 'Приготовить' в течение окна!");
            }
            else
            {
                SetUI("Перевернуть", "Нажми 'Перевернуть' в течение окна!");
            }
        }
    }

    private void TickWindow(CookState nextStateOnSuccess, string buttonLabel)
    {
        _windowTimer += Time.deltaTime;
        SetTimerUI(tapWindow - _windowTimer);

        // Подсказка/текст кнопки держим
        if (actionButtonText != null) actionButtonText.text = buttonLabel;

        if (_windowTimer >= tapWindow)
        {
            BurnPancake();
        }
    }

    private void OnActionButtonClicked()
    {
        if (_inputLocked) return;
        switch (_state)
        {
            case CookState.WaitingPour:
                _inputLocked = true;
                if (actionButton != null) actionButton.interactable = false;
                StartCoroutine(PourThenStartSideA_Locked());
                break;

            case CookState.WaitingFlip:
                _inputLocked = true;
                if (actionButton != null) actionButton.interactable = false;

                StopFryLoop();

                if (sfxSource != null && flipClip != null)
                    sfxSource.PlayOneShot(flipClip);

                StartCoroutine(FlipThenStartSideB_Physical_Locked());
                break;

            case CookState.WaitingFinish:
                // успех: приготовили
                StopFryLoop();
                if (sfxSource != null && side2ReadyClip != null)
                    sfxSource.PlayOneShot(side2ReadyClip);
                FinishPancake();
                break;

            case CookState.Done:
                // В Done ничего не делаем — дальше рулит GameFlow (начинка/упаковка)
                break;

            case CookState.Burned:
                // В Burned разрешаем начать новый блин вручную (позже тут будет "спасти за рекламу")
                ResetToWaitingPour();
                break;

            case CookState.CookingSideA:
            case CookState.CookingSideB:
            default:
                // нажали слишком рано — игнорируем (можно потом штраф/комментарий)
                break;
        }
    }

    private void StartCookingSideA()
    {
        _state = CookState.CookingSideA;
        _stateTimer = 0f;

        if (pancakeImage != null)
            pancakeImage.color = sideAColor;

        SetUI("...", "Жарим сторону 1...");
        SetTimerUI(cookDuration);
        StartFryLoop();
    }

    private void StartCookingSideB()
    {
        _state = CookState.CookingSideB;
        _stateTimer = 0f;

        SetUI("...", "Жарим сторону 2...");
        SetTimerUI(cookDuration);
        StartFryLoop();
    }

    private void FinishPancake()
    {
        _state = CookState.Done;
        SetUI("Новый блин", "Готово! (кликни, чтобы начать снова)");
        SetTimerUI(0f);
        Cooked?.Invoke();
    }

    private void BurnPancake()
    {
        StopFryLoop();
        _state = CookState.Burned;

        if (pancakeImage != null)
            pancakeImage.color = burnedColor;

        SetUI("Новый блин", "Сгорел! (кликни, чтобы начать снова)");
        SetTimerUI(0f);

        // TODO позже: штраф по монетам
        Debug.Log("Pancake burned! Apply penalty later.");

        Burned?.Invoke();


    }

    private void ResetToWaitingPour()
    {
        _state = CookState.WaitingPour;

        SetUI("Залить тесто", "");   //  важно: вернуть UI в исходный вид
        SetTimerUIOff();

        if (actionButton != null)
            actionButton.interactable = true;

    }

    private void SetUI(string buttonLabel, string hint)
    {
        if (actionButtonText != null) actionButtonText.text = buttonLabel;
        if (hintText != null) hintText.text = hint;
    }

    private void SetTimerUI(float secondsLeft)
    {
        if (timerText == null) return;

        if (secondsLeft <= 0f)
        {
            timerText.text = "";
            return;
        }

        int s = Mathf.CeilToInt(secondsLeft);
        timerText.text = s.ToString();
    }
    public void ResetForNewPancake()
    {
        ResetToWaitingPour();
    }
    public void HandleActionButton()
    {
        OnActionButtonClicked();
    }
    private void SetTimerUIOff()
    {
        if (timerText != null) timerText.text = "";
    }
    private void StartFryLoop()
    {
        if (sfxSource == null || fryLoopClip == null) return;

        if (_fryLoopActive && sfxSource.isPlaying && sfxSource.loop && sfxSource.clip == fryLoopClip)
            return;

        sfxSource.loop = true;
        sfxSource.clip = fryLoopClip;
        sfxSource.Play();
        _fryLoopActive = true;
    }

    private void StopFryLoop()
    {
        if (sfxSource == null) return;

        // стопаем ТОЛЬКО если реально играет петля жарки
        if (sfxSource.loop && sfxSource.clip == fryLoopClip)
        {
            sfxSource.Stop();
            sfxSource.loop = false;
        }
        _fryLoopActive = false;
    }


    private IEnumerator PlayFlipAnim()
    {
        if (pancakeImage == null) yield break;

        var rt = pancakeImage.rectTransform;

        Vector3 baseScale = rt.localScale;

        float half = Mathf.Max(0.01f, flipAnimSeconds * 0.5f);

        // фаза 1: сжимаем по X до "почти нуля"
        float t = 0f;
        while (t < half)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / half);
            float x = Mathf.Lerp(1f, 0.05f, Mathf.SmoothStep(0f, 1f, k));
            rt.localScale = new Vector3(baseScale.x * x, baseScale.y, baseScale.z);
            yield return null;
        }

        // (если позже захочешь разные спрайты "сторона A/B" — вот тут самое место менять sprite)

        // фаза 2: возвращаем, с лёгким overshoot
        t = 0f;
        while (t < half)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / half);

            // сначала до overshoot, потом на 1
            float x = Mathf.Lerp(0.05f, flipOvershoot, Mathf.SmoothStep(0f, 1f, k));
            rt.localScale = new Vector3(baseScale.x * x, baseScale.y, baseScale.z);
            yield return null;
        }

        // settle обратно в 1
        rt.localScale = baseScale;
    }
    private IEnumerator FlipThenStartSideB()
    {
        yield return PlayFlipAnim();

        StartCookingSideB();
        StartFryLoop();
    }

    private IEnumerator PlayFlipAnimPhysical()
    {
        if (pancakeImage == null) yield break;

        var rt = pancakeImage.rectTransform;

        // базовые значения (восстановим в конце)
        Vector2 basePos = rt.anchoredPosition;
        Vector3 baseScale = rt.localScale;
        Quaternion baseRot = rt.localRotation;

        float half = Mathf.Max(0.01f, flipAnimSeconds * 0.5f);

        // Фаза 1: взлёт + наклон + squeeze (как будто поддеваем лопаткой)
        float t = 0f;
        while (t < half)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / half);
            float s = Mathf.SmoothStep(0f, 1f, k);

            // подлёт вверх (дуга)
            float y = Mathf.Lerp(0f, flipLift, s);

            // небольшой наклон
            float rot = Mathf.Lerp(0f, -flipRotateDeg, s);

            // squash/stretch: X сжимаем к середине, Y чуть вытягиваем
            float sx = Mathf.Lerp(1f, flipSquashX, s);
            float sy = Mathf.Lerp(1f, flipStretchY, s);

            rt.anchoredPosition = basePos + new Vector2(0f, y);
            rt.localRotation = baseRot * Quaternion.Euler(0f, 0f, rot);
            rt.localScale = new Vector3(baseScale.x * sx, baseScale.y * sy, baseScale.z);

            yield return null;
        }

        // ✅ МОМЕНТ “переворота”
        // Если позже захочешь менять спрайт "сторона A/B" — делай это вот здесь.

        // Фаза 2: приземление + наклон в другую сторону + возврат масштаба

        // меняем цвет на сторону B в момент переворота
        if (pancakeImage != null)
            pancakeImage.color = sideBColor;

        t = 0f;
        while (t < half)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / half);
            float s = Mathf.SmoothStep(0f, 1f, k);

            // возвращаемся вниз
            float y = Mathf.Lerp(flipLift, 0f, s);

            // наклон в другую сторону (чтобы чувствовался инерционный “флоп”)
            float rot = Mathf.Lerp(flipRotateDeg, 0f, s);

            // возвращаем scale к норме
            float sx = Mathf.Lerp(flipSquashX, 1f, s);
            float sy = Mathf.Lerp(flipStretchY, 1f, s);

            rt.anchoredPosition = basePos + new Vector2(0f, y);
            rt.localRotation = baseRot * Quaternion.Euler(0f, 0f, rot);
            rt.localScale = new Vector3(baseScale.x * sx, baseScale.y * sy, baseScale.z);

            yield return null;
        }

        // финальная фиксация
        rt.anchoredPosition = basePos;
        rt.localRotation = baseRot;
        rt.localScale = baseScale;
    }

    private IEnumerator FlipThenStartSideB_Physical()
    {
        yield return PlayFlipAnimPhysical();
        StartCookingSideB();
        StartFryLoop();
    }

    private IEnumerator FlipThenStartSideB_Physical_Locked()
    {
        yield return PlayFlipAnimPhysical();

        StartCookingSideB();
        StartFryLoop();

        if (actionButton != null) actionButton.interactable = true;
        _inputLocked = false;
    }

    private IEnumerator PourThenStartSideA_Locked()
    {
        yield return PourThenStartSideA();

        if (actionButton != null) actionButton.interactable = true;
        _inputLocked = false;
    }

    private IEnumerator PlayPourAnim()
    {
        if (pancakeImage == null) yield break;

        var rt = pancakeImage.rectTransform;

        // базовое состояние
        Vector3 baseScale = Vector3.one; // у UI блина обычно scale = 1
        rt.localScale = baseScale * pourStartScale;

        // fade-in (если хочешь)
        var c = pancakeImage.color;
        float baseA = c.a;
        c.a = 0f;
        pancakeImage.color = c;

        float dur = Mathf.Max(0.01f, pourAnimSeconds);

        // Фаза 1: раздуваем до overshoot + проявляем
        float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / dur);
            float s = Mathf.SmoothStep(pourStartScale, pourOvershoot, k);

            rt.localScale = baseScale * s;

            c = pancakeImage.color;
            c.a = Mathf.SmoothStep(0f, baseA, k);
            pancakeImage.color = c;

            yield return null;
        }

        // Фаза 2: settle на 1.0
        t = 0f;
        float settle = 0.08f;
        while (t < settle)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / settle);
            float s = Mathf.SmoothStep(pourOvershoot, 1f, k);
            rt.localScale = baseScale * s;
            yield return null;
        }

        rt.localScale = baseScale;
        c = pancakeImage.color;
        c.a = baseA;
        pancakeImage.color = c;
    }

    private IEnumerator PourThenStartSideA()
    {
        _state = CookState.Pouring;

        // ставим цвет сырого теста (сторона A), чтобы заливка была правильного цвета
        if (pancakeImage != null)
            pancakeImage.color = sideAColor;

        StartFryLoop();

        yield return PlayPourAnim();

        StartCookingSideA();   // после анимации реально начинаем жарку + таймер
    }

    public void ApplyStoveLevel(int level)
    {
        level = Mathf.Max(1, level);

        // Настройка прогрессии:
        // каждые +1 уровень: готовка быстрее на 12%, окно нажатия больше на 10%
        float speedMult = 1f + 0.18f * (level - 1);
        float windowMult = 1f + 0.15f * (level - 1);

        // База -> фактические значения
        cookDuration = baseCookTime / speedMult;
        tapWindow = baseBurnTime * windowMult;

    }


}
