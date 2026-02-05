using System.Collections;
using UnityEngine;

public class UITabSlideFx : MonoBehaviour
{
    [SerializeField] private CanvasGroup cg;
    [SerializeField] private RectTransform rt;
    [SerializeField] private float dur = 0.14f;
    [SerializeField] private float slidePx = 18f;

    private Coroutine _co;
    private Vector2 _basePos;

    private void Awake()
    {
        if (cg == null) cg = GetComponent<CanvasGroup>();
        if (rt == null) rt = GetComponent<RectTransform>();
        if (rt != null) _basePos = rt.anchoredPosition;
    }

    public void PlayInFromRight()
    {
        PlayIn(+slidePx);
    }

    public void PlayInFromLeft()
    {
        PlayIn(-slidePx);
    }

    private void PlayIn(float offset)
    {
        if (!isActiveAndEnabled) return;
        if (cg == null || rt == null) return;

        if (_co != null) StopCoroutine(_co);
        _co = StartCoroutine(InRoutine(offset));
    }

    private IEnumerator InRoutine(float offset)
    {
        cg.alpha = 0f;
        rt.anchoredPosition = _basePos + Vector2.right * offset;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / Mathf.Max(0.0001f, dur);
            float s = Mathf.SmoothStep(0f, 1f, t);
            cg.alpha = s;
            rt.anchoredPosition = Vector2.Lerp(_basePos + Vector2.right * offset, _basePos, s);
            yield return null;
        }

        cg.alpha = 1f;
        rt.anchoredPosition = _basePos;
        _co = null;
    }
}