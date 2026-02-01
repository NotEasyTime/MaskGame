using UnityEngine;
using UnityEngine.InputSystem;

namespace Dwayne.Masks
{
    /// <summary>
    /// Handles input for mask abilities.
    /// Connects Unity Input System to MaskManager.
    /// </summary>
    [RequireComponent(typeof(MaskManager))]
    public class MaskInputHandler : MonoBehaviour
    {
        [Header("Debug")]
        [SerializeField] private bool showDebugLogs = true;

        private MaskManager maskManager;

        void Awake()
        {
            maskManager = GetComponent<MaskManager>();

            if (showDebugLogs)
                Debug.Log("MaskInputHandler: Initialized");
        }

        /// <summary>
        /// Called by Input System when the CombatAbility action is triggered.
        /// Maps to: Left Mouse Button / Primary Attack
        /// Uses the weapon's fire ability (combat ability from the mask).
        /// </summary>
        public void OnCombatAbility(InputValue value)
        {
            if (showDebugLogs)
                Debug.Log($"MaskInputHandler: OnCombatAbility triggered (isPressed: {value.isPressed})");

            if (maskManager == null || !value.isPressed)
                return;

            maskManager.UseCombatAbility();
        }

        /// <summary>
        /// Called by Input System when the Alt Combat Ability action is triggered.
        /// Maps to: E Key / Middle Mouse Button
        /// Uses the weapon's alt-fire ability.
        /// </summary>
        public void OnAltCombatAbility(InputValue value)
        {
            if (showDebugLogs)
                Debug.Log($"MaskInputHandler: OnAltCombatAbility triggered (isPressed: {value.isPressed})");

            if (maskManager == null || !value.isPressed)
                return;

            maskManager.UseAltCombatAbility();
        }

        /// <summary>
        /// Called by Input System when the Movement Ability action is triggered.
        /// Maps to: Shift / Space / F Key (depending on configuration)
        /// Uses the mask's movement ability (dash, teleport, etc.).
        /// </summary>
        public void OnMovementAbility(InputValue value)
        {
            if (showDebugLogs)
                Debug.Log($"MaskInputHandler: OnMovementAbility triggered (isPressed: {value.isPressed})");

            if (maskManager == null || !value.isPressed)
                return;

            maskManager.UseMovementAbility();
        }

        /// <summary>
        /// Called by Input System when the Previous action is triggered.
        /// Maps to: 1 Key / D-Pad Left
        /// Switches to the previous mask in the array.
        /// </summary>
        public void OnPrevious(InputValue value)
        {
            if (showDebugLogs)
                Debug.Log($"MaskInputHandler: OnPrevious triggered (isPressed: {value.isPressed})");

            if (maskManager == null || !value.isPressed)
                return;

            maskManager.PreviousMask();
        }

        /// <summary>
        /// Called by Input System when the Next action is triggered.
        /// Maps to: 2 Key / D-Pad Right
        /// Switches to the next mask in the array.
        /// </summary>
        public void OnNext(InputValue value)
        {
            if (showDebugLogs)
                Debug.Log($"MaskInputHandler: OnNext triggered (isPressed: {value.isPressed})");

            if (maskManager == null || !value.isPressed)
                return;

            maskManager.NextMask();
        }
    }
}
