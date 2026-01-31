using UnityEngine;

public class SetScreenResolution : MonoBehaviour
{
    public int resolutionX = 800;
    public int resolutionY = 1280;

    private void Awake()
    {
        Screen.fullScreenMode = FullScreenMode.Windowed;

        Screen.SetResolution(resolutionX, resolutionY, false);

    }
    public void ChangeResolution(int inResX, int inResY)
    {
        Screen.SetResolution(inResX, inResY, false);
    }
}