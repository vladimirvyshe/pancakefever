using UnityEngine;
using UnityEngine.EventSystems;

public class OrderPaperExpand : MonoBehaviour, IPointerDownHandler
{
    [SerializeField] private GameObject overlay; // тот самый OrderPanelOverlay

    private int _originalIndex;
    private bool _isExpanded;

    private void Awake()
    {
        _originalIndex = transform.GetSiblingIndex();
        if (overlay != null) overlay.SetActive(false);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (_isExpanded) Collapse();
        else Expand();

        eventData.Use();
    }

    private void Expand()
    {
        transform.SetAsLastSibling();
        _isExpanded = true;

        if (overlay != null)
        {
            overlay.SetActive(true);
            overlay.transform.SetAsLastSibling(); // overlay поверх всего…
            transform.SetAsLastSibling();         // …а панель поверх overlay
        }
    }

    public void Collapse()
    {
        transform.SetSiblingIndex(_originalIndex);
        _isExpanded = false;

        if (overlay != null) overlay.SetActive(false);
    }
}
