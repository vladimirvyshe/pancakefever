using UnityEngine;
using UnityEngine.UI;
using System.Collections;

[RequireComponent(typeof(Toggle))]
public class ToggleIconSwap : MonoBehaviour
{
    [Header("Icon")]
    public Image icon;
    public Sprite onSprite;   // ✅
    public Sprite offSprite;  // ❌

    [Header("Scale Animation")]
    public float pressedScale = 0.95f;
    public float animDuration = 0.08f;

    private Toggle toggle;
    private Coroutine scaleRoutine;
    private RectTransform iconRect;

    void Awake()
    {
        toggle = GetComponent<Toggle>();
        iconRect = icon.rectTransform;

        toggle.onValueChanged.AddListener(OnToggleChanged);
        ApplyVisual(toggle.isOn);
    }

    void OnToggleChanged(bool isOn)
    {
        ApplyVisual(isOn);
        PlayScaleAnim();
    }

    void ApplyVisual(bool isOn)
    {
        icon.sprite = isOn ? onSprite : offSprite;
    }

    void PlayScaleAnim()
    {
        if (scaleRoutine != null)
            StopCoroutine(scaleRoutine);

        scaleRoutine = StartCoroutine(ScalePunch());
    }

    IEnumerator ScalePunch()
    {
        Vector3 start = Vector3.one * pressedScale;
        Vector3 end = Vector3.one;

        iconRect.localScale = start;

        float t = 0f;
        while (t < animDuration)
        {
            t += Time.unscaledDeltaTime; // UI не зависит от TimeScale
            float k = t / animDuration;
            iconRect.localScale = Vector3.Lerp(start, end, k);
            yield return null;
        }

        iconRect.localScale = end;
    }
    public void Refresh()
    {
        if (toggle == null) toggle = GetComponent<Toggle>();
        ApplyVisual(toggle.isOn);
    }

}