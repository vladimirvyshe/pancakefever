using UnityEngine;
using TMPro;

public class DiamondsController : MonoBehaviour
{
    [SerializeField] private TMP_Text diamondsText; // UI: "💎 3"

    private ProgressService.Data _progress;

    public int Diamonds => _progress.diamonds;

    public void Init(ProgressService.Data progress)
    {
        _progress = progress;
        RefreshUI();
    }

    public void RefreshUI()
    {
        if (diamondsText != null)
            diamondsText.text = _progress.diamonds.ToString();
    }

    public void Add(int amount)
    {
        if (amount <= 0) return;
        _progress.diamonds += amount;
        ProgressService.Save(_progress);
        RefreshUI();
    }

    public bool TrySpend(int amount)
    {
        if (amount <= 0) return true;
        if (_progress.diamonds < amount) return false;

        _progress.diamonds -= amount;
        ProgressService.Save(_progress);
        RefreshUI();
        return true;
    }
}
