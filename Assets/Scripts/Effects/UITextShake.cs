using System.Collections;
using UnityEngine;

public class UITextShake : MonoBehaviour
{
    [SerializeField] private RectTransform target;
    [SerializeField] private float duration = 0.16f;
    [SerializeField] private float strength = 6f; // px
    [SerializeField] private int shakes = 10;

    private Coroutine _co;

    public void Play()
    {
        Debug.Log($"[SHAKE] active={isActiveAndEnabled} target={(target != null)}");
        if (!isActiveAndEnabled) return;
        if (target == null) return;

        if (_co != null) StopCoroutine(_co);
        _co = StartCoroutine(Run());
    }

    private IEnumerator Run()
    {
        Vector2 basePos = target.anchoredPosition;

        for (int i = 0; i < shakes; i++)
        {
            float k = (float)i / Mathf.Max(1, shakes - 1);
            float damper = 1f - k;

            float x = ((i % 2 == 0) ? 1f : -1f) * strength * damper;
            target.anchoredPosition = basePos + new Vector2(x, 0);

            yield return new WaitForSecondsRealtime(duration / shakes);
        }

        target.anchoredPosition = basePos;
        _co = null;
    }
}