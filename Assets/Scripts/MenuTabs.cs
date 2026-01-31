using UnityEngine;

public class MenuTabs : MonoBehaviour
{
    [SerializeField] private GameObject settingsPage;
    [SerializeField] private GameObject creditsPage;

    void Start()
    {
        ShowNone();
    }

    public void ShowSettings()
    {
        ShowNone();
        if (settingsPage != null) settingsPage.SetActive(true);
    }

    public void ShowCredits()
    {
        ShowNone();
        if (creditsPage != null) creditsPage.SetActive(true);
    }

    public void ShowNone()
    {
        if (settingsPage != null) settingsPage.SetActive(false);
        if (creditsPage != null) creditsPage.SetActive(false);
    }
}

