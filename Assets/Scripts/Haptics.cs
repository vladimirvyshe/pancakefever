using UnityEngine;

public static class Haptics
{
    public static void VibrateIfEnabled()
    {
        if (!SettingsPopupController.IsVibroEnabled()) return;
        Handheld.Vibrate();
    }
}
