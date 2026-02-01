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
        [Header("Debug")]
        [SerializeField] bool showDebug = false;

        public void LoadMainMenu()
        {
            if (showDebug)
                Debug.Log("[LoadMainMenuButton] Load main menu pressed.");

            GameManager gm = GameManager.Instance;
            if (gm == null)
                gm = Object.FindFirstObjectByType<GameManager>();

            if (gm != null)
            {
                if (showDebug)
                    Debug.Log("[LoadMainMenuButton] GameManager found, calling LoadMainMenu().");
                gm.LoadMainMenu();
            }
            else
            {
                if (showDebug)
                    Debug.LogWarning("[LoadMainMenuButton] No GameManager found, loading MainMenu via SceneManager.");
                UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
            }
        }
    }
}
