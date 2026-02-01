using UnityEngine;
using Object = UnityEngine.Object;

namespace Managers
{
    /// <summary>
    /// Trigger volume that respawns the player when they enter (e.g. pit, lava).
    /// Attach to a GameObject with a Collider set to "Is Trigger".
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class KillVolume : MonoBehaviour
    {
        [Tooltip("Tag to identify the player. Leave empty to use 'Player'.")]
        [SerializeField] private string playerTag = "Player";

        private void OnTriggerEnter(Collider other)
        {
            string effectiveTag = string.IsNullOrEmpty(playerTag) ? "Player" : playerTag;
            if (!other.CompareTag(effectiveTag))
                return;

            GameManager gm = GameManager.Instance;
            if (gm == null)
                gm = Object.FindFirstObjectByType<GameManager>();

            if (gm != null)
                gm.RespawnPlayer();
            else
                Debug.LogWarning("KillVolume: No GameManager found, cannot respawn player.");
        }
    }
}
