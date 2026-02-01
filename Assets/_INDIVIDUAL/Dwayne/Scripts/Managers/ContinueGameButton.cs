using UnityEngine;
using Object = UnityEngine.Object;

namespace Managers
{
    /// <summary>
    /// Call ContinueGame() from a Button's On Click (e.g. Continue in Pause Menu).
    /// Resolves GameManager at runtime and calls ResumeGame() so the button works reliably.
    /// Attach to the same GameObject as the Button (or a child) and assign in On Click.
    /// </summary>
    public class ContinueGameButton : MonoBehaviour
    {
        [Header("Debug")]
        [SerializeField] bool showDebug = false;

        public void ContinueGame()
        {
            if (showDebug)
                Debug.Log("[ContinueGameButton] Continue pressed.");

            GameManager gm = GameManager.Instance;
            if (gm == null)
                gm = Object.FindFirstObjectByType<GameManager>();

            if (gm != null)
            {
                if (showDebug)
                    Debug.Log("[ContinueGameButton] GameManager found, calling ResumeGame().");
                gm.ResumeGame();
            }
            else if (showDebug)
                Debug.LogWarning("[ContinueGameButton] No GameManager found.");
        }
    }
}
