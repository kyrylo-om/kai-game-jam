using UnityEngine;
using UnityEngine.UI; // Required for the ScrollRect

public class CreditsManager : MonoBehaviour
{
    [Header("UI References")]
    public ScrollRect scrollRect; // Drag your ScrollMask object here!

    public void OpenCredits()
    {
        gameObject.SetActive(true);

        if (scrollRect != null)
        {
            // Forces the scrollbar to the absolute top (1 = top, 0 = bottom)
            scrollRect.verticalNormalizedPosition = 1f;
        }
    }

    public void CloseCredits()
    {
        gameObject.SetActive(false);
    }
}
