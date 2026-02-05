using System.Collections;
using UnityEngine;

public class UIBadgePop : MonoBehaviour
{
    [SerializeField] private CanvasGroup cg;
    [SerializeField] private RectTransform rt;
    [SerializeField] private float dur = 0.14f;
    [SerializeField] private float from = 0.80f;
    [SerializeField] private float to = 1.06f;

    private Coroutine _co;

    private void Awake()
    {
        if (cg == null) cg = GetComponent<CanvasGroup>();
        if (rt == null) rt = GetComponent<RectTransform>();
    }

    public void PlayShow()
    {
        if (!isActiveAndEnabled) return;
        if (cg == null || rt == null) return;

        if (_co != null) StopCoroutine(_co);
        _co = StartCoroutine(ShowRoutine());
    }

    private IEnumerator ShowRoutine()
    {
        cg.alpha = 0f;
        rt.localScale = Vector3.one * from;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / Mathf.Max(0.0001f, dur);
            float s = Mathf.SmoothStep(0f, 1f, t);

            cg.alpha = s;
            rt.localScale = Vector3.Lerp(Vector3.one * from, Vector3.one * to, s);
            yield return null;
        }

        // чуть назад к 1
        t = 0f;
        float back = dur * 0.7f;
        Vector3 start = Vector3.one * to;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / Mathf.Max(0.0001f, back);
            float s = Mathf.SmoothStep(0f, 1f, t);
            rt.localScale = Vector3.Lerp(start, Vector3.one, s);
            yield return null;
        }

        cg.alpha = 1f;
        rt.localScale = Vector3.one;
        _co = null;
    }
}
