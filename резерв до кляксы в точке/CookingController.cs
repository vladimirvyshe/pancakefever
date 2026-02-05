using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;


public class CookingController : MonoBehaviour
{
    public CookState CurrentState => _state;
    public event Action Cooked;
    public event Action Burned;
    public enum CookState
    {
        WaitingPour,     // ждём "Залить тесто"
        CookingSideA,    // жарим сторону A
        WaitingFlip,     // ждём нажатие "Перевернуть" (окно)
        CookingSideB,    // жарим сторону B
        WaitingFinish,   // ждём нажатие "Приготовить" (окно)
        Burned,          // блин сгорел
        Done             // блин приготовлен
    }

    [Header("SFX")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioClip flipClip;

    [Header("UI References")]
    [SerializeField] private Button actionButton;
    [SerializeField] private TMP_Text actionButtonText;
    [SerializeField] private TMP_Text hintText;          // можно не подключать (необязательно)
    [SerializeField] private TMP_Text timerText;         // можно не подключать (необязательно)
    [SerializeField] private Image pancakeImage;

    [Header("Timing (seconds)")]
    [SerializeField] private float cookDuration = 5f;   // сколько жарится сторона
    [SerializeField] private float tapWindow = 3f;       // сколько секунд даётся на нажатие

    [Header("Colors")]
    [SerializeField] private Color rawColor = new Color(1f, 0.95f, 0.85f, 1f);
    [SerializeField] private Color cookedColor = new Color(0.85f, 0.65f, 0.35f, 1f);
    [SerializeField] private Color burnedColor = new Color(0.1f, 0.1f, 0.1f, 1f);

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
            pancakeImage.color = Color.Lerp(rawColor, cookedColor, t);

        SetTimerUI(cookDuration - _stateTimer);

        if (_stateTimer >= cookDuration)
        {
            // переходим в окно нажатия
            _windowTimer = 0f;
            _state = isSecondSide ? CookState.WaitingFinish : CookState.WaitingFlip;

            if (isSecondSide)
                SetUI("Приготовить", "Нажми 'Приготовить' в течение окна!");
            else
                SetUI("Перевернуть", "Нажми 'Перевернуть' в течение окна!");

            // в окно мы НЕ сбрасываем цвет — он остаётся прожаренным
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
        switch (_state)
        {
            case CookState.WaitingPour:
                StartCookingSideA();
                break;

            case CookState.WaitingFlip:
                // успех: перевернули
                if (sfxSource != null && flipClip != null)
                    sfxSource.PlayOneShot(flipClip);
                StartCookingSideB();
                break;

            case CookState.WaitingFinish:
                // успех: приготовили
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
            pancakeImage.color = rawColor;

        SetUI("...", "Жарим сторону 1...");
        SetTimerUI(cookDuration);
    }

    private void StartCookingSideB()
    {
        _state = CookState.CookingSideB;
        _stateTimer = 0f;

        SetUI("...", "Жарим сторону 2...");
        SetTimerUI(cookDuration);
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
}
