using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class UIShineFx : MonoBehaviour
{
    [Header("Assign")]
    [SerializeField] private RectTransform shineRect; // сам блик (Image)
    [SerializeField] private CanvasGroup shineCg;      // на блике

    [Header("Tuning")]
    [SerializeField] private float duration = 0.35f;
    [SerializeField] private float offsetX = 220f;     // насколько проезжает
    [SerializeField] private float maxAlpha = 0.35f;

    private Coroutine _co;

    private Vector2 _basePos;
    private bool _basePosCached;

    private void Awake()
    {
        if (shineRect != null)
        {
            _basePos = shineRect.anchoredPosition;
            _basePosCached = true;
            shineRect.gameObject.SetActive(false);
        }
        if (shineCg != null) shineCg.alpha = 0f;
        if (shineRect != null) shineRect.gameObject.SetActive(false);
        if (shineCg != null) shineCg.alpha = 0f;
    }

    public void Play()
    {
        Debug.Log($"[SHINE] Play on {name} active={gameObject.activeInHierarchy} rect={(shineRect != null)} cg={(shineCg != null)}");

        if (!isActiveAndEnabled) return;
        if (shineRect == null || shineCg == null) return;

        if (_co != null) StopCoroutine(_co);
        _co = StartCoroutine(PlayRoutine());
    }

    private IEnumerator PlayRoutine()
    {
        shineRect.gameObject.SetActive(true);

        Vector2 basePos = _basePosCached ? _basePos : shineRect.anchoredPosition;
        Vector2 from = basePos + Vector2.left * offsetX;
        Vector2 to = basePos + Vector2.right * offsetX;

        shineRect.anchoredPosition = from;
        shineCg.alpha = 0f;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / Mathf.Max(0.0001f, duration);
            float s = Mathf.SmoothStep(0f, 1f, t);

            shineRect.anchoredPosition = Vector2.Lerp(from, to, s);

            // alpha: 0 -> max -> 0 (треугольник)
            float a = (s < 0.5f) ? (s / 0.5f) : ((1f - s) / 0.5f);
            shineCg.alpha = a * maxAlpha;

            yield return null;
        }

        shineCg.alpha = 0f;
        shineRect.anchoredPosition = basePos;
        shineRect.gameObject.SetActive(false);
        _co = null;
    }
}