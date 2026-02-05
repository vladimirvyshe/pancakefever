using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class BlobPop : MonoBehaviour
{

    [HideInInspector]
    public float baseScale = 1f;

    [Header("Scale")]
    [SerializeField] private float duration = 0.12f;
    [SerializeField] private float startScale = 0.2f;
    [SerializeField] private float overshootScale = 1.15f;

    [Header("Alpha")]
    [SerializeField] private float startAlpha = 0f;
    [SerializeField] private float endAlpha = 1f;

    private void OnEnable()
    {
        StartCoroutine(Play());
    }

    private IEnumerator Play()
    {
        var rt = (RectTransform)transform;
        var img = GetComponent<Image>();

        // init
        rt.localScale = Vector3.one * (baseScale * startScale);
        if (img != null)
        {
            var c = img.color;
            c.a = startAlpha;
            img.color = c;
        }

        float t = 0f;

        // phase 1: pop to overshoot
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / duration);

            // smooth
            float s = Mathf.SmoothStep(startScale, overshootScale, k);
            rt.localScale = Vector3.one * (baseScale * s);

            if (img != null)
            {
                var c = img.color;
                c.a = Mathf.SmoothStep(startAlpha, endAlpha, k);
                img.color = c;
            }

            yield return null;
        }

        // phase 2: settle to 1.0
        t = 0f;
        float settle = 0.08f;
        while (t < settle)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / settle);
            float s = Mathf.SmoothStep(overshootScale, 1f, k);
            rt.localScale = Vector3.one * s;
            yield return null;
        }

        rt.localScale = Vector3.one * baseScale;
    }
}