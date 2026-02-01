using UnityEngine;
using Object = UnityEngine.Object;

namespace Managers
{
    /// <summary>
    /// Call StartGame() from the main menu Play button's On Click.
    /// Resolves GameManager at runtime (Instance or FindFirstObjectByType) so Play works
    /// after returning to the main menu from in-game.
    /// Attach to the same GameObject as the Button (or a child) and assign in On Click.
    /// </summary>
    public class StartGameButton : MonoBehaviour
    {
        public void StartGame()
        {
            GameManager gm = GameManager.Instance;
            if (gm == null)
                gm = Object.FindFirstObjectByType<GameManager>();

            if (gm != null)
                gm.StartFirstLevel();
            else
                Debug.LogWarning("StartGameButton: No GameManager found. Assign Game Scene Names on a GameManager in the scene.");
        }
    }
}
