using UnityEngine;

namespace Managers
{
    /// <summary>
    /// Call LoadMainMenu() from a Button's On Click to return to the main menu scene.
    /// Attach to the same GameObject as the Button (or a child) and assign in On Click.
    /// </summary>
    public class LoadMainMenuButton : MonoBehaviour
    {
        public void LoadMainMenu()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.LoadMainMenu();
            else
                UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
        }
    }
}
