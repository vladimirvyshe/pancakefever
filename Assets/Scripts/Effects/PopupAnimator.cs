using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PopupAnimator : MonoBehaviour
{
    [Header("Links")]
    [SerializeField] private Image dimImage;            // черный фон
    [SerializeField] private CanvasGroup panelGroup;    // группа панели
    [SerializeField] private RectTransform panelRoot;   // панель (scale)

    [Header("Timings")]
    [SerializeField] private float openDuration = 0.12f;
    [SerializeField] private float closeDuration = 0.10f;

    [Header("Scale")]
    [SerializeField] private float fromScale = 0.95f;
    [SerializeField] private float toScale = 1.0f;

    [Header("Dim")]
    [Range(0, 1f)][SerializeField] private float dimTargetAlpha = 0.95f; // 242/255 ≈ 0.95

    private Coroutine routine;

    void Awake()
    {
        // ВАЖНО: выставить стартовое состояние ДО первого кадра
        SetInstantClosed();
    }

    private void SetInstantClosed()
    {
        if (dimImage != null)
        {
            var c = dimImage.color;
            c.a = 0f;
            dimImage.color = c;
            dimImage.raycastTarget = false;
        }

        if (panelGroup != null)
        {
            panelGroup.alpha = 0f;
            panelGroup.interactable = false;
            panelGroup.blocksRaycasts = false;
        }

        if (panelRoot != null)
            panelRoot.localScale = Vector3.one * fromScale;
    }

    public void Open()
    {
        gameObject.SetActive(true);      // объект уже активен, но визуально всё = 0
        Play(true);
    }

    public void Close()
    {
        Play(false);
    }

    private void Play(bool open)
    {
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(Animate(open));
    }

    private IEnumerator Animate(bool open)
    {
        float dur = open ? openDuration : closeDuration;

        float startDim = open ? 0f : dimTargetAlpha;
        float endDim = open ? dimTargetAlpha : 0f;

        float startA = open ? 0f : 1f;
        float endA = open ? 1f : 0f;

        float startS = open ? fromScale : toScale;
        float endS = open ? toScale : fromScale;

        // на время анимации блокируем клики
        if (panelGroup != null)
        {
            panelGroup.interactable = false;
            panelGroup.blocksRaycasts = false;
            panelGroup.alpha = startA;
        }

        if (panelRoot != null)
            panelRoot.localScale = Vector3.one * startS;

        SetDimAlpha(startDim);

        float t = 0f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / dur);
            float eased = 1f - Mathf.Pow(1f - k, 3f);

            if (panelGroup != null)
                panelGroup.alpha = Mathf.Lerp(startA, endA, eased);

            if (panelRoot != null)
            {
                float s = Mathf.Lerp(startS, endS, eased);
                panelRoot.localScale = Vector3.one * s;
            }

            SetDimAlpha(Mathf.Lerp(startDim, endDim, eased));

            yield return null;
        }

        if (panelGroup != null)
        {
            panelGroup.alpha = endA;
            bool isOpen = open;
            panelGroup.interactable = isOpen;
            panelGroup.blocksRaycasts = isOpen;
        }

        SetDimAlpha(endDim);

        if (!open)
            gameObject.SetActive(false);
    }

    private void SetDimAlpha(float a)
    {
        if (dimImage == null) return;
        var c = dimImage.color;
        c.a = a;
        dimImage.color = c;
        dimImage.raycastTarget = a > 0.01f; // чтобы можно было кликнуть по затемнению
    }
}
