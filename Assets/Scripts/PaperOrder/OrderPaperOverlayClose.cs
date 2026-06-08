using UnityEngine;
using UnityEngine.EventSystems;

public class OrderPaperOverlayClose : MonoBehaviour, IPointerDownHandler
{
    [SerializeField] private OrderPaperExpand target;

    public void OnPointerDown(PointerEventData eventData)
    {
        if (target != null) target.Collapse();
        eventData.Use();
    }
}
