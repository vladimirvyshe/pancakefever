using UnityEngine;

public class PerformanceBootstrap : MonoBehaviour
{
    private int target;

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
        Apply();
    }

    void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus) Apply();
    }

    void OnApplicationPause(bool paused)
    {
        if (!paused) Apply(); // вернулись из паузы
    }

    private void Apply()
    {
        QualitySettings.vSyncCount = 0; // обязательно

        int refreshRate = (int)Screen.currentResolution.refreshRate;

        // защита от странных значений
        if (refreshRate < 60) refreshRate = 60;
        else if (refreshRate > 120) refreshRate = 120;

        target = refreshRate;
        Application.targetFrameRate = target;

        Debug.Log($"[PERF] Apply | Screen Hz = {Screen.currentResolution.refreshRate}, Target FPS = {Application.targetFrameRate}, vSync={QualitySettings.vSyncCount}");
    }
}
