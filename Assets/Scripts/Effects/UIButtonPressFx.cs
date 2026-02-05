using UnityEngine;
using UnityEngine.EventSystems;

public class UIButtonPressFx : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [SerializeField] private float pressedScale = 0.96f;
    [SerializeField] private float speed = 18f;

    private Vector3 _baseScale;
    private Vector3 _targetScale;
    private bool _inited;

    private void Awake()
    {
        _baseScale = transform.localScale;
        _targetScale = _baseScale;
        _inited = true;
    }

    private void OnEnable()
    {
        if (!_inited) return;
        transform.localScale = _baseScale;
        _targetScale = _baseScale;
    }

    private void Update()
    {
        transform.localScale = Vector3.Lerp(transform.localScale, _targetScale, Time.unscaledDeltaTime * speed);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        _targetScale = _baseScale * pressedScale;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        _targetScale = _baseScale;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _targetScale = _baseScale;
    }
}
