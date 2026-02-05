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

    private void Awake()
    {
        if (selectButton != null)
            selectButton.onClick.AddListener(OnClicked);

        SetSelected(false);
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
}
