using TMPro;
using UnityEngine;

public class BurnResultPopup : MonoBehaviour
{
    [SerializeField] private TMP_Text resultText;

    public void ShowBurned(int lostCoins)
    {
        resultText.text = $"СГОРЕЛ − {lostCoins} монет";
        gameObject.SetActive(true);
    }

    public void ShowSaved(int earnedCoins)
    {
        resultText.text = $"СПАСЁН + {earnedCoins} монет";
        gameObject.SetActive(true);
    }
}
