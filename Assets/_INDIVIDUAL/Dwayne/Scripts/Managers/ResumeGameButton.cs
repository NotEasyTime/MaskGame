using UnityEngine;

namespace Managers
{
    /// <summary>
    /// Call ResumeGame() from a Button's On Click (e.g. Resume in PauseMenu).
    /// Attach to the same GameObject as the Button (or a child) and assign in On Click.
    /// </summary>
    public class ResumeGameButton : MonoBehaviour
    {
        public void ResumeGame()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.ResumeGame();
        }
    }
}
