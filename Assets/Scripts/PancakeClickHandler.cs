using UnityEngine;
using UnityEngine.EventSystems;

public class PancakeClickHandler : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private GameFlowController gameFlow;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (gameFlow == null) return;
        gameFlow.OnPancakePointerClick(eventData);
    }
}
