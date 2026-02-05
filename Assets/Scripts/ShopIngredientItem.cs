using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class ShopIngredientItem : MonoBehaviour
{
    public IngredientId ingredientId;
    public string title;
    public int price;
    public int requiredDay = 1;

    [Header("Locked by day UI")]
    [SerializeField] private GameObject dayLock;     // объект Lock
    [SerializeField] private TMP_Text dayLockText;   // текст Д2/Д3 (может быть null)


    [Header("UI")]
    [SerializeField] private Button selectButton;
    [SerializeField] private GameObject selectedFrame;

    [Header("Bought UI")]
    [SerializeField] private GameObject boughtBadge; // например маленький текст/иконка "Куплено"
    [SerializeField] private CanvasGroup canvasGroup; // чтобы приглушать

    public event Action<ShopIngredientItem> Clicked;

    private bool _bought;
    private bool _dayLocked;

    private bool _prevSelected;
    private Coroutine _popRoutine;

    private void Awake()
    {
        if (selectButton != null)
            selectButton.onClick.AddListener(OnClicked);

        SetSelected(false);
        _prevSelected = false;
        SetBought(false);
    }

    private void OnClicked()
    {
        Clicked?.Invoke(this);
    }

    public void SetSelected(bool selected)
    {
        if (selectedFrame != null)
            selectedFrame.SetActive(selected);

        // POP только если стало selected (false -> true)
        if (selected && !_prevSelected && isActiveAndEnabled)
        {
            if (_popRoutine != null) StopCoroutine(_popRoutine);
            _popRoutine = StartCoroutine(Pop(selectedFrame != null ? selectedFrame.transform : transform));
        }

        _prevSelected = selected;
    }

    private System.Collections.IEnumerator Pop(Transform t)
    {
        Vector3 a = Vector3.one * 0.92f;
        Vector3 b = Vector3.one * 1.05f;

        t.localScale = a;
        float x = 0f;
        while (x < 1f)
        {
            x += Time.unscaledDeltaTime * 12f;
            t.localScale = Vector3.Lerp(a, b, Mathf.SmoothStep(0f, 1f, x));
            yield return null;
        }

        x = 0f;
        while (x < 1f)
        {
            x += Time.unscaledDeltaTime * 12f;
            t.localScale = Vector3.Lerp(b, Vector3.one, Mathf.SmoothStep(0f, 1f, x));
            yield return null;
        }

        t.localScale = Vector3.one;
    }

    public void SetBought(bool bought)
    {
        _bought = bought;

        if (boughtBadge != null)
            boughtBadge.SetActive(bought);

        RefreshVisual();
    }

    public void SetDayLocked(bool locked, int reqDay)
    {
        _dayLocked = locked;

        if (dayLock != null)
            dayLock.SetActive(locked);

        if (dayLockText != null)
            dayLockText.text = $"Д{Mathf.Max(1, reqDay)}";

        RefreshVisual();
    }

    private void RefreshVisual()
    {
        if (canvasGroup == null) return;

        // приоритет: закрыто по дню (самое приглушенное)
        if (_dayLocked)
            canvasGroup.alpha = 0.65f;
        else if (_bought)
            canvasGroup.alpha = 0.8f;
        else
            canvasGroup.alpha = 1f;
    }
    public void SetSelectedSilent(bool selected)
    {
        if (selectedFrame != null)
            selectedFrame.SetActive(selected);

        _prevSelected = selected;
    }
}
