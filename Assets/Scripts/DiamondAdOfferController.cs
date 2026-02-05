using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class DiamondAdOfferController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private GameFlowController flow;
    [SerializeField] private DiamondsController diamonds;
    [SerializeField] private RewardedAdsStub rewardedAds;

    [Header("UI")]
    [SerializeField] private Button offerButton; // кнопка "💎 +1"
    [SerializeField] private GameObject offerRoot; // объект кнопки (чтобы SetActive)

    [Header("Limits")]
    [SerializeField] private int maxPerDay = 2;      // 1–2 в день
    [SerializeField] private float minDelaySec = 45; // рандом после старта/после закрытия
    [SerializeField] private float maxDelaySec = 120;

    [Header("Soft Cooldown")]
    [SerializeField] private float visibleTimeSec = 12f;   // сколько кнопка видна

    [Header("Anim")]
    [SerializeField] private CanvasGroup offerCg;      // CanvasGroup на offerRoot
    [SerializeField] private RectTransform offerRt;    // RectTransform offerRoot
    [SerializeField] private float appearSec = 0.22f;
    [SerializeField] private float pulseEverySec = 5f; // как часто "дышит"
    [SerializeField] private float pulseSec = 0.18f;



    private Coroutine _anim;
    private Coroutine _pulse;

    private float _hideAtTime;
    private bool _autoHideScheduled;

    private float _nextShowTime;
    private bool _showing;
    private int _dayCached;
    private int _claimsToday;

    private const string PP_DAY = "pf_diamond_ad_day";
    private const string PP_COUNT = "pf_diamond_ad_count";

    private int _showsToday;

    private const string PP_SHOWS = "pf_diamond_ad_shows";

    void Awake()
    {
        if (offerButton != null)
            offerButton.onClick.AddListener(OnOfferClicked);

        LoadCounters();
        HideOffer();
        if (offerRoot != null)
        {
            if (offerRt == null) offerRt = offerRoot.GetComponent<RectTransform>();
            if (offerCg == null) offerCg = offerRoot.GetComponent<CanvasGroup>();
            if (offerCg == null) offerCg = offerRoot.AddComponent<CanvasGroup>();
        }
        ScheduleNext();
    }

    void Update()
    {
        if (flow == null || offerRoot == null) return;

        // день сменился — сброс лимита
        int day = flow.GetDayIndex();
        if (day != _dayCached)
        {
            _dayCached = day;
            _claimsToday = 0;
            _showsToday = 0;
            SaveCounters();
            ScheduleNext();
        }

        // если кнопка уже видна — обслуживаем авто-скрытие и выходим
        if (offerRoot.activeSelf)
        {
            if (_autoHideScheduled && Time.unscaledTime >= _hideAtTime)
            {
                HideOffer();
                _autoHideScheduled = false;

                // ✅ после игнора просто планируем следующий показ обычным рандомом
                ScheduleNext();
            }
            return;
        }

        if (_showing) return;
        if (_showsToday >= maxPerDay) return;
        if (Time.unscaledTime < _nextShowTime) return;

        ShowOffer();
    }


    private void OnOfferClicked()
    {
        if (_showing) return;
        if (_claimsToday >= maxPerDay) return;
        if (rewardedAds == null || diamonds == null) return;

        _showing = true;
        if (offerButton != null) offerButton.interactable = false;

        rewardedAds.Show("diamond_1", () =>
        {
            diamonds.Add(1);
            _claimsToday++;
            SaveCounters();

            _showing = false;
            HideOffer();
            ScheduleNext();
        });
    }

    private void ShowOffer()
    {
        _showsToday++;
        SaveCounters();

        offerRoot.SetActive(true);
        offerButton.interactable = true;

        _hideAtTime = Time.unscaledTime + visibleTimeSec;
        _autoHideScheduled = true;

        if (_anim != null) StopCoroutine(_anim);
        _anim = StartCoroutine(AppearAnim());

        if (_pulse != null) StopCoroutine(_pulse);
        _pulse = StartCoroutine(PulseLoop());
    }


    private void HideOffer()
    {
        if (_anim != null) StopCoroutine(_anim);
        if (_pulse != null) StopCoroutine(_pulse);

        if (offerRoot != null && offerRoot.activeSelf)
            StartCoroutine(HideRoutine());
        else
            offerRoot?.SetActive(false);

        if (offerButton != null)
            offerButton.interactable = false;

        _showing = false;
    }

    private IEnumerator HideRoutine()
    {
        yield return DisappearAnim();
        if (offerCg != null) offerCg.alpha = 0f;
        if (offerRt != null) offerRt.localScale = Vector3.one;
        offerRoot.SetActive(false);
    }

    private void ScheduleNext()
    {
        float delay = Random.Range(minDelaySec, maxDelaySec);
        _nextShowTime = Time.unscaledTime + delay;
    }

    private void LoadCounters()
    {
        _dayCached = PlayerPrefs.GetInt(PP_DAY, 0);
        _claimsToday = PlayerPrefs.GetInt(PP_COUNT, 0);
        _showsToday = PlayerPrefs.GetInt(PP_SHOWS, 0);
    }

    private void SaveCounters()
    {
        PlayerPrefs.SetInt(PP_DAY, flow != null ? flow.GetDayIndex() : _dayCached);
        PlayerPrefs.SetInt(PP_COUNT, _claimsToday);
        PlayerPrefs.SetInt(PP_SHOWS, _showsToday);
        PlayerPrefs.Save();
    }

    private IEnumerator AppearAnim()
    {
        if (offerCg == null || offerRt == null) yield break;

        offerCg.alpha = 0f;
        offerCg.blocksRaycasts = true;
        offerCg.interactable = true;

        Vector3 a = Vector3.one * 0.92f;
        Vector3 b = Vector3.one * 1.03f;
        Vector3 c = Vector3.one;

        offerRt.localScale = a;

        float t = 0f;
        while (t < appearSec)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / Mathf.Max(0.0001f, appearSec));
            float s = Mathf.SmoothStep(0f, 1f, k);

            offerCg.alpha = s;
            offerRt.localScale = Vector3.Lerp(a, b, s);
            yield return null;
        }

        // небольшой "возврат" к 1.0
        t = 0f;
        float back = appearSec * 0.6f;
        while (t < back)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / Mathf.Max(0.0001f, back));
            float s = Mathf.SmoothStep(0f, 1f, k);

            offerRt.localScale = Vector3.Lerp(b, c, s);
            yield return null;
        }

        offerCg.alpha = 1f;
        offerRt.localScale = Vector3.one;
    }

    private IEnumerator PulseLoop()
    {
        if (offerRt == null) yield break;

        // Подождём, чтобы не пульсило сразу после появления
        yield return new WaitForSecondsRealtime(2.5f);

        while (offerRoot != null && offerRoot.activeSelf)
        {
            yield return new WaitForSecondsRealtime(pulseEverySec);

            // мягкий микропульс
            Vector3 from = Vector3.one;
            Vector3 to = Vector3.one * 1.04f;

            float t = 0f;
            while (t < pulseSec)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / Mathf.Max(0.0001f, pulseSec));
                float s = Mathf.SmoothStep(0f, 1f, k);
                offerRt.localScale = Vector3.Lerp(from, to, s);
                yield return null;
            }

            t = 0f;
            while (t < pulseSec)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / Mathf.Max(0.0001f, pulseSec));
                float s = Mathf.SmoothStep(0f, 1f, k);
                offerRt.localScale = Vector3.Lerp(to, from, s);
                yield return null;
            }

            offerRt.localScale = Vector3.one;
        }
    }

    private IEnumerator DisappearAnim()
    {
        if (offerCg == null || offerRt == null) yield break;

        offerCg.blocksRaycasts = false;
        offerCg.interactable = false;

        float sec = 0.12f;
        float t = 0f;

        float startA = offerCg.alpha;
        Vector3 startS = offerRt.localScale;
        Vector3 endS = Vector3.one * 0.96f;

        while (t < sec)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / Mathf.Max(0.0001f, sec));
            float s = Mathf.SmoothStep(0f, 1f, k);

            offerCg.alpha = Mathf.Lerp(startA, 0f, s);
            offerRt.localScale = Vector3.Lerp(startS, endS, s);
            yield return null;
        }
    }
}
