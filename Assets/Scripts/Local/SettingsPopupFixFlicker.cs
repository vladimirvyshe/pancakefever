using System.Collections;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

public class SettingsPopupFixFlicker : MonoBehaviour
{
    [SerializeField] private CanvasGroup cardCanvasGroup;
    [SerializeField] private RectTransform cardRoot; // Card

    private void OnEnable()
    {
        StartCoroutine(FixFlicker());
    }

    private IEnumerator FixFlicker()
    {
        Hide();

        // дождаться локализации
        yield return LocalizationSettings.InitializationOperation;

        // дать локализаторам/TMP/лайауту время обновиться
        yield return null;
        yield return null;
        yield return new WaitForEndOfFrame();

        // форснуть перестроение канваса/лейаута
        Canvas.ForceUpdateCanvases();
        if (cardRoot != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(cardRoot);

        yield return new WaitForEndOfFrame();

        Show();
    }

    private void Hide()
    {
        if (cardCanvasGroup == null) return;
        cardCanvasGroup.alpha = 0f;
        cardCanvasGroup.interactable = false;
        cardCanvasGroup.blocksRaycasts = false;
    }

    private void Show()
    {
        if (cardCanvasGroup == null) return;
        cardCanvasGroup.alpha = 1f;
        cardCanvasGroup.interactable = true;
        cardCanvasGroup.blocksRaycasts = true;
    }
}