using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class LoadingProgressUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image fillImage;
    [SerializeField] private TMP_Text loadingText;

    [Header("Timing")]
    [SerializeField, Range(0.5f, 3f)] private float fillToEightyFiveTime = 1.2f;
    [SerializeField, Range(0.05f, 1f)] private float finalFillTime = 0.25f;

    [Header("Target Scene")]
    [SerializeField] private string nextSceneName = "Selo_Level";

    [Header("Fade")]
    public ScreenFader fader;

    private AsyncOperation loadOperation;

    private void Start()
    {
        fillImage.fillAmount = 0f;

        StartCoroutine(LoadingSequence());
        if (loadingText != null)
            StartCoroutine(DotsRoutine());
    }

    private IEnumerator LoadingSequence()
    {
        // 1️⃣ Фейковая загрузка до 85%
        yield return FillTo(0.85f, fillToEightyFiveTime);

        // 2️⃣ Реальная загрузка сцены (но не активируем)
        loadOperation = SceneManager.LoadSceneAsync(nextSceneName);
        loadOperation.allowSceneActivation = false;

        while (loadOperation.progress < 0.9f)
        {
            float target = Mathf.Lerp(0.85f, 0.93f, loadOperation.progress / 0.9f);
            fillImage.fillAmount = Mathf.MoveTowards(
                fillImage.fillAmount,
                target,
                Time.deltaTime * 0.2f
            );
            yield return null;
        }

        // 3️⃣ Догоняем до 100%
        yield return FillTo(1f, finalFillTime);

        // 4️⃣ FADE OUT
        if (fader != null)
            yield return fader.FadeOut();

        // 5️⃣ Активируем сцену
        loadOperation.allowSceneActivation = true;
    }

    private IEnumerator FillTo(float target, float duration)
    {
        float start = fillImage.fillAmount;
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);
            k = k * k * (3f - 2f * k); // SmoothStep
            fillImage.fillAmount = Mathf.Lerp(start, target, k);
            yield return null;
        }

        fillImage.fillAmount = target;
    }

    private IEnumerator DotsRoutine()
    {
        string baseText = "Загрузка";
        int dots = 0;

        while (true)
        {
            dots = (dots + 1) % 4;
            loadingText.text = baseText + new string('.', dots);
            yield return new WaitForSeconds(0.35f);
        }
    }
}
