using UnityEngine;
using UnityEngine.UI;
using TMPro;


public class DecorSetListItem : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Image previewImage;
    [SerializeField] private TMP_Text titleText;

    [Header("State")]
    [SerializeField] private GameObject lockGO;
    [SerializeField] private GameObject ownedGO;
    [SerializeField] private GameObject activeGO;
    [SerializeField] private GameObject selectedGO;
    [SerializeField] private CanvasGroup canvasGroup;



    private int _id;
    private int _requiredDay;
    private string _baseTitle;
    private DecorShopController _shop;

    private Coroutine _popRoutine;
    private bool _prevSelected;

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


    public void Bind(DecorShopController shop, int id, string title, int requiredDay)
    {
        _shop = shop;
        _id = id;
        _baseTitle = title;
        _requiredDay = requiredDay;

        if (titleText != null)
            titleText.text = title;

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => _shop.OnItemClicked(_id));
        }
    }

    public void SetState(bool owned, bool isSelected, bool isActive, bool dayLocked)
    {
        if (lockGO != null)
            lockGO.SetActive(dayLocked && !owned);

        if (ownedGO != null)
            ownedGO.SetActive(owned);

        // рамка выбора
        if (selectedGO != null)
            selectedGO.SetActive(isSelected);

        // метка "включено"
        if (activeGO != null)
            activeGO.SetActive(isActive);

        if (canvasGroup != null)
            canvasGroup.alpha = (dayLocked && !owned) ? 0.6f : 1f;

        if (isSelected && !_prevSelected && isActiveAndEnabled)
        {
            if (_popRoutine != null) StopCoroutine(_popRoutine);
            _popRoutine = StartCoroutine(Pop(transform));
        }
        _prevSelected = isSelected;

    }

    public void SetIcon(Sprite sprite)
    {
        if (previewImage == null) return;

        previewImage.sprite = sprite;
        previewImage.enabled = sprite != null;
    }
}
