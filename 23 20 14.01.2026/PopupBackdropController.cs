using System.Collections;
using UnityEngine;

public sealed class PopupBackdropController : MonoBehaviour
{
    [Header("Backdrop")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField, Range(0f, 1f)] private float targetAlpha = 0.55f;
    [SerializeField] private float fadeSeconds = 0.12f;

    private int _blockCount;
    private Coroutine _anim;

    private void Reset()
    {
        canvasGroup = GetComponent<CanvasGroup>();
    }

    private void Awake()
    {
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        // Backdrop не должен перехватывать клики
        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
            // стартуем скрытым
            canvasGroup.alpha = 0f;
        }
    }

    public void Begin()
    {
        _blockCount++;
        if (_blockCount == 1)
            FadeTo(targetAlpha);
    }

    public void End()
    {
        _blockCount = Mathf.Max(0, _blockCount - 1);
        if (_blockCount == 0)
            FadeTo(0f);
    }

    public void ForceHide()
    {
        _blockCount = 0;
        if (_anim != null) StopCoroutine(_anim);
        _anim = null;

        if (canvasGroup != null)
            canvasGroup.alpha = 0f;
    }

    private void FadeTo(float alpha)
    {
        if (canvasGroup == null) return;

        if (_anim != null) StopCoroutine(_anim);
        _anim = StartCoroutine(FadeRoutine(alpha));
    }

    private IEnumerator FadeRoutine(float target)
    {
        float start = canvasGroup.alpha;
        float t = 0f;
        float sec = Mathf.Max(0.0001f, fadeSeconds);

        while (t < sec)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / sec);
            canvasGroup.alpha = Mathf.Lerp(start, target, Mathf.SmoothStep(0f, 1f, k));
            yield return null;
        }

        canvasGroup.alpha = target;
        _anim = null;
    }
}
