using UnityEngine;
using Object = UnityEngine.Object;

namespace Managers
{
    /// <summary>
    /// Call LoadMainMenu() from a Button's On Click to return to the main menu scene.
    /// Uses the GameManager singleton so the same managers scene stays loaded and state is updated.
    /// Attach to the same GameObject as the Button (or a child) and assign in On Click.
    /// </summary>
    public class LoadMainMenuButton : MonoBehaviour
    {
        public void LoadMainMenu()
        {
            GameManager gm = GameManager.Instance;
            if (gm == null)
                gm = Object.FindFirstObjectByType<GameManager>();

            if (gm != null)
                gm.LoadMainMenu();
            else
                UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
        }
    }
}
