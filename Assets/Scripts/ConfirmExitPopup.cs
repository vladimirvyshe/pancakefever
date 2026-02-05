using UnityEngine;

public class ConfirmExitPopup : MonoBehaviour
{
    [SerializeField] private PopupAnimator popupAnim;

    public void Open()
    {
        popupAnim.Open();
    }

    public void Close()
    {
        popupAnim.Close();
    }

    public void ConfirmExit()
    {
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
