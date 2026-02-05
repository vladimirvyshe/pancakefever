using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class BurnedPopupAnimator : MonoBehaviour
{
    [Header("Links")]
    [SerializeField] private Image dimImage;          // опционально
    [SerializeField] private CanvasGroup panelGroup;  // панель
    [SerializeField] private RectTransform panelRoot; // панель

    [Header("Timings")]
    [SerializeField] private float openDuration = 0.14f;
    [SerializeField] private float closeDuration = 0.12f;

    [Header("Scale Pop")]
    [SerializeField] private float openFrom = 0.85f;
    [SerializeField] private float openOvershoot = 1.05f;
    [SerializeField] private float openTo = 1.00f;

    [Header("Dim")]
    [Range(0, 1f)][SerializeField] private float dimTargetAlpha = 0.35f;

    Coroutine _routine;

    void Awake()
    {
        // стартовое состояние, чтобы не было вспышек
        SetDim(0f);
        if (panelGroup != null)
        {
            panelGroup.alpha = 0f;
            panelGroup.interactable = false;
            panelGroup.blocksRaycasts = false;
        }
        if (panelRoot != null) panelRoot.localScale = Vector3.one * openFrom;
    }

    public void Open()
    {
        gameObject.SetActive(true);
        Play(true);
    }

    public void Close()
    {
        Play(false);
    }

    void Play(bool open)
    {
        if (_routine != null) StopCoroutine(_routine);
        _routine = StartCoroutine(Animate(open));
    }

    IEnumerator Animate(bool open)
    {
        float dur = open ? openDuration : closeDuration;

        float dimA0 = open ? 0f : dimTargetAlpha;
        float dimA1 = open ? dimTargetAlpha : 0f;

        float a0 = open ? 0f : 1f;
        float a1 = open ? 1f : 0f;

        if (panelGroup != null)
        {
            panelGroup.alpha = a0;
            panelGroup.interactable = false;
            panelGroup.blocksRaycasts = false;
        }

        SetDim(dimA0);

        // scale keyframes
        float t = 0f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / dur);
            float eased = 1f - Mathf.Pow(1f - k, 3f);

            // POP: 0..0.65 -> overshoot, 0.65..1 -> settle
            float s;
            if (open)
            {
                if (eased < 0.65f)
                {
                    float kk = eased / 0.65f;
                    s = Mathf.Lerp(openFrom, openOvershoot, kk);
                }
                else
                {
                    float kk = (eased - 0.65f) / 0.35f;
                    s = Mathf.Lerp(openOvershoot, openTo, kk);
                }
            }
            else
            {
                s = Mathf.Lerp(openTo, openFrom, eased);
            }

            if (panelRoot != null) panelRoot.localScale = Vector3.one * s;
            if (panelGroup != null) panelGroup.alpha = Mathf.Lerp(a0, a1, eased);
            SetDim(Mathf.Lerp(dimA0, dimA1, eased));

            yield return null;
        }

        if (panelRoot != null) panelRoot.localScale = Vector3.one * (open ? openTo : openFrom);
        if (panelGroup != null)
        {
            panelGroup.alpha = a1;
            panelGroup.interactable = open;
            panelGroup.blocksRaycasts = open;
        }
        SetDim(dimA1);

        if (!open) gameObject.SetActive(false);
    }

    void SetDim(float a)
    {
        if (dimImage == null) return;
        var c = dimImage.color;
        c.a = a;
        dimImage.color = c;
        dimImage.raycastTarget = a > 0.01f; // блок кликов вниз
    }
}
