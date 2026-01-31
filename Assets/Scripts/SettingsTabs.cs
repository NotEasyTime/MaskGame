using UnityEngine;

public class SettingsTabs : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject keybindsPanel;
    [SerializeField] private GameObject audioPanel;
    [SerializeField] private GameObject videoPanel;
    [SerializeField] private GameObject accessibilityPanel;

    void Start()
    {
        ShowKeybinds(); // default tab
    }

    public void ShowKeybinds()
    {
        HideAll();
        if (keybindsPanel) keybindsPanel.SetActive(true);
    }

    public void ShowAudio()
    {
        HideAll();
        if (audioPanel) audioPanel.SetActive(true);
    }

    public void ShowVideo()
    {
        HideAll();
        if (videoPanel) videoPanel.SetActive(true);
    }

    public void ShowAccessibility()
    {
        HideAll();
        if (accessibilityPanel) accessibilityPanel.SetActive(true);
    }

    private void HideAll()
    {
        if (keybindsPanel) keybindsPanel.SetActive(false);
        if (audioPanel) audioPanel.SetActive(false);
        if (videoPanel) videoPanel.SetActive(false);
        if (accessibilityPanel) accessibilityPanel.SetActive(false);
    }
}
